using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using MechChess.ProceduralMech;

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

                    // Detect Git LFS pointer files (~130 bytes starting with 'version https://git-lfs')
                    if (bytes.Length > 0 && bytes.Length < 300)
                    {
                        string header = System.Text.Encoding.UTF8.GetString(bytes, 0, Math.Min(bytes.Length, 32));
                        if (header.StartsWith("version https://git-lfs"))
                        {
                            Debug.LogWarning($"[ArcaneAssetLoader] File '{fileName}' is a Git LFS pointer, not a real image! Run 'git lfs pull' to download actual assets.");
                            return null;
                        }
                    }

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
        /// Clears all cached sprites and procedural textures to force reload from disk.
        /// </summary>
        public static void ClearCache()
        {
            _spriteCache.Clear();
            _proceduralCache.Clear();
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

        private static readonly Dictionary<string, Texture2D> _pieceTextureCache = new Dictionary<string, Texture2D>();

        /// <summary>
        /// Converts standard Unicode chess symbols or letters to PieceType enum.
        /// </summary>
        public static PieceType GetPieceTypeFromSymbol(string symbol)
        {
            if (string.IsNullOrEmpty(symbol)) return PieceType.Pawn;
            switch (symbol)
            {
                case "♔": case "♚": case "K": case "k": return PieceType.King;
                case "♕": case "♛": case "Q": case "q": return PieceType.Queen;
                case "♖": case "♜": case "R": case "r": return PieceType.Rook;
                case "♗": case "♝": case "B": case "b": return PieceType.Bishop;
                case "♘": case "♞": case "N": case "n": return PieceType.Knight;
                case "♙": case "♟": case "P": case "p": return PieceType.Pawn;
                default:
                    char c = symbol[0];
                    if (c == 'K' || c == 'k') return PieceType.King;
                    if (c == 'Q' || c == 'q') return PieceType.Queen;
                    if (c == 'R' || c == 'r') return PieceType.Rook;
                    if (c == 'B' || c == 'b') return PieceType.Bishop;
                    if (c == 'N' || c == 'n') return PieceType.Knight;
                    return PieceType.Pawn;
            }
        }

        /// <summary>
        /// Loads or generates a standard 2D chess piece Texture2D (always readable RGBA32).
        /// Prioritizes disk byte loading (always readable in Editor/player), then Resources, then procedural silhouette.
        /// </summary>
        public static Texture2D GetChessPiece2DTexture(PieceType type, bool isWhite, int size = 128)
        {
            string colorKey = isWhite ? "white" : "black";
            string typeKey = type.ToString().ToLower();
            string cacheKey = $"Chess2DTex_{colorKey}_{typeKey}_{size}";

            if (_pieceTextureCache.TryGetValue(cacheKey, out Texture2D cached) && cached != null)
                return cached;

            // 1. Try direct file byte loading from disk (fastest, full fidelity 32-bit RGBA, 100% readable)
            try
            {
                string diskPath1 = Path.Combine(Application.dataPath, "Resources", "Textures", "ArcaneUI", "Chess2D", $"chess_2d_{colorKey}_{typeKey}.png");
                string diskPath2 = Path.Combine(Application.dataPath, "..", "Assets", "Resources", "Textures", "ArcaneUI", "Chess2D", $"chess_2d_{colorKey}_{typeKey}.png");
                string fullPath = null;
                if (File.Exists(diskPath1)) fullPath = diskPath1;
                else if (File.Exists(diskPath2)) fullPath = diskPath2;

                if (!string.IsNullOrEmpty(fullPath))
                {
                    byte[] bytes = File.ReadAllBytes(fullPath);
                    Texture2D diskTex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    if (diskTex.LoadImage(bytes))
                    {
                        diskTex.wrapMode = TextureWrapMode.Clamp;
                        diskTex.filterMode = FilterMode.Bilinear;
                        _pieceTextureCache[cacheKey] = diskTex;
                        return diskTex;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ArcaneAssetLoader] Could not load piece {cacheKey} from disk: {ex.Message}");
            }

            // 2. Try Resources.Load as Texture2D (only if isReadable)
            try
            {
                string resPath = $"Textures/ArcaneUI/Chess2D/chess_2d_{colorKey}_{typeKey}";
                Texture2D resTex = Resources.Load<Texture2D>(resPath);
                if (resTex != null && resTex.isReadable)
                {
                    _pieceTextureCache[cacheKey] = resTex;
                    return resTex;
                }
            }
            catch { }

            // 3. Fallback to procedural vector silhouette texture (guaranteed 100% readable RGBA32)
            Texture2D procTex = GenerateProceduralPieceTexture(type, isWhite, Mathf.Max(size, 128));
            _pieceTextureCache[cacheKey] = procTex;
            return procTex;
        }

        /// <summary>
        /// Loads or generates a standard 2D Dark Fantasy chess piece sprite (Pawn, Knight, Bishop, Rook, Queen, King).
        /// White: Royal Ivory with Antique Gold rim. Black: Obsidian Slate with Blood Red Ruby rim.
        /// </summary>
        public static Sprite GetChessPiece2DSprite(PieceType type, bool isWhite, int size = 64)
        {
            string colorKey = isWhite ? "white" : "black";
            string typeKey = type.ToString().ToLower();
            string cacheKey = $"Chess2D_{colorKey}_{typeKey}_{size}";

            if (_proceduralCache.TryGetValue(cacheKey, out Sprite cached) && cached != null)
                return cached;

            Texture2D tex = GetChessPiece2DTexture(type, isWhite, size);
            if (tex != null)
            {
                Sprite s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                _proceduralCache[cacheKey] = s;
                return s;
            }

            return null;
        }

        /// <summary>
        /// Generates a high-quality procedural 2D chess piece texture with recognizable silhouette.
        /// Each piece type (King, Queen, Rook, Bishop, Knight, Pawn) has a distinct profile.
        /// White pieces: Ivory body with gold outline. Black pieces: Dark body with crimson outline.
        /// </summary>
        public static Texture2D GenerateProceduralPieceTexture(PieceType type, bool isWhite, int size = 128)
        {
            string key = $"ProceduralPieceTex_{type}_{isWhite}_{size}";
            if (_pieceTextureCache.TryGetValue(key, out Texture2D cached) && cached != null) return cached;

            int ss = Mathf.Max(64, size);
            Texture2D tex = new Texture2D(ss, ss, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;

            // Color palette
            Color fillColor = isWhite
                ? new Color(0.96f, 0.93f, 0.85f, 1f)   // Ivory cream
                : new Color(0.15f, 0.17f, 0.22f, 1f);   // Dark charcoal
            Color outlineColor = isWhite
                ? new Color(0.72f, 0.58f, 0.32f, 1f)    // Antique Gold
                : new Color(0.65f, 0.16f, 0.20f, 1f);   // Crimson Ruby
            Color highlightColor = isWhite
                ? new Color(1.0f, 0.98f, 0.94f, 1f)     // Bright ivory highlight
                : new Color(0.28f, 0.30f, 0.38f, 1f);   // Lighter slate highlight
            Color shadowColor = isWhite
                ? new Color(0.70f, 0.65f, 0.55f, 1f)    // Warm shadow
                : new Color(0.06f, 0.07f, 0.10f, 1f);   // Deep shadow

            Color[] colors = new Color[ss * ss];
            // Initialize transparent
            for (int i = 0; i < colors.Length; i++) colors[i] = Color.clear;

            // Get silhouette points for this piece type (normalized 0..1 coordinates)
            Vector2[] silhouette = GetPieceSilhouettePoints(type);

            // Render the piece silhouette with outline
            float outlineThickness = ss * 0.035f; // ~3.5% of size

            for (int y = 0; y < ss; y++)
            {
                float ny = (float)y / ss;
                for (int x = 0; x < ss; x++)
                {
                    float nx = (float)x / ss;
                    float distToEdge = DistanceToPolygon(nx, ny, silhouette);
                    bool inside = IsInsidePolygon(nx, ny, silhouette);

                    float outlinePx = outlineThickness / ss;
                    float aaPx = 1.5f / ss; // Anti-alias edge

                    if (inside)
                    {
                        if (distToEdge < outlinePx)
                        {
                            // Outline region
                            float t = Mathf.Clamp01(distToEdge / outlinePx);
                            colors[y * ss + x] = Color.Lerp(outlineColor, fillColor, t * 0.3f);
                        }
                        else
                        {
                            // Interior: apply subtle gradient for 3D look
                            float gradY = 1.0f - ny; // top = brighter
                            float shade = Mathf.Clamp01(gradY * 0.7f + 0.3f);
                            Color baseC = Color.Lerp(shadowColor, fillColor, shade);
                            // Side highlight (subtle left-side light)
                            if (nx < 0.45f)
                            {
                                float highlightT = Mathf.Clamp01((0.45f - nx) / 0.2f) * 0.25f;
                                baseC = Color.Lerp(baseC, highlightColor, highlightT);
                            }
                            colors[y * ss + x] = baseC;
                        }
                    }
                    else if (distToEdge < aaPx)
                    {
                        // Anti-aliased edge
                        float alpha = Mathf.Clamp01(1.0f - distToEdge / aaPx);
                        Color c = outlineColor;
                        c.a = alpha;
                        colors[y * ss + x] = c;
                    }
                }
            }

            // Add piece-specific details (cross for King, crown points for Queen, etc.)
            AddPieceDetails(colors, ss, type, isWhite, outlineColor, highlightColor);

            tex.SetPixels(colors);
            tex.Apply();

            _pieceTextureCache[key] = tex;
            return tex;
        }

        private static Sprite GenerateProceduralPieceSprite(PieceType type, bool isWhite, int size = 128)
        {
            string key = $"ProceduralPiece_{type}_{isWhite}_{size}";
            if (_proceduralCache.TryGetValue(key, out Sprite cached) && cached != null) return cached;

            Texture2D tex = GenerateProceduralPieceTexture(type, isWhite, size);
            Sprite s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            _proceduralCache[key] = s;
            return s;
        }

        /// <summary>
        /// Returns normalized silhouette polygon points (0..1) for each chess piece type.
        /// Origin is bottom-left (0,0), top-right is (1,1).
        /// </summary>
        private static Vector2[] GetPieceSilhouettePoints(PieceType type)
        {
            switch (type)
            {
                case PieceType.King:
                    // King: Tall body with cross on top
                    return new Vector2[]
                    {
                        // Base platform
                        new Vector2(0.22f, 0.08f), new Vector2(0.78f, 0.08f),
                        new Vector2(0.78f, 0.15f), new Vector2(0.72f, 0.15f),
                        // Body
                        new Vector2(0.68f, 0.22f), new Vector2(0.64f, 0.40f),
                        // Collar/neck narrowing
                        new Vector2(0.62f, 0.50f), new Vector2(0.58f, 0.55f),
                        // Crown rim
                        new Vector2(0.63f, 0.60f), new Vector2(0.64f, 0.65f),
                        // Cross arm right
                        new Vector2(0.62f, 0.72f), new Vector2(0.66f, 0.72f),
                        new Vector2(0.66f, 0.78f), new Vector2(0.62f, 0.78f),
                        // Cross top
                        new Vector2(0.56f, 0.78f), new Vector2(0.56f, 0.92f),
                        new Vector2(0.44f, 0.92f), new Vector2(0.44f, 0.78f),
                        // Cross arm left
                        new Vector2(0.38f, 0.78f), new Vector2(0.34f, 0.78f),
                        new Vector2(0.34f, 0.72f), new Vector2(0.38f, 0.72f),
                        // Crown rim left
                        new Vector2(0.36f, 0.65f), new Vector2(0.37f, 0.60f),
                        // Collar left
                        new Vector2(0.42f, 0.55f), new Vector2(0.38f, 0.50f),
                        // Body left
                        new Vector2(0.36f, 0.40f), new Vector2(0.32f, 0.22f),
                        new Vector2(0.28f, 0.15f), new Vector2(0.22f, 0.15f),
                    };

                case PieceType.Queen:
                    // Queen: Crown with 5 points on top
                    return new Vector2[]
                    {
                        // Base
                        new Vector2(0.20f, 0.08f), new Vector2(0.80f, 0.08f),
                        new Vector2(0.80f, 0.15f), new Vector2(0.73f, 0.15f),
                        // Body
                        new Vector2(0.68f, 0.25f), new Vector2(0.65f, 0.42f),
                        new Vector2(0.60f, 0.52f),
                        // Crown right
                        new Vector2(0.66f, 0.58f),
                        // Crown points (5 prongs)
                        new Vector2(0.72f, 0.60f), new Vector2(0.68f, 0.75f), // right point
                        new Vector2(0.62f, 0.65f), new Vector2(0.58f, 0.82f), // mid-right point
                        new Vector2(0.54f, 0.68f), new Vector2(0.50f, 0.90f), // center point (tallest)
                        new Vector2(0.46f, 0.68f), new Vector2(0.42f, 0.82f), // mid-left point
                        new Vector2(0.38f, 0.65f), new Vector2(0.32f, 0.75f), // left point
                        new Vector2(0.28f, 0.60f),
                        // Crown left
                        new Vector2(0.34f, 0.58f),
                        new Vector2(0.40f, 0.52f),
                        // Body left
                        new Vector2(0.35f, 0.42f), new Vector2(0.32f, 0.25f),
                        new Vector2(0.27f, 0.15f), new Vector2(0.20f, 0.15f),
                    };

                case PieceType.Rook:
                    // Rook: Castle tower with battlements
                    return new Vector2[]
                    {
                        // Base
                        new Vector2(0.20f, 0.08f), new Vector2(0.80f, 0.08f),
                        new Vector2(0.80f, 0.16f), new Vector2(0.72f, 0.16f),
                        // Body - straight walls
                        new Vector2(0.68f, 0.28f), new Vector2(0.66f, 0.56f),
                        // Battlement top right
                        new Vector2(0.68f, 0.60f), new Vector2(0.68f, 0.62f),
                        // Crenellations (battlements)
                        new Vector2(0.70f, 0.62f), new Vector2(0.70f, 0.82f), // right merlon
                        new Vector2(0.62f, 0.82f), new Vector2(0.62f, 0.70f), // crenel
                        new Vector2(0.56f, 0.70f), new Vector2(0.56f, 0.86f), // center-right merlon
                        new Vector2(0.44f, 0.86f), new Vector2(0.44f, 0.70f), // crenel
                        new Vector2(0.38f, 0.70f), new Vector2(0.38f, 0.82f), // left merlon
                        new Vector2(0.30f, 0.82f), new Vector2(0.30f, 0.62f),
                        // Battlement top left
                        new Vector2(0.32f, 0.62f), new Vector2(0.32f, 0.60f),
                        // Body left
                        new Vector2(0.34f, 0.56f), new Vector2(0.32f, 0.28f),
                        new Vector2(0.28f, 0.16f), new Vector2(0.20f, 0.16f),
                    };

                case PieceType.Bishop:
                    // Bishop: Pointed mitre hat with notch
                    return new Vector2[]
                    {
                        // Base
                        new Vector2(0.22f, 0.08f), new Vector2(0.78f, 0.08f),
                        new Vector2(0.78f, 0.15f), new Vector2(0.70f, 0.15f),
                        // Body
                        new Vector2(0.66f, 0.25f), new Vector2(0.63f, 0.38f),
                        // Collar
                        new Vector2(0.60f, 0.45f), new Vector2(0.57f, 0.50f),
                        // Mitre right side
                        new Vector2(0.62f, 0.55f), new Vector2(0.60f, 0.62f),
                        // Notch right
                        new Vector2(0.57f, 0.68f),
                        // Pointed top
                        new Vector2(0.53f, 0.78f), new Vector2(0.50f, 0.88f),
                        // Small ball on top
                        new Vector2(0.47f, 0.88f),
                        // Pointed top left
                        new Vector2(0.43f, 0.68f),
                        // Notch left
                        new Vector2(0.40f, 0.62f), new Vector2(0.38f, 0.55f),
                        // Collar left
                        new Vector2(0.43f, 0.50f), new Vector2(0.40f, 0.45f),
                        // Body left
                        new Vector2(0.37f, 0.38f), new Vector2(0.34f, 0.25f),
                        new Vector2(0.30f, 0.15f), new Vector2(0.22f, 0.15f),
                    };

                case PieceType.Knight:
                    // Knight: Horse head profile facing left
                    return new Vector2[]
                    {
                        // Base
                        new Vector2(0.18f, 0.08f), new Vector2(0.82f, 0.08f),
                        new Vector2(0.82f, 0.15f), new Vector2(0.72f, 0.15f),
                        // Neck (back of horse)
                        new Vector2(0.70f, 0.25f), new Vector2(0.68f, 0.40f),
                        new Vector2(0.66f, 0.55f),
                        // Ear right
                        new Vector2(0.64f, 0.72f), new Vector2(0.58f, 0.88f),
                        // Ear notch
                        new Vector2(0.52f, 0.80f),
                        // Ear left  
                        new Vector2(0.46f, 0.90f), new Vector2(0.42f, 0.82f),
                        // Forehead / top of head
                        new Vector2(0.38f, 0.76f), new Vector2(0.32f, 0.72f),
                        // Nose / muzzle
                        new Vector2(0.24f, 0.68f), new Vector2(0.20f, 0.62f),
                        new Vector2(0.18f, 0.56f),
                        // Mouth / chin line
                        new Vector2(0.22f, 0.52f), new Vector2(0.28f, 0.50f),
                        // Jaw
                        new Vector2(0.32f, 0.46f), new Vector2(0.34f, 0.40f),
                        // Chest / front of neck
                        new Vector2(0.30f, 0.30f), new Vector2(0.28f, 0.15f),
                        new Vector2(0.18f, 0.15f),
                    };

                case PieceType.Pawn:
                default:
                    // Pawn: Simple rounded top with narrow neck
                    return new Vector2[]
                    {
                        // Base
                        new Vector2(0.25f, 0.08f), new Vector2(0.75f, 0.08f),
                        new Vector2(0.75f, 0.15f), new Vector2(0.68f, 0.15f),
                        // Body lower
                        new Vector2(0.64f, 0.25f), new Vector2(0.60f, 0.35f),
                        // Neck narrowing
                        new Vector2(0.56f, 0.42f), new Vector2(0.54f, 0.48f),
                        // Head (circular top) - right side
                        new Vector2(0.60f, 0.54f), new Vector2(0.64f, 0.62f),
                        new Vector2(0.65f, 0.70f), new Vector2(0.64f, 0.78f),
                        new Vector2(0.60f, 0.84f),
                        // Head top
                        new Vector2(0.54f, 0.88f), new Vector2(0.46f, 0.88f),
                        // Head left side
                        new Vector2(0.40f, 0.84f), new Vector2(0.36f, 0.78f),
                        new Vector2(0.35f, 0.70f), new Vector2(0.36f, 0.62f),
                        new Vector2(0.40f, 0.54f),
                        // Neck left
                        new Vector2(0.46f, 0.48f), new Vector2(0.44f, 0.42f),
                        // Body left
                        new Vector2(0.40f, 0.35f), new Vector2(0.36f, 0.25f),
                        new Vector2(0.32f, 0.15f), new Vector2(0.25f, 0.15f),
                    };
            }
        }

        /// <summary>
        /// Tests if a point (px,py) in normalized coords is inside a polygon defined by vertices.
        /// Uses ray casting algorithm.
        /// </summary>
        private static bool IsInsidePolygon(float px, float py, Vector2[] poly)
        {
            bool inside = false;
            int n = poly.Length;
            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                float yi = poly[i].y, yj = poly[j].y;
                float xi = poly[i].x, xj = poly[j].x;
                if (((yi > py) != (yj > py)) &&
                    (px < (xj - xi) * (py - yi) / (yj - yi) + xi))
                {
                    inside = !inside;
                }
            }
            return inside;
        }

        /// <summary>
        /// Computes minimum distance from point to polygon edge (normalized coords).
        /// </summary>
        private static float DistanceToPolygon(float px, float py, Vector2[] poly)
        {
            float minDist = float.MaxValue;
            int n = poly.Length;
            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                float dist = DistanceToSegment(px, py, poly[j].x, poly[j].y, poly[i].x, poly[i].y);
                if (dist < minDist) minDist = dist;
            }
            return minDist;
        }

        /// <summary>
        /// Distance from point (px,py) to line segment (ax,ay)-(bx,by).
        /// </summary>
        private static float DistanceToSegment(float px, float py, float ax, float ay, float bx, float by)
        {
            float dx = bx - ax, dy = by - ay;
            float lenSq = dx * dx + dy * dy;
            if (lenSq < 1e-10f) return Mathf.Sqrt((px - ax) * (px - ax) + (py - ay) * (py - ay));
            float t = Mathf.Clamp01(((px - ax) * dx + (py - ay) * dy) / lenSq);
            float closestX = ax + t * dx;
            float closestY = ay + t * dy;
            float ex = px - closestX, ey = py - closestY;
            return Mathf.Sqrt(ex * ex + ey * ey);
        }

        /// <summary>
        /// Adds piece-specific accent details (small circle on King's cross, small dots on Queen's crown tips, etc.)
        /// </summary>
        private static void AddPieceDetails(Color[] colors, int size, PieceType type, bool isWhite, Color outlineColor, Color highlightColor)
        {
            // Small decorative dot at crown tips or cross top
            switch (type)
            {
                case PieceType.King:
                    // Small circle at cross intersection
                    DrawCircle(colors, size, 0.50f, 0.75f, 0.025f, outlineColor);
                    break;
                case PieceType.Queen:
                    // Small circles at crown tips
                    DrawCircle(colors, size, 0.50f, 0.90f, 0.022f, outlineColor);
                    DrawCircle(colors, size, 0.58f, 0.82f, 0.018f, outlineColor);
                    DrawCircle(colors, size, 0.42f, 0.82f, 0.018f, outlineColor);
                    DrawCircle(colors, size, 0.68f, 0.75f, 0.016f, outlineColor);
                    DrawCircle(colors, size, 0.32f, 0.75f, 0.016f, outlineColor);
                    break;
                case PieceType.Bishop:
                    // Diagonal slit/notch on the mitre
                    DrawLine(colors, size, 0.44f, 0.60f, 0.56f, 0.72f, 0.015f, outlineColor);
                    // Small ball on top
                    DrawCircle(colors, size, 0.50f, 0.90f, 0.028f, outlineColor);
                    break;
                case PieceType.Knight:
                    // Eye
                    DrawCircle(colors, size, 0.38f, 0.66f, 0.022f, outlineColor);
                    break;
                case PieceType.Pawn:
                    // No extra details needed
                    break;
                case PieceType.Rook:
                    // No extra details needed - battlements are enough
                    break;
            }
        }

        /// <summary>
        /// Draws a filled circle on the pixel array at normalized position.
        /// </summary>
        private static void DrawCircle(Color[] colors, int size, float cx, float cy, float radius, Color color)
        {
            int minX = Mathf.Max(0, (int)((cx - radius) * size) - 1);
            int maxX = Mathf.Min(size - 1, (int)((cx + radius) * size) + 1);
            int minY = Mathf.Max(0, (int)((cy - radius) * size) - 1);
            int maxY = Mathf.Min(size - 1, (int)((cy + radius) * size) + 1);
            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float nx = (float)x / size;
                    float ny = (float)y / size;
                    float dist = Mathf.Sqrt((nx - cx) * (nx - cx) + (ny - cy) * (ny - cy));
                    if (dist < radius)
                    {
                        float aa = Mathf.Clamp01((radius - dist) / (1.5f / size));
                        colors[y * size + x] = Color.Lerp(colors[y * size + x], color, aa);
                    }
                }
            }
        }

        /// <summary>
        /// Draws a line segment on the pixel array between two normalized positions.
        /// </summary>
        private static void DrawLine(Color[] colors, int size, float x1, float y1, float x2, float y2, float thickness, Color color)
        {
            int margin = (int)(thickness * size) + 2;
            int minX = Mathf.Max(0, (int)(Mathf.Min(x1, x2) * size) - margin);
            int maxX = Mathf.Min(size - 1, (int)(Mathf.Max(x1, x2) * size) + margin);
            int minY = Mathf.Max(0, (int)(Mathf.Min(y1, y2) * size) - margin);
            int maxY = Mathf.Min(size - 1, (int)(Mathf.Max(y1, y2) * size) + margin);
            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float nx = (float)x / size;
                    float ny = (float)y / size;
                    float dist = DistanceToSegment(nx, ny, x1, y1, x2, y2);
                    if (dist < thickness)
                    {
                        float aa = Mathf.Clamp01((thickness - dist) / (1.5f / size));
                        colors[y * size + x] = Color.Lerp(colors[y * size + x], color, aa);
                    }
                }
            }
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

                    // Ground pedestal shadow under pieces (subtle, doesn't obscure the tile)
                    if (pieces != null)
                    {
                        for (int pIdx = 0; pIdx < pieces.Count; pIdx++)
                        {
                            var p = pieces[pIdx];
                            if (p.file == file && p.rank == rank)
                            {
                                float dx = normX - 0.5f;
                                float dy = (normY - 0.22f) / 0.55f;
                                float dist = dx * dx + dy * dy;
                                if (dist < 0.10f)
                                {
                                    float shadowA = Mathf.Clamp01((0.10f - dist) / 0.10f) * 0.35f;
                                    sqColor = Color.Lerp(sqColor, new Color(0.04f, 0.06f, 0.08f, 1f), shadowA);
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

            // Blit chess pieces directly into the chessboard texture
            if (pieces != null && pieces.Count > 0)
            {
                int piecePx = (int)(squarePx * 0.86f);
                int marginX = (squarePx - piecePx) / 2;
                int marginY = (squarePx - piecePx) / 2;

                for (int pIdx = 0; pIdx < pieces.Count; pIdx++)
                {
                    var p = pieces[pIdx];
                    if (p.file < 0 || p.file >= 8 || p.rank < 0 || p.rank >= 8) continue;

                    PieceType pType = GetPieceTypeFromSymbol(p.symbol);
                    Texture2D pTex = GetChessPiece2DTexture(pType, p.isWhite, piecePx);
                    if (pTex == null) continue;

                    int originX = p.file * squarePx + marginX;
                    int originY = p.rank * squarePx + marginY;

                    for (int py = 0; py < piecePx; py++)
                    {
                        float v = (float)py / Mathf.Max(1, piecePx - 1);
                        int by = originY + py;
                        if (by < 0 || by >= ss) continue;

                        for (int px = 0; px < piecePx; px++)
                        {
                            float u = (float)px / Mathf.Max(1, piecePx - 1);
                            int bx = originX + px;
                            if (bx < 0 || bx >= ss) continue;

                            try
                            {
                                Color src = pTex.GetPixelBilinear(u, v);
                                if (src.a > 0.01f)
                                {
                                    int idx = by * ss + bx;
                                    Color dst = colors[idx];
                                    Color blended = Color.Lerp(dst, src, src.a);
                                    blended.a = 1.0f;
                                    colors[idx] = blended;
                                }
                            }
                            catch
                            {
                                break;
                            }
                        }
                    }
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

        private static float RoundedRectDist(float px, float py, float minX, float minY, float maxX, float maxY, float r)
        {
            float cx = Mathf.Clamp(px, minX + r, maxX - r);
            float cy = Mathf.Clamp(py, minY + r, maxY - r);
            return Vector2.Distance(new Vector2(px, py), new Vector2(cx, cy)) - r;
        }

        private static float SegmentDist(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 pa = p - a;
            Vector2 ba = b - a;
            float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Vector2.Dot(ba, ba));
            return (pa - ba * h).magnitude;
        }


        /// <summary>
        /// Antialiased procedural Camera / Snapshot icon sprite.
        /// </summary>
        public static Sprite GetCameraIconSprite(int size = 64, Color? iconColor = null)
        {
            Color baseCol = iconColor ?? new Color(0.96f, 0.94f, 0.88f, 1f);
            string key = $"CameraIcon_{size}_{baseCol}";
            if (_proceduralCache.TryGetValue(key, out Sprite cached) && cached != null) return cached;

            int sw = size * PROC_SCALE;
            int sh = size * PROC_SCALE;
            float scale = (float)sw / 256f;

            Texture2D tex = new Texture2D(sw, sh, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;

            Color[] colors = new Color[sw * sh];

            for (int y = 0; y < sh; y++)
            {
                float py = (y + 0.5f) / scale;
                for (int x = 0; x < sw; x++)
                {
                    float px = (x + 0.5f) / scale;

                    // Camera main body: 38..218 (W: 180), 44..172 (H: 128), r = 22
                    float bodyDist = RoundedRectDist(px, py, 38f, 44f, 218f, 172f, 22f);
                    // Top viewfinder bump: 84..140 (W: 56), 168..204 (H: 36), r = 12
                    float bumpDist = RoundedRectDist(px, py, 84f, 168f, 140f, 204f, 12f);

                    float cameraDist = Mathf.Min(bodyDist, bumpDist);
                    if (cameraDist > 1.2f)
                    {
                        colors[y * sw + x] = Color.clear;
                        continue;
                    }

                    // Lens cutout groove around center (128, 108)
                    float dLens = Vector2.Distance(new Vector2(px, py), new Vector2(128f, 108f));
                    // Cutout ring between radius 30 and 44
                    float ringDist = Mathf.Abs(dLens - 37f) - 7f; // < 0 inside cutout ring

                    // Flash glass dot at (184, 146), radius 9
                    float dFlash = Vector2.Distance(new Vector2(px, py), new Vector2(184f, 146f)) - 9f;

                    // Calculate interior cutout mask (1 = solid, 0 = cutout)
                    float cutoutAlpha = 1f;
                    if (ringDist <= 1f)
                    {
                        cutoutAlpha = Mathf.Clamp01(ringDist);
                    }
                    if (dFlash <= 1f)
                    {
                        cutoutAlpha = Mathf.Min(cutoutAlpha, Mathf.Clamp01(dFlash));
                    }

                    // Outer antialiased alpha
                    float outerAlpha = Mathf.Clamp01(-cameraDist + 1f);
                    float finalAlpha = outerAlpha * cutoutAlpha;

                    Color c = baseCol;
                    // Subtle glass highlight reflection on top edge of lens
                    float dReflection = Vector2.Distance(new Vector2(px, py), new Vector2(136f, 116f));
                    if (dReflection <= 6f && dLens <= 26f)
                    {
                        c = Color.Lerp(c, Color.white, 0.45f);
                    }
                    c.a *= finalAlpha;
                    colors[y * sw + x] = c;
                }
            }

            tex.SetPixels(colors);
            tex.Apply();

            Sprite s = Sprite.Create(tex, new Rect(0, 0, sw, sh), new Vector2(0.5f, 0.5f), 100f * PROC_SCALE);
            _proceduralCache[key] = s;
            return s;
        }

        /// <summary>
        /// Antialiased procedural Video / Camcorder / Record icon sprite.
        /// </summary>
        public static Sprite GetVideoIconSprite(int size = 64, Color? iconColor = null)
        {
            Color baseCol = iconColor ?? new Color(0.96f, 0.94f, 0.88f, 1f);
            string key = $"VideoIcon_{size}_{baseCol}";
            if (_proceduralCache.TryGetValue(key, out Sprite cached) && cached != null) return cached;

            int sw = size * PROC_SCALE;
            int sh = size * PROC_SCALE;
            float scale = (float)sw / 256f;

            Texture2D tex = new Texture2D(sw, sh, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;

            Color[] colors = new Color[sw * sh];

            for (int y = 0; y < sh; y++)
            {
                float py = (y + 0.5f) / scale;
                for (int x = 0; x < sw; x++)
                {
                    float px = (x + 0.5f) / scale;

                    // Camera main body: 36..158 (W: 122), 64..192 (H: 128), r = 20
                    float bodyDist = RoundedRectDist(px, py, 36f, 64f, 158f, 192f, 20f);

                    // Lens cone / trapeze from x = 168 to 226
                    float coneDist = 999f;
                    if (px >= 166f && px <= 228f)
                    {
                        float t = Mathf.Clamp01((px - 168f) / 58f);
                        float halfH = Mathf.Lerp(26f, 54f, t);
                        float vertDist = Mathf.Abs(py - 128f) - halfH;
                        float horizDist = Mathf.Max(168f - px, px - 226f);
                        coneDist = Mathf.Max(vertDist, horizDist);
                    }

                    float videoDist = Mathf.Min(bodyDist, coneDist);
                    if (videoDist > 1.2f)
                    {
                        colors[y * sw + x] = Color.clear;
                        continue;
                    }

                    // Recording indicator circle cutout inside body at (97, 128)
                    float dCenter = Vector2.Distance(new Vector2(px, py), new Vector2(97f, 128f));
                    // Cutout ring between radius 20 and 32
                    float ringDist = Mathf.Abs(dCenter - 26f) - 6f; // < 0 inside cutout ring

                    float cutoutAlpha = (ringDist <= 1f) ? Mathf.Clamp01(ringDist) : 1f;
                    float outerAlpha = Mathf.Clamp01(-videoDist + 1f);
                    float finalAlpha = outerAlpha * cutoutAlpha;

                    Color c = baseCol;
                    // Center recording dot tinted with subtle crimson gold
                    if (dCenter <= 14f)
                    {
                        c = new Color(0.95f, 0.35f, 0.38f, 1f);
                    }
                    c.a *= finalAlpha;
                    colors[y * sw + x] = c;
                }
            }

            tex.SetPixels(colors);
            tex.Apply();

            Sprite s = Sprite.Create(tex, new Rect(0, 0, sw, sh), new Vector2(0.5f, 0.5f), 100f * PROC_SCALE);
            _proceduralCache[key] = s;
            return s;
        }

        /// <summary>
        /// Antialiased procedural Settings Gear icon sprite.
        /// </summary>
        public static Sprite GetSettingsGearIconSprite(int size = 64, Color? iconColor = null)
        {
            Color baseCol = iconColor ?? new Color(0.96f, 0.94f, 0.88f, 1f);
            string key = $"GearIcon_{size}_{baseCol}";
            if (_proceduralCache.TryGetValue(key, out Sprite cached) && cached != null) return cached;

            int sw = size * PROC_SCALE;
            int sh = size * PROC_SCALE;
            float scale = (float)sw / 256f;

            Texture2D tex = new Texture2D(sw, sh, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;

            Color[] colors = new Color[sw * sh];

            for (int y = 0; y < sh; y++)
            {
                float py = (y + 0.5f) / scale;
                for (int x = 0; x < sw; x++)
                {
                    float px = (x + 0.5f) / scale;

                    float dx = px - 128f;
                    float dy = py - 128f;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    if (dist > 105f)
                    {
                        colors[y * sw + x] = Color.clear;
                        continue;
                    }

                    float angle = Mathf.Atan2(dy, dx);
                    // 6-teeth gear profile
                    float toothWave = Mathf.Cos(6f * angle);
                    float toothProfile = Mathf.SmoothStep(-0.25f, 0.25f, toothWave);
                    float rOuter = Mathf.Lerp(76f, 98f, toothProfile);
                    float rHole = 30f;

                    float outerDist = dist - rOuter;
                    float holeDist = rHole - dist;

                    if (outerDist > 1.2f || holeDist > 1.2f)
                    {
                        colors[y * sw + x] = Color.clear;
                        continue;
                    }

                    float outerAlpha = Mathf.Clamp01(-outerDist + 1f);
                    float holeAlpha = Mathf.Clamp01(-holeDist + 1f);
                    float alpha = outerAlpha * holeAlpha;

                    Color c = baseCol;
                    // Groove ring at radius 50..54
                    float grooveDist = Mathf.Abs(dist - 52f) - 2f;
                    if (grooveDist <= 1f)
                    {
                        alpha *= Mathf.Clamp01(grooveDist);
                    }

                    c.a *= alpha;
                    colors[y * sw + x] = c;
                }
            }

            tex.SetPixels(colors);
            tex.Apply();

            Sprite s = Sprite.Create(tex, new Rect(0, 0, sw, sh), new Vector2(0.5f, 0.5f), 100f * PROC_SCALE);
            _proceduralCache[key] = s;
            return s;
        }

        /// <summary>
        /// Antialiased procedural Crosshair / Reticle icon sprite for Exit AR.
        /// </summary>
        public static Sprite GetCrosshairIconSprite(int size = 64, Color? iconColor = null)
        {
            Color baseCol = iconColor ?? new Color(0.96f, 0.94f, 0.88f, 1f);
            string key = $"CrosshairIcon_{size}_{baseCol}";
            if (_proceduralCache.TryGetValue(key, out Sprite cached) && cached != null) return cached;

            int sw = size * PROC_SCALE;
            int sh = size * PROC_SCALE;
            float scale = (float)sw / 256f;

            Texture2D tex = new Texture2D(sw, sh, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;

            Color[] colors = new Color[sw * sh];

            for (int y = 0; y < sh; y++)
            {
                float py = (y + 0.5f) / scale;
                for (int x = 0; x < sw; x++)
                {
                    float px = (x + 0.5f) / scale;

                    float dx = px - 128f;
                    float dy = py - 128f;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    // Circular ring between radius 64 and 80
                    float ringDist = Mathf.Abs(dist - 72f) - 8f;

                    // Center dot radius 14
                    float dotDist = dist - 14f;

                    // 4 Crosshair ticks (W: 10, extending from radius 56 to 98)
                    float tickX = Mathf.Abs(dx) - 5f;
                    float tickY = Mathf.Abs(dy) - 5f;
                    float tickHoriz = Mathf.Max(tickY, Mathf.Abs(dx - 77f * Mathf.Sign(dx)) - 21f);
                    float tickVert = Mathf.Max(tickX, Mathf.Abs(dy - 77f * Mathf.Sign(dy)) - 21f);
                    float ticksDist = Mathf.Min(tickHoriz, tickVert);

                    float totalDist = Mathf.Min(ringDist, Mathf.Min(dotDist, ticksDist));

                    if (totalDist > 1.2f)
                    {
                        colors[y * sw + x] = Color.clear;
                        continue;
                    }

                    float alpha = Mathf.Clamp01(-totalDist + 1f);
                    Color c = baseCol;
                    c.a *= alpha;
                    colors[y * sw + x] = c;
                }
            }

            tex.SetPixels(colors);
            tex.Apply();

            Sprite s = Sprite.Create(tex, new Rect(0, 0, sw, sh), new Vector2(0.5f, 0.5f), 100f * PROC_SCALE);
            _proceduralCache[key] = s;
            return s;
        }

        /// <summary>
        /// Antialiased procedural Pause (two rounded bars) icon sprite.
        /// </summary>
        public static Sprite GetPauseIconSprite(int size = 64, Color? iconColor = null)
        {
            Color baseCol = iconColor ?? new Color(0.96f, 0.94f, 0.88f, 1f);
            string key = $"PauseIcon_{size}_{baseCol}";
            if (_proceduralCache.TryGetValue(key, out Sprite cached) && cached != null) return cached;

            int sw = size * PROC_SCALE;
            int sh = size * PROC_SCALE;
            float scale = (float)sw / 256f;

            Texture2D tex = new Texture2D(sw, sh, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;

            Color[] colors = new Color[sw * sh];

            for (int y = 0; y < sh; y++)
            {
                float py = (y + 0.5f) / scale;
                for (int x = 0; x < sw; x++)
                {
                    float px = (x + 0.5f) / scale;

                    // Left vertical rounded bar: X 72..110, Y 58..198, radius 10
                    float dLeft = RoundedRectDist(px, py, 72f, 58f, 110f, 198f, 10f);
                    // Right vertical rounded bar: X 146..184, Y 58..198, radius 10
                    float dRight = RoundedRectDist(px, py, 146f, 58f, 184f, 198f, 10f);

                    float dist = Mathf.Min(dLeft, dRight);
                    if (dist > 1.2f)
                    {
                        colors[y * sw + x] = Color.clear;
                        continue;
                    }

                    float alpha = Mathf.Clamp01(-dist + 1f);
                    Color c = baseCol;
                    c.a *= alpha;
                    colors[y * sw + x] = c;
                }
            }

            tex.SetPixels(colors);
            tex.Apply();

            Sprite s = Sprite.Create(tex, new Rect(0, 0, sw, sh), new Vector2(0.5f, 0.5f), 100f * PROC_SCALE);
            _proceduralCache[key] = s;
            return s;
        }

        /// <summary>
        /// Antialiased procedural Hint (illuminating idea lightbulb) icon sprite.
        /// </summary>
        public static Sprite GetHintIconSprite(int size = 64, Color? iconColor = null)
        {
            Color baseCol = iconColor ?? new Color(0.96f, 0.94f, 0.88f, 1f);
            string key = $"HintIcon_{size}_{baseCol}";
            if (_proceduralCache.TryGetValue(key, out Sprite cached) && cached != null) return cached;

            int sw = size * PROC_SCALE;
            int sh = size * PROC_SCALE;
            float scale = (float)sw / 256f;

            Texture2D tex = new Texture2D(sw, sh, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;

            Color[] colors = new Color[sw * sh];

            for (int y = 0; y < sh; y++)
            {
                float py = (y + 0.5f) / scale;
                for (int x = 0; x < sw; x++)
                {
                    float px = (x + 0.5f) / scale;

                    // 1. Bulb sphere head at (128, 144), radius 48
                    float dCircle = Vector2.Distance(new Vector2(px, py), new Vector2(128f, 144f)) - 48f;

                    // 2. Bulb neck tapering downwards
                    float dNeck = 999f;
                    if (py >= 94f && py <= 130f)
                    {
                        float t = (py - 94f) / 36f;
                        float halfW = Mathf.Lerp(26f, 44f, t);
                        float dX = Mathf.Abs(px - 128f) - halfW;
                        float dY = Mathf.Max(94f - py, py - 130f);
                        dNeck = Mathf.Max(dX, dY);
                    }

                    // 3. Threaded screw base at bottom: X 104..152, Y 72..94, radius 6
                    float dBase = RoundedRectDist(px, py, 104f, 72f, 152f, 94f, 6f);

                    // 4. Contact tip at bottom: X 116..140, Y 56..68, radius 5
                    float dTip = RoundedRectDist(px, py, 116f, 56f, 140f, 68f, 5f);

                    float bulbDist = Mathf.Min(dCircle, Mathf.Min(dNeck, Mathf.Min(dBase, dTip)));

                    // 5. Radiance rays radiating around the bulb head
                    float dRayTop = RoundedRectDist(px, py, 124f, 210f, 132f, 226f, 4f);
                    float dRayL = SegmentDist(new Vector2(px, py), new Vector2(60f, 196f), new Vector2(78f, 178f)) - 4.5f;
                    float dRayR = SegmentDist(new Vector2(px, py), new Vector2(196f, 196f), new Vector2(178f, 178f)) - 4.5f;

                    float totalDist = Mathf.Min(bulbDist, Mathf.Min(dRayTop, Mathf.Min(dRayL, dRayR)));
                    if (totalDist > 1.2f)
                    {
                        colors[y * sw + x] = Color.clear;
                        continue;
                    }

                    float alpha = Mathf.Clamp01(-totalDist + 1f);

                    // Screw thread cutout grooves at py around 78 and 86
                    if ((py >= 77f && py <= 79f) || (py >= 85f && py <= 87f))
                    {
                        if (Mathf.Abs(px - 128f) <= 24f)
                        {
                            alpha *= 0.25f;
                        }
                    }

                    Color c = baseCol;
                    c.a *= alpha;
                    colors[y * sw + x] = c;
                }
            }

            tex.SetPixels(colors);
            tex.Apply();

            Sprite s = Sprite.Create(tex, new Rect(0, 0, sw, sh), new Vector2(0.5f, 0.5f), 100f * PROC_SCALE);
            _proceduralCache[key] = s;
            return s;
        }

        /// <summary>
        /// Antialiased procedural Undo (counter-clockwise circular return arrow) icon sprite.
        /// </summary>
        public static Sprite GetUndoIconSprite(int size = 64, Color? iconColor = null)
        {
            Color baseCol = iconColor ?? new Color(0.96f, 0.94f, 0.88f, 1f);
            string key = $"UndoIcon_{size}_{baseCol}";
            if (_proceduralCache.TryGetValue(key, out Sprite cached) && cached != null) return cached;

            int sw = size * PROC_SCALE;
            int sh = size * PROC_SCALE;
            float scale = (float)sw / 256f;

            Texture2D tex = new Texture2D(sw, sh, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;

            Color[] colors = new Color[sw * sh];

            for (int y = 0; y < sh; y++)
            {
                float py = (y + 0.5f) / scale;
                for (int x = 0; x < sw; x++)
                {
                    float px = (x + 0.5f) / scale;

                    // 1. Circular arc ring: Center at (136, 120), radius 54, thickness 18 (half-thickness 9)
                    float dCenter = Vector2.Distance(new Vector2(px, py), new Vector2(136f, 120f));
                    float dRing = Mathf.Abs(dCenter - 54f) - 9f;

                    float angDeg = Mathf.Atan2(py - 120f, px - 136f) * Mathf.Rad2Deg;
                    float dArc = (angDeg >= -55f && angDeg <= 145f) ? dRing : 999f;

                    // Rounded cap at bottom-right tail of arc (-55 deg)
                    float dCapEnd = Vector2.Distance(new Vector2(px, py), new Vector2(167f, 76f)) - 9f;
                    float arcDist = Mathf.Min(dArc, dCapEnd);

                    // 2. Bold arrow head pointing left, tip at (52, 142), base at x = 90
                    float dTri = Mathf.Max(px - 90f, -(0.644f * (px - 52f) - 0.765f * Mathf.Abs(py - 142f)));

                    float totalDist = Mathf.Min(arcDist, dTri);
                    if (totalDist > 1.2f)
                    {
                        colors[y * sw + x] = Color.clear;
                        continue;
                    }

                    float alpha = Mathf.Clamp01(-totalDist + 1f);
                    Color c = baseCol;
                    c.a *= alpha;
                    colors[y * sw + x] = c;
                }
            }

            tex.SetPixels(colors);
            tex.Apply();

            Sprite s = Sprite.Create(tex, new Rect(0, 0, sw, sh), new Vector2(0.5f, 0.5f), 100f * PROC_SCALE);
            _proceduralCache[key] = s;
            return s;
        }
    }
}
