"""
Pydantic v2 Schemas for 3D Mech AR Chess.
Requests & Responses for Player Profile, Stats, Match Records, and Leaderboards.
"""

from typing import Optional, List, Dict, Any
from pydantic import BaseModel, Field

class LoginRequest(BaseModel):
    gmail: str = Field(..., description="Gmail address of player (e.g. player@gmail.com)")
    username: Optional[str] = Field(None, description="Optional custom alphanumeric username (3-20 chars)")

class PlayerProfileResponse(BaseModel):
    id: str
    gmail: str
    username: str
    avatar_url: str
    level: int
    exp: int
    max_exp: int
    gold: int
    gems: int
    energy: int
    elo: int
    matches_played: int
    wins: int
    losses: int
    draws: int
    win_rate: float
    created_at: str

class LoginResponse(BaseModel):
    success: bool
    message: str
    user: PlayerProfileResponse
    access_token: Optional[str] = None
    token_type: Optional[str] = "bearer"

class PlayerStatsResponse(BaseModel):
    username: str
    elo: int
    matches_played: int
    wins: int
    losses: int
    draws: int
    win_rate: float
    win_streak: int
    highest_elo: int

class MatchStartRequest(BaseModel):
    opponent_name: Optional[str] = "Cyber AI"
    is_ai: Optional[bool] = False
    player_role: Optional[str] = "white"

class MatchStartResponse(BaseModel):
    success: bool
    match_id: str
    match_ticket: str
    issued_at: float

class MatchHistoryItemResponse(BaseModel):
    id: str
    match_code: str
    opponent_name: str
    is_ai: bool
    player_role: str
    result: str
    score: str
    elo_change: int
    moves_count: int
    duration_sec: float
    ended_at: str

class MatchHistoryResponse(BaseModel):
    total: int
    matches: List[MatchHistoryItemResponse]

class MatchRecordRequest(BaseModel):
    username: str
    opponent_name: Optional[str] = "Cyber AI"
    is_ai: Optional[bool] = False
    player_role: Optional[str] = "white"
    result: str = Field(..., description="Result: 'win', 'loss', or 'draw'")
    moves_count: Optional[int] = 0
    duration_sec: Optional[float] = 0.0
    match_ticket: Optional[str] = Field(None, description="Cryptographic ticket issued by /api/match/start")
    match_id: Optional[str] = Field(None, description="Match ID tied to the ticket")

class MatchRecordResponse(BaseModel):
    success: bool
    message: str
    elo_change: int
    new_elo: int
    exp_gained: int
    gold_gained: int
    leveled_up: bool
    new_level: int
    new_exp: int
    new_gold: int

class LeaderboardItemResponse(BaseModel):
    rank: int
    username: str
    elo: int
    level: int
    wins: int
    matches_played: int
    win_rate: float

class LeaderboardResponse(BaseModel):
    total: int
    leaderboard: List[LeaderboardItemResponse]
