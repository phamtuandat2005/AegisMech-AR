"""
Unit & Integration Tests for Supabase PostgreSQL / SQLAlchemy 2.0 Database Layer.
Tests:
- Table creation and database connectivity
- Player registration, login, and validation
- Profile fetching and serialization
- Secure match recording, Elo transition, and currency/exp progression
- Match history fetching
- Leaderboard ranking
"""

import os
import sys
import asyncio

# Add backend directory to sys.path
sys.path.insert(0, os.path.abspath(os.path.join(os.path.dirname(__file__), "..")))

from app.database import init_db, AsyncSessionLocal
from app import crud, schemas

async def run_tests():
    print("=" * 60)
    print(">>> RUNNING SUPABASE POSTGRESQL / SQLALCHEMY TEST SUITE <<<")
    print("=" * 60)

    # 1. Initialize Tables
    await init_db()
    print("[PASS] Database engine connected and tables verified.")

    import uuid
    rand_id = uuid.uuid4().hex[:6]
    test_gmail = f"hero_{rand_id}@gmail.com"
    test_uname = f"Hero{rand_id}"

    async with AsyncSessionLocal() as session:
        # 2. Test Registration & Login
        ok, msg, p1 = await crud.register_or_login_player(session, test_gmail, test_uname)
        assert ok, f"Registration failed: {msg}"
        assert p1.username == test_uname
        assert p1.stats is not None
        assert p1.stats.elo == 1200
        print(f"[PASS] Register Player: {p1.username} (Elo: {p1.stats.elo})")

        # 3. Test Profile Serialization
        prof = crud.format_profile_response(p1)
        assert prof.username == test_uname
        assert prof.level == 1
        assert prof.gold == 500
        print(f"[PASS] Profile Model: Level {prof.level}, Gold {prof.gold}, Gems {prof.gems}")

        # 4. Test Duplicate Username Rejection
        ok_dup, msg_dup, _ = await crud.register_or_login_player(session, f"imposter_{rand_id}@gmail.com", test_uname)
        assert not ok_dup, "Duplicate username was not rejected!"
        print(f"[PASS] Duplicate username rejection verified.")

        # 5. Test Match Record (Win)
        req_win = schemas.MatchRecordRequest(
            username=p1.username,
            opponent_name="Cyber AI (Hard)",
            is_ai=True,
            player_role="white",
            result="win",
            moves_count=42,
            duration_sec=516.0
        )
        ok_win, msg_win, res_win = await crud.record_match_result(session, req_win)
        assert ok_win, f"Record match failed: {msg_win}"
        assert res_win["elo_change"] == 12
        assert res_win["new_elo"] == 1212
        assert res_win["gold_gained"] == 50
        print(f"[PASS] Match Win: +{res_win['elo_change']} Elo -> New Elo {res_win['new_elo']}, +{res_win['gold_gained']} Gold")

        # 6. Test Match Record (Loss)
        req_loss = schemas.MatchRecordRequest(
            username=p1.username,
            opponent_name="Grandmaster Bot",
            is_ai=True,
            player_role="black",
            result="loss",
            moves_count=28,
            duration_sec=310.0
        )
        ok_loss, _, res_loss = await crud.record_match_result(session, req_loss)
        assert ok_loss
        assert res_loss["elo_change"] == -8
        assert res_loss["new_elo"] == 1204
        print(f"[PASS] Match Loss: {res_loss['elo_change']} Elo -> New Elo {res_loss['new_elo']}")

        # 7. Test Match History Retrieval
        history = await crud.get_match_history_list(session, p1.username, limit=5)
        assert len(history) == 2, f"Expected 2 matches, got {len(history)}"
        assert history[0].opponent_name == "Grandmaster Bot"
        assert history[0].result == "loss"
        assert history[1].opponent_name == "Cyber AI (Hard)"
        assert history[1].result == "win"
        print(f"[PASS] Match History: Retrieved {len(history)} match records successfully.")

        # 8. Test Leaderboard
        board = await crud.get_leaderboard_list(session, limit=10)
        assert len(board) >= 1
        print(f"[PASS] Leaderboard: Rank 1 is '{board[0].username}' with {board[0].elo} Elo.")

    print("\n>>> ALL SUPABASE POSTGRESQL DATABASE TESTS PASSED 100%! <<<\n")

if __name__ == "__main__":
    asyncio.run(run_tests())
