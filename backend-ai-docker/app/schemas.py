"""
Pydantic v2 Schemas for 3D Mech AR Chess.
Requests & Responses for Player Profile, Stats, Match Records, and Leaderboards.
"""

from typing import Optional, List, Dict, Any
from pydantic import BaseModel, Field

class LoginRequest(BaseModel):
    account: Optional[str] = Field(None, description="Username or Email for login")
    password: Optional[str] = Field(None, description="Password for account authentication")
    gmail: Optional[str] = Field(None, description="Gmail address of player (e.g. player@gmail.com)")
    username: Optional[str] = Field(None, description="Optional custom alphanumeric username (3-20 chars)")
    is_register: Optional[bool] = Field(False, description="Whether to register a new account")

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
    pgn: Optional[str] = None
    moves: Optional[List[str]] = None
    san_moves: Optional[List[str]] = None
    analysis: Optional[Dict[str, Any]] = None

class MatchHistoryResponse(BaseModel):
    total: int
    matches: List[MatchHistoryItemResponse]

class MatchRecordRequest(BaseModel):
    username: str
    opponent_name: Optional[str] = "Cyber AI"
    opponent_elo: Optional[int] = None
    is_ai: Optional[bool] = False
    player_role: Optional[str] = "white"
    result: str = Field(..., description="Result: 'win', 'loss', 'draw', or 'aborted'")
    moves_count: Optional[int] = 0
    duration_sec: Optional[float] = 0.0
    match_ticket: Optional[str] = Field(None, description="Cryptographic ticket issued by /api/match/start")
    match_id: Optional[str] = Field(None, description="Match ID tied to the ticket")
    moves: Optional[List[str]] = Field(None, description="List of UCI moves e.g. ['e2e4', 'e7e5']")
    san_moves: Optional[List[str]] = Field(None, description="List of SAN moves e.g. ['e4', 'e5']")
    pgn: Optional[str] = Field(None, description="Complete PGN notation of the match")

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

class GoogleAuthRequest(BaseModel):
    id_token: Optional[str] = None
    code: Optional[str] = None
    redirect_uri: Optional[str] = None
    access_token: Optional[str] = None

class GooglePollResponse(BaseModel):
    status: str  # "pending", "completed", "error"
    message: Optional[str] = None
    data: Optional[LoginResponse] = None

class AnalyzeMatchRequest(BaseModel):
    moves: Optional[List[str]] = Field(None, description="List of UCI moves")
    san_moves: Optional[List[str]] = Field(None, description="List of SAN moves")
    pgn: Optional[str] = Field(None, description="PGN string")
    depth: Optional[int] = Field(2, ge=1, le=4, description="Analysis search depth")

class MoveAnalysisItem(BaseModel):
    ply: int
    move_number: int
    color: str
    uci: str
    san: str
    from_square: str
    to_square: str
    fen_before: str
    fen_after: str
    score: float
    best_move_uci: str
    best_move_san: str
    judgment: str
    judgment_vi: str
    judgment_en: str
    comment_vi: str
    comment_en: str
    accuracy: float

class GameAnalysisResponse(BaseModel):
    success: bool
    total_moves: int
    white_accuracy: float
    black_accuracy: float
    white_stats: Dict[str, int]
    black_stats: Dict[str, int]
    final_fen: str
    moves: List[MoveAnalysisItem]
