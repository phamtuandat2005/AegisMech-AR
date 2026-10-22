using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace MechChess.UI
{
    /// <summary>
    /// Arcane UI Asset Loader & Procedural Graphic Generator:
    /// High-performance loader for all 15 Arcane 3D AR Chess screens,
    /// complete with fallback byte loader and antialiased sci-fi glassmorphism sprites.
    /// All procedural textures use 4x supersampling for crisp, clean edges at any resolution.
    /// </summary>
    public static class ArcaneAssetLoader
    {
        private static readonly Dictionary<string, Sprite> _spriteCache = new Dictionary<string, Sprite>();
        private static readonly Dictionary<string, Sprite> _proceduralCache = new Dictionary<string, Sprite>();

        // Scale factor for procedural sprites to ensure sharp rendering on high-DPI screens
        private const int PROC_SCALE = 4;

        /// <summary>
        /// Loads an Arcane UI sprite either via Unity Resources or via direct PNG byte fallback.
        /// </summary>
        public static Sprite LoadArcaneSprite(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return null;

            if (_spriteCache.TryGetValue(fileName, out Sprite cached) && cached != null)
                return cached;

            // 1. Try Resources.Load (without extension)
            string resName = fileName;
            if (resName.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                resName = resName.Substring(0, resName.Length - 4);

            Sprite resSprite = Resources.Load<Sprite>("Textures/ArcaneUI/" + resName);
            if (resSprite != null)
            {
                _spriteCache[fileName] = resSprite;
                return resSprite;
            }

            // 2. Direct File load from Assets/Resources/Textures/ArcaneUI/
            string fullPath = Path.Combine(Application.dataPath, "Resources", "Textures", "ArcaneUI", fileName);
            if (!fileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                fullPath += ".png";

            if (File.Exists(fullPath))
            {
                try
                {
                    byte[] bytes = File.ReadAllBytes(fullPath);
                    Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    if (tex.LoadImage(bytes))
                    {
                        tex.wrapMode = TextureWrapMode.Clamp;
                        tex.filterMode = FilterMode.Bilinear;
                        Sprite s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                        _spriteCache[fileName] = s;
                        return s;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[ArcaneAssetLoader] Failed to direct-load {fullPath}: {e.Message}");
                }
            }

            return null;
        }

        /// <summary>
        /// Antialiased rounded rectangle with glassmorphic fill and radiant neon/gold border.
        /// Uses 4x supersampling for crisp edges, then creates a 9-slice sprite for pixel-perfect scaling.
        /// </summary>
        public static Sprite GetGlassBoxSprite(int width, int height, float radius, Color fillColor, Color borderColor, float borderWidth = 1.5f)
        {
            string key = $"GlassBox_{width}_{height}_{radius}_{fillColor}_{borderColor}_{borderWidth}";
            if (_proceduralCache.TryGetValue(key, out Sprite s) && s != null) return s;

            // Use supersampling for crisp procedural graphics
            int sw = width * PROC_SCALE;
            int sh = height * PROC_SCALE;
            float sr = radius * PROC_SCALE;
            float sbw = borderWidth * PROC_SCALE;

            Texture2D tex = new Texture2D(sw, sh, TextureFormat.RGBA32, true);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;

            Color[] colors = new Color[sw * sh];

            for (int y = 0; y < sh; y++)
            {
                float py = y + 0.5f;
                for (int x = 0; x < sw; x++)
                {
                    float px = x + 0.5f;

                    float cx = (px < sr) ? sr : (px > sw - sr) ? sw - sr : px;
                    float cy = (py < sr) ? sr : (py > sh - sr) ? sh - sr : py;

                    float dist = Vector2.Distance(new Vector2(px, py), new Vector2(cx, cy)) - sr;

                    if (dist > 1.0f)
                    {
                        colors[y * sw + x] = Color.clear;
                        continue;
                    }

                    // Smooth antialiased alpha
                    float alpha = Mathf.Clamp01(-dist + 1.0f);

                    if (sbw > 0f && dist >= -sbw)
                    {
                        // Border zone - embossed beveled metallic / carved stone edge
                        float borderFade = Mathf.Clamp01((-dist) / sbw); // 0 at outer edge, 1 at inner edge
                        Color borderCol = borderColor;
                        borderCol.a *= alpha;

                        // Directional lighting from top-left (-0.707, 0.707) for authentic carved metal/stone bevel
                        float nx = (px - cx) / Mathf.Max(0.001f, sr);
                        float ny = (py - cy) / Mathf.Max(0.001f, sr);
                        float lightFactor = Mathf.Clamp(-nx * 0.707f + ny * 0.707f, -1f, 1f);

                        // Highlight top-left with warm antique sheen, shade bottom-right with deep shadow
                        if (lightFactor > 0f)
                        {
                            borderCol = Color.Lerp(borderCol, new Color(1.0f, 0.95f, 0.85f, borderCol.a), lightFactor * 0.35f);
                        }
                        else
                        {
                            borderCol = Color.Lerp(borderCol, new Color(0.06f, 0.05f, 0.04f, borderCol.a), -lightFactor * 0.40f);
                        }

                        // At the inner edge of border, blend with fill
                        if (borderFade > 0.85f)
                        {
                            float blendT = (borderFade - 0.85f) / 0.15f;
                            Color f = fillColor;
                            f.a *= alpha;
                            colors[y * sw + x] = Color.Lerp(borderCol, f, blendT);
                        }
                        else
                        {
                            colors[y * sw + x] = borderCol;
                        }
                    }
                    else
                    {
                        // Interior fill - rich medieval stone or aged parchment gradient
                        float gradientT = (float)y / sh;
                        Color f = fillColor;
                        // Subtle warm highlight at top, dark stone depth at bottom
                        f.r += 0.025f * (1f - gradientT);
                        f.g += 0.020f * (1f - gradientT);
                        f.b += 0.015f * (1f - gradientT);
                        f.a *= alpha;
                        colors[y * sw + x] = f;
                    }
                }
            }

            tex.SetPixels(colors);
            tex.Apply(true); // Generate mipmaps for quality

            // Calculate 9-slice border in the supersampled texture
            int borderPx = Mathf.Min(sw / 2 - 1, (int)(sr + sbw + 4));
            Sprite sprite = Sprite.Create(
                tex,
                new Rect(0, 0, sw, sh),
                new Vector2(0.5f, 0.5f),
                100f * PROC_SCALE, // Scale pixelsPerUnit to match
                0,
                SpriteMeshType.FullRect,
                new Vector4(borderPx, borderPx, borderPx, borderPx)
            );

            _proceduralCache[key] = sprite;
            return sprite;
        }

        /// <summary>
        /// Anti-aliased circle sprite for avatars and circular icons.
        /// </summary>
        public static Sprite GetCircleSprite(int diameter, Color fillColor, Color borderColor, float borderWidth = 2f)
        {
            int r = diameter / 2;
            return GetGlassBoxSprite(diameter, diameter, r - 1, fillColor, borderColor, borderWidth);
        }

        /// <summary>
        /// Procedural solid color sprite (no border) for quick flat fills.
        /// </summary>
        public static Sprite GetSolidSprite(int width, int height, Color color)
        {
            string key = $"Solid_{width}_{height}_{color}";
            if (_proceduralCache.TryGetValue(key, out Sprite cached) && cached != null) return cached;

            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;

            Color[] colors = new Color[width * height];
            for (int i = 0; i < colors.Length; i++) colors[i] = color;
            tex.SetPixels(colors);
            tex.Apply();

            Sprite s = Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
            _proceduralCache[key] = s;
            return s;
        }

        /// <summary>
        /// Highly detailed procedural Dark Fantasy chess piece token medallion for 2D boards and UI markers.
        /// Features double metal filigree rim (Gold for White, Crimson for Black), subtle radial shading, and crisp contrast.
        /// </summary>
        public static Sprite GetPieceTokenSprite(bool isWhite, int diameter = 64)
        {
            string key = $"PieceToken_{isWhite}_{diameter}";
            if (_proceduralCache.TryGetValue(key, out Sprite cached) && cached != null) return cached;

            int size = Mathf.Max(32, diameter);
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;

            Color[] colors = new Color[size * size];
            float center = (size - 1) * 0.5f;
            float radius = center;

            // White: Ivory marble body + Antique gold rim
            // Black: Obsidian slate body + Crimson ruby rim
            Color outerRim = isWhite 
                ? new Color(0.85f, 0.72f, 0.42f, 1f)  // Radiant Antique Gold
                : new Color(0.72f, 0.18f, 0.22f, 1f); // Blood Red Ruby
            Color innerRim = isWhite 
                ? new Color(0.98f, 0.90f, 0.65f, 1f)  // Bright Gold Highlight
                : new Color(0.95f, 0.35f, 0.40f, 1f); // Bright Crimson Highlight
            Color fillCenter = isWhite 
                ? new Color(0.98f, 0.96f, 0.92f, 1.0f) // Pure Ivory Pearl
                : new Color(0.12f, 0.15f, 0.20f, 1.0f); // Deep Obsidian Slate
            Color fillEdge = isWhite 
                ? new Color(0.88f, 0.83f, 0.74f, 1.0f) // Aged Parchment Marble
                : new Color(0.06f, 0.08f, 0.11f, 1.0f); // Pitch Black Void

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float normDist = dist / radius;

                    if (normDist > 1.0f)
                    {
                        colors[y * size + x] = Color.clear;
                    }
                    else if (normDist > 0.92f)
                    {
                        float alpha = Mathf.Clamp01((1.0f - normDist) / 0.08f);
                        Color c = outerRim;
                        c.a *= alpha;
                        colors[y * size + x] = c;
                    }
                    else if (normDist > 0.80f)
                    {
                        colors[y * size + x] = outerRim;
                    }
                    else if (normDist > 0.72f)
                    {
                        colors[y * size + x] = innerRim;
                    }
                    else
                    {
                        float bodyNorm = normDist / 0.72f;
                        Color body = Color.Lerp(fillCenter, fillEdge, bodyNorm * bodyNorm);
                        colors[y * size + x] = body;
                    }
                }
            }

            tex.SetPixels(colors);
            tex.Apply();

            Sprite s = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
            _proceduralCache[key] = s;
            return s;
        }

        /// <summary>
        /// Procedural 2D Chessboard diagram texture with highlighted squares for Tutorial & Learn screens.
        /// </summary>
        public static Sprite GetMiniChessboardDiagram(int size = 256)
        {
            return GetCustomBoardDiagram(size, false, Vector2Int.zero, 
                new List<Vector2Int> { new Vector2Int(5, 2) }, 
                null, 
                new List<Vector2Int> { new Vector2Int(4, 3), new Vector2Int(4, 4) });
        }

        /// <summary>
        /// Highly versatile procedural 8x8 Chessboard diagram generator for the Tutorial & Strategy views.
        /// Renders selected piece (Cyan), valid moves (Neon Green), captures (Crimson Red), and special tiles (Solar Gold)
        /// with crisp antialiased grid lines and cached sprite reuse.
        /// </summary>
        public static Sprite GetCustomBoardDiagram(
            int size,
            bool hasSelected,
            Vector2Int selected,
            IList<Vector2Int> moveSquares,
            IList<Vector2Int> captureSquares,
            IList<Vector2Int> specialSquares,
            IList<TutorialPieceMarker> pieces = null)
        {
            int hash = 17;
            if (hasSelected) hash = hash * 31 + selected.GetHashCode();
            if (moveSquares != null) foreach (var v in moveSquares) hash = hash * 31 + v.GetHashCode();
            if (captureSquares != null) foreach (var v in captureSquares) hash = hash * 31 + v.GetHashCode();
            if (specialSquares != null) foreach (var v in specialSquares) hash = hash * 31 + v.GetHashCode();
            if (pieces != null)
            {
                foreach (var p in pieces)
                {
                    hash = hash * 31 + p.file;
                    hash = hash * 31 + p.rank;
                    hash = hash * 31 + (p.isWhite ? 1 : 2);
                    if (p.symbol != null) hash = hash * 31 + p.symbol.GetHashCode();
                }
            }

            string key = $"CustomBoard_{size}_{hasSelected}_{hash}";
            if (_proceduralCache.TryGetValue(key, out Sprite cached) && cached != null) return cached;

            int ss = Mathf.Max(256, size * 2);
            Texture2D tex = new Texture2D(ss, ss, TextureFormat.RGBA32, true);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;

            // Dark Fantasy Medieval Color Palette
            Color lightSquare = new Color(0.851f, 0.784f, 0.635f, 1f); // #D9C8A2 Ivory Parchment / Marble
            Color darkSquare = new Color(0.090f, 0.137f, 0.176f, 1f);  // #17232D Midnight Slate Stone
            Color colSelected = new Color(0.725f, 0.604f, 0.380f, 1f); // #B99A61 Radiant Antique Gold
            Color colMove = new Color(0.322f, 0.650f, 0.380f, 1f);     // #526D57 Magic Emerald Green
            Color colCapture = new Color(0.680f, 0.180f, 0.220f, 1f);  // #7D3942 Blood Red Combat Alert Ring
            Color colSpecial = new Color(0.920f, 0.780f, 0.250f, 1f);  // Solar Crown Gold
            Color colGrid = new Color(0.350f, 0.280f, 0.200f, 0.85f);  // #47352B Antique Bronze Carved Filigree

            Color[] colors = new Color[ss * ss];
            int squarePx = ss / 8;

            for (int y = 0; y < ss; y++)
            {
                int rank = y / squarePx;
                int innerY = y % squarePx;
                float normY = (innerY + 0.5f) / squarePx;

                for (int x = 0; x < ss; x++)
                {
                    int file = x / squarePx;
                    int innerX = x % squarePx;
                    float normX = (innerX + 0.5f) / squarePx;

                    bool isLight = (file + rank) % 2 != 0;
                    Color sqColor = isLight ? lightSquare : darkSquare;
                    Vector2Int coord = new Vector2Int(file, rank);

                    // Check tile highlight category
                    if (hasSelected && coord == selected)
                    {
                        // Selected tile: Radiant Antique Gold glow with imperial core
                        float distCenter = Vector2.Distance(new Vector2(normX, normY), new Vector2(0.5f, 0.5f));
                        float glow = Mathf.Clamp01(1.0f - distCenter * 1.3f);
                        sqColor = Color.Lerp(sqColor, colSelected, 0.70f + glow * 0.25f);
                    }
                    else if (captureSquares != null && captureSquares.Contains(coord))
                    {
                        // Capture tile: Blood Red combat alert with concentric ring
                        float distCenter = Vector2.Distance(new Vector2(normX, normY), new Vector2(0.5f, 0.5f));
                        bool isRing = Mathf.Abs(distCenter - 0.28f) < 0.05f;
                        sqColor = Color.Lerp(sqColor, colCapture, isRing ? 0.95f : 0.65f);
                    }
                    else if (specialSquares != null && specialSquares.Contains(coord))
                    {
                        // Special tile (Promotion / Castling): Solar Crown Gold
                        float distCenter = Vector2.Distance(new Vector2(normX, normY), new Vector2(0.5f, 0.5f));
                        float glow = Mathf.Clamp01(1.0f - distCenter * 1.5f);
                        sqColor = Color.Lerp(sqColor, colSpecial, 0.65f + glow * 0.30f);
                    }
                    else if (moveSquares != null && moveSquares.Contains(coord))
                    {
                        // Valid move tile: Magic Emerald Green with central rune beacon
                        float distCenter = Vector2.Distance(new Vector2(normX, normY), new Vector2(0.5f, 0.5f));
                        bool isDot = distCenter < 0.16f;
                        sqColor = Color.Lerp(sqColor, colMove, isDot ? 0.95f : 0.55f);
                    }

                    // Render chess piece medallion token directly on the board square
                    if (pieces != null)
                    {
                        for (int pIdx = 0; pIdx < pieces.Count; pIdx++)
                        {
                            var p = pieces[pIdx];
                            if (p.file == file && p.rank == rank)
                            {
                                float distCenter = Vector2.Distance(new Vector2(normX, normY), new Vector2(0.5f, 0.5f));
                                if (distCenter <= 0.36f)
                                {
                                    if (distCenter >= 0.31f)
                                    {
                                        // Outer metallic rim (Antique Gold for White, Blood Red for Black)
                                        sqColor = p.isWhite ? colSelected : colCapture;
                                    }
                                    else if (distCenter <= 0.11f)
                                    {
                                        // Central heraldic jewel
                                        sqColor = p.isWhite ? colSpecial : new Color(0.95f, 0.30f, 0.35f, 1f);
                                    }
                                    else
                                    {
                                        // Piece body fill
                                        sqColor = p.isWhite 
                                            ? new Color(0.96f, 0.94f, 0.88f, 1f) 
                                            : new Color(0.10f, 0.14f, 0.19f, 1f);
                                    }
                                }
                                break;
                            }
                        }
                    }

                    // Grid border lines - carved antique bronze filigree
                    bool isBorderX = (innerX == 0) || (innerX == squarePx - 1);
                    bool isBorderY = (innerY == 0) || (innerY == squarePx - 1);
                    if (isBorderX || isBorderY)
                    {
                        sqColor = Color.Lerp(sqColor, colGrid, 0.65f);
                    }

                    // Outer perimeter frame
                    if (x < 2 || x >= ss - 2 || y < 2 || y >= ss - 2)
                    {
                        sqColor = colSelected;
                    }

                    colors[y * ss + x] = sqColor;
                }
            }

            tex.SetPixels(colors);
            tex.Apply(true);

            Sprite s = Sprite.Create(tex, new Rect(0, 0, ss, ss), new Vector2(0.5f, 0.5f), 100f * 2);
            _proceduralCache[key] = s;
            return s;
        }

        /// <summary>
        /// Procedural visual graphic generator for the AR Tutorial Tab.
        /// Dark Fantasy Medieval aesthetic: Arcane Summoning Circles, Leyline Battlefield Grids,
        /// Ancient Stone Tablet Scale Runes, and Ornate Grimoire Folio scanning frames.
        /// </summary>
        public static Sprite GetARDiagramSprite(int size, string graphicType)
        {
            string key = $"ARDiagram_Fantasy_{size}_{graphicType}";
            if (_proceduralCache.TryGetValue(key, out Sprite cached) && cached != null) return cached;

            int ss = Mathf.Max(256, size * 2);
            Texture2D tex = new Texture2D(ss, ss, TextureFormat.RGBA32, true);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;

            Color bgDark = new Color(0.090f, 0.137f, 0.176f, 1f);     // #17232D Midnight Slate
            Color colGold = new Color(0.725f, 0.604f, 0.380f, 1f);     // #B99A61 Antique Gold
            Color colBrightGold = new Color(0.92f, 0.80f, 0.45f, 1f);  // Radiant Gold
            Color colGreen = new Color(0.322f, 0.650f, 0.380f, 1f);    // #526D57 Magic Emerald
            Color colBronze = new Color(0.35f, 0.26f, 0.18f, 0.75f);   // #47352B Antique Bronze

            Color[] colors = new Color[ss * ss];

            for (int y = 0; y < ss; y++)
            {
                float ny = (float)y / ss;
                for (int x = 0; x < ss; x++)
                {
                    float nx = (float)x / ss;
                    Color c = bgDark;

                    // Subtle background medieval rune grid
                    if ((x % 32 == 0) || (y % 32 == 0))
                    {
                        c = Color.Lerp(c, colBronze, 0.35f);
                    }

                    float distCenter = Vector2.Distance(new Vector2(nx, ny), new Vector2(0.5f, 0.5f));

                    switch (graphicType)
                    {
                        case "ar_permissions":
                            // Arcane Summoning Circle / Grimoire Oculus Seal
                            if (Mathf.Abs(distCenter - 0.38f) < 0.009f || Mathf.Abs(distCenter - 0.28f) < 0.007f || Mathf.Abs(distCenter - 0.16f) < 0.007f)
                            {
                                c = colGold;
                            }
                            else if (distCenter < 0.06f)
                            {
                                c = Color.Lerp(colBrightGold, Color.white, 0.75f);
                            }
                            // 8-Fold Radial Arcane Rays
                            float angle = Mathf.Atan2(ny - 0.5f, nx - 0.5f);
                            if (distCenter > 0.08f && distCenter < 0.42f)
                            {
                                float angleDeg = (angle * Mathf.Rad2Deg + 360f) % 45f;
                                if (angleDeg < 1.5f || angleDeg > 43.5f)
                                {
                                    c = Color.Lerp(c, colGold, 0.85f);
                                }
                            }
                            break;

                        case "ar_plane_scan":
                            // 3D Perspective Battlefield Leyline Grid vanishing into misty castle darkness
                            float planeY = (ny - 0.15f) / 0.7f;
                            if (planeY > 0f && planeY < 1f)
                            {
                                float perspX = (nx - 0.5f) / (0.3f + planeY * 0.7f) + 0.5f;
                                if (perspX >= 0f && perspX <= 1f)
                                {
                                    bool isHoriz = Mathf.Abs((planeY * 8f) % 1f) < 0.06f;
                                    bool isVert = Mathf.Abs((perspX * 8f) % 1f) < 0.06f;
                                    if (isHoriz || isVert)
                                    {
                                        c = Color.Lerp(c, colGreen, 0.65f + planeY * 0.3f);
                                    }
                                }
                            }
                            // Central Summoning Altar Diamond (Antique Gold)
                            float dAnchor = Mathf.Abs(nx - 0.5f) + Mathf.Abs(ny - 0.45f);
                            if (Mathf.Abs(dAnchor - 0.10f) < 0.012f || dAnchor < 0.04f)
                            {
                                c = colBrightGold;
                            }
                            break;

                        case "ar_scale_controls":
                            // Ancient Stone Chessboard in center with twin arcane touch runes & golden scale gauge
                            if (nx >= 0.25f && nx <= 0.75f && ny >= 0.25f && ny <= 0.75f)
                            {
                                int bx = (int)((nx - 0.25f) * 16f);
                                int by = (int)((ny - 0.25f) * 16f);
                                c = ((bx + by) % 2 == 0) ? new Color(0.851f, 0.784f, 0.635f) : new Color(0.18f, 0.14f, 0.10f);
                            }
                            // Touch Rune 1 (top-left) & Touch Rune 2 (bottom-right)
                            float dTouch1 = Vector2.Distance(new Vector2(nx, ny), new Vector2(0.20f, 0.80f));
                            float dTouch2 = Vector2.Distance(new Vector2(nx, ny), new Vector2(0.80f, 0.20f));
                            if (dTouch1 < 0.07f || dTouch2 < 0.07f)
                            {
                                c = Color.Lerp(c, colBrightGold, 0.85f);
                            }
                            // Scale gauge bar at bottom with antique gold notches
                            if (ny >= 0.08f && ny <= 0.12f && nx >= 0.15f && nx <= 0.85f)
                            {
                                c = Color.Lerp(c, colGold, 0.85f);
                            }
                            break;

                        case "ar_ai_scanner":
                        default:
                            // Ornate Antique Grimoire Folio with filigree corner seals and mystical scan wave
                            if ((nx >= 0.22f && nx <= 0.78f && (Mathf.Abs(ny - 0.18f) < 0.008f || Mathf.Abs(ny - 0.82f) < 0.008f)) ||
                                (ny >= 0.18f && ny <= 0.82f && (Mathf.Abs(nx - 0.22f) < 0.008f || Mathf.Abs(nx - 0.78f) < 0.008f)))
                            {
                                c = colGold;
                            }
                            // Corner heavy brackets with ancient seals
                            bool nearCornerX = (nx >= 0.20f && nx <= 0.26f) || (nx >= 0.74f && nx <= 0.80f);
                            bool nearCornerY = (ny >= 0.16f && ny <= 0.22f) || (ny >= 0.78f && ny <= 0.84f);
                            if (nearCornerX && nearCornerY)
                            {
                                c = colBrightGold;
                            }
                            // Central emerald mystical scanning wave
                            if (Mathf.Abs(ny - 0.52f) < 0.007f && nx >= 0.24f && nx <= 0.76f)
                            {
                                c = colGreen;
                            }
                            break;
                    }

                    // Outer border frame - Antique Gold filigree rim
                    if (x < 3 || x >= ss - 3 || y < 3 || y >= ss - 3)
                    {
                        c = colGold;
                    }

                    colors[y * ss + x] = c;
                }
            }

            tex.SetPixels(colors);
            tex.Apply(true);

            Sprite s = Sprite.Create(tex, new Rect(0, 0, ss, ss), new Vector2(0.5f, 0.5f), 100f * 2);
            _proceduralCache[key] = s;
            return s;
        }

        /// <summary>
        /// Gradient horizontal bar sprite for progress bars and dividers.
        /// </summary>
        public static Sprite GetGradientBar(int width, int height, Color leftColor, Color rightColor)
        {
            string key = $"Gradient_{width}_{height}_{leftColor}_{rightColor}";
            if (_proceduralCache.TryGetValue(key, out Sprite cached) && cached != null) return cached;

            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;

            Color[] colors = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float t = (float)x / (width - 1);
                    colors[y * width + x] = Color.Lerp(leftColor, rightColor, t);
                }
            }
            tex.SetPixels(colors);
            tex.Apply();

            Sprite s = Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
            _proceduralCache[key] = s;
            return s;
        }
    }
}
