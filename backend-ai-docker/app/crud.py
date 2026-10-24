"""
CRUD Operations & Game Logic for 3D Mech AR Chess.
Handles Player Profile, Secure Match Results, Elo Rating Transitions, and Leaderboards.
"""

import re
import uuid
from datetime import datetime, timezone
from typing import Optional, Tuple, Dict, Any, List

from sqlalchemy.ext.asyncio import AsyncSession
from sqlalchemy import select, func, desc
from sqlalchemy.orm import selectinload

from app.models import Player, PlayerStats, MatchHistory
from app.schemas import (
    PlayerProfileResponse, PlayerStatsResponse, MatchHistoryItemResponse,
    MatchRecordRequest, MatchRecordResponse, LeaderboardItemResponse
)

def is_valid_username(username: str) -> bool:
    if not username:
        return False
    return bool(re.match(r"^[a-zA-Z0-9]{3,20}$", username.strip()))

def derive_default_username(gmail: str) -> str:
    prefix = gmail.split("@")[0]
    clean = re.sub(r"[^a-zA-Z0-9]", "", prefix)
    if len(clean) < 3:
        clean = f"Player{clean}"
    return clean[:20]

async def get_player_by_gmail(session: AsyncSession, gmail: str) -> Optional[Player]:
    clean_gmail = gmail.strip().lower()
    stmt = select(Player).options(selectinload(Player.stats)).where(func.lower(Player.gmail) == clean_gmail)
    res = await session.execute(stmt)
    return res.scalar_one_or_none()

async def get_player_by_username(session: AsyncSession, username: str) -> Optional[Player]:
    clean_username = username.strip().lower()
    stmt = select(Player).options(selectinload(Player.stats)).where(func.lower(Player.username) == clean_username)
    res = await session.execute(stmt)
    return res.scalar_one_or_none()

async def register_or_login_player(
    session: AsyncSession, gmail: str, requested_username: Optional[str] = None
) -> Tuple[bool, str, Optional[Player]]:
    clean_gmail = gmail.strip().lower()
    if not clean_gmail.endswith("@gmail.com") or len(clean_gmail) < 11:
        return False, "Địa chỉ email không hợp lệ! Yêu cầu đăng nhập bằng Gmail (@gmail.com).", None

    existing_player = await get_player_by_gmail(session, clean_gmail)

    target_name = requested_username.strip() if requested_username and requested_username.strip() else None

    if not target_name:
        if existing_player:
            target_name = existing_player.username
        else:
            target_name = derive_default_username(clean_gmail)

    if not is_valid_username(target_name):
        return False, f"Tên '{target_name}' không hợp lệ! Chỉ gồm chữ cái, chữ số (3-20 ký tự, ví dụ: PhamVaA, DatIT, SiuVipPro).", None

    # Check if requested username is taken by another account
    username_owner = await get_player_by_username(session, target_name)
    if username_owner and username_owner.gmail.lower() != clean_gmail:
        return False, f"Tên người chơi '{target_name}' đã được người khác sử dụng! Vui lòng chọn tên khác.", None

    if existing_player:
        if existing_player.username != target_name:
            existing_player.username = target_name
            existing_player.updated_at = datetime.now(timezone.utc)
        
        # Ensure stats exist
        if not existing_player.stats:
            stats = PlayerStats(player_id=existing_player.id, elo=1200)
            session.add(stats)
            existing_player.stats = stats

        await session.commit()
        await session.refresh(existing_player)
        return True, "Đăng nhập thành công!", existing_player

    # New Player Registration
    new_player = Player(
        gmail=clean_gmail,
        username=target_name,
        level=1,
        exp=0,
        gold=500,
        gems=50,
        energy=100
    )
    session.add(new_player)
    await session.flush()

    new_stats = PlayerStats(
        player_id=new_player.id,
        elo=1200,
        matches_played=0,
        wins=0,
        losses=0,
        draws=0,
        highest_elo=1200
    )
    session.add(new_stats)
    new_player.stats = new_stats

    await session.commit()
    await session.refresh(new_player)
    return True, "Đăng ký tài khoản thành công!", new_player

def format_profile_response(player: Player) -> PlayerProfileResponse:
    stats = player.stats
    elo = stats.elo if stats else 1200
    matches_played = stats.matches_played if stats else 0
    wins = stats.wins if stats else 0
    losses = stats.losses if stats else 0
    draws = stats.draws if stats else 0
    win_rate = stats.win_rate if stats else 0.0

    max_exp = max(100, player.level * 100)

    return PlayerProfileResponse(
        id=str(player.id),
        gmail=player.gmail,
        username=player.username,
        avatar_url=player.avatar_url,
        level=player.level,
        exp=player.exp,
        max_exp=max_exp,
        gold=player.gold,
        gems=player.gems,
        energy=player.energy,
        elo=elo,
        matches_played=matches_played,
        wins=wins,
        losses=losses,
        draws=draws,
        win_rate=win_rate,
        created_at=player.created_at.strftime("%Y-%m-%d %H:%M:%S") if player.created_at else ""
    )

async def get_match_history_list(
    session: AsyncSession, username: str, limit: int = 10
) -> List[MatchHistoryItemResponse]:
    player = await get_player_by_username(session, username)
    if not player:
        return []

    stmt = (
        select(MatchHistory)
        .where(MatchHistory.player_id == player.id)
        .order_by(desc(MatchHistory.ended_at))
        .limit(limit)
    )
    res = await session.execute(stmt)
    records = res.scalars().all()

    items = []
    for r in records:
        items.append(MatchHistoryItemResponse(
            id=str(r.id),
            match_code=r.match_code,
            opponent_name=r.opponent_name,
            is_ai=r.is_ai,
            player_role=r.player_role,
            result=r.result,
            score=r.score,
            elo_change=r.elo_change,
            moves_count=r.moves_count,
            duration_sec=r.duration_sec,
            ended_at=r.ended_at.strftime("%d/%m/%Y") if r.ended_at else ""
        ))
    return items

async def record_match_result(
    session: AsyncSession, data: MatchRecordRequest
) -> Tuple[bool, str, Dict[str, Any]]:
    """
    Secure backend computation and transaction for match conclusion.
    Prevents client manipulation of Elo and reward values.
    """
    player = await get_player_by_username(session, data.username)
    if not player:
        return False, f"Người chơi '{data.username}' không tồn tại trong hệ thống!", {}

    if not player.stats:
        player.stats = PlayerStats(player_id=player.id, elo=1200)
        session.add(player.stats)

    res_clean = data.result.strip().lower()
    if res_clean not in ("win", "loss", "draw"):
        return False, "Kết quả trận đấu không hợp lệ! (Phải là 'win', 'loss' hoặc 'draw').", {}

    # Elo change formula
    if res_clean == "win":
        elo_change = 12
        exp_gained = 60
        gold_gained = 50
        score_str = "1 - 0"
    elif res_clean == "loss":
        elo_change = -8
        exp_gained = 20
        gold_gained = 15
        score_str = "0 - 1"
    else: # draw
        elo_change = 0
        exp_gained = 30
        gold_gained = 25
        score_str = "½ - ½"

    # Update stats
    player.stats.matches_played += 1
    if res_clean == "win":
        player.stats.wins += 1
        player.stats.win_streak += 1
    elif res_clean == "loss":
        player.stats.losses += 1
        player.stats.win_streak = 0
    else:
        player.stats.draws += 1

    player.stats.elo = max(100, player.stats.elo + elo_change)
    if player.stats.elo > player.stats.highest_elo:
        player.stats.highest_elo = player.stats.elo
    player.stats.updated_at = datetime.now(timezone.utc)

    # Update player currencies and level progression
    player.gold += gold_gained
    player.exp += exp_gained

    leveled_up = False
    max_exp = max(100, player.level * 100)
    while player.exp >= max_exp:
        player.exp -= max_exp
        player.level += 1
        player.gems += 20 # Level up reward!
        leveled_up = True
        max_exp = max(100, player.level * 100)

    player.updated_at = datetime.now(timezone.utc)

    # Insert Match History
    match_code = f"M_{uuid.uuid4().hex[:8].upper()}"
    history_record = MatchHistory(
        match_code=match_code,
        player_id=player.id,
        opponent_name=data.opponent_name or ("Cyber AI" if data.is_ai else "Opponent"),
        is_ai=data.is_ai or False,
        player_role=data.player_role or "white",
        result=res_clean,
        score=score_str,
        elo_change=elo_change,
        moves_count=data.moves_count or 0,
        duration_sec=data.duration_sec or 0.0,
        ended_at=datetime.now(timezone.utc)
    )
    session.add(history_record)

    await session.commit()
    await session.refresh(player)

    result_payload = {
        "elo_change": elo_change,
        "new_elo": player.stats.elo,
        "exp_gained": exp_gained,
        "gold_gained": gold_gained,
        "leveled_up": leveled_up,
        "new_level": player.level,
        "new_exp": player.exp,
        "new_gold": player.gold
    }
    return True, "Ghi nhận kết quả trận đấu thành công!", result_payload

async def get_leaderboard_list(
    session: AsyncSession, limit: int = 20
) -> List[LeaderboardItemResponse]:
    stmt = (
        select(Player, PlayerStats)
        .join(PlayerStats, Player.id == PlayerStats.player_id)
        .order_by(desc(PlayerStats.elo), desc(PlayerStats.wins))
        .limit(limit)
    )
    res = await session.execute(stmt)
    rows = res.all()

    board = []
    for rank, (player, stats) in enumerate(rows, start=1):
        board.append(LeaderboardItemResponse(
            rank=rank,
            username=player.username,
            elo=stats.elo,
            level=player.level,
            wins=stats.wins,
            matches_played=stats.matches_played,
            win_rate=stats.win_rate
        ))
    return board
