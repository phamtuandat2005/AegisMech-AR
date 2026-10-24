"""
Configuration & Environment Settings for 3D Mech AR Chess Backend.
Handles Supabase PostgreSQL connection strings, pooling, and fallbacks.
"""

import os
import re
from pathlib import Path
from typing import Optional, Dict, Any
from dotenv import load_dotenv

# Base directory: backend-ai-docker/
BASE_DIR = Path(__file__).resolve().parent.parent

# Load .env file from backend-ai-docker/.env if present
ENV_PATH = BASE_DIR / ".env"
if ENV_PATH.exists():
    load_dotenv(dotenv_path=ENV_PATH)
else:
    load_dotenv()

APP_ENV = os.getenv("APP_ENV", "development")
DEBUG = os.getenv("DEBUG", "False").lower() in ("true", "1", "t")
PORT = int(os.getenv("PORT", "8000"))
HOST = os.getenv("HOST", "0.0.0.0")

# Database Configuration
RAW_DB_URL = os.getenv("DATABASE_URL", "").strip()

def normalize_database_url(url: str) -> str:
    """
    Normalizes Supabase / PostgreSQL URLs for SQLAlchemy asyncpg engine.
    - Replaces postgres:// or postgresql:// with postgresql+asyncpg://
    - Removes incompatible query parameters for asyncpg if any.
    """
    if not url:
        # Graceful local fallback to async SQLite
        local_db = BASE_DIR / "app" / "mech_chess.db"
        return f"sqlite+aiosqlite:///{local_db.as_posix()}"

    # Replace legacy scheme
    if url.startswith("postgres://"):
        url = url.replace("postgres://", "postgresql+asyncpg://", 1)
    elif url.startswith("postgresql://") and not url.startswith("postgresql+"):
        url = url.replace("postgresql://", "postgresql+asyncpg://", 1)

    return url

DATABASE_URL = normalize_database_url(RAW_DB_URL)
IS_SQLITE = DATABASE_URL.startswith("sqlite")
IS_SUPABASE = "supabase.com" in DATABASE_URL or "supabase.co" in DATABASE_URL

# Pool Settings
DB_POOL_SIZE = int(os.getenv("DB_POOL_SIZE", "10"))
DB_MAX_OVERFLOW = int(os.getenv("DB_MAX_OVERFLOW", "20"))
DB_POOL_TIMEOUT = int(os.getenv("DB_POOL_TIMEOUT", "30"))
DB_POOL_RECYCLE = int(os.getenv("DB_POOL_RECYCLE", "300"))

# Supabase Connect Args (specifically handling transaction pooler PgBouncer prepared statements)
def get_engine_args() -> Dict[str, Any]:
    args: Dict[str, Any] = {
        "echo": DEBUG,
    }
    if IS_SQLITE:
        args["connect_args"] = {"check_same_thread": False}
    else:
        args["pool_size"] = DB_POOL_SIZE
        args["max_overflow"] = DB_MAX_OVERFLOW
        args["pool_timeout"] = DB_POOL_TIMEOUT
        args["pool_recycle"] = DB_POOL_RECYCLE
        args["pool_pre_ping"] = True

        # Connect args for asyncpg
        connect_args: Dict[str, Any] = {"timeout": 5.0}
        if ":6543" in DATABASE_URL or "pooler" in DATABASE_URL:
            # PgBouncer does not support prepared statements across pooled connections
            connect_args["prepared_statement_cache_size"] = 0
            connect_args["statement_cache_size"] = 0
        
        args["connect_args"] = connect_args

    return args

SUPABASE_URL = os.getenv("SUPABASE_URL", "")
SUPABASE_ANON_KEY = os.getenv("SUPABASE_ANON_KEY", "")

# ---------------------------------------------------------------------------
# Security Configuration
# ---------------------------------------------------------------------------
DEFAULT_DEV_SECRET = "mech-chess-insecure-dev-secret-key-change-in-production-2026"
JWT_SECRET_KEY = os.getenv("JWT_SECRET_KEY", DEFAULT_DEV_SECRET)
JWT_ALGORITHM = os.getenv("JWT_ALGORITHM", "HS256")
JWT_ACCESS_TOKEN_EXPIRE_MINUTES = int(os.getenv("JWT_ACCESS_TOKEN_EXPIRE_MINUTES", str(60 * 24 * 7))) # 7 days

# CORS Allowed Origins
RAW_ALLOWED_ORIGINS = os.getenv(
    "ALLOWED_ORIGINS",
    "http://localhost,http://localhost:3000,http://localhost:8000,http://localhost:8080,http://127.0.0.1,http://127.0.0.1:8000,http://127.0.0.1:8080"
)
ALLOWED_ORIGINS = [orig.strip() for orig in RAW_ALLOWED_ORIGINS.split(",") if orig.strip()]

# Maximum payload size (10 MB default)
MAX_PAYLOAD_SIZE_BYTES = int(os.getenv("MAX_PAYLOAD_SIZE_BYTES", str(10 * 1024 * 1024)))

def sanitize_db_url(url: str) -> str:
    """
    Strips credentials from database URL for safe logging and metrics.
    Example: postgresql+asyncpg://postgres:secret123@db.supabase.com:5432/postgres
             -> postgresql+asyncpg://postgres:***@db.supabase.com:5432/postgres
    """
    if not url:
        return "None"
    return re.sub(r"://([^:]+):([^@]+)@", r"://\1:***@", url)
