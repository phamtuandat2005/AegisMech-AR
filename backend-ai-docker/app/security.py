"""
Security & Authentication Module for 3D Mech AR Chess.
Implements:
1. JWT Token Creation, Verification, and Claims Validation (HS256).
2. FastAPI Authentication Dependencies (get_current_player, get_optional_current_player).
3. Cryptographic Match Ticket System (HMAC-SHA256, Anti-Replay, Anti-Cheat, Anti-Farming).
4. Sliding-Window In-Memory Rate Limiter.
5. Sensitive Data & PII Masking Utilities.
"""

import os
import hmac
import hashlib
import time
from datetime import datetime, timedelta, timezone
from typing import Optional, Dict, Any, Tuple, List
from collections import defaultdict
import uuid
import threading
import logging

import jwt
from fastapi import HTTPException, status, Depends, Request
from fastapi.security import HTTPBearer, HTTPAuthorizationCredentials
from sqlalchemy.ext.asyncio import AsyncSession
from sqlalchemy import select

from app.config import (
    JWT_SECRET_KEY,
    JWT_ALGORITHM,
    JWT_ACCESS_TOKEN_EXPIRE_MINUTES,
    APP_ENV
)
from app.models import Player
from app.database import get_db

logger = logging.getLogger("MechChessSecurity")

# HTTP Bearer scheme
security_scheme = HTTPBearer(auto_error=False)

# ---------------------------------------------------------------------------
# 1. JWT Authentication
# ---------------------------------------------------------------------------
def create_access_token(data: Dict[str, Any], expires_delta: Optional[timedelta] = None) -> str:
    """
    Creates a signed JWT access token with standard claims (sub, iat, exp).
    """
    to_encode = data.copy()
    now = datetime.now(timezone.utc)
    if expires_delta:
        expire = now + expires_delta
    else:
        expire = now + timedelta(minutes=JWT_ACCESS_TOKEN_EXPIRE_MINUTES)

    to_encode.update({
        "exp": expire,
        "iat": now,
        "iss": "MechChessBackend"
    })
    encoded_jwt = jwt.encode(to_encode, JWT_SECRET_KEY, algorithm=JWT_ALGORITHM)
    return encoded_jwt

def decode_access_token(token: str) -> Dict[str, Any]:
    """
    Decodes and validates a JWT token. Raises HTTPException(401) on failure.
    """
    try:
        payload = jwt.decode(
            token,
            JWT_SECRET_KEY,
            algorithms=[JWT_ALGORITHM],
            issuer="MechChessBackend"
        )
        return payload
    except jwt.ExpiredSignatureError:
        raise HTTPException(
            status_code=status.HTTP_401_UNAUTHORIZED,
            detail="Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại!",
            headers={"WWW-Authenticate": "Bearer"}
        )
    except jwt.PyJWTError as e:
        logger.warning(f"[Security] JWT decode error: {e}")
        raise HTTPException(
            status_code=status.HTTP_401_UNAUTHORIZED,
            detail="Token xác thực không hợp lệ hoặc bị giả mạo!",
            headers={"WWW-Authenticate": "Bearer"}
        )

async def get_current_player(
    credentials: Optional[HTTPAuthorizationCredentials] = Depends(security_scheme),
    db: AsyncSession = Depends(get_db)
) -> Player:
    """
    Enforces Bearer authentication. Returns authenticated Player object from DB.
    """
    if not credentials or not credentials.credentials:
        raise HTTPException(
            status_code=status.HTTP_401_UNAUTHORIZED,
            detail="Yêu cầu xác thực. Thiếu Authorization Bearer token!",
            headers={"WWW-Authenticate": "Bearer"}
        )

    payload = decode_access_token(credentials.credentials)
    username = payload.get("sub") or payload.get("username")
    if not username:
        raise HTTPException(
            status_code=status.HTTP_401_UNAUTHORIZED,
            detail="Token không chứa định danh người chơi hợp lệ!",
            headers={"WWW-Authenticate": "Bearer"}
        )

    # Fetch player from database
    result = await db.execute(select(Player).where(Player.username == username))
    player = result.scalars().first()

    if not player:
        raise HTTPException(
            status_code=status.HTTP_401_UNAUTHORIZED,
            detail=f"Tài khoản người chơi '{username}' không tồn tại hoặc đã bị khóa!",
            headers={"WWW-Authenticate": "Bearer"}
        )

    return player

async def get_optional_current_player(
    credentials: Optional[HTTPAuthorizationCredentials] = Depends(security_scheme),
    db: AsyncSession = Depends(get_db)
) -> Optional[Player]:
    """
    Returns current Player if a valid token is provided, or None if unauthenticated.
    """
    if not credentials or not credentials.credentials:
        return None
    try:
        payload = decode_access_token(credentials.credentials)
        username = payload.get("sub") or payload.get("username")
        if not username:
            return None
        result = await db.execute(select(Player).where(Player.username == username))
        return result.scalars().first()
    except Exception:
        return None

# ---------------------------------------------------------------------------
# 2. Cryptographic Match Tickets (Anti-Cheat & Anti-Replay)
# ---------------------------------------------------------------------------
class MatchTicketManager:
    """
    Tracks and validates active match tickets to prevent replay attacks,
    match result spoofing, and bot-driven ELO/gold farming.
    """
    def __init__(self):
        self._lock = threading.Lock()
        # match_id -> {username, issued_at, consumed, opponent, is_ai, role}
        self._tickets: Dict[str, Dict[str, Any]] = {}

    def _sign_payload(self, raw_str: str) -> str:
        return hmac.new(
            JWT_SECRET_KEY.encode("utf-8"),
            raw_str.encode("utf-8"),
            hashlib.sha256
        ).hexdigest()

    def create_ticket(self, username: str, opponent: str, is_ai: bool, role: str) -> Tuple[str, str, float]:
        """
        Issues a cryptographically signed match ticket.
        Returns (match_id, ticket_token, issued_at).
        """
        # pyrefly: ignore[unknown-name]
        match_id = f"MCH_{uuid.uuid4().hex[:12].upper()}"
        issued_at = time.time()
        payload = f"{match_id}:{username}:{opponent}:{is_ai}:{role}:{int(issued_at)}"
        sig = self._sign_payload(payload)
        ticket_token = f"{payload}.{sig}"

        with self._lock:
            self._cleanup_expired()
            self._tickets[match_id] = {
                "username": username.lower(),
                "opponent": opponent,
                "is_ai": is_ai,
                "role": role,
                "issued_at": issued_at,
                "consumed": False
            }

        return match_id, ticket_token, issued_at

    def verify_and_consume(
        self,
        ticket_token: Optional[str],
        match_id: Optional[str],
        username: str,
        duration_sec: float,
        moves_count: int
    ) -> Tuple[bool, str]:
        """
        Validates match ticket integrity, non-repetition, and gameplay sanity checks.
        """
        # If no ticket provided, check if required
        if not ticket_token:
            # Enforce ticket requirement in all environments for protected record
            return False, "Thiếu Match Ticket hợp lệ. Hãy bắt đầu ván đấu qua /api/match/start!"

        parts = ticket_token.split(".")
        if len(parts) != 2:
            return False, "Định dạng Match Ticket không hợp lệ!"

        payload, signature = parts[0], parts[1]
        expected_sig = self._sign_payload(payload)
        if not hmac.compare_digest(signature, expected_sig):
            return False, "Chữ ký số Match Ticket không hợp lệ hoặc bị can thiệp trái phép!"

        fields = payload.split(":")
        if len(fields) < 6:
            return False, "Nội dung Match Ticket không đầy đủ trường dữ liệu!"

        t_match_id, t_user, t_opp, t_ai, t_role, t_issued = fields[0], fields[1], fields[2], fields[3], fields[4], float(fields[5])

        if match_id and match_id != t_match_id:
            return False, "Match ID không khớp với Match Ticket đã cấp!"

        if t_user.lower() != username.lower():
            return False, "Match Ticket không thuộc về người chơi này!"

        now = time.time()
        # Check ticket validity window (max 3 hours for a match)
        if now - t_issued > 10800:
            return False, "Match Ticket đã hết hạn (quá 3 giờ kể từ khi bắt đầu)!"
        if t_issued > now + 30: # 30s clock skew allowance
            return False, "Thời gian trên Match Ticket không hợp lệ (từ tương lai)!"

        # Gameplay sanity checks (Anti-farming & instant-win bots)
        if duration_sec < 4.0 and moves_count < 1:
            return False, "Ván cờ kết thúc bất thường (< 4 giây và 0 nước đi). Kết quả bị từ chối bởi hệ thống chống gian lận!"

        with self._lock:
            ticket_meta = self._tickets.get(t_match_id)
            if not ticket_meta:
                # Ticket not in memory (server restarted or forged ID)
                # If signature is valid and within 10 min, we can register it or check consumed
                ticket_meta = {
                    "username": t_user.lower(),
                    "issued_at": t_issued,
                    "consumed": False
                }
                self._tickets[t_match_id] = ticket_meta

            if ticket_meta["consumed"]:
                return False, "Match Ticket này đã được ghi nhận trước đó! Nghi vấn Replay Attack."

            # Mark ticket as consumed
            ticket_meta["consumed"] = True

        return True, "Match ticket verified and consumed."

    def _cleanup_expired(self):
        """Purge tickets older than 4 hours from memory."""
        now = time.time()
        to_del = [m_id for m_id, data in self._tickets.items() if now - data.get("issued_at", 0) > 14400]
        for m_id in to_del:
            del self._tickets[m_id]

match_ticket_manager = MatchTicketManager()

# ---------------------------------------------------------------------------
# 3. Sliding-Window In-Memory Rate Limiter
# ---------------------------------------------------------------------------
class SlidingWindowRateLimiter:
    """
    High-performance sliding-window in-memory rate limiter per IP / bucket.
    """
    def __init__(self):
        self._lock = threading.Lock()
        # bucket_key -> List[timestamp]
        self._history: Dict[str, List[float]] = defaultdict(list)

    def is_allowed(self, client_ip: str, bucket: str, max_requests: int, window_sec: int = 60) -> Tuple[bool, int, int]:
        """
        Checks if the request is permitted within the rate limit.
        Returns: (allowed: bool, remaining_requests: int, retry_after_sec: int)
        """
        now = time.time()
        key = f"{client_ip}:{bucket}"
        threshold = now - window_sec

        with self._lock:
            timestamps = self._history[key]
            # Remove timestamps outside current window
            while timestamps and timestamps[0] <= threshold:
                timestamps.pop(0)

            count = len(timestamps)
            if count >= max_requests:
                earliest = timestamps[0]
                retry_after = int(earliest + window_sec - now) + 1
                return False, 0, max(1, retry_after)

            timestamps.append(now)
            remaining = max_requests - count - 1
            return True, remaining, 0

    def cleanup(self):
        """Purges inactive keys."""
        now = time.time()
        with self._lock:
            empty_keys = []
            for k, ts_list in self._history.items():
                while ts_list and ts_list[0] <= now - 3600:
                    ts_list.pop(0)
                if not ts_list:
                    empty_keys.append(k)
            for k in empty_keys:
                del self._history[k]

rate_limiter = SlidingWindowRateLimiter()

# ---------------------------------------------------------------------------
# 4. Sensitive Data & PII Masking Utilities
# ---------------------------------------------------------------------------
def mask_email(email: str) -> str:
    """
    Masks Gmail address to protect player privacy (PII).
    Example: 'nguyenvana@gmail.com' -> 'n***a@gmail.com'
             'dat@gmail.com'        -> 'd*t@gmail.com'
    """
    if not email or "@" not in email:
        return email or ""
    parts = email.split("@", 1)
    user, domain = parts[0], parts[1]
    if len(user) <= 1:
        masked_user = "*"
    elif len(user) == 2:
        masked_user = f"{user[0]}*"
    elif len(user) <= 4:
        masked_user = f"{user[0]}**{user[-1]}"
    else:
        masked_user = f"{user[0]}***{user[-1]}"
    return f"{masked_user}@{domain}"
