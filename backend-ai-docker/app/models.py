"""
SQLAlchemy 2.0 ORM Models for 3D Mech AR Chess.
Tables:
  - players: Core identity, level, currencies (gold, gems, energy)
  - player_stats: Elo rating, win/loss/draw records, streaks
  - match_history: Detailed records of past matches
"""

import uuid
from datetime import datetime, timezone
from typing import List, Optional

from sqlalchemy import (
    String, Integer, Float, Boolean, DateTime, ForeignKey, func, Text
)
from sqlalchemy.orm import (
    DeclarativeBase, Mapped, mapped_column, relationship
)
from sqlalchemy.types import Uuid

class Base(DeclarativeBase):
    pass

class Player(Base):
    __tablename__ = "players"

    id: Mapped[uuid.UUID] = mapped_column(
        Uuid(as_uuid=True), primary_key=True, default=uuid.uuid4
    )
    gmail: Mapped[str] = mapped_column(
        String(255), unique=True, index=True, nullable=False
    )
    username: Mapped[str] = mapped_column(
        String(50), unique=True, index=True, nullable=False
    )
    avatar_url: Mapped[str] = mapped_column(
        String(255), default="profile_avatar_round.png", nullable=False
    )
    level: Mapped[int] = mapped_column(
        Integer, default=1, nullable=False
    )
    exp: Mapped[int] = mapped_column(
        Integer, default=0, nullable=False
    )
    gold: Mapped[int] = mapped_column(
        Integer, default=500, nullable=False
    )
    gems: Mapped[int] = mapped_column(
        Integer, default=50, nullable=False
    )
    energy: Mapped[int] = mapped_column(
        Integer, default=100, nullable=False
    )
    created_at: Mapped[datetime] = mapped_column(
        DateTime(timezone=True), default=lambda: datetime.now(timezone.utc), server_default=func.now(), nullable=False
    )
    updated_at: Mapped[datetime] = mapped_column(
        DateTime(timezone=True), default=lambda: datetime.now(timezone.utc), server_default=func.now(), onupdate=func.now(), nullable=False
    )

    # Relationships
    stats: Mapped[Optional["PlayerStats"]] = relationship(
        "PlayerStats", back_populates="player", uselist=False, cascade="all, delete-orphan", lazy="selectin"
    )
    matches: Mapped[List["MatchHistory"]] = relationship(
        "MatchHistory", back_populates="player", cascade="all, delete-orphan", order_by="desc(MatchHistory.ended_at)", lazy="selectin"
    )

    def __repr__(self) -> str:
        return f"<Player(username='{self.username}', level={self.level})>"


class PlayerStats(Base):
    __tablename__ = "player_stats"

    id: Mapped[uuid.UUID] = mapped_column(
        Uuid(as_uuid=True), primary_key=True, default=uuid.uuid4
    )
    player_id: Mapped[uuid.UUID] = mapped_column(
        Uuid(as_uuid=True), ForeignKey("players.id", ondelete="CASCADE"), unique=True, index=True, nullable=False
    )
    elo: Mapped[int] = mapped_column(
        Integer, default=1200, index=True, nullable=False
    )
    matches_played: Mapped[int] = mapped_column(
        Integer, default=0, nullable=False
    )
    wins: Mapped[int] = mapped_column(
        Integer, default=0, nullable=False
    )
    losses: Mapped[int] = mapped_column(
        Integer, default=0, nullable=False
    )
    draws: Mapped[int] = mapped_column(
        Integer, default=0, nullable=False
    )
    win_streak: Mapped[int] = mapped_column(
        Integer, default=0, nullable=False
    )
    highest_elo: Mapped[int] = mapped_column(
        Integer, default=1200, nullable=False
    )
    updated_at: Mapped[datetime] = mapped_column(
        DateTime(timezone=True), default=lambda: datetime.now(timezone.utc), server_default=func.now(), onupdate=func.now(), nullable=False
    )

    # Relationship
    player: Mapped["Player"] = relationship("Player", back_populates="stats")

    @property
    def win_rate(self) -> float:
        if self.matches_played == 0:
            return 0.0
        return round((self.wins / self.matches_played) * 100.0, 1)

    def __repr__(self) -> str:
        return f"<PlayerStats(elo={self.elo}, W/L/D={self.wins}/{self.losses}/{self.draws})>"


class MatchHistory(Base):
    __tablename__ = "match_history"

    id: Mapped[uuid.UUID] = mapped_column(
        Uuid(as_uuid=True), primary_key=True, default=uuid.uuid4
    )
    match_code: Mapped[str] = mapped_column(
        String(64), index=True, nullable=False
    )
    player_id: Mapped[uuid.UUID] = mapped_column(
        Uuid(as_uuid=True), ForeignKey("players.id", ondelete="CASCADE"), index=True, nullable=False
    )
    opponent_name: Mapped[str] = mapped_column(
        String(100), default="Cyber AI", nullable=False
    )
    is_ai: Mapped[bool] = mapped_column(
        Boolean, default=False, nullable=False
    )
    player_role: Mapped[str] = mapped_column(
        String(10), default="white", nullable=False
    )
    result: Mapped[str] = mapped_column(
        String(10), nullable=False  # "win", "loss", "draw"
    )
    score: Mapped[str] = mapped_column(
        String(10), default="1 - 0", nullable=False
    )
    elo_change: Mapped[int] = mapped_column(
        Integer, default=0, nullable=False
    )
    moves_count: Mapped[int] = mapped_column(
        Integer, default=0, nullable=False
    )
    duration_sec: Mapped[float] = mapped_column(
        Float, default=0.0, nullable=False
    )
    ended_at: Mapped[datetime] = mapped_column(
        DateTime(timezone=True), default=lambda: datetime.now(timezone.utc), server_default=func.now(), index=True, nullable=False
    )

    # Relationship
    player: Mapped["Player"] = relationship("Player", back_populates="matches")

    def __repr__(self) -> str:
        return f"<MatchHistory(code='{self.match_code}', result='{self.result}', vs='{self.opponent_name}')>"
