using System;
using System.Collections.Generic;
using UnityEngine;

namespace MechChess.UI
{
    public enum TutorialTabType
    {
        Basics = 0,
        Rules = 1,
        Pieces = 2,
        AR = 3
    }

    [Serializable]
    public class TutorialPieceMarker
    {
        public int file; // 0..7 (A..H)
        public int rank; // 0..7 (1..8)
        public string symbol; // Unicode: ♔, ♕, ♖, ♗, ♘, ♙, etc.
        public bool isWhite;

        public TutorialPieceMarker() { }

        public TutorialPieceMarker(int file, int rank, string symbol, bool isWhite = true)
        {
            this.file = file;
            this.rank = rank;
            this.symbol = symbol;
            this.isWhite = isWhite;
        }
    }

    [Serializable]
    public class TutorialStep
    {
        public string number;
        public string titleVi;
        public string titleEn;
        public string descVi;
        public string descEn;

        public TutorialStep() { }

        public TutorialStep(string number, string titleVi, string titleEn, string descVi, string descEn)
        {
            this.number = number;
            this.titleVi = titleVi;
            this.titleEn = titleEn;
            this.descVi = descVi;
            this.descEn = descEn;
        }

        public string GetTitle() => AegisLocalization.T(titleVi, titleEn);
        public string GetDesc() => AegisLocalization.T(descVi, descEn);
    }

    [Serializable]
    public class TutorialPageData
    {
        public string pageId;
        public TutorialTabType tab;
        public int pageIndex;

        public string titleVi;
        public string titleEn;
        public string categoryBadgeVi;
        public string categoryBadgeEn;
        public string overviewVi;
        public string overviewEn;

        public List<TutorialStep> steps = new List<TutorialStep>();

        public string captionVi;
        public string captionEn;
        public string tacticalTipVi;
        public string tacticalTipEn;

        // Visual Diagram Data
        public bool hasSelectedSquare;
        public Vector2Int selectedSquare;
        public List<Vector2Int> moveSquares = new List<Vector2Int>();
        public List<Vector2Int> captureSquares = new List<Vector2Int>();
        public List<Vector2Int> specialSquares = new List<Vector2Int>();
        public List<TutorialPieceMarker> pieces = new List<TutorialPieceMarker>();

        // Specialized AR Diagram identifier (if tab == AR)
        public string arGraphicType = "";

        public string GetTitle() => AegisLocalization.T(titleVi, titleEn);
        public string GetBadge() => AegisLocalization.T(categoryBadgeVi, categoryBadgeEn);
        public string GetOverview() => AegisLocalization.T(overviewVi, overviewEn);
        public string GetCaption() => AegisLocalization.T(captionVi, captionEn);
        public string GetTip() => AegisLocalization.T(tacticalTipVi, tacticalTipEn);
    }

    /// <summary>
    /// Optional ScriptableObject container for Tutorial pages so designers can
    /// tweak or inspect content directly in Unity Inspector if desired.
    /// </summary>
    [CreateAssetMenu(fileName = "TutorialDataAsset", menuName = "AegisMech/Tutorial Data Asset")]
    public class TutorialDataAsset : ScriptableObject
    {
        public List<TutorialPageData> pages = new List<TutorialPageData>();
    }

    /// <summary>
    /// Centralized Repository providing all 20 verified Tutorial pages across the 4 tabs.
    /// Contains complete bilingual Vietnamese & English data, matching real gameplay code in
    /// ChessBoard3DManager and HybridARController.
    /// </summary>
    public static class TutorialRepository
    {
        private static readonly Dictionary<TutorialTabType, List<TutorialPageData>> _cachedPages = new Dictionary<TutorialTabType, List<TutorialPageData>>();
        private static bool _initialized = false;

        public static IReadOnlyList<TutorialPageData> GetPages(TutorialTabType tab)
        {
            EnsureInitialized();
            if (_cachedPages.TryGetValue(tab, out var list))
            {
                return list;
            }
            return Array.Empty<TutorialPageData>();
        }

        public static TutorialPageData GetPage(TutorialTabType tab, int index)
        {
            var pages = GetPages(tab);
            if (pages.Count == 0) return null;
            int clamped = Mathf.Clamp(index, 0, pages.Count - 1);
            return pages[clamped];
        }

        public static int GetPageCount(TutorialTabType tab)
        {
            return GetPages(tab).Count;
        }

        public static string GetTabTitle(TutorialTabType tab)
        {
            return tab switch
            {
                TutorialTabType.Basics => AegisLocalization.T("Cơ bản", "Basics"),
                TutorialTabType.Rules => AegisLocalization.T("Luật chơi", "Rules"),
                TutorialTabType.Pieces => AegisLocalization.T("Các quân cờ", "Pieces"),
                TutorialTabType.AR => "AR",
                _ => tab.ToString()
            };
        }

        private static void EnsureInitialized()
        {
            if (_initialized) return;
            _initialized = true;

            _cachedPages[TutorialTabType.Basics] = BuildBasicsPages();
            _cachedPages[TutorialTabType.Rules] = BuildRulesPages();
            _cachedPages[TutorialTabType.Pieces] = BuildPiecesPages();
            _cachedPages[TutorialTabType.AR] = BuildARPages();
        }

        // =========================================================================
        // TAB 1: CƠ BẢN (BASICS) - 5 PAGES
        // =========================================================================
        private static List<TutorialPageData> BuildBasicsPages()
        {
            var list = new List<TutorialPageData>();

            // Page 1: Match Objective
            list.Add(new TutorialPageData
            {
                pageId = "basics_objective",
                tab = TutorialTabType.Basics,
                pageIndex = 0,
                titleVi = "Mục tiêu Trận chiến",
                titleEn = "Match Objective",
                categoryBadgeVi = "[CƠ BẢN 1/5 - TỔNG QUAN]",
                categoryBadgeEn = "[BASICS 1/5 - OVERVIEW]",
                overviewVi = "Trong AegisMech-AR, bạn chỉ huy Quân Đoàn Hoàng Gia Cổ Thành. Mục tiêu tối thượng là bảo vệ Đức Vua Chỉ Huy (King) của phe mình và dồn Đức Vua của đối phương vào thế bị Chiếu hết (Checkmate).",
                overviewEn = "In AegisMech-AR, you command a legendary medieval chess legion. Your supreme objective is to protect your Royal King while maneuvering to put the enemy King into Checkmate.",
                steps = new List<TutorialStep>
                {
                    new TutorialStep("1", "Kiểm soát trung tâm", "Control the Center",
                        "Chiếm lĩnh và kiểm soát 4 ô trung tâm then chốt (d4, d5, e4, e5) để mở rộng tầm cơ động cho toàn quân.",
                        "Occupy and control key center squares (d4, d5, e4, e5) to grant your entire royal army maximum mobility."),
                    new TutorialStep("2", "Triển khai lực lượng", "Deploy Your Forces",
                        "Xuất kích các chiến binh cơ động như Kỵ Sĩ (Mã) và Pháp Sư (Tượng) sớm để hình thành thế gọng kìm kiểm soát thế trận.",
                        "Deploy mobile vanguard units such as Knights and Bishops early to establish lethal tactical crossfire."),
                    new TutorialStep("3", "Bảo vệ Đức Vua", "Fortify Your King",
                        "Luôn xây dựng hàng rào che chắn cho Vua bằng Tốt và thực hiện Nhập thành để đưa Vua vào vị trí kiên cố.",
                        "Shield your King behind reinforced Pawn barriers and execute Castling to anchor your monarch safely.")
                },
                captionVi = "Sơ đồ 4 ô trung tâm chiến lược (d4, d5, e4, e5)",
                captionEn = "Strategic Center Squares (d4, d5, e4, e5)",
                tacticalTipVi = "💡 MẸO TÁC CHIẾN: Phe Trắng (Hoàng Gia) luôn xuất kích trước. Nắm quyền chủ động phủ đầu là lợi thế cực lớn!",
                tacticalTipEn = "💡 TACTICAL TIP: White (Royal Legion) always moves first. Seizing tempo at opening dictates the board!",
                hasSelectedSquare = false,
                moveSquares = new List<Vector2Int> { new Vector2Int(3, 3), new Vector2Int(4, 3), new Vector2Int(3, 4), new Vector2Int(4, 4) },
                pieces = new List<TutorialPieceMarker>
                {
                    new TutorialPieceMarker(3, 3, "♙", true),
                    new TutorialPieceMarker(4, 3, "♙", true),
                    new TutorialPieceMarker(3, 4, "♟", false),
                    new TutorialPieceMarker(4, 4, "♟", false)
                }
            });

            // Page 2: Piece Selection & Deselection
            list.Add(new TutorialPageData
            {
                pageId = "basics_selection",
                tab = TutorialTabType.Basics,
                pageIndex = 1,
                titleVi = "Chọn & Hủy chọn Quân Cờ",
                titleEn = "Piece Selection & Deselect",
                categoryBadgeVi = "[CƠ BẢN 2/5 - THAO TÁC]",
                categoryBadgeEn = "[BASICS 2/5 - CONTROLS]",
                overviewVi = "Hệ thống điều khiển tương tác trực tiếp bằng chuột hoặc chạm cảm ứng. Bạn có thể chọn quân, quan sát tầm đánh và hủy chọn nhanh chóng bất kỳ lúc nào.",
                overviewEn = "Direct interaction via mouse click or touchscreen tap. Select units, preview movement pathways, and toggle deselect instantly at any moment.",
                steps = new List<TutorialStep>
                {
                    new TutorialStep("1", "Chạm để chọn quân", "Tap to Select Unit",
                        "Chạm vào quân cờ phe mình. Quân cờ sẽ nâng lên nhẹ và vầng hào quang Vàng Cổ Hoàng Gia bừng sáng dưới chân.",
                        "Tap an allied piece. The piece levitates upward and a radiant Antique Gold focus ring ignites beneath it."),
                    new TutorialStep("2", "Chạm lại để hủy chọn", "Tap Again to Deselect",
                        "Chạm lại chính quân cờ đang chọn hoặc chạm vào ô trống bên ngoài để hủy chọn và tắt highlight.",
                        "Tap the active piece again or tap any empty non-target tile to cleanly cancel selection and clear highlights."),
                    new TutorialStep("3", "Chuyển nhanh quân khác", "Switch Target Piece",
                        "Chạm trực tiếp vào một quân cờ đồng minh khác để chuyển quyền điều khiển mà không cần hủy chọn trước.",
                        "Tap directly on any other allied piece to instantaneously reassign focus without manual deselect.")
                },
                captionVi = "Mã c3 được chọn (Vàng Kim) & các ô nhảy hợp lệ (Lục Bảo)",
                captionEn = "Knight selected at c3 (Gold) & valid jump squares (Emerald Green)",
                tacticalTipVi = "💡 MẸO TÁC CHIẾN: Bạn chỉ có thể chọn các quân cờ thuộc phe của lượt đi hiện tại (hoặc Role phe mình khi đấu Online).",
                tacticalTipEn = "💡 TACTICAL TIP: You can only select units matching the active turn color (or your assigned online match role).",
                hasSelectedSquare = true,
                selectedSquare = new Vector2Int(2, 2), // c3
                moveSquares = new List<Vector2Int>
                {
                    new Vector2Int(0, 3), new Vector2Int(1, 4), new Vector2Int(3, 4), new Vector2Int(4, 3),
                    new Vector2Int(4, 1), new Vector2Int(3, 0), new Vector2Int(1, 0), new Vector2Int(0, 1)
                },
                pieces = new List<TutorialPieceMarker>
                {
                    new TutorialPieceMarker(2, 2, "♘", true)
                }
            });

            // Page 3: Valid Moves
            list.Add(new TutorialPageData
            {
                pageId = "basics_valid_moves",
                tab = TutorialTabType.Basics,
                pageIndex = 2,
                titleVi = "Xem Ô Di chuyển Hợp lệ",
                titleEn = "Tactical Move Highlights",
                categoryBadgeVi = "[CƠ BẢN 3/5 - NƯỚC ĐI]",
                categoryBadgeEn = "[BASICS 3/5 - MOVEMENT]",
                overviewVi = "Khi một quân cờ được chọn, pháp trận huyền bí sẽ tự động tính toán và chiếu sáng tất cả các tọa độ nước đi hợp lệ theo chuẩn FIDE.",
                overviewEn = "Upon selecting any piece, the arcane chess circle calculates all strictly legal FIDE coordinates and illuminates them in emerald green.",
                steps = new List<TutorialStep>
                {
                    new TutorialStep("1", "Ô xanh Lục Bảo (Move)", "Emerald Green Tiles",
                        "Các ô cờ trống hoàn toàn an toàn mà quân cờ của bạn có thể cơ động di chuyển đến trong lượt này.",
                        "Empty, unhindered destination squares where your piece can safely maneuver this turn."),
                    new TutorialStep("2", "Vật cản đường đi", "Path Obstruction",
                        "Trừ Kỵ Sĩ (Mã), không quân nào có thể đi xuyên qua quân đồng minh hay quân địch đang cản đường.",
                        "Except for the Royal Knight, no piece can jump through friendly or enemy units obstructing its path."),
                    new TutorialStep("3", "Lọc nước đi tự động", "Auto-Check Filter",
                        "Hệ thống tự động ngăn chặn mọi nước đi khiến Đức Vua rơi vào thế bị chiếu, đảm bảo tính hợp lệ tuyệt đối.",
                        "The chess engine strictly prevents any illegal move that would leave your King exposed to check.")
                },
                captionVi = "Tượng c4 (Vàng Kim) kiểm soát đường chéo (Xanh Lục Bảo)",
                captionEn = "Bishop at c4 (Gold) commanding diagonal pathways (Emerald Green)",
                tacticalTipVi = "💡 MẸO TÁC CHIẾN: Luôn quan sát kỹ các ô xanh để tìm ra góc tấn công bất ngờ mở đường cho các đợt công kích tiếp theo.",
                tacticalTipEn = "💡 TACTICAL TIP: Scan neon target tiles to uncover unexpected infiltration angles against the enemy vanguard.",
                hasSelectedSquare = true,
                selectedSquare = new Vector2Int(2, 3), // c4
                moveSquares = new List<Vector2Int>
                {
                    new Vector2Int(0, 1), new Vector2Int(1, 2), new Vector2Int(3, 4), new Vector2Int(4, 5), new Vector2Int(5, 6), new Vector2Int(6, 7),
                    new Vector2Int(1, 4), new Vector2Int(0, 5), new Vector2Int(3, 2), new Vector2Int(4, 1), new Vector2Int(5, 0)
                },
                pieces = new List<TutorialPieceMarker>
                {
                    new TutorialPieceMarker(2, 3, "♗", true)
                }
            });

            // Page 4: Piece Capture
            list.Add(new TutorialPageData
            {
                pageId = "basics_capturing",
                tab = TutorialTabType.Basics,
                pageIndex = 3,
                titleVi = "Tác chiến Bắt quân Địch",
                titleEn = "Piece Capture Maneuver",
                categoryBadgeVi = "[CƠ BẢN 4/5 - GIAO CHIẾN]",
                categoryBadgeEn = "[BASICS 4/5 - COMBAT]",
                overviewVi = "Khi quân cờ đối phương lọt vào tầm di chuyển hợp lệ của bạn, ô cờ của kẻ thù sẽ lập tức rực sáng sắc Huyết Tinh Ruby (Capture Highlight).",
                overviewEn = "When an enemy piece falls inside your legal attack range, its tile flashes in radiant Blood Crimson Red (Capture Target).",
                steps = new List<TutorialStep>
                {
                    new TutorialStep("1", "Nhận diện ô Huyết Tinh", "Identify Crimson Tiles",
                        "Ô cờ chứa quân địch trong tầm di chuyển hợp lệ sẽ được đánh dấu màu Đỏ Huyết Tinh nổi bật.",
                        "Squares containing capturable hostile pieces illuminate with intense Blood Crimson warning highlights."),
                    new TutorialStep("2", "Khai chiến tiêu diệt", "Execute Strike Order",
                        "Chạm trực tiếp vào ô cờ hoặc quân địch đó. Quân cờ của bạn sẽ lao tới và trảm hạ đối thủ ngay trên bàn cờ!",
                        "Tap the target square or the enemy piece directly. Your unit executes a rapid combat strike to eliminate it!"),
                    new TutorialStep("3", "Bỏ qua hoạt ảnh nhanh", "Instant Animation Skip",
                        "Nếu muốn nhịp chơi thần tốc, bạn có thể nhấp chuột hoặc chạm nhẹ vào màn hình để bỏ qua hoạt ảnh ngay.",
                        "Tap anywhere on the screen during the battle sequence to immediately skip animation and advance.")
                },
                captionVi = "Xe d4 (Vàng Kim) có thể ăn Mã d7 và Tốt g4 (Đỏ Huyết Tinh)",
                captionEn = "Rook at d4 (Gold) targeting enemy Knight d7 & Pawn g4 (Crimson)",
                tacticalTipVi = "💡 MẸO TÁC CHIẾN: Hoạt ảnh chiến đấu diễn ra ngắn gọn, dứt khoát trực tiếp trên bàn cờ 3D mà không làm gián đoạn ván cờ.",
                tacticalTipEn = "💡 TACTICAL TIP: Combat skirmishes trigger crisp inline strikes directly on the 3D board without pausing the game.",
                hasSelectedSquare = true,
                selectedSquare = new Vector2Int(3, 3), // d4
                moveSquares = new List<Vector2Int>
                {
                    new Vector2Int(3, 1), new Vector2Int(3, 2), new Vector2Int(3, 4), new Vector2Int(3, 5),
                    new Vector2Int(0, 3), new Vector2Int(1, 3), new Vector2Int(2, 3), new Vector2Int(4, 3), new Vector2Int(5, 3)
                },
                captureSquares = new List<Vector2Int>
                {
                    new Vector2Int(3, 6), // d7
                    new Vector2Int(6, 3)  // g4
                },
                pieces = new List<TutorialPieceMarker>
                {
                    new TutorialPieceMarker(3, 3, "♖", true),
                    new TutorialPieceMarker(3, 6, "♞", false),
                    new TutorialPieceMarker(6, 3, "♟", false)
                }
            });

            // Page 5: Turn & Status Tracking
            list.Add(new TutorialPageData
            {
                pageId = "basics_turns_hud",
                tab = TutorialTabType.Basics,
                pageIndex = 4,
                titleVi = "Lượt chơi & Trạng thái Bàn cờ",
                titleEn = "Turn & Battle Status Monitor",
                categoryBadgeVi = "[CƠ BẢN 5/5 - GIAO DIỆN]",
                categoryBadgeEn = "[BASICS 5/5 - BATTLE INTERFACE]",
                overviewVi = "Bảng thông tin chiến trận cung cấp đầy đủ tình hình bàn cờ: Lượt đi, Đồng hồ đếm ngược, Cảnh báo Chiếu tướng và Biên niên sử nước đi.",
                overviewEn = "The battlefield interface keeps you fully informed with Active Turn indicators, Turn Timers, Danger Check Alerts, and Move History Chronicles.",
                steps = new List<TutorialStep>
                {
                    new TutorialStep("1", "Chỉ báo Lượt trên đỉnh", "Active Turn Header",
                        "Thanh trạng thái trên cùng hiển thị rõ ràng phe HOÀNG GIA (Trắng) hoặc ĐẾ CHẾ (Đen) đang tới lượt điều khiển.",
                        "Top header banner clearly displays whether ROYAL ALLIANCE (White) or DARK EMPIRE (Black) currently commands the turn."),
                    new TutorialStep("2", "Cảnh báo Chiếu tướng", "Tactical Check Warning",
                        "Khi Đức Vua bị uy hiếp, phù hiệu chiến trận phát tín hiệu Huyết Tinh chớp sáng 'CHECK!' để cảnh báo người chơi.",
                        "When your King is under direct threat, the battle banner flashes a high-priority red alert: 'CHECK!'."),
                    new TutorialStep("3", "Nhật ký nước đi chuẩn", "Official Move Chronicles",
                        "Khung bên phải ghi nhận từng nước đi theo ký hiệu cờ vua quốc tế (e.g. e4, Nf3, O-O, exd5).",
                        "Right sidebar tracks every tactical maneuver using standardized algebraic chess notation.")
                },
                captionVi = "Hậu e8 đang Chiếu Vua e1 (Tia đỏ đe dọa dọc cột e)",
                captionEn = "Enemy Queen e8 threatening King e1 (Red threat ray along e-file)",
                tacticalTipVi = "💡 MẸO TÁC CHIẾN: Khi đấu trực tuyến, hãy luôn để mắt tới thanh đồng hồ đếm ngược để không bị xử thua do hết giờ!",
                tacticalTipEn = "💡 TACTICAL TIP: In multiplayer matches, always watch the turn timer countdown to avoid forfeit on time!",
                hasSelectedSquare = true,
                selectedSquare = new Vector2Int(4, 0), // e1 White King
                captureSquares = new List<Vector2Int>
                {
                    new Vector2Int(4, 7) // e8 Black Queen
                },
                specialSquares = new List<Vector2Int>
                {
                    new Vector2Int(4, 1), new Vector2Int(4, 2), new Vector2Int(4, 3), new Vector2Int(4, 4), new Vector2Int(4, 5), new Vector2Int(4, 6)
                },
                pieces = new List<TutorialPieceMarker>
                {
                    new TutorialPieceMarker(4, 0, "♔", true),
                    new TutorialPieceMarker(4, 7, "♛", false)
                }
            });

            return list;
        }

        // =========================================================================
        // TAB 2: LUẬT CHƠI (RULES) - 5 PAGES
        // =========================================================================
        private static List<TutorialPageData> BuildRulesPages()
        {
            var list = new List<TutorialPageData>();

            // Page 1: Turn Alternation & Legal Move Rules
            list.Add(new TutorialPageData
            {
                pageId = "rules_alternation",
                tab = TutorialTabType.Rules,
                pageIndex = 0,
                titleVi = "Quy tắc Luân phiên & Bắt buộc đi",
                titleEn = "Alternation & Mandatory Move",
                categoryBadgeVi = "[LUẬT CHƠI 1/5 - FIDE]",
                categoryBadgeEn = "[RULES 1/5 - FIDE]",
                overviewVi = "AegisMech-AR tích hợp chuẩn xác luật Cờ vua Quốc tế (FIDE). Trận đấu bắt đầu bằng lượt của phe Trắng, hai bên luân phiên nhau và tuyệt đối không được bỏ qua lượt.",
                overviewEn = "AegisMech-AR is built upon authentic FIDE International Chess rules. White moves first, turns strictly alternate, and passing a turn is strictly forbidden.",
                steps = new List<TutorialStep>
                {
                    new TutorialStep("1", "Không được bỏ lượt (No Pass)", "No Passing Allowed",
                        "Đến lượt của mình, bạn BẮT BUỘC phải thực hiện một nước đi hợp lệ. Không có khái niệm bỏ lượt trong cờ vua.",
                        "On your turn, you MUST execute a legal maneuver. Players can never skip or pass a turn in chess."),
                    new TutorialStep("2", "Quyền ưu tiên Trắng", "White Initiative",
                        "Phe Trắng luôn là bên khai cuộc đầu tiên, sở hữu quyền chủ động kiểm soát nhịp độ mở màn trận đấu.",
                        "White always launches the opening move, commanding initial initiative to dictate opening tempo."),
                    new TutorialStep("3", "Bảo vệ Vua là tối thượng", "Supreme King Defense",
                        "Mọi nước đi đẩy Vua của mình vào thế bị đối phương ăn đều bị coi là phạm luật và bị hệ thống khóa lại.",
                        "Any move that puts or leaves your own King in line of enemy fire is illegal and rejected by the engine.")
                },
                captionVi = "Nước mở màn chuẩn: Tốt e2 tiến 2 ô lên e4 (Xanh Neon)",
                captionEn = "Classic Opening: Pawn at e2 advancing 2 ranks to e4 (Neon Green)",
                tacticalTipVi = "💡 MẸO TÁC CHIẾN: Bộ quy tắc trong game đã được kiểm thử và xác minh 100% bằng bộ 15 Audit Tests chuyên sâu.",
                tacticalTipEn = "💡 TACTICAL TIP: All rules are verified and validated against 15 automated Unity audit test suites.",
                hasSelectedSquare = true,
                selectedSquare = new Vector2Int(4, 1), // e2
                moveSquares = new List<Vector2Int>
                {
                    new Vector2Int(4, 2), // e3
                    new Vector2Int(4, 3)  // e4
                },
                pieces = new List<TutorialPieceMarker>
                {
                    new TutorialPieceMarker(4, 1, "♙", true)
                }
            });

            // Page 2: Check & Evading Check
            list.Add(new TutorialPageData
            {
                pageId = "rules_check",
                tab = TutorialTabType.Rules,
                pageIndex = 1,
                titleVi = "Chiếu & Biện pháp Thoát chiếu",
                titleEn = "Check & Evading Threats",
                categoryBadgeVi = "[LUẬT CHƠI 2/5 - CHIẾU TƯỚNG]",
                categoryBadgeEn = "[RULES 2/5 - CHECK]",
                overviewVi = "Khi Đức Vua (King) nằm trong tầm tấn công trực diện của bất kỳ quân địch nào, bạn rơi vào trạng thái bị 'CHIẾU' (Check) và buộc phải giải cứu Vua ngay lập tức.",
                overviewEn = "When your King is directly threatened by an enemy piece, you enter 'CHECK' status and must immediately resolve the threat on that turn.",
                steps = new List<TutorialStep>
                {
                    new TutorialStep("1", "Chạy Vua (Evade)", "Move King Away",
                        "Di chuyển Đức Vua sang một ô kế bên an toàn không bị quân địch nào kiểm soát.",
                        "Step your King onto an adjacent safe square that is free from enemy crossfire."),
                    new TutorialStep("2", "Chặn đường chiếu (Block)", "Interpose / Block",
                        "Điều một quân cờ đồng minh chắn ngang giữa đường tấn công của quân địch (không áp dụng với Mã).",
                        "Deploy an allied piece into the trajectory between attacker and King (cannot block jumping Knights)."),
                    new TutorialStep("3", "Bắt quân chiếu (Capture)", "Eliminate Attacker",
                        "Dùng bất kỳ quân cờ hợp lệ nào để tiêu diệt ngay lập tức quân địch đang đe dọa Đức Vua.",
                        "Use any available legal unit to capture and eliminate the hostile piece delivering check.")
                },
                captionVi = "Tượng c5 chiếu Vua f2. Vua có thể chạy g1 hoặc d4 chặn",
                captionEn = "Bishop c5 checks King f2. King can evade to g1 or d4 can block",
                tacticalTipVi = "💡 MẸO TÁC CHIẾN: Nếu bạn bị chiếu và không còn bất kỳ cách nào trong 3 cách trên để giải chiếu, ván cờ kết thúc bằng Chiếu hết!",
                tacticalTipEn = "💡 TACTICAL TIP: If in check and none of the 3 evasion methods can be played, you are in Checkmate!",
                hasSelectedSquare = true,
                selectedSquare = new Vector2Int(5, 1), // f2 White King
                moveSquares = new List<Vector2Int>
                {
                    new Vector2Int(6, 0), // g1
                    new Vector2Int(4, 0)  // e1
                },
                specialSquares = new List<Vector2Int>
                {
                    new Vector2Int(3, 3), // d4 block
                    new Vector2Int(4, 2)  // e3 block
                },
                captureSquares = new List<Vector2Int>
                {
                    new Vector2Int(2, 4)  // c5 Black Bishop
                },
                pieces = new List<TutorialPieceMarker>
                {
                    new TutorialPieceMarker(5, 1, "♔", true),
                    new TutorialPieceMarker(2, 4, "♝", false),
                    new TutorialPieceMarker(3, 3, "♘", true)
                }
            });

            // Page 3: Checkmate & Stalemate
            list.Add(new TutorialPageData
            {
                pageId = "rules_checkmate",
                tab = TutorialTabType.Rules,
                pageIndex = 2,
                titleVi = "Chiếu hết & Hòa cờ (Checkmate)",
                titleEn = "Checkmate & Stalemate",
                categoryBadgeVi = "[LUẬT CHƠI 3/5 - KẾT THÚC]",
                categoryBadgeEn = "[RULES 3/5 - RESOLUTION]",
                overviewVi = "Ván cờ kết thúc khi xảy ra Chiếu hết (Checkmate), Hòa cờ Pat (Stalemate), Đầu hàng hoặc Hết thời gian thi đấu.",
                overviewEn = "The game concludes when reaching Checkmate, Stalemate, Resignation, or Turn Timer expiration.",
                steps = new List<TutorialStep>
                {
                    new TutorialStep("1", "Chiếu hết (Checkmate)", "Checkmate Victory",
                        "Vua bị chiếu và phe bị chiếu KHÔNG CÒN BẤT KỲ NƯỚC ĐI HỢP LỆ NÀO để thoát. Bên tấn công giành chiến thắng!",
                        "King is attacked and the defender has ZERO legal moves to escape. Attacking player wins victory!"),
                    new TutorialStep("2", "Hòa cờ Pat (Stalemate)", "Stalemate Draw",
                        "Bên đến lượt KHÔNG BỊ CHIẾU nhưng cũng KHÔNG CÒN BẤT KỲ NƯỚC ĐI HỢP LỆ NÀO. Trận đấu được xử hòa ngay.",
                        "Player to move is NOT in check, but has NO legal moves remaining anywhere on board. Match ends in a Draw."),
                    new TutorialStep("3", "Màn hình Kết thúc trận", "Victory / Defeat Screen",
                        "Màn hình vinh quang hiển thị kết quả, cộng điểm ELO (nếu đấu Online) và nút Tái chiến ngay ván mới.",
                        "Grand victory modal displays stats, calculates ELO rating gains, and provides instant Rematch buttons.")
                },
                captionVi = "Thế cờ Fool's Mate: Hậu h4 chiếu hết Vua e1 (Chiếu hết!)",
                captionEn = "Fool's Mate: Queen at h4 delivers unavoidable mate to King e1",
                tacticalTipVi = "💡 MẸO TÁC CHIẾN: Hãy cẩn trọng khi dồn ép đối thủ ở tàn cuộc. Đừng để hòa cờ Pat đáng tiếc khi đang áp đảo quân!",
                tacticalTipEn = "💡 TACTICAL TIP: Be cautious when cornering enemy King in endgame. Avoid accidental stalemate draws!",
                hasSelectedSquare = true,
                selectedSquare = new Vector2Int(4, 0), // e1 White King
                captureSquares = new List<Vector2Int>
                {
                    new Vector2Int(7, 3) // h4 Black Queen
                },
                specialSquares = new List<Vector2Int>
                {
                    new Vector2Int(5, 0), new Vector2Int(5, 1), new Vector2Int(6, 1)
                },
                pieces = new List<TutorialPieceMarker>
                {
                    new TutorialPieceMarker(4, 0, "♔", true),
                    new TutorialPieceMarker(7, 3, "♛", false),
                    new TutorialPieceMarker(5, 0, "♗", true),
                    new TutorialPieceMarker(5, 1, "♙", true),
                    new TutorialPieceMarker(6, 2, "♙", true)
                }
            });

            // Page 4: Castling
            list.Add(new TutorialPageData
            {
                pageId = "rules_castling",
                tab = TutorialTabType.Rules,
                pageIndex = 3,
                titleVi = "Nhập thành Chiến thuật (Castling)",
                titleEn = "Tactical Castling Maneuver",
                categoryBadgeVi = "[LUẬT CHƠI 4/5 - NHẬP THÀNH]",
                categoryBadgeEn = "[RULES 4/5 - CASTLING]",
                overviewVi = "Nhập thành là nước đi đặc biệt duy nhất cho phép di chuyển cả Vua và Xe cùng một lúc, giúp đưa Vua vào góc boongke an toàn và giải phóng Xe tham chiến.",
                overviewEn = "Castling is the unique chess maneuver allowing both King and Rook to move in a single turn, tucking King into safety while unleashing the Rook.",
                steps = new List<TutorialStep>
                {
                    new TutorialStep("1", "Chưa từng di chuyển", "Neither Piece Has Moved",
                        "Cả Đức Vua (King) và Xe tham gia nhập thành đều CHƯA TỪNG DI CHUYỂN bất kỳ nước nào trước đó.",
                        "Both King and participating Rook must have remained untouched and never moved since game start."),
                    new TutorialStep("2", "Đường giữa trống trải", "Clear Path Between Units",
                        "Toàn bộ các ô nằm giữa Vua và Xe phải hoàn toàn trống, không có bất kỳ quân đồng minh hay quân địch nào.",
                        "All intervening squares between King and Rook must be completely vacant."),
                    new TutorialStep("3", "Vùng an toàn không bị chiếu", "Safe Passage Transit",
                        "Vua KHÔNG ĐANG BỊ CHIẾU, và các ô Vua lướt qua hoặc dừng chân KHÔNG BỊ QUÂN ĐỊCH TẤN CÔNG.",
                        "King cannot castle while in check, nor pass through or land on any square attacked by enemy units.")
                },
                captionVi = "Vua e1 nhập thành cánh Vua g1 (O-O) hoặc cánh Hậu c1 (O-O-O)",
                captionEn = "King e1 can castle Kingside to g1 (O-O) or Queenside to c1 (O-O-O)",
                tacticalTipVi = "💡 MẸO TÁC CHIẾN: Nhập thành cánh Vua (O-O) nhanh hơn 1 nước so với cánh Hậu (O-O-O) và cực kỳ thông dụng.",
                tacticalTipEn = "💡 TACTICAL TIP: Kingside castling (O-O) requires clearing fewer pieces and is often the fastest defense.",
                hasSelectedSquare = true,
                selectedSquare = new Vector2Int(4, 0), // e1 White King
                specialSquares = new List<Vector2Int>
                {
                    new Vector2Int(6, 0), // g1 (Kingside)
                    new Vector2Int(2, 0)  // c1 (Queenside)
                },
                pieces = new List<TutorialPieceMarker>
                {
                    new TutorialPieceMarker(4, 0, "♔", true),
                    new TutorialPieceMarker(0, 0, "♖", true),
                    new TutorialPieceMarker(7, 0, "♖", true)
                }
            });

            // Page 5: En Passant & Pawn Promotion
            list.Add(new TutorialPageData
            {
                pageId = "rules_enpassant_promotion",
                tab = TutorialTabType.Rules,
                pageIndex = 4,
                titleVi = "Bắt tốt qua đường & Phong cấp",
                titleEn = "En Passant & Pawn Promotion",
                categoryBadgeVi = "[LUẬT CHƠI 5/5 - ĐẶC BIỆT]",
                categoryBadgeEn = "[RULES 5/5 - SPECIAL]",
                overviewVi = "Hai cơ chế tác chiến đột phá của Chiến binh Tiền tuyến (Tốt): Bắt tốt qua đường (En Passant) và Phong cấp (Pawn Promotion) khi chạm phòng tuyến cuối.",
                overviewEn = "Two vanguard breakthroughs exclusive to Pawns: En Passant tactical interception, and Promotion upon reaching the enemy baseline.",
                steps = new List<TutorialStep>
                {
                    new TutorialStep("1", "Bắt tốt qua đường (En Passant)", "En Passant Interception",
                        "Khi Tốt đối phương tiến 2 ô vượt qua ô ngắm của Tốt bạn, bạn có thể ăn chéo vào ô nó vừa lướt qua.",
                        "When enemy pawn sprints 2 squares past your pawn's diagonal line of fire, you may capture it en passant."),
                    new TutorialStep("2", "Giới hạn ngay lượt kế tiếp", "Immediate Turn Only",
                        "Quyền Bắt tốt qua đường CHỈ CÓ HIỆU LỰC NGAY TẠI LƯỢT ĐI TIẾP THEO. Sau lượt đó, quyền này vĩnh viễn mất đi!",
                        "En Passant rights expire immediately if not executed on the exact turn following the enemy pawn push!"),
                    new TutorialStep("3", "Phong cấp Tốt (Promotion)", "Pawn Promotion Selection",
                        "Khi Tốt tiến tới hàng cuối cùng (hàng 8 với Trắng, hàng 1 với Đen), một cửa sổ hoàng gia sẽ mở ra để chọn Hậu, Xe, Tượng hoặc Mã!",
                        "When Pawn reaches the furthest rank, a royal arcane prompt opens to promote it into Queen, Rook, Bishop, or Knight!")
                },
                captionVi = "Tốt e5 ăn tốt qua đường d6 (Đỏ). Tốt e7 thăng cấp tại e8 (Vàng)",
                captionEn = "Pawn e5 captures en passant at d6 (Red). Pawn e7 promotes at e8 (Gold)",
                tacticalTipVi = "💡 MẸO TÁC CHIẾN: Phong cấp thành Hậu (Queen) là sự lựa chọn áp đảo tới 98% để kết liễu trận đấu nhanh nhất.",
                tacticalTipEn = "💡 TACTICAL TIP: Promoting to Queen is overwhelmingly the most lethal option to seal the game.",
                hasSelectedSquare = true,
                selectedSquare = new Vector2Int(4, 4), // e5 White Pawn
                captureSquares = new List<Vector2Int>
                {
                    new Vector2Int(3, 5) // d6 En Passant square
                },
                specialSquares = new List<Vector2Int>
                {
                    new Vector2Int(4, 7) // e8 Promotion square
                },
                pieces = new List<TutorialPieceMarker>
                {
                    new TutorialPieceMarker(4, 4, "♙", true),
                    new TutorialPieceMarker(3, 4, "♟", false),
                    new TutorialPieceMarker(4, 6, "♙", true)
                }
            });

            return list;
        }

        // =========================================================================
        // TAB 3: CÁC QUÂN CỜ (PIECES) - 6 PAGES
        // =========================================================================
        private static List<TutorialPageData> BuildPiecesPages()
        {
            var list = new List<TutorialPageData>();

            // Page 1: King
            list.Add(new TutorialPageData
            {
                pageId = "piece_king",
                tab = TutorialTabType.Pieces,
                pageIndex = 0,
                titleVi = "Vua — Minh Chủ Chỉ Huy (King)",
                titleEn = "King — Royal Sovereign",
                categoryBadgeVi = "[QUÂN CỜ 1/6 - VUA ♔]",
                categoryBadgeEn = "[PIECES 1/6 - KING ♔]",
                overviewVi = "Vua là trái tim của vương triều cờ vua. Toàn bộ sinh mệnh của ván cờ gắn liền với sự an nguy của Đức Vua.",
                overviewEn = "The King is the heart of the royal realm. The ultimate fate of the entire conflict hinges upon his survival.",
                steps = new List<TutorialStep>
                {
                    new TutorialStep("1", "Tầm di chuyển 1 ô", "1 Square Range",
                        "Di chuyển chính xác 1 ô theo mọi hướng (ngang, dọc, chéo) xung quanh vị trí đứng.",
                        "Moves exactly 1 square in any direction (horizontal, vertical, or diagonal)."),
                    new TutorialStep("2", "Vùng cấm tự sát", "No Suicide Rule",
                        "Không bao giờ được di chuyển vào ô đang bị bất kỳ quân địch nào kiểm soát (tự đưa mình vào thế bị chiếu).",
                        "May never step onto any square under enemy attack (cannot put oneself in check)."),
                    new TutorialStep("3", "Kỹ năng Nhập thành", "Castling Maneuver",
                        "Phối hợp cùng Xe để thực hiện nước đi Nhập thành đổi chỗ chiến thuật khi đủ điều kiện.",
                        "Partners with an unmoved Rook to execute strategic Castling when path is clear and safe.")
                },
                captionVi = "Đức Vua tại d4 kiểm soát 8 ô lân cận (Lục Bảo)",
                captionEn = "Royal King at d4 commanding 8 perimeter squares (Emerald Green)",
                tacticalTipVi = "💡 MẸO TÁC CHIẾN: Ở giai đoạn tàn cuộc khi ít quân, Đức Vua trở thành một đơn vị tác chiến cận chiến rất mạnh!",
                tacticalTipEn = "💡 TACTICAL TIP: In the endgame, the King transforms into an active, aggressive combatant unit!",
                hasSelectedSquare = true,
                selectedSquare = new Vector2Int(3, 3), // d4
                moveSquares = new List<Vector2Int>
                {
                    new Vector2Int(2, 4), new Vector2Int(3, 4), new Vector2Int(4, 4),
                    new Vector2Int(2, 3),                       new Vector2Int(4, 3),
                    new Vector2Int(2, 2), new Vector2Int(3, 2), new Vector2Int(4, 2)
                },
                pieces = new List<TutorialPieceMarker>
                {
                    new TutorialPieceMarker(3, 3, "♔", true)
                }
            });

            // Page 2: Queen
            list.Add(new TutorialPageData
            {
                pageId = "piece_queen",
                tab = TutorialTabType.Pieces,
                pageIndex = 1,
                titleVi = "Hậu — Nữ Hoàng Quyền Năng (Queen)",
                titleEn = "Queen — Arcane Empress",
                categoryBadgeVi = "[QUÂN CỜ 2/6 - HẬU ♕]",
                categoryBadgeEn = "[PIECES 2/6 - QUEEN ♕]",
                overviewVi = "Hậu là quân cờ quyền năng tối thượng sở hữu sức mạnh cơ động và khả năng công kích áp đảo nhất trên bàn cờ hoàng gia.",
                overviewEn = "The Queen is the supreme empress on the battlefield, combining unmatched mobility and lethal magical range.",
                steps = new List<TutorialStep>
                {
                    new TutorialStep("1", "Tầm phóng vô hạn 8 hướng", "Unbounded 8-Direction Rays",
                        "Di chuyển không giới hạn số ô theo cả hàng ngang, cột dọc và các đường chéo cho đến khi gặp vật cản.",
                        "Moves any distance horizontally, vertically, or diagonally across uninterrupted vectors."),
                    new TutorialStep("2", "Không thể nhảy vật cản", "Blocked by Obstacles",
                        "Hậu không thể nhảy qua quân cờ khác; đường di chuyển bị chặn lại bởi quân cờ đầu tiên gặp phải.",
                        "Cannot jump over other pieces; movement terminates at the first ally or enemy encountered."),
                    new TutorialStep("3", "Triệt hạ mục tiêu địch", "Target Strike",
                        "Bắt quân địch nằm tại ô đích đến trên đường di chuyển và chiếm giữ vị trí đó.",
                        "Captures hostile unit occupying target square along its vector and holds the ground.")
                },
                captionVi = "Hậu d4 (Vàng Kim) kiểm soát toàn diện 8 hướng (Lục Bảo)",
                captionEn = "Queen at d4 commanding 8 unobstructed directions (Emerald Green)",
                tacticalTipVi = "💡 MẸO TÁC CHIẾN: Đừng xuất Hậu quá sớm ở khai cuộc kẻo bị đối thủ dùng quân nhỏ truy đuổi làm mất nhịp!",
                tacticalTipEn = "💡 TACTICAL TIP: Avoid deploying Queen too early, as minor enemy pieces can harass it and gain tempo!",
                hasSelectedSquare = true,
                selectedSquare = new Vector2Int(3, 3), // d4
                moveSquares = new List<Vector2Int>
                {
                    // Diagonals
                    new Vector2Int(0, 0), new Vector2Int(1, 1), new Vector2Int(2, 2), new Vector2Int(4, 4), new Vector2Int(5, 5), new Vector2Int(6, 6), new Vector2Int(7, 7),
                    new Vector2Int(0, 6), new Vector2Int(1, 5), new Vector2Int(2, 4), new Vector2Int(4, 2), new Vector2Int(5, 1), new Vector2Int(6, 0),
                    // Orthogonal
                    new Vector2Int(3, 0), new Vector2Int(3, 1), new Vector2Int(3, 2), new Vector2Int(3, 4), new Vector2Int(3, 5), new Vector2Int(3, 6), new Vector2Int(3, 7),
                    new Vector2Int(0, 3), new Vector2Int(1, 3), new Vector2Int(2, 3), new Vector2Int(4, 3), new Vector2Int(5, 3), new Vector2Int(6, 3), new Vector2Int(7, 3)
                },
                pieces = new List<TutorialPieceMarker>
                {
                    new TutorialPieceMarker(3, 3, "♕", true)
                }
            });

            // Page 3: Rook
            list.Add(new TutorialPageData
            {
                pageId = "piece_rook",
                tab = TutorialTabType.Pieces,
                pageIndex = 2,
                titleVi = "Xe — Chiến Pháo Đài Cổ (Rook)",
                titleEn = "Rook — Citadel Bastion",
                categoryBadgeVi = "[QUÂN CỜ 3/6 - XE ♖]",
                categoryBadgeEn = "[PIECES 3/6 - ROOK ♖]",
                overviewVi = "Xe là pháo đài thành trì cổ kính, chuyên kiểm soát các cột mở thẳng tắp và phong tỏa các hàng ngang chiến lược.",
                overviewEn = "The Rook is a heavy stone fortress specializing in dominating open files and locking down critical ranks.",
                steps = new List<TutorialStep>
                {
                    new TutorialStep("1", "Di chuyển ngang và dọc", "Orthogonal Movement",
                        "Di chuyển không giới hạn số ô theo hàng ngang và cột dọc trên các ô thông thoáng.",
                        "Moves any distance horizontally and vertically along unblocked lines."),
                    new TutorialStep("2", "Kiểm soát cột mở", "Open File Domination",
                        "Phát huy tối đa uy lực khi được đặt trên các cột mở không có Tốt cản trở tầm ngắm.",
                        "Reaches peak tactical efficiency when deployed on open files free of pawns."),
                    new TutorialStep("3", "Hiệp đồng Nhập thành", "Castling Synergy",
                        "Phối hợp với Đức Vua để tạo thế phòng thủ nhập thành vững như tường đồng.",
                        "Coordinates with the Royal King to execute defensive castling setups.")
                },
                captionVi = "Xe d4 (Vàng Kim) kiểm soát toàn bộ cột d và hàng 4 (Lục Bảo)",
                captionEn = "Rook at d4 controlling entire d-file and 4th rank (Emerald Green)",
                tacticalTipVi = "💡 MẸO TÁC CHIẾN: Hai Xe chồng trên cùng một cột mở (Xe đôi) tạo nên sức công phá khủng khiếp!",
                tacticalTipEn = "💡 TACTICAL TIP: Doubling two Rooks along the same open file creates immense battering power!",
                hasSelectedSquare = true,
                selectedSquare = new Vector2Int(3, 3), // d4
                moveSquares = new List<Vector2Int>
                {
                    new Vector2Int(3, 0), new Vector2Int(3, 1), new Vector2Int(3, 2), new Vector2Int(3, 4), new Vector2Int(3, 5), new Vector2Int(3, 6), new Vector2Int(3, 7),
                    new Vector2Int(0, 3), new Vector2Int(1, 3), new Vector2Int(2, 3), new Vector2Int(4, 3), new Vector2Int(5, 3), new Vector2Int(6, 3), new Vector2Int(7, 3)
                },
                pieces = new List<TutorialPieceMarker>
                {
                    new TutorialPieceMarker(3, 3, "♖", true)
                }
            });

            // Page 4: Bishop
            list.Add(new TutorialPageData
            {
                pageId = "piece_bishop",
                tab = TutorialTabType.Pieces,
                pageIndex = 3,
                titleVi = "Tượng — Đại Pháp Sư Huyền Bí (Bishop)",
                titleEn = "Bishop — Arcane Archmage",
                categoryBadgeVi = "[QUÂN CỜ 4/6 - TƯỢNG ♗]",
                categoryBadgeEn = "[PIECES 4/6 - BISHOP ♗]",
                overviewVi = "Tượng là pháp sư huyền bí tầm xa, chuyên phóng luồng ma thuật qua các đường chéo rộng lớn của chiến trường.",
                overviewEn = "The Bishop acts as an arcane spellcaster slicing through the board's diagonal corridors with precision magic.",
                steps = new List<TutorialStep>
                {
                    new TutorialStep("1", "Tầm ngắm đường chéo", "Diagonal Vector Magic",
                        "Di chuyển không giới hạn số ô theo các đường chéo thông thoáng không bị vật cản.",
                        "Moves any number of squares along uninterrupted diagonal lines."),
                    new TutorialStep("2", "Định mệnh một màu ô", "Monochrome Destiny",
                        "Luôn luôn di chuyển trên duy nhất một màu ô (ô sáng hoặc ô tối) trong suốt toàn bộ ván đấu.",
                        "Strictly locked to a single square color (light or dark) for the entire duration of the match."),
                    new TutorialStep("3", "Cặp Tượng song sát", "Bishop Pair Synergy",
                        "Mỗi bên sở hữu 1 Tượng ô sáng và 1 Tượng ô tối bù trừ tầm kiểm soát cho nhau một cách hoàn hảo.",
                        "Retaining the Bishop pair yields lethal complementary board control across both colors.")
                },
                captionVi = "Tượng d4 (Vàng Kim) kiểm soát xuyên suốt 4 hướng chéo (Lục Bảo)",
                captionEn = "Bishop at d4 casting spells along all 4 diagonal lines (Emerald Green)",
                tacticalTipVi = "💡 MẸO TÁC CHIẾN: Hãy giải phóng các đường chéo bằng cách đẩy Tốt lên sớm để mở tầm bắn cho Tượng.",
                tacticalTipEn = "💡 TACTICAL TIP: Open diagonal corridors early by advancing center pawns to unleash Bishop spells.",
                hasSelectedSquare = true,
                selectedSquare = new Vector2Int(3, 3), // d4
                moveSquares = new List<Vector2Int>
                {
                    new Vector2Int(0, 0), new Vector2Int(1, 1), new Vector2Int(2, 2), new Vector2Int(4, 4), new Vector2Int(5, 5), new Vector2Int(6, 6), new Vector2Int(7, 7),
                    new Vector2Int(0, 6), new Vector2Int(1, 5), new Vector2Int(2, 4), new Vector2Int(4, 2), new Vector2Int(5, 1), new Vector2Int(6, 0)
                },
                pieces = new List<TutorialPieceMarker>
                {
                    new TutorialPieceMarker(3, 3, "♗", true)
                }
            });

            // Page 5: Knight
            list.Add(new TutorialPageData
            {
                pageId = "piece_knight",
                tab = TutorialTabType.Pieces,
                pageIndex = 4,
                titleVi = "Mã — Kỵ Sĩ Hoàng Thánh (Knight)",
                titleEn = "Knight — Arcane Cavalier",
                categoryBadgeVi = "[QUÂN CỜ 5/6 - MÃ ♘]",
                categoryBadgeEn = "[PIECES 5/6 - KNIGHT ♘]",
                overviewVi = "Mã là kỵ sĩ dũng mãnh sở hữu thần mã thiện chiến, có khả năng phi thân nhảy vượt qua mọi chướng ngại vật trên bàn cờ.",
                overviewEn = "The Knight is a valiant cavalier mounted on a legendary steed capable of leaping directly over intervening obstacles.",
                steps = new List<TutorialStep>
                {
                    new TutorialStep("1", "Quỹ đạo chữ L độc đáo", "Unique L-Shape Trajectory",
                        "Di chuyển theo hình chữ L: Tiến 2 ô và rẽ 1 ô (hoặc tiến 1 ô và rẽ 2 ô) sang hai bên.",
                        "Moves in an 'L' shape: 2 squares along one axis followed by 1 square orthogonally."),
                    new TutorialStep("2", "Nhảy xuyên qua vật cản", "Jump Over Obstacles",
                        "Quân cờ DUY NHẤT có thể nhảy xuyên qua các quân cờ khác trên đường di chuyển mà không bị cản.",
                        "The ONLY piece on board that can leap over other pieces standing between origin and destination."),
                    new TutorialStep("3", "Đòn chĩa ba hiểm hóc", "Lethal Multi-Fork Attacks",
                        "Khả năng tấn công đồng thời nhiều mục tiêu quan trọng cùng một lúc (Fork Vua và Hậu).",
                        "Excels at sudden multi-target forks, threatening enemy King and Queen simultaneously.")
                },
                captionVi = "Mã d4 (Vàng Kim) có thể nhảy tới 8 ô chữ L khác màu (Lục Bảo)",
                captionEn = "Knight d4 (Gold) leaping to 8 opposite-color L-targets (Emerald Green)",
                tacticalTipVi = "💡 MẸO TÁC CHIẾN: Đặt Mã ở các ô trung tâm (như d4, d5, e4, e5) để phát huy tối đa sức mạnh kiểm soát 8 ô!",
                tacticalTipEn = "💡 TACTICAL TIP: Outpost Knights in the center (d4, d5, e4, e5) to unleash full 8-square jump radius!",
                hasSelectedSquare = true,
                selectedSquare = new Vector2Int(3, 3), // d4
                moveSquares = new List<Vector2Int>
                {
                    new Vector2Int(1, 4), new Vector2Int(2, 5), new Vector2Int(4, 5), new Vector2Int(5, 4),
                    new Vector2Int(5, 2), new Vector2Int(4, 1), new Vector2Int(2, 1), new Vector2Int(1, 2)
                },
                pieces = new List<TutorialPieceMarker>
                {
                    new TutorialPieceMarker(3, 3, "♘", true)
                }
            });

            // Page 6: Pawn
            list.Add(new TutorialPageData
            {
                pageId = "piece_pawn",
                tab = TutorialTabType.Pieces,
                pageIndex = 5,
                titleVi = "Tốt — Chiến Binh Tiên Phong (Pawn)",
                titleEn = "Pawn — Royal Vanguard",
                categoryBadgeVi = "[QUÂN CỜ 6/6 - TỐT ♙]",
                categoryBadgeEn = "[PIECES 6/6 - PAWN ♙]",
                overviewVi = "Tốt là hàng rào phòng thủ kiên cố và linh hồn của thế trận cờ vua. Tuy nhỏ bé nhưng Tốt có tiềm năng phong cấp thành Nữ Hoàng tối thượng.",
                overviewEn = "Pawns are the soul of chess strategy and your frontline defensive shield, harboring the potential to promote into a supreme Queen.",
                steps = new List<TutorialStep>
                {
                    new TutorialStep("1", "Đi thẳng, ăn chéo", "Advance Straight, Strike Diagonally",
                        "Chỉ tiến thẳng về phía trước 1 ô. Khi khai cuộc nước đầu tiên có thể chọn tiến 1 hoặc 2 ô. Ăn quân theo đường chéo.",
                        "Marches straight ahead 1 tile (or optional 2 tiles on initial move). Captures hostile units 1 square diagonally."),
                    new TutorialStep("2", "Không bao giờ đi lùi", "Never Retreats",
                        "Tốt là quân cờ duy nhất KHÔNG THỂ ĐI LÙI. Mỗi bước tiến của Tốt là cam kết chiến thuật không thể đảo ngược.",
                        "Pawns are the only pieces that cannot retreat. Every pawn advance irreversibly alters the board structure."),
                    new TutorialStep("3", "Tiến hóa Phong cấp", "Heroic Promotion",
                        "Chạm tới hàng cờ cuối cùng của đối thủ, Tốt lập tức được tái sinh thành Hậu, Xe, Tượng hoặc Mã!",
                        "Reaching the opponent's back rank instantly triggers promotion into Queen, Rook, Bishop, or Knight!")
                },
                captionVi = "Tốt d2 tiến d3/d4 (Xanh Lục). Tốt d4 ăn chéo c5/e5 (Huyết Tinh Đỏ)",
                captionEn = "Pawn d2 advances d3/d4 (Emerald). Pawn d4 captures c5/e5 (Ruby Crimson)",
                tacticalTipVi = "💡 MẸO TÁC CHIẾN: Chuỗi Tốt liên kết bảo bọc lẫn nhau tạo thành lá chắn bất khả xâm phạm cho Đức Vua Chỉ Huy.",
                tacticalTipEn = "💡 TACTICAL TIP: Linked pawn chains guarding each other form an impenetrable barrier for your Royal King.",
                hasSelectedSquare = true,
                selectedSquare = new Vector2Int(3, 1), // d2
                moveSquares = new List<Vector2Int>
                {
                    new Vector2Int(3, 2), // d3
                    new Vector2Int(3, 3)  // d4
                },
                captureSquares = new List<Vector2Int>
                {
                    new Vector2Int(2, 2), // c3
                    new Vector2Int(4, 2)  // e3
                },
                pieces = new List<TutorialPieceMarker>
                {
                    new TutorialPieceMarker(3, 1, "♙", true),
                    new TutorialPieceMarker(2, 2, "♟", false),
                    new TutorialPieceMarker(4, 2, "♟", false)
                }
            });

            return list;
        }

        // =========================================================================
        // TAB 4: AR (THỰC TẾ ẢO TĂNG CƯỜNG) - 4 PAGES
        // =========================================================================
        private static List<TutorialPageData> BuildARPages()
        {
            var list = new List<TutorialPageData>();

            // Page 1: Permissions & Setup
            list.Add(new TutorialPageData
            {
                pageId = "ar_permissions",
                tab = TutorialTabType.AR,
                pageIndex = 0,
                titleVi = "Cấp quyền Camera & Khởi chạy AR",
                titleEn = "Camera Access & AR Launch",
                categoryBadgeVi = "[AR 1/4 - CẢM BIẾN]",
                categoryBadgeEn = "[AR 1/4 - SENSORS]",
                overviewVi = "Chế độ AR biến không gian phòng thực tế của bạn thành đấu trường cờ Hoàng Gia 3D tương tác. Hệ thống kết nối camera thiết bị và xử lý hình ảnh ma thuật trực tiếp.",
                overviewEn = "AR mode projects the 3D Fantasy Chess battlefield directly onto your real tabletop surface using real-time camera feed processing.",
                steps = new List<TutorialStep>
                {
                    new TutorialStep("1", "Cho phép quyền Camera", "Grant Camera Permission",
                        "Khi thiết bị yêu cầu quyền truy cập Camera, hãy chọn 'Cho phép' (Allow) để kích hoạt thị giác không gian ma thuật.",
                        "When prompted for camera authorization, select 'Allow' to activate optical spatial tracking."),
                    new TutorialStep("2", "Chọn môi trường sáng tốt", "Optimize Lighting Conditions",
                        "Đặt thiết bị trong phòng có ánh sáng đồng đều; tránh hướng thẳng vào nguồn đèn chói hoặc phòng quá tối.",
                        "Operate in a well-lit environment; avoid pointing directly into blinding glare or pitch-dark rooms."),
                    new TutorialStep("3", "Chọn bề mặt có vân rõ", "Textured Surface Selection",
                        "Mặt bàn có vân gỗ, khăn trải bàn hoặc họa tiết giúp camera tracking và giữ vị trí bàn cờ ổn định nhất.",
                        "Textured tabletops (wood grain, cloth pattern) provide superior feature points for rock-solid tracking.")
                },
                captionVi = "Cảm biến Camera sẵn sàng quét không gian thực",
                captionEn = "Optical Camera Sensor ready for spatial environmental scan",
                tacticalTipVi = "💡 MẸO TÁC CHIẾN: Nếu chơi trên PC không có webcam ngoài, hệ thống sẽ tự động bật phông nền Đại Điện Huyền Thuật (Arcane Realm Studio) tiện lợi.",
                tacticalTipEn = "💡 TACTICAL TIP: On desktop PCs without a webcam, the system seamlessly defaults to Arcane Realm Studio mode.",
                arGraphicType = "ar_permissions"
            });

            // Page 2: Plane Tracking & Placement
            list.Add(new TutorialPageData
            {
                pageId = "ar_plane_scan",
                tab = TutorialTabType.AR,
                pageIndex = 1,
                titleVi = "Quét Mặt phẳng & Đặt Bàn cờ",
                titleEn = "Surface Scan & Anchor Placement",
                categoryBadgeVi = "[AR 2/4 - ĐỊNH VỊ]",
                categoryBadgeEn = "[AR 2/4 - ANCHOR]",
                overviewVi = "AegisMech-AR ứng dụng công nghệ AR Foundation và Raycasting thông minh để phát hiện mặt bàn và ghim bàn cờ 3D trong không gian thực.",
                overviewEn = "AegisMech-AR leverages AR Foundation plane detection and raycasting to detect tabletops and anchor 3D chess pieces in reality.",
                steps = new List<TutorialStep>
                {
                    new TutorialStep("1", "Quét nhẹ nhàng theo số 8", "Figure-8 Surface Scan",
                        "Di chuyển camera nhẹ nhàng hình số 8 xung quanh bề mặt bàn để cảm biến nhận diện pháp trận mặt phẳng.",
                        "Gently move camera in a figure-eight sweep over your desk until arcane plane grid appears."),
                    new TutorialStep("2", "Chạm màn hình để đặt cờ", "Tap Screen to Anchor",
                        "Chạm vào điểm mong muốn trên lưới mặt bàn đã nhận diện để neo bàn cờ Hoàng Gia tại tọa độ đó.",
                        "Tap the detected surface plane on your touchscreen to anchor the 3D battlefield at that exact spot."),
                    new TutorialStep("3", "Tự do đi quanh quan sát", "Walk Around Freely",
                        "Bàn cờ được cố định trong không gian thực. Bạn có thể tự do cầm điện thoại đi xung quanh để ngắm binh đoàn cờ 360°!",
                        "The board is firmly anchored in physical space. Walk around your table to inspect units from any 360° angle!")
                },
                captionVi = "Lưới AR nhận diện mặt phẳng bàn & Neo vị trí trung tâm",
                captionEn = "Arcane AR plane mesh & central surface anchor point",
                tacticalTipVi = "💡 MẸO TÁC CHIẾN: Bạn có thể chạm lại vị trí khác trên bàn bất cứ lúc nào để dời bàn cờ đến góc quan sát đẹp nhất.",
                tacticalTipEn = "💡 TACTICAL TIP: Tap any new surface location at any time to instantly reposition your board.",
                arGraphicType = "ar_plane_scan"
            });

            // Page 3: Scale Controls & Pinch Gestures
            list.Add(new TutorialPageData
            {
                pageId = "ar_scale_controls",
                tab = TutorialTabType.AR,
                pageIndex = 2,
                titleVi = "Thu phóng Kích thước & Cử chỉ Pinch",
                titleEn = "Board Scaling & Pinch Controls",
                categoryBadgeVi = "[AR 3/4 - KÍCH THƯỚC]",
                categoryBadgeEn = "[AR 3/4 - SCALE]",
                overviewVi = "Tùy chỉnh kích thước bàn cờ linh hoạt từ 50% đến 200% để vừa khít với bàn ăn gia đình, bàn làm việc nhỏ hoặc sân đấu rộng lớn.",
                overviewEn = "Seamlessly scale the board from 50% to 200% to fit anywhere from cozy desks to expansive outdoor grounds.",
                steps = new List<TutorialStep>
                {
                    new TutorialStep("1", "Cử chỉ Pinch 2 ngón tay", "Two-Finger Pinch Gesture",
                        "Dùng 2 ngón tay chụm lại hoặc mở rộng trên màn hình để phóng to / thu nhỏ bàn cờ mượt mà theo ý muốn.",
                        "Pinch with two fingers on the touch screen to smoothly expand or shrink the chessboard in real-time."),
                    new TutorialStep("2", "Phím tắt kích thước nhanh", "Quick Preset Buttons",
                        "Thanh điều khiển AR trên đỉnh cung cấp 3 nút chuẩn: 80% (Gọn gàng), 100% (Chuẩn 0.5m), 130% (Hoành tráng).",
                        "Top AR guidance banner provides 3 instant presets: 80% (Compact), 100% (Standard 0.5m), 130% (Epic)."),
                    new TutorialStep("3", "Xoay Orbit Camera 360°", "360° Orbit Camera Drag",
                        "Giữ chuột phải hoặc vuốt 1 ngón tay trên màn hình để xoay và nâng góc nhìn bàn cờ tùy thích.",
                        "Drag with right mouse button or 1-finger swipe to freely orbit and tilt perspective around the board.")
                },
                captionVi = "Điều khiển kích cỡ bàn cờ (50% - 200%) & Cử chỉ 2 ngón tay",
                captionEn = "Board scale range (50% - 200%) & 2-finger pinch gesture HUD",
                tacticalTipVi = "💡 MẸO TÁC CHIẾN: Cỡ 100% tương ứng bàn cờ ngoài đời thực rộng 50cm x 50cm, tỷ lệ vàng cho bàn học và bàn làm việc.",
                tacticalTipEn = "💡 TACTICAL TIP: 100% scale corresponds precisely to a 50cm x 50cm real-world tournament board.",
                arGraphicType = "ar_scale_controls"
            });

            // Page 4: AI Card Scanner & Diagnostics
            list.Add(new TutorialPageData
            {
                pageId = "ar_ai_scanner",
                tab = TutorialTabType.AR,
                pageIndex = 3,
                titleVi = "Quét Thẻ AI & Khắc phục Tracking",
                titleEn = "AI Card Scanner & Diagnostics",
                categoryBadgeVi = "[AR 4/4 - TRÍ TUỆ NHÂN TẠO]",
                categoryBadgeEn = "[AR 4/4 - AI INFERENCE]",
                overviewVi = "Tính năng quét thẻ bài nước đi thông minh tích hợp mô hình Deep Learning PyTorch qua backend AI và hệ thống tự phục hồi tracking.",
                overviewEn = "AI Tactical Card Scanner powered by PyTorch Deep Learning backend, featuring automatic tracking recovery diagnostics.",
                steps = new List<TutorialStep>
                {
                    new TutorialStep("1", "Bật khung quét thẻ (PIP)", "Picture-in-Picture Scanner",
                        "Nhấp nút 'QUÉT THẺ' trên thanh HUD để mở khung ngắm camera góc trên bên phải màn hình.",
                        "Click 'SCAN CARD' on the HUD banner to open the top-right camera scanning viewport."),
                    new TutorialStep("2", "Nhận diện thẻ nước đi", "Real-Time AI Card Inference",
                        "Đưa thẻ bài vào khung ngắm; AI sẽ suy luận loại quân và tọa độ nước đi tự động với độ chính xác cao!",
                        "Hold your tactical card in view; our AI model detects piece type and move coordinates with high confidence!"),
                    new TutorialStep("3", "Chuyển chế độ tức thời", "Instant Mode Fallback",
                        "Nếu mất tracking hoặc camera bị che khuất, bấm nút AR để chuyển sang chế độ 'Đại Điện Huyền Thuật (Arcane Realm Studio)' ngay mà không gián đoạn ván cờ.",
                        "If tracking is lost, toggle AR mode to instantly drop back to Arcane Realm Studio mode without disrupting match play.")
                },
                captionVi = "Khung ngắm quét thẻ AI với độ tin cậy suy luận thời gian thực",
                captionEn = "AI Card Scanner viewport with real-time neural inference HUD",
                tacticalTipVi = "💡 MẸO TÁC CHIẾN: Bạn có thể luân chuyển 3 chế độ: Toàn màn hình AR (Mode 0), Khung quét PIP (Mode 1), và Đại Điện Huyền Thuật (Mode 2) bất cứ lúc nào.",
                tacticalTipEn = "💡 TACTICAL TIP: Seamlessly cycle through 3 display modes: Fullscreen AR, Corner PIP, or Arcane Realm Studio at any time.",
                arGraphicType = "ar_ai_scanner"
            });

            return list;
        }
    }
}
