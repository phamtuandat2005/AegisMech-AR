"""
Online Presence & Real-time Matchmaking Engine for 3D Mech AR Chess.
Features:
1. Authoritative Player State Machine (ONLINE, SEARCHING, IN_GAME, OFFLINE).
2. Authenticated Session Tracking with Single-Session Enforcement (kick/supersede duplicate login).
3. Heartbeat (Ping/Pong) Monitoring & Auto-Timeout Detection.
4. Concurrency-Safe Matchmaking Queue (Asyncio Lock protected).
5. Dynamic Adaptive ELO Range Matchmaking with Wait-Time Expansion.
6. Seamless Room Pairing & Lifecycle Tracking.
"""

import asyncio
import time
import uuid
import logging
from enum import Enum
from typing import Dict, List, Optional, Tuple, Any
from fastapi import WebSocket, status

logger = logging.getLogger("MechChessPresence")

# ---------------------------------------------------------------------------
# 1. Authoritative Player Status Enum
# ---------------------------------------------------------------------------
class PlayerStatus(str, Enum):
    OFFLINE = "offline"
    ONLINE = "online"       # In lobby, idle
    SEARCHING = "searching" # Actively in matchmaking queue
    IN_GAME = "in_game"     # In an active match room

# ---------------------------------------------------------------------------
# 2. Presence Client Model
# ---------------------------------------------------------------------------
class PresenceClient:
    def __init__(
        self,
        websocket: WebSocket,
        username: str,
        gmail: str,
        elo: int = 1200,
        session_id: Optional[str] = None,
        client_ip: str = "127.0.0.1"
    ):
        self.websocket = websocket
        self.username = username
        self.gmail = gmail
        self.elo = elo
        self.status = PlayerStatus.ONLINE
        self.session_id = session_id or str(uuid.uuid4())
        self.client_ip = client_ip
        self.time_control = "10 + 0"
        self.queue_time = 0.0
        self.connected_at = time.time()
        self.last_seen = time.time()
        self.current_room_id: Optional[str] = None
        self.last_search_request_time: float = 0.0

    def touch(self):
        """Refreshes heartbeat timestamp."""
        self.last_seen = time.time()

    def to_dict(self) -> Dict[str, Any]:
        return {
            "username": self.username,
            "elo": self.elo,
            "status": self.status.value,
            "last_seen": int(self.last_seen),
            "connected_at": int(self.connected_at)
        }

# ---------------------------------------------------------------------------
# 3. Presence & Matchmaking Manager
# ---------------------------------------------------------------------------
class PresenceManager:
    """
    Thread-safe & asyncio-safe Presence and Matchmaking Manager.
    Manages active socket connections, heartbeats, matchmaking queue, and room assignment.
    """
    HEARTBEAT_TIMEOUT_SEC = 30.0   # Disconnect if no ping/heartbeat within 30s
    HEARTBEAT_CHECK_INTERVAL = 5.0  # Background sweep frequency

    def __init__(self):
        self._lock = asyncio.Lock()
        self.clients: Dict[str, PresenceClient] = {}     # lower_username -> PresenceClient
        self.matchmaking_queue: List[str] = []          # list of lower_usernames in queue
        self._heartbeat_task: Optional[asyncio.Task] = None
        self._running = False

    # -----------------------------------------------------------------------
    # Session Management & Single Active Session Enforcement
    # -----------------------------------------------------------------------
    async def register_client(
        self,
        websocket: WebSocket,
        username: str,
        gmail: str,
        elo: int,
        client_ip: str = "127.0.0.1"
    ) -> PresenceClient:
        """
        Registers an authenticated client socket.
        If an existing session exists for this user, gracefully supersedes it (kicks old socket).
        """
        lower = username.lower()
        new_session_id = str(uuid.uuid4())

        async with self._lock:
            # Handle duplicate connection / multiple device login
            if lower in self.clients:
                old_client = self.clients[lower]
                logger.warning(f"[Presence] Multiple login detected for '{username}'. Superseding session {old_client.session_id} with {new_session_id}")
                
                # Remove from matchmaking if previously queued
                if lower in self.matchmaking_queue:
                    self.matchmaking_queue.remove(lower)

                # Send superseded notification and close old socket
                try:
                    await old_client.websocket.send_json({
                        "type": "session_superseded",
                        "message": "Tài khoản của bạn đã được kết nối từ một thiết bị hoặc phiên mới!"
                    })
                    await old_client.websocket.close(
                        code=status.WS_1000_NORMAL_CLOSURE,
                        reason="Session superseded by another connection"
                    )
                except Exception:
                    pass

            new_client = PresenceClient(
                websocket=websocket,
                username=username,
                gmail=gmail,
                elo=elo,
                session_id=new_session_id,
                client_ip=client_ip
            )
            self.clients[lower] = new_client
            logger.info(f"[Presence] Player '{username}' is now ONLINE (Session: {new_session_id[:8]})")
            return new_client

    async def disconnect_client(self, username: str, session_id: Optional[str] = None) -> Optional[PresenceClient]:
        """
        Removes client upon disconnection.
        Only disconnects if session_id matches (prevents stale socket closing a newly registered session).
        """
        lower = username.lower()
        async with self._lock:
            if lower not in self.clients:
                return None

            client = self.clients[lower]
            if session_id and client.session_id != session_id:
                # Stale connection closing; do not remove the new active session
                return None

            if lower in self.matchmaking_queue:
                self.matchmaking_queue.remove(lower)

            client.status = PlayerStatus.OFFLINE
            del self.clients[lower]
            logger.info(f"[Presence] Player '{username}' disconnected -> OFFLINE")
            return client

    def add_client(self, websocket: WebSocket, username: str, gmail: str, elo: int = 1200):
        """Synchronous wrapper for legacy compatibility."""
        lower = username.lower()
        client = PresenceClient(websocket, username, gmail, elo)
        self.clients[lower] = client
        return client

    def remove_client(self, username: str):
        """Synchronous wrapper for legacy compatibility."""
        lower = username.lower()
        if lower in self.matchmaking_queue:
            self.matchmaking_queue.remove(lower)
        if lower in self.clients:
            del self.clients[lower]

    async def touch_heartbeat(self, username: str) -> bool:
        """Updates last_seen timestamp on ping/heartbeat."""
        lower = username.lower()
        async with self._lock:
            client = self.clients.get(lower)
            if client:
                client.touch()
                return True
        return False

    # -----------------------------------------------------------------------
    # Matchmaking Queue Operations (Concurrency-Safe)
    # -----------------------------------------------------------------------
    async def add_to_queue(self, username: str, time_control: str = "10 + 0") -> Tuple[bool, str]:
        """
        Validates player status and safely enqueues player into matchmaking.
        Rejects duplicate requests and invalid state transitions.
        """
        lower = username.lower()
        now = time.time()

        async with self._lock:
            client = self.clients.get(lower)
            if not client:
                return False, "Người chơi chưa kết nối vào sảnh trực tuyến!"

            # Anti-Spam / Debounce check
            if now - client.last_search_request_time < 0.8:
                return False, "Yêu cầu tìm trận quá nhanh! Vui lòng đợi trong giây lát."

            # Strict State Machine Check
            if client.status == PlayerStatus.SEARCHING or lower in self.matchmaking_queue:
                return False, "Bạn đã ở trong hàng đợi tìm trận!"

            if client.status == PlayerStatus.IN_GAME:
                return False, "Bạn đang trong ván cờ trực tuyến, không thể tìm trận mới!"

            client.status = PlayerStatus.SEARCHING
            client.time_control = time_control
            client.queue_time = now
            client.last_search_request_time = now

            if lower not in self.matchmaking_queue:
                self.matchmaking_queue.append(lower)

            logger.info(f"[Matchmaking] '{username}' entered queue (ELO: {client.elo}, Queue Size: {len(self.matchmaking_queue)})")
            return True, "Đã tham gia hàng đợi tìm trận thành công."

    async def remove_from_queue(self, username: str) -> bool:
        """Safely removes player from matchmaking queue and resets to ONLINE status."""
        lower = username.lower()
        async with self._lock:
            if lower in self.matchmaking_queue:
                self.matchmaking_queue.remove(lower)

            client = self.clients.get(lower)
            if client and client.status == PlayerStatus.SEARCHING:
                client.status = PlayerStatus.ONLINE

            logger.info(f"[Matchmaking] '{username}' left queue. (Queue Size: {len(self.matchmaking_queue)})")
            return True

    # -----------------------------------------------------------------------
    # Adaptive ELO Matchmaking Engine
    # -----------------------------------------------------------------------
    async def try_match_players(self) -> Optional[Tuple[PresenceClient, PresenceClient, str]]:
        """
        Attempts to pair 2 compatible players:
        - Base ELO range: ±150.
        - Expands by ±50 ELO every 5 seconds of queue wait time.
        - After 15 seconds, matches with any available opponent (FIFO fallback).
        - Prevents self-matching.
        """
        async with self._lock:
            if len(self.matchmaking_queue) < 2:
                return None

            now = time.time()
            n = len(self.matchmaking_queue)

            for i in range(n):
                p1_key = self.matchmaking_queue[i]
                p1 = self.clients.get(p1_key)

                if not p1 or p1.status != PlayerStatus.SEARCHING:
                    continue

                wait1 = now - p1.queue_time
                # Adaptive tolerance: 150 + 50 * (seconds / 5)
                elo_window1 = 150 + int(wait1 / 5.0) * 50

                for j in range(i + 1, n):
                    p2_key = self.matchmaking_queue[j]
                    if p1_key == p2_key:
                        continue  # Self-matching guard

                    p2 = self.clients.get(p2_key)
                    if not p2 or p2.status != PlayerStatus.SEARCHING:
                        continue

                    wait2 = now - p2.queue_time
                    elo_window2 = 150 + int(wait2 / 5.0) * 50
                    max_allowed_diff = max(elo_window1, elo_window2)

                    elo_diff = abs(p1.elo - p2.elo)

                    # Either within adaptive ELO window or waited > 15s (FIFO fallback)
                    if elo_diff <= max_allowed_diff or wait1 >= 15.0 or wait2 >= 15.0:
                        # Match Found! Atomically remove both from queue
                        self.matchmaking_queue.remove(p1_key)
                        self.matchmaking_queue.remove(p2_key)

                        room_id = f"MATCH_{uuid.uuid4().hex[:8].upper()}"
                        p1.status = PlayerStatus.IN_GAME
                        p2.status = PlayerStatus.IN_GAME
                        p1.current_room_id = room_id
                        p2.current_room_id = room_id

                        logger.info(
                            f"[Matchmaking] Pair Found: '{p1.username}' (ELO {p1.elo}) vs "
                            f"'{p2.username}' (ELO {p2.elo}) -> Room {room_id} "
                            f"(Diff: {elo_diff}, Max Window: {max_allowed_diff})"
                        )
                        return p1, p2, room_id

            return None

    # -----------------------------------------------------------------------
    # In-Game State Transitions
    # -----------------------------------------------------------------------
    async def set_player_in_game(self, username: str, room_id: str):
        """Marks player as IN_GAME."""
        lower = username.lower()
        async with self._lock:
            client = self.clients.get(lower)
            if client:
                client.status = PlayerStatus.IN_GAME
                client.current_room_id = room_id

    async def set_player_finished_game(self, username: str):
        """Restores player to ONLINE once game concludes."""
        lower = username.lower()
        async with self._lock:
            client = self.clients.get(lower)
            if client:
                client.status = PlayerStatus.ONLINE
                client.current_room_id = None

    # -----------------------------------------------------------------------
    # Heartbeat Sweep & Presence Broadcasting
    # -----------------------------------------------------------------------
    async def check_heartbeat_timeouts(self, timeout_sec: float = HEARTBEAT_TIMEOUT_SEC) -> List[str]:
        """
        Sweeps connected clients. Automatically disconnects clients exceeding timeout.
        """
        now = time.time()
        timed_out: List[str] = []

        async with self._lock:
            for lower, client in list(self.clients.items()):
                if now - client.last_seen > timeout_sec:
                    timed_out.append(lower)

            for lower in timed_out:
                client = self.clients.get(lower)
                if not client:
                    continue

                logger.warning(f"[Presence] Heartbeat timeout for '{client.username}' ({now - client.last_seen:.1f}s > {timeout_sec}s). Disconnecting.")
                if lower in self.matchmaking_queue:
                    self.matchmaking_queue.remove(lower)

                del self.clients[lower]
                try:
                    await client.websocket.close(code=status.WS_1001_GOING_AWAY, reason="Heartbeat timeout")
                except Exception:
                    pass

        return timed_out

    async def broadcast_presence(self):
        """Broadcasts current online players and statuses to all connected lobby sockets."""
        async with self._lock:
            online_list = [
                {
                    "username": c.username,
                    "elo": c.elo,
                    "status": c.status.value
                }
                for c in self.clients.values()
            ]
            recipients = list(self.clients.values())

        payload = {
            "type": "online_players_list",
            "players": online_list
        }

        for c in recipients:
            try:
                await c.websocket.send_json(payload)
            except Exception:
                pass

    async def heartbeat_loop(self):
        """Background coroutine running heartbeat sweeps periodically."""
        self._running = True
        logger.info("[Presence] Heartbeat monitor loop started.")
        try:
            while self._running:
                await asyncio.sleep(self.HEARTBEAT_CHECK_INTERVAL)
                timed_out = await self.check_heartbeat_timeouts(self.HEARTBEAT_TIMEOUT_SEC)
                if timed_out:
                    await self.broadcast_presence()
        except asyncio.CancelledError:
            logger.info("[Presence] Heartbeat monitor loop cancelled.")
        finally:
            self._running = False

    def start_background_task(self):
        if not self._heartbeat_task or self._heartbeat_task.done():
            self._heartbeat_task = asyncio.create_task(self.heartbeat_loop())

    def stop_background_task(self):
        self._running = False
        if self._heartbeat_task and not self._heartbeat_task.done():
            self._heartbeat_task.cancel()

# Global Singleton Presence Manager
presence_manager = PresenceManager()
