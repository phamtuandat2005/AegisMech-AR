"""
Automated Security Hardening Test Suite for 3D Mech AR Chess.
Covers:
1. Unauthenticated request rejection (401 Unauthorized).
2. Forged & expired JWT token rejection (401 Unauthorized).
3. IDOR / BOLA authorization enforcement (403 Forbidden).
4. Match ticket anti-cheat validation & replay attack prevention (400 Bad Request).
5. Fast-win farming & bot spam rejection.
6. Sliding-window rate limiter enforcement (429 Too Many Requests).
7. Payload size limit enforcement (413 Payload Too Large).
8. PII masking on player profile endpoints.
9. Out-of-turn WebSocket move rejection.
"""

import sys
import os
import uuid
import time
from datetime import timedelta
# pyrefly: ignore[missing-import]
try:
    # pyrefly: ignore[missing-import]
    import pytest
except ImportError:
    pytest = None
import httpx
from starlette.testclient import TestClient

# Ensure app package is in sys.path
sys.path.insert(0, os.path.abspath(os.path.join(os.path.dirname(__file__), "..")))

from app.main import app, account_manager, room_manager, lobby_manager
from app.security import (
    create_access_token,
    decode_access_token,
    match_ticket_manager,
    rate_limiter,
    mask_email
)
from app.models import Player
from app.database import get_db, AsyncSessionLocal

def client():
    with TestClient(app) as test_client:
        yield test_client

if pytest:
    client = pytest.fixture(scope="module")(client)

# Helper to create a verified registered player
def register_test_player(client: TestClient, username: str, gmail: str):
    res = client.post("/api/auth/login", json={"gmail": gmail, "username": username})
    assert res.status_code == 200
    data = res.json()
    assert data["success"] is True
    assert "access_token" in data
    return data["access_token"]


# =============================================================================
# 1. AUTHENTICATION & TOKEN VALIDATION TESTS
# =============================================================================

def test_unauthenticated_match_start_rejected(client: TestClient):
    """Calling /api/match/start without Authorization header must return 401."""
    res = client.post("/api/match/start", json={"opponent_name": "AI", "is_ai": True})
    assert res.status_code == 401
    assert "Bearer" in res.json()["detail"] or "xác thực" in res.json()["detail"]

def test_unauthenticated_match_record_rejected(client: TestClient):
    """Calling /api/match/record without Authorization header must return 401."""
    res = client.post("/api/match/record", json={
        "username": "hacker99",
        "result": "win",
        "moves_count": 10,
        "duration_sec": 60.0
    })
    assert res.status_code == 401

def test_tampered_jwt_rejected(client: TestClient):
    """A forged or tampered token must be rejected with 401."""
    headers = {"Authorization": "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.invalid.signature"}
    res = client.post("/api/match/start", json={}, headers=headers)
    assert res.status_code == 401

def test_expired_jwt_rejected(client: TestClient):
    """An expired token must be rejected with 401."""
    expired_token = create_access_token(
        {"sub": "expiredUser", "username": "expiredUser"},
        expires_delta=timedelta(seconds=-10)
    )
    headers = {"Authorization": f"Bearer {expired_token}"}
    res = client.post("/api/match/start", json={}, headers=headers)
    assert res.status_code == 401
    assert "hết hạn" in res.json()["detail"]


# =============================================================================
# 2. AUTHORIZATION & IDOR / BOLA TESTS
# =============================================================================

def test_idor_bola_tampering_blocked(client: TestClient):
    """
    User A cannot submit a match result on behalf of User B.
    Backend must detect username mismatch with token 'sub' and return 403 Forbidden.
    """
    u1 = f"alice{uuid.uuid4().hex[:6]}"
    u2 = f"bob{uuid.uuid4().hex[:6]}"
    g1 = f"{u1}@gmail.com"
    g2 = f"{u2}@gmail.com"

    token_a = register_test_player(client, u1, g1)
    _ = register_test_player(client, u2, g2)

    headers_a = {"Authorization": f"Bearer {token_a}"}

    # Alice attempts to submit a match for Bob (IDOR / BOLA attempt)
    malicious_payload = {
        "username": u2,  # Target victim account
        "opponent_name": "Cyber AI",
        "is_ai": True,
        "player_role": "white",
        "result": "win",
        "moves_count": 10,
        "duration_sec": 60.0
    }

    res = client.post("/api/match/record", json=malicious_payload, headers=headers_a)
    assert res.status_code == 403
    assert "BOLA/IDOR" in res.json()["detail"] or "Quyền truy cập bị từ chối" in res.json()["detail"]


# =============================================================================
# 3. GAME INTEGRITY, MATCH TICKETS & ANTI-REPLAY TESTS
# =============================================================================

def test_match_ticket_issuance_and_consumption(client: TestClient):
    """
    Player starts match -> obtains ticket -> records valid match -> success.
    """
    u = f"player{uuid.uuid4().hex[:6]}"
    g = f"{u}@gmail.com"
    token = register_test_player(client, u, g)
    headers = {"Authorization": f"Bearer {token}"}

    # 1. Request match ticket
    start_res = client.post("/api/match/start", json={
        "opponent_name": "Cyber AI",
        "is_ai": True,
        "player_role": "white"
    }, headers=headers)
    assert start_res.status_code == 200
    ticket_data = start_res.json()
    assert ticket_data["success"] is True
    ticket = ticket_data["match_ticket"]
    match_id = ticket_data["match_id"]

    # 2. Settle match with valid ticket and reasonable duration
    record_res = client.post("/api/match/record", json={
        "username": u,
        "opponent_name": "Cyber AI",
        "is_ai": True,
        "player_role": "white",
        "result": "win",
        "moves_count": 8,
        "duration_sec": 45.0,
        "match_ticket": ticket,
        "match_id": match_id
    }, headers=headers)
    assert record_res.status_code == 200
    res_json = record_res.json()
    assert res_json["success"] is True
    assert res_json["elo_change"] > 0

    # 3. Replay attack attempt (resubmitting same ticket)
    replay_res = client.post("/api/match/record", json={
        "username": u,
        "opponent_name": "Cyber AI",
        "is_ai": True,
        "player_role": "white",
        "result": "win",
        "moves_count": 8,
        "duration_sec": 45.0,
        "match_ticket": ticket,
        "match_id": match_id
    }, headers=headers)
    assert replay_res.status_code == 400
    assert "Replay" in replay_res.json()["detail"] or "trước đó" in replay_res.json()["detail"]

def test_tampered_match_ticket_signature_rejected(client: TestClient):
    """If ticket payload or signature is modified, reject with 400."""
    u = f"player{uuid.uuid4().hex[:6]}"
    g = f"{u}@gmail.com"
    token = register_test_player(client, u, g)
    headers = {"Authorization": f"Bearer {token}"}

    # Start match
    start_res = client.post("/api/match/start", json={}, headers=headers)
    ticket = start_res.json()["match_ticket"]

    # Tamper with the signature
    tampered_ticket = ticket[:-4] + "ffff"

    record_res = client.post("/api/match/record", json={
        "username": u,
        "result": "win",
        "moves_count": 5,
        "duration_sec": 30.0,
        "match_ticket": tampered_ticket
    }, headers=headers)
    assert record_res.status_code == 400
    assert "Chữ ký số" in record_res.json()["detail"]

def test_instant_win_farming_rejected(client: TestClient):
    """
    Matches ending in less than 4 seconds with 0 moves are flagged as bots/farming.
    """
    u = f"player{uuid.uuid4().hex[:6]}"
    g = f"{u}@gmail.com"
    token = register_test_player(client, u, g)
    headers = {"Authorization": f"Bearer {token}"}

    start_res = client.post("/api/match/start", json={}, headers=headers)
    ticket = start_res.json()["match_ticket"]

    record_res = client.post("/api/match/record", json={
        "username": u,
        "result": "win",
        "moves_count": 0,
        "duration_sec": 1.2, # Unrealistic instant win
        "match_ticket": ticket
    }, headers=headers)
    assert record_res.status_code == 400
    assert "chống gian lận" in record_res.json()["detail"] or "bất thường" in record_res.json()["detail"]


# =============================================================================
# 4. PII MASKING & DATA PROTECTION TESTS
# =============================================================================

def test_pii_masking_for_unauthenticated_and_third_parties(client: TestClient):
    """
    When profile is viewed by an unauthenticated caller or a third party,
    the Gmail address must be masked. Only the profile owner sees full email.
    """
    u = f"privacy{uuid.uuid4().hex[:6]}"
    g = f"{u}@gmail.com"
    token_owner = register_test_player(client, u, g)

    # 1. Unauthenticated query
    res_public = client.get(f"/api/player/profile?username={u}")
    assert res_public.status_code == 200
    pub_data = res_public.json()
    assert "@gmail.com" in pub_data["gmail"]
    assert "***" in pub_data["gmail"] # Email is masked
    assert pub_data["gmail"] != g

    # 2. Authenticated query by owner
    headers_owner = {"Authorization": f"Bearer {token_owner}"}
    res_owner = client.get(f"/api/player/profile?username={u}", headers=headers_owner)
    assert res_owner.status_code == 200
    owner_data = res_owner.json()
    assert owner_data["gmail"] == g # Full email visible to owner

    # 3. Authenticated query by another user
    u2 = f"other{uuid.uuid4().hex[:6]}"
    token_other = register_test_player(client, u2, f"{u2}@gmail.com")
    headers_other = {"Authorization": f"Bearer {token_other}"}
    res_third_party = client.get(f"/api/player/profile?username={u}", headers=headers_other)
    assert res_third_party.status_code == 200
    third_party_data = res_third_party.json()
    assert "***" in third_party_data["gmail"] # Email is masked for third parties


# =============================================================================
# 5. RATE LIMITING & PAYLOAD SIZE TESTS
# =============================================================================

def test_rate_limiting_enforcement(client: TestClient):
    """
    Spamming requests beyond bucket capacity triggers HTTP 429 Too Many Requests.
    """
    # Rapidly send 20 requests to /api/auth/login (limit is 15/min)
    hit_429 = False
    for i in range(25):
        res = client.post("/api/auth/login", json={"gmail": "ratelimit_test@gmail.com"})
        if res.status_code == 429:
            hit_429 = True
            assert "Retry-After" in res.headers
            assert "Rate Limit Exceeded" in res.json()["detail"]
            break

    assert hit_429 is True
    rate_limiter.reset()

def test_payload_size_limit_enforced(client: TestClient):
    """
    A request exceeding MAX_PAYLOAD_SIZE_BYTES returns 413 Payload Too Large.
    """
    # Send a request with a fake large Content-Length header
    res = client.post(
        "/api/recognize-card",
        headers={"Content-Length": "15000000"}, # 15MB > 10MB limit
        content=b"{}"
    )
    assert res.status_code == 413
    assert "vượt quá giới hạn" in res.json()["detail"]


# =============================================================================
# 6. WEBSOCKET TURN ENFORCEMENT & SECURITY TESTS
# =============================================================================

def test_websocket_out_of_turn_move_rejected(client: TestClient):
    """
    In /ws/chess/{room_id}, if Black tries to move while active_turn is White,
    the server must reject the move and not advance the game state.
    """
    room_id = f"SEC_ROOM_{uuid.uuid4().hex[:6].upper()}"

    # Connect player White
    with client.websocket_connect(f"/ws/chess/{room_id}?name=WhitePlayer") as ws_white:
        _ = ws_white.receive_json() # room_joined

        # Connect player Black
        with client.websocket_connect(f"/ws/chess/{room_id}?name=BlackPlayer") as ws_black:
            _ = ws_black.receive_json() # room_joined
            _ = ws_white.receive_json() # match_start
            _ = ws_black.receive_json() # match_start

            # Black maliciously attempts to move first (current turn is White)
            ws_black.send_json({
                "type": "move",
                "from": "E7",
                "to": "E5",
                "notation": "e5"
            })

            # Black should receive an error
            err_msg = ws_black.receive_json()
            assert err_msg["type"] == "error"
            assert "Chưa đến lượt" in err_msg["message"]


if __name__ == "__main__":
    print("\n=======================================================")
    print("   3D MECH AR CHESS - SECURITY HARDENING TEST SUITE    ")
    print("=======================================================")

    with TestClient(app) as tc:
        tests = [
            ("Unauthenticated Match Start Rejection", test_unauthenticated_match_start_rejected),
            ("Unauthenticated Match Record Rejection", test_unauthenticated_match_record_rejected),
            ("Tampered JWT Token Rejection", test_tampered_jwt_rejected),
            ("Expired JWT Token Rejection", test_expired_jwt_rejected),
            ("IDOR / BOLA Cross-Account Tampering Rejection", test_idor_bola_tampering_blocked),
            ("Match Ticket Issuance & Settle", test_match_ticket_issuance_and_consumption),
            ("Tampered Match Ticket Signature Rejection", test_tampered_match_ticket_signature_rejected),
            ("Instant-Win Bot Farming Rejection", test_instant_win_farming_rejected),
            ("PII Masking for Public & Third Parties", test_pii_masking_for_unauthenticated_and_third_parties),
            ("Payload Size Limit Enforced", test_payload_size_limit_enforced),
            ("Rate Limiting Enforcement", test_rate_limiting_enforcement),
            ("WebSocket Out-of-Turn Move Enforcement", test_websocket_out_of_turn_move_rejected),
        ]

        passed = 0
        for name, fn in tests:
            try:
                fn(tc)
                print(f"[PASS] {name}")
                passed += 1
            except Exception as e:
                print(f"[FAIL] {name}: {e}")
                raise

        print(f"\n[SUMMARY] {passed}/{len(tests)} Security tests PASSED successfully!")
