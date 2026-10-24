"""
=============================================================================
SUPABASE POSTGRESQL CONNECTION DIAGNOSTIC TOOL
=============================================================================
Chay script nay de kiem tra ket noi database:
python test_connection.py
=============================================================================
"""

import sys
import os
import asyncio
import socket

# Ensure UTF-8 output on Windows console
if sys.platform == "win32":
    try:
        sys.stdout.reconfigure(encoding="utf-8")
        sys.stderr.reconfigure(encoding="utf-8")
    except Exception:
        pass

# Add parent directory to path
sys.path.insert(0, os.path.abspath(os.path.dirname(__file__)))

from app.config import DATABASE_URL, RAW_DB_URL, sanitize_db_url, IS_SUPABASE, IS_SQLITE
from app.database import engine, init_db
from sqlalchemy import text


async def run_diagnostics():
    print("=" * 65)
    print(">>> KIEM TRA KET NOI CSDL SUPABASE / POSTGRESQL <<<")
    print("=" * 65)
    print(f"[*] Database URL: {sanitize_db_url(DATABASE_URL)}")

    # 1. DNS Resolution check
    if IS_SUPABASE:
        try:
            # Extract host
            from urllib.parse import urlparse
            parsed = urlparse(DATABASE_URL.replace("postgresql+asyncpg://", "http://"))
            host = parsed.hostname
            port = parsed.port or 5432
            print(f"[*] Dang phan giai DNS cho host: {host}:{port} ...")
            infos = socket.getaddrinfo(host, port)
            ipv4_list = [i[4][0] for i in infos if i[0] == socket.AF_INET]
            ipv6_list = [i[4][0] for i in infos if i[0] == socket.AF_INET6]
            print(f"    - IPv4 addresses: {ipv4_list or 'Khong co (Chi dung IPv6)'}")
            print(f"    - IPv6 addresses: {ipv6_list}")

            if not ipv4_list and "pooler.supabase.com" not in host:
                print("\n[!] CANH BAO QUAN TRONG:")
                print("    Host truc tiep 'db.<project>.supabase.co' chi ho tro IPv6.")
                print("    Neu mang cua ban khong ho tro IPv6, ket noi se bi Timeout.")
                print("    Giai phap: Hay dung URL 'Connection Pooling' (pooler.supabase.com) tu Supabase Dashboard!")
        except Exception as e:
            print(f"    [!] DNS Resolution warning: {e}")

    print("\n[*] Dang thu ket noi toi Database (timeout 6s)...", flush=True)
    try:
        # Quick timeout check using asyncpg directly first
        from app.config import RAW_DB_URL
        import asyncpg
        conn_str = RAW_DB_URL.replace("postgresql+asyncpg://", "postgresql://")
        if not conn_str:
            conn_str = DATABASE_URL
        
        conn = await asyncpg.connect(conn_str, timeout=6.0)
        pg_ver = await conn.fetchval("SELECT version();")
        await conn.close()
        print(f"\n>>> [SUCCESS] KET NOI THANH CONG!")
        print(f"    Phien ban Database: {pg_ver[:60]}")

        # 3. Test Table Initialization
        print("[*] Dang kiem tra / khoi tao bang (Players, Matches, Stats)...", flush=True)
        await init_db()
        print(">>> [SUCCESS] Tat ca cac bang da san sang de luu tru du lieu game!", flush=True)

    except Exception as e:
        print("\n>>> [THAT BAI] KHONG THE KET NOI TOI DATABASE:")
        print(f"    Loai loi: {type(e).__name__}")
        print(f"    Chi tiet: {e}")
        print("\n[*] HUONG DAN KHAC PHUC:")
        print("1. Neu bi loi [WinError 121] / Timeout: Hay vao Supabase Dashboard:")
        print("   Project Settings -> Database -> Connection string -> Connection pooling")
        print("   Copy URI Pooler (ho tro IPv4) roi dan vao file .env.")
        print("2. Neu bi loi Password authentication failed: Kiem tra lai mat khau Supabase.")
        print("3. Trong khi chua cau hinh Supabase, server se tu dong dung local SQLite.")


if __name__ == "__main__":
    asyncio.run(run_diagnostics())
