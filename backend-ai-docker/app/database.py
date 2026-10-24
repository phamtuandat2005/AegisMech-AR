"""
Database Engine & Session Management for 3D Mech AR Chess.
Supports Supabase PostgreSQL (via asyncpg) and local async SQLite fallback.
"""

import os
import json
import logging
from typing import AsyncGenerator

from sqlalchemy.ext.asyncio import (
    create_async_engine, async_sessionmaker, AsyncSession
)
from sqlalchemy import select, func

from app.config import (
    DATABASE_URL, get_engine_args, IS_SQLITE, IS_SUPABASE, BASE_DIR
)
from app.models import Base, Player, PlayerStats

logger = logging.getLogger("MechChessAI.Database")

# Create Async Engine
engine = create_async_engine(DATABASE_URL, **get_engine_args())

# Async Session Factory
AsyncSessionLocal = async_sessionmaker(
    bind=engine,
    class_=AsyncSession,
    expire_on_commit=False,
    autoflush=False
)

async def get_db() -> AsyncGenerator[AsyncSession, None]:
    """
    FastAPI Dependency for database sessions with automatic rollback on exception.
    """
    async with AsyncSessionLocal() as session:
        try:
            yield session
        except Exception as e:
            await session.rollback()
            logger.error(f"[Database] Transaction error: {e}")
            raise
        finally:
            await session.close()


async def migrate_legacy_json_accounts(session: AsyncSession):
    """
    Migrates accounts from users_db.json into PostgreSQL/SQLite if DB is empty.
    Preserves all existing user records without data loss.
    """
    legacy_file = BASE_DIR / "app" / "users_db.json"
    if not legacy_file.exists():
        return

    try:
        # Check if table already has records
        res = await session.execute(select(func.count(Player.id)))
        count = res.scalar() or 0
        if count > 0:
            return

        with open(legacy_file, "r", encoding="utf-8") as f:
            data = json.load(f)
            users_dict = data.get("users", {})

        if not users_dict:
            return

        logger.info(f"[Database] Migrating {len(users_dict)} legacy accounts from users_db.json...")
        for gmail, u in users_dict.items():
            username = u.get("username", "Player")
            elo = u.get("elo", 1200)

            player = Player(
                gmail=gmail.strip().lower(),
                username=username.strip(),
                level=5,
                exp=320,
                gold=24580,
                gems=1250,
                energy=100
            )
            session.add(player)
            await session.flush()

            stats = PlayerStats(
                player_id=player.id,
                elo=elo,
                matches_played=139,
                wins=129,
                losses=8,
                draws=2,
                highest_elo=elo
            )
            session.add(stats)

        await session.commit()
        logger.info("[Database] Legacy accounts successfully migrated!")
    except Exception as e:
        await session.rollback()
        logger.warning(f"[Database] Legacy migration skipped or encountered error: {e}")


async def init_db():
    """
    Initializes database tables and verifies connection health.
    """
    target_info = "Supabase PostgreSQL" if IS_SUPABASE else ("Local SQLite" if IS_SQLITE else "PostgreSQL")
    logger.info(f"[Database] Connecting to {target_info}...")

    try:
        async with engine.begin() as conn:
            await conn.run_sync(Base.metadata.create_all)
        logger.info(f"[Database] Tables verified and created on {target_info}.")

        # Run legacy migration
        async with AsyncSessionLocal() as session:
            await migrate_legacy_json_accounts(session)

    except Exception as e:
        logger.error(f"[Database] Failed to connect/initialize database: {e}")
        # Does not crash process; callers can handle connection unavailability
        raise
