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
                        // Border zone - blend border with fill at inner edge
                        float borderFade = Mathf.Clamp01((-dist) / sbw); // 0 at outer edge, 1 at inner edge
                        Color borderCol = borderColor;
                        borderCol.a *= alpha;

                        // At the very inner edge of border, blend with fill
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
                        // Interior fill - add subtle vertical gradient for glass effect
                        float gradientT = (float)y / sh;
                        Color f = fillColor;
                        // Slightly brighter at top for glass reflection effect
                        f.r += 0.02f * (1f - gradientT);
                        f.g += 0.02f * (1f - gradientT);
                        f.b += 0.03f * (1f - gradientT);
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
        /// Procedural 2D Chessboard diagram texture with highlighted squares for Tutorial & Learn screens.
        /// </summary>
        public static Sprite GetMiniChessboardDiagram(int size = 256)
        {
            string key = $"MiniChessboard_{size}";
            if (_proceduralCache.TryGetValue(key, out Sprite cached) && cached != null) return cached;

            // Use 2x for chessboard to keep it crisp but not too large
            int ss = size * 2;
            Texture2D tex = new Texture2D(ss, ss, TextureFormat.RGBA32, true);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;

            Color lightSquare = new Color(0.18f, 0.32f, 0.48f, 1f); // Arcane light slate blue
            Color darkSquare = new Color(0.08f, 0.16f, 0.28f, 1f);  // Arcane deep navy
            Color highlightSquare = new Color(0.95f, 0.75f, 0.15f, 0.85f); // Gold highlight
            Color moveSquare = new Color(0.0f, 0.85f, 0.95f, 0.8f); // Cyan move highlight

            Color[] colors = new Color[ss * ss];
            int squarePx = ss / 8;

            for (int y = 0; y < ss; y++)
            {
                int rank = y / squarePx;
                for (int x = 0; x < ss; x++)
                {
                    int file = x / squarePx;
                    bool isLight = (file + rank) % 2 != 0;

                    Color sqColor = isLight ? lightSquare : darkSquare;

                    // Highlight e4, e5, f3 squares for tutorial
                    if (file == 4 && (rank == 3 || rank == 4))
                    {
                        sqColor = Color.Lerp(sqColor, highlightSquare, 0.6f);
                    }
                    else if (file == 5 && rank == 2)
                    {
                        sqColor = Color.Lerp(sqColor, moveSquare, 0.7f);
                    }

                    // Border grid line (thinner, more subtle)
                    bool isBorderX = (x % squarePx == 0) || (x == ss - 1);
                    bool isBorderY = (y % squarePx == 0) || (y == ss - 1);
                    if (isBorderX || isBorderY)
                    {
                        sqColor = Color.Lerp(sqColor, new Color(0.0f, 0.75f, 1.0f, 0.5f), 0.4f);
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
