import sys
import os
import time
import asyncio

sys.path.insert(0, os.path.abspath(os.path.join(os.path.dirname(__file__), "..")))

# pyrefly: ignore[missing-import]
from fastapi.testclient import TestClient
from app.main import app
from app.security import rate_limiter
from app.database import init_db

def test_live_endpoints():
    rate_limiter.reset()
    try:
        asyncio.run(init_db())
    except Exception as e:
        print(f"[WARN] init_db in test: {e}")

    with TestClient(app) as client:
        print("\n>>> TESTING LIVE FASTAPI REST ENDPOINTS WITH POSTGRESQL/DATABASE <<<")

        # 1. Root & Health
        r_root = client.get("/")
        assert r_root.status_code == 200
        print(f"[PASS] GET / -> {r_root.json()['service']} (status: {r_root.json()['status']})")

        r_health = client.get("/health")
        assert r_health.status_code == 200
        print(f"[PASS] GET /health -> {r_health.json()}")

        # 2. Auth Login (Dynamic Registration)
        login_payload = {"gmail": "commander_live@gmail.com", "username": "CyberCommander"}
        r_login = client.post("/api/auth/login", json=login_payload)
        assert r_login.status_code == 200, f"Login failed: {r_login.text}"
        login_json = r_login.json()
        user_data = login_json["user"]
        token = login_json.get("access_token")
        auth_headers = {"Authorization": f"Bearer {token}"} if token else {}
        print(f"[PASS] POST /api/auth/login -> User: {user_data['username']}, ELO: {user_data['elo']}, Level: {user_data['level']}, Gold: {user_data['gold']}")

        # 3. Dynamic Profile Query
        r_prof = client.get(f"/api/player/profile?username={user_data['username']}", headers=auth_headers)
        assert r_prof.status_code == 200
        prof = r_prof.json()
        assert prof["username"] == "CyberCommander"
        print(f"[PASS] GET /api/player/profile -> Level: {prof['level']}, Exp: {prof['exp']}/{prof['max_exp']}, Gold: {prof['gold']}")

        # 4. Dynamic Stats Query
        r_stats = client.get(f"/api/player/stats?username={user_data['username']}")
        assert r_stats.status_code == 200
        stats = r_stats.json()
        print(f"[PASS] GET /api/player/stats -> Elo: {stats['elo']}, W/L/D: {stats['wins']}/{stats['losses']}/{stats['draws']}")

        # 4b. Start Match Session (Obtain Match Ticket)
        r_start = client.post("/api/match/start", json={
            "opponent_name": "Dark Syndicate AI",
            "is_ai": True,
            "player_role": "white"
        }, headers=auth_headers)
        assert r_start.status_code == 200
        ticket_data = r_start.json()
        print(f"[PASS] POST /api/match/start -> Ticket ID: {ticket_data['match_id']}")

        # 5. Record Match Result (Secure Transaction with Match Ticket)
        match_payload = {
            "username": "CyberCommander",
            "opponent_name": "Dark Syndicate AI",
            "is_ai": True,
            "player_role": "white",
            "result": "win",
            "moves_count": 38,
            "duration_sec": 415.5,
            "match_ticket": ticket_data["match_ticket"],
            "match_id": ticket_data["match_id"]
        }
        r_match = client.post("/api/match/record", json=match_payload, headers=auth_headers)
        assert r_match.status_code == 200
        match_res = r_match.json()
        assert match_res["elo_change"] == 12
        print(f"[PASS] POST /api/match/record -> +{match_res['elo_change']} Elo (New: {match_res['new_elo']}), +{match_res['gold_gained']} Gold, +{match_res['exp_gained']} EXP")

        # 6. Match History
        r_hist = client.get(f"/api/player/history?username={user_data['username']}&limit=5")
        assert r_hist.status_code == 200
        history_res = r_hist.json()
        assert history_res["total"] >= 1
        last_m = history_res["matches"][0]
        print(f"[PASS] GET /api/player/history -> Found {history_res['total']} match(es), last vs: {last_m['opponent_name']} ({last_m['score']})")

        # 7. Global Leaderboard
        r_lead = client.get("/api/leaderboard?limit=10")
        assert r_lead.status_code == 200
        lead_res = r_lead.json()
        assert len(lead_res["leaderboard"]) >= 1
        print(f"[PASS] GET /api/leaderboard -> Total: {lead_res['total']}, Top 1: {lead_res['leaderboard'][0]['username']} ({lead_res['leaderboard'][0]['elo']} Elo)")

        # 8. Online Players & Search
        r_online = client.get("/api/players/online")
        assert r_online.status_code == 200
        print(f"[PASS] GET /api/players/online -> Count: {r_online.json()['count']}")

        r_search = client.get("/api/players/search?query=cyber")
        assert r_search.status_code == 200
        matches = r_search.json().get("matches", [])
        print(f"[PASS] GET /api/players/search?query=cyber -> Found {len(matches)} match(es)")

        # 9. AI Move (Minimax)
        ai_payload = {
            "fen": "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1",
            "depth": 2
        }
        r_ai = client.post("/api/ai-move", json=ai_payload)
        assert r_ai.status_code == 200
        ai_res = r_ai.json()
        print(f"[PASS] POST /api/ai-move -> Best Move: {ai_res['best_move']}, Eval: {ai_res['evaluation_score']}")

        # 10. Card Recognition
        dummy_b64 = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg=="
        r_card = client.post("/api/recognize-card", json={"image_base64": dummy_b64})
        assert r_card.status_code == 200
        card_res = r_card.json()
        print(f"[PASS] POST /api/recognize-card -> Card: {card_res['description']}, Conf: {card_res['confidence_score']}")

        print("\n>>> ALL LIVE FASTAPI & DATABASE ENDPOINTS VERIFIED 100% OK! <<<\n")

if __name__ == "__main__":
    test_live_endpoints()
