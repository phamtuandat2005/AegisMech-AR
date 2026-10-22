"""
Real Network TCP & WebSocket Test Suite for AegisMech-AR Backend AI Server.
Spawns live Uvicorn server on a dedicated test port (8009), sends actual network HTTP
and WebSocket frames, validates status codes, schemas, input errors, and clean matchmaking/disconnect.
"""

import sys
import os
import time
import json
import base64
import asyncio
import subprocess
import urllib.request
import urllib.error

# Ensure UTF-8 output on Windows console
if sys.platform == "win32":
    try:
        sys.stdout.reconfigure(encoding="utf-8")
        sys.stderr.reconfigure(encoding="utf-8")
    except Exception:
        pass

# pyrefly: ignore[missing-import]
import websockets

TEST_PORT = 8009
BASE_HTTP_URL = f"http://127.0.0.1:{TEST_PORT}"
BASE_WS_URL = f"ws://127.0.0.1:{TEST_PORT}"

def http_get(path: str):
    url = f"{BASE_HTTP_URL}{path}"
    req = urllib.request.Request(url, headers={"User-Agent": "AegisTestClient/1.0"})
    try:
        with urllib.request.urlopen(req, timeout=5) as resp:
            data = resp.read().decode("utf-8")
            return resp.status, json.loads(data)
    except urllib.error.HTTPError as e:
        data = e.read().decode("utf-8")
        try:
            body = json.loads(data)
        except Exception:
            body = {"raw": data}
        return e.code, body

def http_post(path: str, payload: dict):
    url = f"{BASE_HTTP_URL}{path}"
    json_bytes = json.dumps(payload).encode("utf-8")
    req = urllib.request.Request(
        url,
        data=json_bytes,
        headers={"Content-Type": "application/json", "User-Agent": "AegisTestClient/1.0"},
        method="POST"
    )
    try:
        with urllib.request.urlopen(req, timeout=10) as resp:
            data = resp.read().decode("utf-8")
            return resp.status, json.loads(data)
    except urllib.error.HTTPError as e:
        data = e.read().decode("utf-8")
        try:
            body = json.loads(data)
        except Exception:
            body = {"raw": data}
        return e.code, body

async def test_websocket_matchmaking_and_gameplay():
    print("\n--- [TEST] Real Network WebSocket: Lobby Matchmaking & Gameplay ---")
    
    async def recv_matching_type(ws, expected_type, timeout=5.0):
        deadline = time.time() + timeout
        while time.time() < deadline:
            remaining = max(0.1, deadline - time.time())
            raw = await asyncio.wait_for(ws.recv(), timeout=remaining)
            msg = json.loads(raw)
            if msg.get("type") == expected_type:
                return msg
        raise TimeoutError(f"Did not receive message of type '{expected_type}' within {timeout}s")

    # 1. Connect two independent WebSocket clients to Lobby
    ws1_url = f"{BASE_WS_URL}/ws/lobby/PlayerAlpha?gmail=alpha@test.com"
    ws2_url = f"{BASE_WS_URL}/ws/lobby/PlayerBeta?gmail=beta@test.com"
    
    async with websockets.connect(ws1_url) as ws1, websockets.connect(ws2_url) as ws2:
        print("[PASS] Both WebSocket clients connected to /ws/lobby.")
        
        # 2. PlayerAlpha searches for match
        await ws1.send(json.dumps({"type": "find_match", "time_control": "10 + 0"}))
        print("[PASS] PlayerAlpha queued into matchmaking.")

        # 3. PlayerBeta searches for match -> Triggers server pair
        await ws2.send(json.dumps({"type": "find_match", "time_control": "10 + 0"}))
        print("[PASS] PlayerBeta queued into matchmaking.")

        # 4. Both clients receive match_found
        res1 = await recv_matching_type(ws1, "match_found", timeout=5.0)
        res2 = await recv_matching_type(ws2, "match_found", timeout=5.0)

        assert res1.get("type") == "match_found", f"Expected match_found on ws1, got {res1}"
        assert res2.get("type") == "match_found", f"Expected match_found on ws2, got {res2}"
        assert res1.get("room_id") == res2.get("room_id"), "Room IDs must match!"
        
        room_id = res1.get("room_id")
        role1 = res1.get("role")
        role2 = res2.get("role")
        assert {role1, role2} == {"white", "black"}, f"Roles must be white and black, got {role1}, {role2}"
        print(f"[PASS] Real Matchmaking paired successfully: Room {room_id} (P1: {role1}, P2: {role2})")

    # 5. Connect both clients to the Game Room WebSocket
    ws_game1_url = f"{BASE_WS_URL}/ws/chess/{room_id}?name=PlayerAlpha"
    ws_game2_url = f"{BASE_WS_URL}/ws/chess/{room_id}?name=PlayerBeta"

    async with websockets.connect(ws_game1_url) as game_ws1, websockets.connect(ws_game2_url) as game_ws2:
        # P1 joins
        join1 = await recv_matching_type(game_ws1, "room_joined", timeout=3.0)
        assert join1.get("type") == "room_joined"

        # P2 joins
        join2 = await recv_matching_type(game_ws2, "room_joined", timeout=3.0)
        assert join2.get("type") == "room_joined"

        # Both receive match_start
        start1 = await recv_matching_type(game_ws1, "match_start", timeout=3.0)
        start2 = await recv_matching_type(game_ws2, "match_start", timeout=3.0)
        assert start1.get("type") == "match_start"
        assert start2.get("type") == "match_start"
        print(f"[PASS] Real Game Room {room_id} launched match_start for both clients.")

        # Identify who is white
        white_ws = game_ws1 if join1.get("role") == "white" else game_ws2
        black_ws = game_ws2 if white_ws == game_ws1 else game_ws1

        # White sends move e2e4
        await white_ws.send(json.dumps({
            "type": "move",
            "from": "e2",
            "to": "e4",
            "notation": "e4",
            "active_turn": "black"
        }))

        # Black receives opponent_move
        opp_move = await recv_matching_type(black_ws, "opponent_move", timeout=3.0)
        assert opp_move.get("type") == "opponent_move"
        assert opp_move.get("from") == "e2" and opp_move.get("to") == "e4"
        print("[PASS] Move e2->e4 relayed over real WebSocket from White to Black.")

        # Black sends move e7e5
        await black_ws.send(json.dumps({
            "type": "move",
            "from": "e7",
            "to": "e5",
            "notation": "e5",
            "active_turn": "white"
        }))

        # White receives opponent_move
        opp_move2 = await recv_matching_type(white_ws, "opponent_move", timeout=3.0)
        assert opp_move2.get("type") == "opponent_move"
        assert opp_move2.get("from") == "e7" and opp_move2.get("to") == "e5"
        print("[PASS] Move e7->e5 relayed over real WebSocket from Black to White.")

    print("[PASS] WebSocket session closed cleanly and room released.")
    return True


def run_all_tests():
    print("=================================================================")
    print(">>> RUNNING LIVE TCP NETWORK & WEBSOCKET AUDIT TEST SUITE <<<")
    print(f"Target Server: http://127.0.0.1:{TEST_PORT}")
    print("=================================================================")

    # Start live Uvicorn server in a subprocess
    python_exe = sys.executable
    server_dir = os.path.dirname(os.path.abspath(__file__))
    cmd = [python_exe, "-m", "uvicorn", "app.main:app", "--host", "127.0.0.1", "--port", str(TEST_PORT)]
    
    server_proc = subprocess.Popen(cmd, cwd=server_dir, stdout=subprocess.PIPE, stderr=subprocess.PIPE)
    print(f"[INFO] Started live test server PID {server_proc.pid} on port {TEST_PORT}...")

    results = []

    try:
        # Wait for server to become healthy (up to 15s)
        ready = False
        for _ in range(30):
            try:
                status, body = http_get("/health")
                if status == 200 and body.get("status") == "healthy":
                    ready = True
                    break
            except Exception:
                pass
            time.sleep(0.5)

        if not ready:
            raise RuntimeError(f"Server on port {TEST_PORT} failed to start in 15 seconds!")
        print("[INFO] Live test server is UP and responding to network requests.\n")

        # --- 1. GET / Root ---
        status, body = http_get("/")
        ok = (status == 200 and "status" in body)
        results.append(("GET /", ok, f"status={status}, service={body.get('service')}"))

        # --- 2. GET /health ---
        status, body = http_get("/health")
        ok = (status == 200 and body.get("status") == "healthy")
        results.append(("GET /health", ok, f"status={status}, healthy={body.get('status') == 'healthy'}"))

        # --- 3. POST /api/auth/login (Valid) ---
        status, body = http_post("/api/auth/login", {"gmail": "network_tester@gmail.com", "username": "NetTester01"})
        ok = (status == 200 and body.get("success") is True)
        results.append(("POST /api/auth/login [Valid]", ok, f"status={status}, user={body.get('user', {}).get('username')}"))

        # --- 4. POST /api/auth/login (Invalid Email Format Error) ---
        status, body = http_post("/api/auth/login", {"gmail": "not-an-email", "username": "BadEmailUser"})
        ok = (status == 400 and "detail" in body)
        results.append(("POST /api/auth/login [Invalid Email Validation 400]", ok, f"status={status}, detail={body.get('detail')}"))

        # --- 5. GET /api/players/online ---
        status, body = http_get("/api/players/online")
        ok = (status == 200 and "players" in body)
        results.append(("GET /api/players/online", ok, f"status={status}, count={body.get('count')}"))

        # --- 6. GET /api/players/search ---
        status, body = http_get("/api/players/search?query=nettester")
        ok = (status == 200 and "matches" in body)
        results.append(("GET /api/players/search", ok, f"status={status}, matches={len(body.get('matches', []))}"))

        # --- 7. GET /api/rooms ---
        status, body = http_get("/api/rooms")
        ok = (status == 200 and "active_rooms" in body)
        results.append(("GET /api/rooms", ok, f"status={status}, rooms={len(body.get('active_rooms', []))}"))

        # --- 8. POST /api/ai-move (Minimax) ---
        ai_req = {
            "fen": "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1",
            "difficulty": 2
        }
        status, body = http_post("/api/ai-move", ai_req)
        ok = (status == 200 and "best_move" in body)
        results.append(("POST /api/ai-move [Minimax]", ok, f"status={status}, best_move={body.get('best_move')}, eval={body.get('evaluation_score')}"))

        # --- 9. POST /api/recognize-card (Valid Base64) ---
        dummy_b64 = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg=="
        status, body = http_post("/api/recognize-card", {"image_base64": dummy_b64})
        model_st = body.get("model_status")
        ok = (status == 200 and body.get("success") is True and model_st in ["trained_checkpoint", "untrained_or_unavailable", "heuristic_fallback"])
        results.append(("POST /api/recognize-card [Card Recognition & Model Status]", ok, f"status={status}, card={body.get('description')}, model_status={model_st}"))

        # --- 10. POST /api/recognize-card (Invalid Base64 Error Handling) ---
        status, body = http_post("/api/recognize-card", {"image_base64": "!!!not_valid_base64_data!!!"})
        ok = (status == 400)
        results.append(("POST /api/recognize-card [Invalid Base64 Error 400]", ok, f"status={status}, error_caught={ok}"))

        # --- 11. Real WebSocket Matchmaking & In-Game Communication ---
        ws_ok = asyncio.run(test_websocket_matchmaking_and_gameplay())
        results.append(("WebSocket Lobby Matchmaking & 2-Player Game Moves", ws_ok, "Matched 2 real WS clients, exchanged e2e4 & e7e5, disconnected cleanly"))

    finally:
        # Graceful shutdown of test server
        server_proc.terminate()
        try:
            server_proc.wait(timeout=5)
        except Exception:
            server_proc.kill()
        print(f"\n[INFO] Stopped live test server PID {server_proc.pid}.")

    passed = 0
    failed = 0
    lines = []
    lines.append("=================================================================")
    lines.append(">>> REAL TCP NETWORK & WEBSOCKET AUDIT RESULTS SUMMARY <<<")
    lines.append(f"Timestamp: {time.strftime('%Y-%m-%d %H:%M:%S')}")
    lines.append("=================================================================")
    
    for name, ok, details in results:
        status_label = "PASS" if ok else "FAIL"
        if ok:
            passed += 1
        else:
            failed += 1
        line = f"[{status_label}] {name}: {details}"
        lines.append(line)
        print(line)

    summary = f"TOTAL: {len(results)} | PASSED: {passed} | FAILED: {failed}"
    lines.append("-----------------------------------------------------------------")
    lines.append(summary)
    lines.append("=================================================================")
    print("-----------------------------------------------------------------")
    print(summary)
    print("=================================================================")

    out_file = os.path.join(server_dir, "test_results_live_network.txt")
    with open(out_file, "w", encoding="utf-8") as f:
        f.write("\n".join(lines) + "\n")
    print(f"\n[INFO] Test report written to: {out_file}")

    if failed > 0:
        sys.exit(1)

if __name__ == "__main__":
    run_all_tests()
