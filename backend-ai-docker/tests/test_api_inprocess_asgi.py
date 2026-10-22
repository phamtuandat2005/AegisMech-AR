import sys
import os
import time

sys.path.insert(0, os.path.abspath(os.path.join(os.path.dirname(__file__), "..")))

# pyrefly: ignore[missing-import]
from fastapi.testclient import TestClient
from app.main import app

def test_live_endpoints():
    client = TestClient(app)

    print("\n>>> TESTING LIVE FASTAPI REST ENDPOINTS <<<")

    # 1. Root & Health
    r_root = client.get("/")
    assert r_root.status_code == 200
    print(f"[PASS] GET / -> {r_root.json()['service']} (status: {r_root.json()['status']})")

    r_health = client.get("/health")
    assert r_health.status_code == 200
    print(f"[PASS] GET /health -> {r_health.json()}")

    # 2. Auth Login
    login_payload = {"gmail": "audit_tester@gmail.com", "username": "AuditMech01"}
    r_login = client.post("/api/auth/login", json=login_payload)
    assert r_login.status_code == 200, f"Login failed: {r_login.text}"
    user_data = r_login.json()["user"]
    print(f"[PASS] POST /api/auth/login -> User: {user_data['username']}, ELO: {user_data['elo']}")

    # 3. Online Players & Search
    r_online = client.get("/api/players/online")
    assert r_online.status_code == 200
    print(f"[PASS] GET /api/players/online -> Count: {r_online.json()['count']}")

    r_search = client.get("/api/players/search?query=audit")
    assert r_search.status_code == 200
    matches = r_search.json().get("matches", [])
    print(f"[PASS] GET /api/players/search?query=audit -> Found {len(matches)} match(es)")

    # 4. Rooms
    r_rooms = client.get("/api/rooms")
    assert r_rooms.status_code == 200
    print(f"[PASS] GET /api/rooms -> Active rooms: {len(r_rooms.json()['active_rooms'])}")

    # 5. AI Move (Minimax)
    ai_payload = {
        "fen": "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1",
        "difficulty": 2
    }
    r_ai = client.post("/api/ai-move", json=ai_payload)
    assert r_ai.status_code == 200
    ai_res = r_ai.json()
    print(f"[PASS] POST /api/ai-move -> Best Move: {ai_res['best_move']}, Eval: {ai_res['evaluation_score']}")

    # 6. Card Recognition
    # Dummy base64 1x1 png
    dummy_b64 = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg=="
    r_card = client.post("/api/recognize-card", json={"image_base64": dummy_b64})
    assert r_card.status_code == 200
    card_res = r_card.json()
    print(f"[PASS] POST /api/recognize-card -> Card: {card_res['description']}, Command: {card_res['command']}, Conf: {card_res['confidence_score']}")

    print(">>> ALL LIVE FASTAPI ENDPOINTS VERIFIED 100% OK! <<<\n")

if __name__ == "__main__":
    test_live_endpoints()
