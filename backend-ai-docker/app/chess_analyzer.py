"""
Chess Game Analyzer & Move Review Engine for AegisMech AR / Playchess.
Evaluates games move-by-move, computing:
- Centipawn evaluation before & after each move
- Best engine move suggestion in every position
- Move classification: Brilliant, Great, Best, Good, Book, Inaccuracy, Mistake, Blunder, Missed Win
- Accuracy percentage (CAPS score) for White and Black
- Bilingual commentary and tactical highlights
"""

import math
from typing import List, Dict, Any, Optional, Tuple
import chess
import chess.pgn
import io

PIECE_VALUES = {
    chess.PAWN: 100,
    chess.KNIGHT: 320,
    chess.BISHOP: 330,
    chess.ROOK: 500,
    chess.QUEEN: 900,
    chess.KING: 20000,
}

PAWN_TABLE = [
    0,  0,  0,  0,  0,  0,  0,  0,
    50, 50, 50, 50, 50, 50, 50, 50,
    10, 10, 20, 30, 30, 20, 10, 10,
     5,  5, 10, 25, 25, 10,  5,  5,
     0,  0,  0, 20, 20,  0,  0,  0,
     5, -5,-10,  0,  0,-10, -5,  5,
     5, 10, 10,-20,-20, 10, 10,  5,
     0,  0,  0,  0,  0,  0,  0,  0
]

KNIGHT_TABLE = [
    -50,-40,-30,-30,-30,-30,-40,-50,
    -40,-20,  0,  0,  0,  0,-20,-40,
    -30,  0, 10, 15, 15, 10,  0,-30,
    -30,  5, 15, 20, 20, 15,  5,-30,
    -30,  0, 15, 20, 20, 15,  0,-30,
    -30,  5, 10, 15, 15, 10,  5,-30,
    -40,-20,  0,  5,  5,  0,-20,-40,
    -50,-40,-30,-30,-30,-30,-40,-50,
]

BISHOP_TABLE = [
    -20,-10,-10,-10,-10,-10,-10,-20,
    -10,  0,  0,  0,  0,  0,  0,-10,
    -10,  0,  5, 10, 10,  5,  0,-10,
    -10,  5,  5, 10, 10,  5,  5,-10,
    -10,  0, 10, 10, 10, 10,  0,-10,
    -10, 10, 10, 10, 10, 10, 10,-10,
    -10,  5,  0,  0,  0,  0,  5,-10,
    -20,-10,-10,-10,-10,-10,-10,-20,
]

ROOK_TABLE = [
      0,  0,  0,  0,  0,  0,  0,  0,
      5, 10, 10, 10, 10, 10, 10,  5,
     -5,  0,  0,  0,  0,  0,  0, -5,
     -5,  0,  0,  0,  0,  0,  0, -5,
     -5,  0,  0,  0,  0,  0,  0, -5,
     -5,  0,  0,  0,  0,  0,  0, -5,
     -5,  0,  0,  0,  0,  0,  0, -5,
      0,  0,  0,  5,  5,  0,  0,  0
]

QUEEN_TABLE = [
    -20,-10,-10, -5, -5,-10,-10,-20,
    -10,  0,  0,  0,  0,  0,  0,-10,
    -10,  0,  5,  5,  5,  5,  0,-10,
     -5,  0,  5,  5,  5,  5,  0, -5,
      0,  0,  5,  5,  5,  5,  0, -5,
    -10,  5,  5,  5,  5,  5,  0,-10,
    -10,  0,  5,  0,  0,  0,  0,-10,
    -20,-10,-10, -5, -5,-10,-10,-20
]

# Common opening book sequences (first 6 plies)
OPENING_BOOK = {
    "e2e4": ("Khai cuộc Tốt Vua (King's Pawn Opening)", "King's Pawn Opening"),
    "d2d4": ("Khai cuộc Tốt Hậu (Queen's Pawn Opening)", "Queen's Pawn Opening"),
    "c2c4": ("Khai cuộc Anh (English Opening)", "English Opening"),
    "g1f3": ("Khai cuộc Réti (Réti Opening)", "Réti Opening"),
    "e2e4 e7e5": ("Mở cờ đôi Tốt Vua", "Open Game"),
    "e2e4 c7c5": ("Phòng thủ Sicilian (Sicilian Defense)", "Sicilian Defense"),
    "e2e4 e7e6": ("Phòng thủ Pháp (French Defense)", "French Defense"),
    "e2e4 c7c6": ("Phòng thủ Caro-Kann", "Caro-Kann Defense"),
    "d2d4 d7d5": ("Đóng cờ đôi Tốt Hậu", "Closed Game"),
    "d2d4 g8f6": ("Phòng thủ Ấn Độ", "Indian Defense"),
    "e2e4 e7e5 g1f3 b8c6 f1c4": ("Ván cờ Ý (Italian Game)", "Italian Game"),
    "e2e4 e7e5 g1f3 b8c6 f1b5": ("Tây Ban Nha Ruy Lopez", "Ruy Lopez"),
    "e2e4 c7c5 g1f3 d7d6 d2d4": ("Sicilian Mở", "Open Sicilian"),
}

def evaluate_board(board: chess.Board) -> float:
    """
    Evaluates position in centipawns from White's perspective (+ for White advantage, - for Black).
    """
    if board.is_checkmate():
        return -25000.0 if board.turn == chess.WHITE else 25000.0
    if board.is_stalemate() or board.is_insufficient_material() or board.can_claim_fifty_moves():
        return 0.0

    score = 0.0
    for sq, piece in board.piece_map().items():
        val = PIECE_VALUES.get(piece.piece_type, 0)
        pos = 0
        sq_idx = sq if piece.color == chess.WHITE else chess.square_mirror(sq)
        if piece.piece_type == chess.PAWN:
            pos = PAWN_TABLE[63 - sq_idx]
        elif piece.piece_type == chess.KNIGHT:
            pos = KNIGHT_TABLE[63 - sq_idx]
        elif piece.piece_type == chess.BISHOP:
            pos = BISHOP_TABLE[63 - sq_idx]
        elif piece.piece_type == chess.ROOK:
            pos = ROOK_TABLE[63 - sq_idx]
        elif piece.piece_type == chess.QUEEN:
            pos = QUEEN_TABLE[63 - sq_idx]

        total = val + pos
        if piece.color == chess.WHITE:
            score += total
        else:
            score -= total

    return score

def alpha_beta_minimax(board: chess.Board, depth: int, alpha: float, beta: float, is_white: bool) -> Tuple[float, Optional[chess.Move]]:
    """
    Fast alpha-beta search to evaluate and find the best move in a position.
    """
    if depth == 0 or board.is_game_over():
        return evaluate_board(board), None

    legal_moves = list(board.legal_moves)
    if not legal_moves:
        return evaluate_board(board), None

    # Move ordering: prioritize captures and promotions
    def move_priority(m: chess.Move) -> int:
        score = 0
        if board.is_capture(m):
            victim = board.piece_at(m.to_square)
            aggressor = board.piece_at(m.from_square)
            v_val = PIECE_VALUES.get(victim.piece_type, 0) if victim else 0
            a_val = PIECE_VALUES.get(aggressor.piece_type, 0) if aggressor else 0
            score += 1000 + v_val - (a_val // 10)
        if m.promotion:
            score += 800
        return score

    legal_moves.sort(key=move_priority, reverse=True)
    best_move = legal_moves[0]

    if is_white:
        max_eval = -float('inf')
        for move in legal_moves:
            board.push(move)
            eval_score, _ = alpha_beta_minimax(board, depth - 1, alpha, beta, False)
            board.pop()
            if eval_score > max_eval:
                max_eval = eval_score
                best_move = move
            alpha = max(alpha, eval_score)
            if beta <= alpha:
                break
        return max_eval, best_move
    else:
        min_eval = float('inf')
        for move in legal_moves:
            board.push(move)
            eval_score, _ = alpha_beta_minimax(board, depth - 1, alpha, beta, True)
            board.pop()
            if eval_score < min_eval:
                min_eval = eval_score
                best_move = move
            beta = min(beta, eval_score)
            if beta <= alpha:
                break
        return min_eval, best_move

def classify_move(
    board_before: chess.Board,
    move: chess.Move,
    eval_before: float,
    eval_after: float,
    best_eval: float,
    best_move: Optional[chess.Move],
    ply_idx: int,
    cumulative_uci: str
) -> Tuple[str, str, str, str, str]:
    """
    Classifies a move and returns:
    (judgment_id, judgment_vi, judgment_en, comment_vi, comment_en)
    """
    is_white = (board_before.turn == chess.WHITE)
    
    # Calculate loss from the active player's perspective
    if is_white:
        player_eval_before = eval_before
        player_eval_after = eval_after
        player_best_eval = best_eval
    else:
        player_eval_before = -eval_before
        player_eval_after = -eval_after
        player_best_eval = -best_eval

    loss = max(0.0, player_best_eval - player_eval_after)
    is_same_as_best = (best_move is not None and move == best_move)

    # Check for Opening Book move
    if ply_idx < 6:
        for book_seq, (name_vi, name_en) in OPENING_BOOK.items():
            if cumulative_uci.strip().startswith(book_seq) or book_seq.startswith(cumulative_uci.strip()):
                return (
                    "book", "Khai cuộc 📖", "Book Move 📖",
                    f"Nước đi lý thuyết chuẩn thuộc {name_vi}.",
                    f"Standard theoretical book move in the {name_en}."
                )

    # Check for Brilliant Move (sacrifice that wins material or game)
    moving_piece = board_before.piece_at(move.from_square)
    if moving_piece and moving_piece.piece_type in (chess.BISHOP, chess.KNIGHT, chess.ROOK, chess.QUEEN):
        # Is the piece left on an attacked square or sacrificed?
        if loss <= 25.0 and player_eval_after >= 150.0:
            target_piece = board_before.piece_at(move.to_square)
            target_val = PIECE_VALUES.get(target_piece.piece_type, 0) if target_piece else 0
            moving_val = PIECE_VALUES.get(moving_piece.piece_type, 0)
            if target_val < moving_val - 100:
                # Potential tactical sacrifice!
                return (
                    "brilliant", "Xuất sắc ‼️", "Brilliant ‼️",
                    "Nước thí quân chiến thuật đầy sắc bén và đem lại ưu thế vượt trội!",
                    "A brilliant tactical sacrifice securing a decisive advantage!"
                )

    # Check for Missed Win
    if player_eval_before >= 400.0 and player_eval_after <= 150.0 and loss >= 250.0:
        return (
            "missed_win", "Bỏ lỡ thắng ❌", "Missed Win ❌",
            "Bỏ lỡ cơ hội giành chiến thắng quyết định!",
            "Missed a decisive winning opportunity!"
        )

    # Blunder (Đại sai lầm)
    if loss >= 300.0:
        return (
            "blunder", "Đại sai lầm ⁇", "Blunder ⁇",
            "Nước đi sai lầm nghiêm trọng làm mất quân hoặc đánh mất ưu thế.",
            "A critical blunder that costs material or hands over the game."
        )

    # Mistake (Sai lầm)
    if loss >= 140.0:
        return (
            "mistake", "Sai lầm ❓", "Mistake ❓",
            "Nước đi không tối ưu khiến đối thủ có cơ hội phản công.",
            "A clear mistake allowing the opponent counter-play."
        )

    # Inaccuracy (Chưa chuẩn xác)
    if loss >= 60.0:
        return (
            "inaccuracy", "Chưa chuẩn ⁉️", "Inaccuracy ⁉️",
            "Nước đi có thể tốt hơn để duy trì kiểm soát thế trận.",
            "An inaccuracy that gives away part of the initiative."
        )

    # Best Move (Tối ưu)
    if is_same_as_best or loss <= 15.0:
        return (
            "best", "Tối ưu ⭐", "Best Move ⭐",
            "Nước đi chính xác nhất theo đánh giá của động cơ AI.",
            "The top recommended move calculated by the engine."
        )

    # Good Move (Nước tốt)
    return (
        "good", "Nước tốt 👍", "Good Move 👍",
        "Nước đi an toàn và duy trì cấu trúc vững chắc.",
        "A solid, sound move maintaining a stable position."
    )

def analyze_chess_game(
    moves_list: Optional[List[str]] = None,
    pgn_string: Optional[str] = None,
    search_depth: int = 2
) -> Dict[str, Any]:
    """
    Parses a game and evaluates every move step-by-step.
    Accepts either a list of UCI moves (e.g. ['e2e4', 'e7e5']),
    a list of SAN moves (e.g. ['e4', 'e5']), or a PGN string.
    """
    board = chess.Board()
    moves_to_play: List[chess.Move] = []

    if pgn_string and pgn_string.strip():
        try:
            game = chess.pgn.read_game(io.StringIO(pgn_string))
            if game:
                curr = game
                while curr.variations:
                    next_node = curr.variation(0)
                    moves_to_play.append(next_node.move)
                    curr = next_node
        except Exception:
            pass

    if not moves_to_play and moves_list:
        for m_str in moves_list:
            if not m_str:
                continue
            clean = m_str.strip().replace("x", "").replace("+", "").replace("#", "")
            # Try UCI first (4 or 5 chars e.g. e2e4, e7e8q)
            try:
                move = chess.Move.from_uci(clean.lower())
                if move in board.legal_moves:
                    moves_to_play.append(move)
                    board.push(move)
                    continue
            except Exception:
                pass

            # Try SAN
            try:
                move = board.parse_san(m_str.strip())
                moves_to_play.append(move)
                board.push(move)
            except Exception:
                pass

    # Reset board for evaluation
    board.reset()
    results = []
    white_accuracies = []
    black_accuracies = []

    white_counts = {"brilliant": 0, "great": 0, "best": 0, "good": 0, "book": 0, "inaccuracy": 0, "mistake": 0, "blunder": 0, "missed_win": 0}
    black_counts = {"brilliant": 0, "great": 0, "best": 0, "good": 0, "book": 0, "inaccuracy": 0, "mistake": 0, "blunder": 0, "missed_win": 0}

    cumulative_uci_list: List[str] = []

    for ply, move in enumerate(moves_to_play):
        is_white = (board.turn == chess.WHITE)
        move_number = (ply // 2) + 1
        fen_before = board.fen()
        san_move = board.san(move)
        uci_move = move.uci()
        cumulative_uci_list.append(uci_move)

        # 1. Best move & eval in position before
        best_eval, best_move = alpha_beta_minimax(board, depth=search_depth, alpha=-float('inf'), beta=float('inf'), is_white=is_white)
        eval_before = best_eval

        # 2. Make move
        board.push(move)
        fen_after = board.fen()
        
        # 3. Dynamic evaluation after move (from opponent's perspective, then inverted)
        if board.is_checkmate():
            eval_after = 25000.0 if is_white else -25000.0
        elif board.is_game_over():
            eval_after = 0.0
        else:
            eval_after, _ = alpha_beta_minimax(board, depth=max(1, search_depth - 1), alpha=-float('inf'), beta=float('inf'), is_white=(not is_white))

        # 4. Classify move
        board_before = chess.Board(fen_before)
        judgment_id, j_vi, j_en, comm_vi, comm_en = classify_move(
            board_before=board_before,
            move=move,
            eval_before=eval_before,
            eval_after=eval_after,
            best_eval=best_eval,
            best_move=best_move,
            ply_idx=ply,
            cumulative_uci=" ".join(cumulative_uci_list)
        )

        # Update stats
        if is_white:
            white_counts[judgment_id] = white_counts.get(judgment_id, 0) + 1
        else:
            black_counts[judgment_id] = black_counts.get(judgment_id, 0) + 1

        # Accuracy calculation (CAPS formula based on loss)
        player_eval_after = eval_after if is_white else -eval_after
        player_best_eval = best_eval if is_white else -best_eval
        loss = max(0.0, player_best_eval - player_eval_after)
        acc = round(max(0.0, min(100.0, 100.0 * math.exp(-0.004 * loss))), 1)

        if is_white:
            white_accuracies.append(acc)
        else:
            black_accuracies.append(acc)

        # Convert centipawns to pawns for friendly display e.g. +1.42
        score_pawns = round(eval_after / 100.0, 2)
        best_san = board_before.san(best_move) if best_move else ""

        results.append({
            "ply": ply,
            "move_number": move_number,
            "color": "white" if is_white else "black",
            "uci": uci_move,
            "san": san_move,
            "from_square": uci_move[:2].upper(),
            "to_square": uci_move[2:4].upper(),
            "fen_before": fen_before,
            "fen_after": fen_after,
            "score": score_pawns,
            "best_move_uci": best_move.uci() if best_move else "",
            "best_move_san": best_san,
            "judgment": judgment_id,
            "judgment_vi": j_vi,
            "judgment_en": j_en,
            "comment_vi": comm_vi,
            "comment_en": comm_en,
            "accuracy": acc
        })

    avg_white_acc = round(sum(white_accuracies) / max(1, len(white_accuracies)), 1)
    avg_black_acc = round(sum(black_accuracies) / max(1, len(black_accuracies)), 1)

    return {
        "success": True,
        "total_moves": len(results),
        "white_accuracy": avg_white_acc,
        "black_accuracy": avg_black_acc,
        "white_stats": white_counts,
        "black_stats": black_counts,
        "final_fen": board.fen(),
        "moves": results
    }
