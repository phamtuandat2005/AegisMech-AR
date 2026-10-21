using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AegisMech.UI.EditorTools
{
    /// <summary>Builds the Aegis menu scene (screens 1-7 + popup/toast). Menu: Aegis/Build Menu UI Scene.</summary>
    public static class AegisUIBuilder
    {
        const string Art = "Assets/Art/AegisUI";
        const string Res = "Assets/Resources/Textures/ArcaneUI/";
        const string ScenePath = "Assets/Scenes/AegisMenu.unity";

        static Dictionary<string, Sprite> S;
        static Font F;
        static List<AegisScreen> Screens;

        [MenuItem("Aegis/Build Menu UI Scene")]
        static void BuildMenu() { Debug.Log(BuildAll()); }

        [MenuItem("Aegis/Capture Main Menu Screenshot", false, 50)]
        public static void CaptureScreenshot()
        {
            var canvas = GameObject.Find("AegisUIRoot")?.GetComponent<Canvas>();
            if (canvas == null) canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogWarning("[AegisUIBuilder] No canvas found. Building menu first...");
                BuildAll();
                canvas = GameObject.Find("AegisUIRoot")?.GetComponent<Canvas>();
            }

            Camera cam = Camera.main;
            bool createdCam = false;
            if (cam == null)
            {
                var camObj = new GameObject("TempCaptureCam");
                cam = camObj.AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.027f, 0.051f, 0.094f, 1f);
                createdCam = true;
            }

            int width = 2130;
            int height = 978;
            RenderTexture rt = new RenderTexture(width, height, 24);
            RenderTexture prevRT = RenderTexture.active;
            RenderTexture.active = rt;

            var prevTarget = cam.targetTexture;
            cam.targetTexture = rt;
            cam.Render();

            Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();

            cam.targetTexture = prevTarget;
            RenderTexture.active = prevRT;
            Object.DestroyImmediate(rt);
            if (createdCam) Object.DestroyImmediate(cam.gameObject);

            byte[] bytes = tex.EncodeToPNG();
            Object.DestroyImmediate(tex);

            string outDir = Path.Combine(Directory.GetCurrentDirectory(), "Reference");
            if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);
            string outFile = Path.Combine(outDir, "Current_Screenshot_HD.png");
            File.WriteAllBytes(outFile, bytes);
            Debug.Log("[AegisUIBuilder] Screenshot successfully captured and saved to: " + outFile);
        }

        // ------------------------------------------------------------------ sprites
        static Sprite Save(string name, Texture2D tex, Vector4 border)
        {
            string path = Art + "/" + name + ".png";
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.filterMode = FilterMode.Bilinear;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.spritePixelsPerUnit = 100f;
            ti.spriteBorder = border;
            ti.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static Sprite MakeSprite(string name, int w, int h, float r, Color fill, Color border, float bw, Color glow, int g, bool slice, bool grad)
        {
            int W = w + 2 * g, H = h + 2 * g;
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            var px = new Color[W * H];
            float hw = w * 0.5f, hh = h * 0.5f, cx = W * 0.5f, cy = H * 0.5f;
            for (int y = 0; y < H; y++)
            {
                for (int x = 0; x < W; x++)
                {
                    float ppx = x + 0.5f - cx, ppy = y + 0.5f - cy;
                    float qx = Mathf.Abs(ppx) - (hw - r), qy = Mathf.Abs(ppy) - (hh - r);
                    float d = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude + Mathf.Min(Mathf.Max(qx, qy), 0f) - r;
                    float cover = Mathf.Clamp01(0.5f - d);
                    float band = bw > 0f ? Mathf.Clamp01(d + bw + 0.5f) : 0f;
                    float tv = grad ? Mathf.Clamp01((float)y / H) : 1f;
                    float k = Mathf.Lerp(0.7f, 1f, tv);
                    Color f = new Color(fill.r * k, fill.g * k, fill.b * k, fill.a);
                    Color s = Color.Lerp(f, border, band);
                    s.a *= cover;
                    float ga = 0f;
                    if (g > 0 && d > 0f) ga = glow.a * Mathf.Exp(-d / (g * 0.4f)) * Mathf.Clamp01(1f - d / g);
                    float a = s.a + ga * (1f - s.a);
                    Color o = Color.clear;
                    if (a > 0.0001f)
                    {
                        float gw = ga * (1f - s.a);
                        o = new Color((s.r * s.a + glow.r * gw) / a, (s.g * s.a + glow.g * gw) / a, (s.b * s.a + glow.b * gw) / a, a);
                    }
                    px[y * W + x] = o;
                }
            }
            tex.SetPixels(px);
            tex.Apply();
            Vector4 b4 = Vector4.zero;
            if (slice)
            {
                int b = Mathf.CeilToInt(g + r) + 1;
                int bv = Mathf.Min(b, H / 2 - 1), bh = Mathf.Min(b, W / 2 - 1);
                b4 = new Vector4(bh, bv, bh, bv);
            }
            return Save(name, tex, b4);
        }

        static Sprite MakeGrad(string name, bool horizontal, float a0, float a1)
        {
            int W = horizontal ? 128 : 4, H = horizontal ? 4 : 128;
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            var px = new Color[W * H];
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float t = horizontal ? x / (float)(W - 1) : y / (float)(H - 1);
                    px[y * W + x] = new Color(0.01f, 0.03f, 0.07f, Mathf.Lerp(a0, a1, t));
                }
            tex.SetPixels(px);
            tex.Apply();
            return Save(name, tex, Vector4.zero);
        }

        static void Disc(Color[] px, int W, float cx, float cy, float r, Color c)
        {
            int x0 = Mathf.Max(0, (int)(cx - r - 2f)), x1 = Mathf.Min(W - 1, (int)(cx + r + 2f));
            int y0 = Mathf.Max(0, (int)(cy - r - 2f)), y1 = Mathf.Min(W - 1, (int)(cy + r + 2f));
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy)) - r;
                    float a = Mathf.Clamp01(0.5f - d) * c.a;
                    if (a <= 0f) continue;
                    px[y * W + x] = Color.Lerp(px[y * W + x], new Color(c.r, c.g, c.b, 1f), a);
                }
        }

        static void Piece(Color[] px, int W, int sx, int sy, Color c, float body, float head)
        {
            float cx = sx * 64 + 32, cy = sy * 64 + 26;
            Disc(px, W, cx, cy, body + 2f, new Color(0.02f, 0.05f, 0.10f, 0.85f));
            Disc(px, W, cx, cy, body, c);
            Disc(px, W, cx, cy + body * 0.9f, head + 1.5f, new Color(0.02f, 0.05f, 0.10f, 0.85f));
            Disc(px, W, cx, cy + body * 0.9f, head, Color.Lerp(c, Color.white, 0.35f));
        }

        static Sprite MakeBoard()
        {
            const int N = 8, Sq = 64, W = N * Sq;
            var tex = new Texture2D(W, W, TextureFormat.RGBA32, false);
            var px = new Color[W * W];
            Color light = new Color(0.80f, 0.90f, 0.97f, 1f), dark = new Color(0.14f, 0.30f, 0.46f, 1f);
            for (int y = 0; y < W; y++)
                for (int x = 0; x < W; x++)
                    px[y * W + x] = ((x / Sq + y / Sq) % 2 == 1) ? light : dark;
            // selected square (d2)
            for (int y = Sq; y < 2 * Sq; y++)
                for (int x = 3 * Sq; x < 4 * Sq; x++)
                    px[y * W + x] = Color.Lerp(px[y * W + x], new Color(0.3f, 0.95f, 0.55f, 1f), 0.55f);
            Color blue = new Color(0.35f, 0.72f, 1f), red = new Color(1f, 0.38f, 0.42f);
            for (int sx = 0; sx < N; sx++)
            {
                Piece(px, W, sx, 1, blue, 17f, 9f);
                Piece(px, W, sx, 6, red, 17f, 9f);
                Piece(px, W, sx, 0, blue, 22f, 11f);
                Piece(px, W, sx, 7, red, 22f, 11f);
            }
            // valid-move markers
            Disc(px, W, 3 * Sq + 32, 2 * Sq + 32, 11f, new Color(0.3f, 0.85f, 1f, 0.95f));
            Disc(px, W, 3 * Sq + 32, 3 * Sq + 32, 11f, new Color(0.3f, 0.85f, 1f, 0.95f));
            tex.SetPixels(px);
            tex.Apply();
            return Save("board_preview", tex, Vector4.zero);
        }

        static void BuildSprites()
        {
            S = new Dictionary<string, Sprite>();
            Color cy = AegisTheme.Cyan;
            Color clear = new Color(0f, 0f, 0f, 0f);
            S["panel"] = MakeSprite("panel", 96, 96, 14f, new Color(0.035f, 0.08f, 0.15f, 0.88f), new Color(cy.r, cy.g, cy.b, 0.55f), 1.6f, new Color(cy.r, cy.g, cy.b, 0.25f), 8, true, true);
            S["panel_hi"] = MakeSprite("panel_hi", 96, 96, 14f, new Color(0.05f, 0.14f, 0.24f, 0.93f), new Color(0.5f, 0.9f, 1f, 1f), 2.2f, new Color(cy.r, cy.g, cy.b, 0.6f), 10, true, true);
            S["panel_flat"] = MakeSprite("panel_flat", 64, 64, 10f, new Color(0.03f, 0.07f, 0.13f, 0.75f), new Color(0.3f, 0.6f, 0.9f, 0.30f), 1.2f, clear, 0, true, false);
            S["button"] = MakeSprite("button", 96, 64, 12f, new Color(0.07f, 0.34f, 0.54f, 0.97f), new Color(0.55f, 0.93f, 1f, 1f), 2f, new Color(cy.r, cy.g, cy.b, 0.55f), 8, true, true);
            S["button_gold"] = MakeSprite("button_gold", 96, 64, 12f, new Color(0.55f, 0.33f, 0.05f, 0.97f), new Color(1f, 0.82f, 0.38f, 1f), 2f, new Color(1f, 0.75f, 0.2f, 0.55f), 8, true, true);
            S["chip"] = MakeSprite("chip", 64, 48, 24f, new Color(0.03f, 0.07f, 0.13f, 0.88f), new Color(cy.r, cy.g, cy.b, 0.55f), 1.6f, clear, 0, true, false);
            S["circle"] = MakeSprite("circle", 64, 64, 32f, new Color(0.03f, 0.08f, 0.15f, 0.92f), new Color(cy.r, cy.g, cy.b, 0.8f), 2f, clear, 0, false, false);
            S["knob"] = MakeSprite("knob", 64, 64, 32f, Color.white, Color.white, 0f, clear, 0, false, false);
            S["switch_track"] = MakeSprite("switch_track", 96, 48, 24f, Color.white, Color.white, 0f, clear, 0, true, false);
            S["fill"] = MakeSprite("fill", 32, 32, 8f, Color.white, Color.white, 0f, clear, 0, true, false);
            S["solid"] = MakeSprite("solid", 8, 8, 0f, Color.white, Color.white, 0f, clear, 0, false, false);
            S["grad_left"] = MakeGrad("grad_left", true, 0.92f, 0f);
            S["grad_bottom"] = MakeGrad("grad_bottom", false, 0.95f, 0f);
            S["board_preview"] = MakeBoard();
        }

        // ------------------------------------------------------------------ ui helpers
        static RectTransform Rt(string name, Transform parent, float x0, float y0, float x1, float y1)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(x0, y0);
            rt.anchorMax = new Vector2(x1, y1);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return rt;
        }

        static RectTransform Pt(string name, Transform parent, float ax, float ay, float w, float h, float x = 0f, float y = 0f, float px = 0.5f, float py = 0.5f)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(ax, ay);
            rt.pivot = new Vector2(px, py);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(x, y);
            return rt;
        }

        static Image Im(RectTransform rt, string sprite, Color c, bool raycast = false)
        {
            var im = rt.gameObject.AddComponent<Image>();
            if (sprite != null)
            {
                if (S != null && S.TryGetValue(sprite, out Sprite spr))
                {
                    im.sprite = spr;
                    im.type = spr.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
                }
                else
                {
                    var loaded = AssetDatabase.LoadAssetAtPath<Sprite>(Res + sprite + ".png");
                    if (loaded != null) im.sprite = loaded;
                }
            }
            im.color = c;
            im.raycastTarget = raycast;
            return im;
        }

        static Text Tx(RectTransform rt, string s, int size, Color c, TextAnchor a, FontStyle fs = FontStyle.Normal)
        {
            var t = rt.gameObject.AddComponent<Text>();
            t.font = F;
            t.text = s;
            t.fontSize = size;
            t.color = c;
            t.alignment = a;
            t.fontStyle = fs;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            t.supportRichText = true;
            return t;
        }

        static Button Btn(RectTransform rt, string sprite, Color tint, string label, int size, Color txt)
        {
            var im = Im(rt, sprite, tint, true);
            var b = rt.gameObject.AddComponent<Button>();
            b.targetGraphic = im;
            b.transition = Selectable.Transition.None;
            rt.gameObject.AddComponent<AegisButtonFx>();
            if (!string.IsNullOrEmpty(label)) Tx(Rt("Label", rt, 0f, 0f, 1f, 1f), label, size, txt, TextAnchor.MiddleCenter, FontStyle.Bold);
            return b;
        }

        static void Nav(Button b, AegisScreenId target)
        {
            var n = b.gameObject.AddComponent<AegisNavButton>();
            n.target = target;
        }

        static RawImage Raw(RectTransform rt, string texName)
        {
            var raw = rt.gameObject.AddComponent<RawImage>();
            raw.texture = AssetDatabase.LoadAssetAtPath<Texture2D>(Res + texName + ".png");
            raw.raycastTarget = false;
            return raw;
        }

        static void Cover(string name, Transform parent, string texName)
        {
            var rt = Pt(name, parent, 0.5f, 0.5f, 0f, 0f);
            var raw = Raw(rt, texName);
            var fit = rt.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fit.aspectRatio = raw.texture != null ? raw.texture.width / (float)raw.texture.height : 1.78f;
        }

        static void Backdrop(Transform p)
        {
            Cover("Bg", p, "splash_bg");
            Im(Rt("Dim", p, 0f, 0f, 1f, 1f), "solid", new Color(0.01f, 0.03f, 0.07f, 0.86f), true);
        }

        static T MakeScreen<T>(Transform parent, string name, AegisScreenId id) where T : AegisScreen
        {
            var rt = Rt(name, parent, 0f, 0f, 1f, 1f);
            rt.gameObject.AddComponent<CanvasGroup>();
            var s = rt.gameObject.AddComponent<T>();
            s.id = id;
            Screens.Add(s);
            return s;
        }

        static void TopBar(Transform p, string title, bool back, bool gear)
        {
            if (back)
            {
                var b = Btn(Pt("Back", p, 0f, 1f, 72f, 72f, 50f, -50f, 0f, 1f), "circle", Color.white, "<", 38, AegisTheme.CyanBright);
                Nav(b, AegisScreenId.Back);
            }
            Tx(Pt("Title", p, 0.5f, 1f, 1000f, 90f, 0f, -50f, 0.5f, 1f), title, 52, AegisTheme.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            Im(Pt("TitleBar", p, 0.5f, 1f, 420f, 4f, 0f, -146f, 0.5f, 1f), "solid", AegisTheme.Cyan);
            if (gear)
            {
                var g = Btn(Pt("Gear", p, 1f, 1f, 72f, 72f, -50f, -50f, 1f, 1f), "circle", Color.white, "=", 38, AegisTheme.CyanBright);
                Nav(g, AegisScreenId.Settings);
            }
        }

        static void PanelHeader(RectTransform panel, string title)
        {
            var back = Btn(Pt("Back", panel, 0f, 1f, 60f, 60f, 24f, -22f, 0f, 1f), "circle", Color.white, "<", 32, AegisTheme.CyanBright);
            Nav(back, AegisScreenId.Back);
            var t = Tx(Rt("Title", panel, 0f, 0.885f, 0.8f, 0.995f), title, 40, AegisTheme.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            t.rectTransform.offsetMin = new Vector2(104f, 0f);
            var close = Btn(Pt("Close", panel, 1f, 1f, 60f, 60f, -24f, -22f, 1f, 1f), "circle", Color.white, "X", 28, AegisTheme.CyanBright);
            Nav(close, AegisScreenId.Back);
            var line = Rt("Divider", panel, 0.02f, 0.885f, 0.98f, 0.885f);
            line.offsetMin = new Vector2(0f, -1f);
            line.offsetMax = new Vector2(0f, 1f);
            Im(line, "solid", new Color(AegisTheme.Cyan.r, AegisTheme.Cyan.g, AegisTheme.Cyan.b, 0.35f));
        }

        static AegisTabs MakeTopTabs(RectTransform pn, string[] names, float step, float w, float y0, float y1, GameObject[] panels)
        {
            var host = Rt("Tabs", pn, 0f, 0f, 1f, 1f);
            var btns = new Button[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                float x0 = 0.03f + i * step;
                var tr = Rt("Tab_" + i, host, x0, y0, x0 + w, y1);
                btns[i] = Btn(tr, i == 0 ? "button" : "panel_flat", Color.white, names[i], 28, i == 0 ? Color.white : AegisTheme.Muted);
            }
            var tabs = host.gameObject.AddComponent<AegisTabs>();
            tabs.tabs = btns;
            tabs.panels = panels;
            tabs.onSprite = S["button"];
            tabs.offSprite = S["panel_flat"];
            tabs.selected = 0;
            return tabs;
        }

        static RectTransform NewRow(RectTransform content, int i, string label)
        {
            float y1 = 1f - i * 0.16f;
            var r = Rt("Row_" + i, content, 0f, y1 - 0.15f, 1f, y1);
            var line = Rt("Line", r, 0f, 0f, 1f, 0f);
            line.offsetMin = Vector2.zero;
            line.offsetMax = new Vector2(0f, 2f);
            Im(line, "solid", new Color(AegisTheme.Cyan.r, AegisTheme.Cyan.g, AegisTheme.Cyan.b, 0.15f));
            Tx(Rt("Label", r, 0f, 0f, 0.55f, 1f), label, 30, AegisTheme.Ink, TextAnchor.MiddleLeft);
            return r;
        }

        static void AddSwitch(RectTransform row, string key, bool def)
        {
            var sw = Pt("Switch", row, 1f, 0.5f, 110f, 54f, -10f, 0f, 1f, 0.5f);
            var track = Im(sw, "switch_track", AegisTheme.Cyan, true);
            var tg = sw.gameObject.AddComponent<Toggle>();
            tg.targetGraphic = track;
            tg.transition = Selectable.Transition.None;
            tg.isOn = def;
            var knob = Pt("Knob", sw, 0.75f, 0.5f, 42f, 42f);
            Im(knob, "knob", Color.white);
            var a = sw.gameObject.AddComponent<AegisSwitch>();
            a.prefKey = key;
            a.defaultValue = def;
            a.track = track;
            a.knob = knob;
        }

        static AegisChoice AddChoice(RectTransform row, string key, string[] opts, int def)
        {
            var cr = Pt("Choice", row, 1f, 0.5f, 330f, 58f, -10f, 0f, 1f, 0.5f);
            Btn(cr, "chip", Color.white, null, 0, Color.white);
            var lbl = Tx(Rt("Label", cr, 0f, 0f, 1f, 1f), opts[def], 28, AegisTheme.Ink, TextAnchor.MiddleLeft);
            lbl.rectTransform.offsetMin = new Vector2(24f, 0f);
            var chev = Tx(Rt("Chevron", cr, 0f, 0f, 1f, 1f), "v", 24, AegisTheme.Cyan, TextAnchor.MiddleRight);
            chev.rectTransform.offsetMax = new Vector2(-22f, 0f);
            var ch = cr.gameObject.AddComponent<AegisChoice>();
            ch.prefKey = key;
            ch.options = opts;
            ch.defaultIndex = def;
            ch.index = def;
            ch.label = lbl;
            return ch;
        }

        static Toggle RadioRow(Transform parent, string name, string label, float y0, float y1, ToggleGroup grp, bool on)
        {
            var row = Rt(name, parent, 0f, y0, 1f, y1);
            Im(row, null, new Color(0f, 0f, 0f, 0f), true);
            var ring = Pt("Ring", row, 0f, 0.5f, 38f, 38f, 0f, 0f, 0f, 0.5f);
            var ringImg = Im(ring, "circle", Color.white, true);
            var dotImg = Im(Pt("Dot", ring, 0.5f, 0.5f, 20f, 20f), "knob", AegisTheme.Cyan);
            var t = row.gameObject.AddComponent<Toggle>();
            t.targetGraphic = ringImg;
            t.graphic = dotImg;
            t.transition = Selectable.Transition.None;
            t.group = grp;
            t.isOn = on;
            var lt = Tx(Rt("Label", row, 0f, 0f, 1f, 1f), label, 28, AegisTheme.Ink, TextAnchor.MiddleLeft);
            lt.rectTransform.offsetMin = new Vector2(56f, 0f);
            return t;
        }

        // ------------------------------------------------------------------ build
        public static string BuildAll()
        {
            if (EditorApplication.isPlaying) return "ERROR: stop Play Mode first";
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
                if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty) return "ERROR: a loaded scene has unsaved changes - save it first";

            Directory.CreateDirectory(Art);
            F = AegisTheme.Font;
            BuildSprites();
            Screens = new List<AegisScreen>();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // camera + canvas + event system
            var camGo = new GameObject("UICamera", typeof(Camera), typeof(AudioListener));
            var cam = camGo.GetComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = AegisTheme.Navy;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 200f;
            camGo.transform.position = new Vector3(0f, 0f, -10f);

            var cgo = new GameObject("AegisCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = cgo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 100f;
            var sc = cgo.GetComponent<CanvasScaler>();
            sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            sc.referenceResolution = new Vector2(1920f, 1080f);
            sc.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            sc.matchWidthOrHeight = 0.5f;
            var root = (RectTransform)cgo.transform;

            var es = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            var mod = es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            mod.AssignDefaultActions();
#else
            es.AddComponent<StandaloneInputModule>();
#endif

            // ============ 1. SPLASH ============
            var splash = MakeScreen<AegisSplashScreen>(root, "Screen_1_Splash", AegisScreenId.Splash);
            Cover("Bg", splash.transform, "splash_bg");
            // Logo and tagline are already present in high-res splash_bg artwork
            var track = Pt("ProgressTrack", splash.transform, 0.5f, 0.14f, 1100f, 18f);
            Im(track, "chip", new Color(0.03f, 0.08f, 0.15f, 0.9f));
            track.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            var fill = Im(Rt("Fill", track, 0f, 0f, 1f, 1f), "solid", AegisTheme.Cyan);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;
            fill.fillAmount = 0.62f;
            var pct = Tx(Pt("Percent", splash.transform, 0.5f, 0.14f, 140f, 50f, 640f, 0f), "62%", 32, AegisTheme.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            Tx(Pt("Status", splash.transform, 0.5f, 0.14f, 1100f, 40f, 0f, -46f), "Đang tải...", 28, AegisTheme.Muted, TextAnchor.MiddleLeft);
            splash.fill = fill;
            splash.percent = pct;

            // ============ 2. MAIN MENU ============
            var mm = MakeScreen<AegisScreen>(root, "Screen_2_MainMenu", AegisScreenId.MainMenu);
            Cover("Bg", mm.transform, "main_menu_bg");
            
            // Left Shadow Vignette
            var shade = Rt("ShadeLeft", mm.transform, 0f, 0f, 0.38f, 1f);
            Im(shade, "grad_left", new Color(0.02f, 0.05f, 0.12f, 0.55f));
            
            // Top-Left Logo Badge: CHESS 3D AR (Exact User Design 100%)
            var logoBadge = Pt("TopLeftLogo", mm.transform, 0f, 1f, 400f, 157f, 60f, -36f, 0f, 1f);
            Im(logoBadge, "hud_logo_chess_3dar", Color.white, false);
            logoBadge.gameObject.AddComponent<AegisButtonFx>();

            // Left Navigation: Dynamic Menu Nav (Vertical Layout)
            var navContainer = Pt("LeftNavMenu", mm.transform, 0f, 1f, 480f, 480f, 60f, -220f, 0f, 1f);
            var vlg = navContainer.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 18f;
            vlg.childAlignment = TextAnchor.UpperLeft;
            vlg.childControlWidth = false;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = false;
            vlg.childForceExpandHeight = false;

            var navComp = navContainer.gameObject.AddComponent<AegisMainMenuNav>();
            navComp.activeSprite = S.ContainsKey("btn_play_chevron") ? S["btn_play_chevron"] : null;
            navComp.buttons = new Button[5];
            navComp.icons = new Image[5];
            navComp.backgrounds = new Image[5];
            navComp.itemRts = new RectTransform[5];
            navComp.labels = new Text[5];
            navComp.chevrons = new GameObject[5];

            string[] allNavIcons = { "icon_nav_home", "icon_nav_guide", "icon_nav_settings", "icon_nav_shop", "icon_nav_exit" };
            string[] allNavLabels = { "Chơi ngay", "Hướng dẫn", "Cài đặt", "Cửa hàng", "Thoát" };
            AegisScreenId[] allNavTargets = { AegisScreenId.ModeSelect, AegisScreenId.Tutorial, AegisScreenId.Settings, AegisScreenId.Shop, AegisScreenId.Quit };

            for (int i = 0; i < 5; i++)
            {
                bool isInitActive = (i == 0);
                var row = Pt("NavItem_" + i, navContainer.transform, 0f, 0.5f, isInitActive ? 440f : 380f, isInitActive ? 105f : 68f, 0f, 0f, 0f, 0.5f);
                var navLe = row.gameObject.AddComponent<LayoutElement>();
                navLe.preferredWidth = isInitActive ? 440f : 380f;
                navLe.preferredHeight = isInitActive ? 105f : 68f;

                var bg = Im(row, isInitActive ? "btn_play_chevron" : null, isInitActive ? Color.white : Color.clear, true);
                var b = row.gameObject.AddComponent<Button>();
                b.targetGraphic = bg;
                Nav(b, allNavTargets[i]);
                row.gameObject.AddComponent<AegisButtonFx>();

                var ic = Im(Pt("Icon", row, 0f, 0.5f, isInitActive ? 58f : 48f, isInitActive ? 58f : 48f, isInitActive ? 22f : 20f, 0f, 0f, 0.5f), allNavIcons[i], isInitActive ? Color.white : new Color(0.0f, 0.85f, 1.0f, 1.0f));
                ic.raycastTarget = false;

                var lb = Tx(Pt("Label", row, 0f, 0.5f, 280f, 50f, isInitActive ? 98f : 86f, 0f, 0f, 0.5f), allNavLabels[i], isInitActive ? 32 : 28, isInitActive ? Color.white : new Color(0.95f, 0.98f, 1.0f, 0.95f), TextAnchor.MiddleLeft, FontStyle.Bold);
                lb.raycastTarget = false;

                var chevRt = Pt("RightChevron", row, 1f, 0.5f, 30f, 30f, -28f, 0f, 1f, 0.5f);
                var chTxt = Tx(chevRt, ">", 32, new Color(0.15f, 0.85f, 1.0f, 0.95f), TextAnchor.MiddleCenter, FontStyle.Bold);
                chTxt.raycastTarget = false;
                chevRt.gameObject.SetActive(isInitActive);

                navComp.buttons[i] = b;
                navComp.icons[i] = ic;
                navComp.backgrounds[i] = bg;
                navComp.itemRts[i] = row;
                navComp.labels[i] = lb;
                navComp.chevrons[i] = chevRt.gameObject;
            }

            // Top-Right Profile Header Status Pill (Exact User Design 100%)
            var chip = Pt("ProfileChip", mm.transform, 1f, 1f, 540f, 86f, -60f, -36f, 1f, 1f);
            Im(chip, "hud_profile_pill", Color.white);
            chip.gameObject.AddComponent<AegisButtonFx>();
            var pBtn = chip.gameObject.AddComponent<Button>();
            Nav(pBtn, AegisScreenId.Profile);

            var av = Pt("Avatar", chip, 0f, 0.5f, 64f, 64f, 12f, 0f, 0f, 0.5f);
            Im(av, "profile_avatar_round", Color.white);

            Tx(Pt("Name", chip, 0f, 0.5f, 160f, 26f, 88f, 14f, 0f, 0.5f), "Player001", 22, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            Tx(Pt("Level", chip, 0f, 0.5f, 65f, 22f, 88f, -14f, 0f, 0.5f), "Lv. 5", 17, AegisTheme.Cyan, TextAnchor.MiddleLeft, FontStyle.Bold);
            Tx(Pt("Diamond", chip, 0f, 0.5f, 20f, 22f, 156f, -14f, 0f, 0.5f), "◊", 18, AegisTheme.Cyan, TextAnchor.MiddleCenter);

            // 3 Quick Action Icons (Mail, Reward with red badge, Settings) inside recessed right sub-panel
            var btnMail = Btn(Pt("BtnMail", chip, 0f, 0.5f, 42f, 42f, 355f, 0f, 0.5f, 0.5f), null, Color.clear, "✉", 20, Color.white);
            btnMail.gameObject.AddComponent<AegisButtonFx>();

            var btnInbox = Btn(Pt("BtnInbox", chip, 0f, 0.5f, 42f, 42f, 415f, 0f, 0.5f, 0.5f), null, Color.clear, "📥", 20, Color.white);
            btnInbox.gameObject.AddComponent<AegisButtonFx>();
            var redDot = Pt("RedBadge", btnInbox.transform, 1f, 1f, 12f, 12f, -2f, -2f, 0.5f, 0.5f);
            Im(redDot, "circle", new Color(1f, 0.2f, 0.2f, 1f));

            var btnCog = Btn(Pt("BtnQuickSettings", chip, 0f, 0.5f, 42f, 42f, 475f, 0f, 0.5f, 0.5f), null, Color.clear, "⚙", 20, Color.white);
            btnCog.gameObject.AddComponent<AegisButtonFx>();
            Nav(btnCog, AegisScreenId.Settings);

            // Bottom-Right Champion Speech Dialogue Bubble (Exact User Design with pointer)
            var tg = Pt("DialogueBubble", mm.transform, 1f, 0f, 490f, 108f, -60f, 32f, 1f, 0f);
            Im(tg, "hud_dialogue_bubble", Color.white);
            Tx(Rt("Text", tg, 0.08f, 0.1f, 0.95f, 0.9f), "Khám phá thế giới cờ vua\ntrong không gian 3D và AR", 21, new Color(0.95f, 0.98f, 1f), TextAnchor.MiddleCenter);

            // ============ 3. MODE SELECT ============
            var ms = MakeScreen<AegisScreen>(root, "Screen_3_ModeSelect", AegisScreenId.ModeSelect);
            Backdrop(ms.transform);
            TopBar(ms.transform, "CHỌN CHẾ ĐỘ CHƠI", true, true);
            string[] mt = { "Đấu với máy", "Hai người", "AR", "Học cờ & Puzzle" };
            string[] md = { "Chọn cấp độ và bắt đầu thử thách AI", "Chơi cùng bạn bè trên cùng một thiết bị", "Đặt bàn cờ lên mặt bàn thật qua camera", "Học luật, nước đi và tự luyện chiến thuật" };
            string[] mg = { "AI", "2P", "AR", "?" };
            Color[] mc = { new Color(0.10f, 0.25f, 0.50f), new Color(0.25f, 0.15f, 0.45f), new Color(0.05f, 0.40f, 0.45f), new Color(0.45f, 0.30f, 0.10f) };
            AegisGameMode[] modes = { AegisGameMode.VsAI, AegisGameMode.TwoPlayer, AegisGameMode.AR, AegisGameMode.Learn };
            for (int i = 0; i < 4; i++)
            {
                float x0 = 0.06f + i * 0.2267f;
                var card = Rt("Card_" + i, ms.transform, x0, 0.16f, x0 + 0.2f, 0.80f);
                var cb = Btn(card, i == 0 ? "panel_hi" : "panel", Color.white, null, 0, Color.white);
                var nav = cb.gameObject.AddComponent<AegisNavButton>();
                nav.setMode = true;
                nav.mode = modes[i];
                var art = Rt("Art", card, 0.05f, 0.40f, 0.95f, 0.96f);
                Im(art, "fill", mc[i]);
                Tx(Rt("Glyph", art, 0f, 0f, 1f, 1f), mg[i], 96, new Color(1f, 1f, 1f, 0.45f), TextAnchor.MiddleCenter, FontStyle.Bold);
                Tx(Rt("Title", card, 0.06f, 0.26f, 0.94f, 0.38f), mt[i], 34, AegisTheme.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
                Tx(Rt("Desc", card, 0.10f, 0.06f, 0.90f, 0.26f), md[i], 22, AegisTheme.Muted, TextAnchor.UpperCenter);
            }

            // ============ 4. AI DIFFICULTY ============
            var ai = MakeScreen<AegisAIDifficultyScreen>(root, "Screen_4_AIDifficulty", AegisScreenId.AIDifficulty);
            Backdrop(ai.transform);
            TopBar(ai.transform, "ĐẤU VỚI MÁY", true, false);
            var pf = Rt("PortraitFrame", ai.transform, 0.06f, 0.14f, 0.36f, 0.80f);
            Im(pf, "panel", Color.white);
            var pin = Rt("PortraitMask", pf, 0.03f, 0.03f, 0.97f, 0.97f);
            pin.gameObject.AddComponent<RectMask2D>();
            Cover("Portrait", pin, "commander_portrait");
            Im(Rt("Shade", pf, 0.03f, 0.03f, 0.97f, 0.40f), "grad_bottom", Color.white);
            Tx(Rt("Caption", pf, 0.08f, 0.05f, 0.92f, 0.22f), "Chọn cấp độ của AI và thiết lập thời gian.", 26, AegisTheme.Ink, TextAnchor.MiddleLeft);

            string[] ln = { "Dễ", "Trung bình", "Khó", "Rất khó" };
            string[] eloLevelTexts = { "Elo ~ 800", "Elo ~ 1200", "Elo ~ 1600", "Elo ~ 2000" };
            var lvBtns = new Button[4];
            var lvBgs = new Image[4];
            var lvDots = new Image[4];
            for (int i = 0; i < 4; i++)
            {
                float y1 = 0.78f - i * 0.135f;
                var row = Rt("Level_" + i, ai.transform, 0.42f, y1 - 0.10f, 0.70f, y1);
                lvBtns[i] = Btn(row, i == 1 ? "panel_hi" : "panel", Color.white, null, 0, Color.white);
                lvBgs[i] = (Image)lvBtns[i].targetGraphic;
                var ring = Pt("Ring", row, 0f, 0.5f, 36f, 36f, 36f, 0f, 0f, 0.5f);
                Im(ring, "circle", Color.white);
                lvDots[i] = Im(Pt("Dot", ring, 0.5f, 0.5f, 18f, 18f), "knob", AegisTheme.Cyan);
                lvDots[i].enabled = i == 1;
                var nm = Tx(Rt("Name", row, 0f, 0f, 1f, 1f), ln[i], 34, AegisTheme.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
                nm.rectTransform.offsetMin = new Vector2(100f, 0f);
                var el = Tx(Rt("Elo", row, 0f, 0f, 1f, 1f), eloLevelTexts[i], 24, AegisTheme.Muted, TextAnchor.MiddleRight);
                el.rectTransform.offsetMax = new Vector2(-28f, 0f);
            }
            Tx(Rt("TimeLabel", ai.transform, 0.74f, 0.72f, 0.94f, 0.78f), "Thời gian", 26, AegisTheme.Muted, TextAnchor.MiddleLeft);
            var timeRt = Rt("TimeChoice", ai.transform, 0.74f, 0.62f, 0.94f, 0.71f);
            Btn(timeRt, "chip", Color.white, null, 0, Color.white);
            var tl = Tx(Rt("Label", timeRt, 0f, 0f, 1f, 1f), "10 phút", 28, AegisTheme.Ink, TextAnchor.MiddleLeft);
            tl.rectTransform.offsetMin = new Vector2(24f, 0f);
            var tchev = Tx(Rt("Chevron", timeRt, 0f, 0f, 1f, 1f), "v", 24, AegisTheme.Cyan, TextAnchor.MiddleRight);
            tchev.rectTransform.offsetMax = new Vector2(-22f, 0f);
            var timeChoice = timeRt.gameObject.AddComponent<AegisChoice>();
            timeChoice.prefKey = "";
            timeChoice.options = new[] { "5 phút", "10 phút", "15 phút", "30 phút", "Không giới hạn" };
            timeChoice.defaultIndex = 1;
            timeChoice.index = 1;
            timeChoice.label = tl;

            Tx(Rt("ColorLabel", ai.transform, 0.74f, 0.52f, 0.94f, 0.58f), "Màu quân", 26, AegisTheme.Muted, TextAnchor.MiddleLeft);
            var grpRt = Rt("ColorGroup", ai.transform, 0.74f, 0.34f, 0.94f, 0.51f);
            var grp = grpRt.gameObject.AddComponent<ToggleGroup>();
            grp.allowSwitchOff = false;
            var whiteT = RadioRow(grpRt, "White", "Trắng", 0.52f, 1f, grp, true);
            RadioRow(grpRt, "Black", "Đen", 0f, 0.48f, grp, false);
            var startBtn = Btn(Rt("StartButton", ai.transform, 0.74f, 0.10f, 0.94f, 0.21f), "button_gold", Color.white, "BẮT ĐẦU", 40, Color.white);

            ai.levelButtons = lvBtns;
            ai.levelBgs = lvBgs;
            ai.levelDots = lvDots;
            ai.spriteOn = S["panel_hi"];
            ai.spriteOff = S["panel"];
            ai.timeChoice = timeChoice;
            ai.whiteToggle = whiteT;
            ai.startButton = startBtn;

            // ============ 5. SETTINGS ============
            var st = MakeScreen<AegisScreen>(root, "Screen_5_Settings", AegisScreenId.Settings);
            Backdrop(st.transform);
            var sp = Rt("Panel", st.transform, 0.12f, 0.10f, 0.88f, 0.90f);
            Im(sp, "panel", Color.white, true);
            PanelHeader(sp, "CÀI ĐẶT");
            string[] stNames = { "Game", "Đồ họa", "Âm thanh", "Điều khiển", "Ngôn ngữ" };
            var contents = new RectTransform[5];
            for (int i = 0; i < 5; i++) contents[i] = Rt("Content_" + i, sp, 0.30f, 0.06f, 0.97f, 0.84f);
            string[] fxOpts = { "Thấp", "Trung bình", "Cao" };
            AddChoice(NewRow(contents[0], 0, "Độ khó AI"), "aiLevel", ln, 1);
            AddChoice(NewRow(contents[0], 1, "Hiệu ứng hình ảnh*"), "fx", fxOpts, 2);
            AddSwitch(NewRow(contents[0], 2, "Nhạc nền"), "music", true);
            AddSwitch(NewRow(contents[0], 3, "Âm thanh"), "sfx", true);
            AddSwitch(NewRow(contents[0], 4, "Rung"), "haptic", true);
            AddChoice(NewRow(contents[0], 5, "Ngôn ngữ"), "lang", new[] { "Tiếng Việt", "English" }, 0);
            AddChoice(NewRow(contents[1], 0, "Hiệu ứng hình ảnh*"), "fx", fxOpts, 2);
            AddChoice(NewRow(contents[1], 1, "Chất lượng bóng"), "shadow", new[] { "Tắt", "Thấp", "Cao" }, 1);
            AddChoice(NewRow(contents[1], 2, "Giới hạn FPS"), "fps", new[] { "30", "60", "90" }, 1);
            AddSwitch(NewRow(contents[2], 0, "Nhạc nền"), "music", true);
            AddSwitch(NewRow(contents[2], 1, "Âm thanh"), "sfx", true);
            AddSwitch(NewRow(contents[2], 2, "Rung"), "haptic", true);
            AddChoice(NewRow(contents[3], 0, "Kiểu di chuyển"), "move", new[] { "Chạm - chạm", "Kéo thả" }, 0);
            AddSwitch(NewRow(contents[3], 1, "Gợi ý nước đi"), "hints", true);
            AddSwitch(NewRow(contents[3], 2, "Xác nhận nước đi"), "confirm", false);
            AddChoice(NewRow(contents[4], 0, "Ngôn ngữ"), "lang", new[] { "Tiếng Việt", "English" }, 0);
            Tx(Rt("Note", sp, 0.30f, 0.015f, 0.97f, 0.06f), "* Áp dụng từ lần khởi động tiếp theo.", 22, AegisTheme.Muted, TextAnchor.MiddleRight);

            var sb = Rt("Sidebar", sp, 0f, 0f, 1f, 1f);
            var sBtns = new Button[5];
            var sPanels = new GameObject[5];
            for (int i = 0; i < 5; i++)
            {
                float y1 = 0.81f - i * 0.145f;
                var tr = Rt("Tab_" + i, sb, 0.03f, y1 - 0.12f, 0.26f, y1);
                sBtns[i] = Btn(tr, i == 0 ? "button" : "panel_flat", Color.white, null, 0, Color.white);
                var tlb = Tx(Rt("Label", tr, 0f, 0f, 1f, 1f), stNames[i], 30, i == 0 ? Color.white : AegisTheme.Muted, TextAnchor.MiddleLeft, FontStyle.Bold);
                tlb.rectTransform.offsetMin = new Vector2(34f, 0f);
                sPanels[i] = contents[i].gameObject;
            }
            var sTabs = sb.gameObject.AddComponent<AegisTabs>();
            sTabs.tabs = sBtns;
            sTabs.panels = sPanels;
            sTabs.onSprite = S["button"];
            sTabs.offSprite = S["panel_flat"];
            sTabs.selected = 0;

            // ============ 6. TUTORIAL ============
            var tu = MakeScreen<AegisTutorialScreen>(root, "Screen_6_Tutorial", AegisScreenId.Tutorial);
            Backdrop(tu.transform);
            var tp = Rt("Panel", tu.transform, 0.07f, 0.10f, 0.93f, 0.90f);
            Im(tp, "panel", Color.white, true);
            PanelHeader(tp, "HƯỚNG DẪN");
            var tTabs = MakeTopTabs(tp, new[] { "Cơ bản", "Luật chơi", "Các quân cờ", "AR" }, 0.18f, 0.17f, 0.76f, 0.86f, null);
            var bd = Rt("BoardFrame", tp, 0.03f, 0.14f, 0.47f, 0.73f);
            Im(bd, "panel", Color.white);
            var boardImg = Im(Rt("Board", bd, 0.03f, 0.03f, 0.97f, 0.97f), "board_preview", Color.white);
            boardImg.preserveAspect = true;
            var pageTitle = Tx(Rt("PageTitle", tp, 0.52f, 0.62f, 0.97f, 0.73f), "Di chuyển quân cờ", 40, AegisTheme.Cyan, TextAnchor.MiddleLeft, FontStyle.Bold);
            var heads = new Text[3];
            var descs = new Text[3];
            for (int i = 0; i < 3; i++)
            {
                float yTop = 0.58f - i * 0.15f;
                var row = Rt("Step_" + i, tp, 0.52f, yTop - 0.13f, 0.97f, yTop);
                var num = Pt("Num", row, 0f, 0.5f, 60f, 60f, 0f, 0f, 0f, 0.5f);
                Im(num, "circle", Color.white);
                Tx(Rt("N", num, 0f, 0f, 1f, 1f), (i + 1).ToString(), 30, AegisTheme.Cyan, TextAnchor.MiddleCenter, FontStyle.Bold);
                heads[i] = Tx(Rt("Head", row, 0f, 0.5f, 1f, 1f), "Chọn quân", 32, AegisTheme.Ink, TextAnchor.LowerLeft, FontStyle.Bold);
                heads[i].rectTransform.offsetMin = new Vector2(84f, 0f);
                descs[i] = Tx(Rt("Desc", row, 0f, 0f, 1f, 0.5f), "Chạm vào quân cờ để chọn.", 24, AegisTheme.Muted, TextAnchor.UpperLeft);
                descs[i].rectTransform.offsetMin = new Vector2(84f, 0f);
            }
            var nav2 = Rt("Nav", tp, 0.03f, 0.02f, 0.47f, 0.12f);
            var prevB = Btn(Pt("Prev", nav2, 0.5f, 0.5f, 64f, 64f, -110f, 0f), "circle", Color.white, "<", 32, AegisTheme.CyanBright);
            var pageTxt = Tx(Pt("Page", nav2, 0.5f, 0.5f, 140f, 50f), "1 / 5", 30, AegisTheme.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            var arrowB = Btn(Pt("ArrowNext", nav2, 0.5f, 0.5f, 64f, 64f, 110f, 0f), "circle", Color.white, ">", 32, AegisTheme.CyanBright);
            var nextB = Btn(Rt("NextButton", tp, 0.72f, 0.03f, 0.97f, 0.13f), "button", Color.white, "Tiếp theo", 32, Color.white);
            tu.tabs = tTabs;
            tu.pageTitle = pageTitle;
            tu.pageIndicator = pageTxt;
            tu.stepHeads = heads;
            tu.stepDescs = descs;
            tu.prevButton = prevB;
            tu.arrowNextButton = arrowB;
            tu.nextButton = nextB;

            // ============ 7. SHOP ============
            var sh = MakeScreen<AegisShopScreen>(root, "Screen_7_Shop", AegisScreenId.Shop);
            Backdrop(sh.transform);
            var shp = Rt("Panel", sh.transform, 0.07f, 0.08f, 0.93f, 0.92f);
            Im(shp, "panel", Color.white, true);
            PanelHeader(shp, "CỬA HÀNG");
            var cc = Pt("CoinChip", shp, 1f, 1f, 190f, 60f, -100f, -22f, 1f, 1f);
            Im(cc, "chip", Color.white);
            Im(Pt("Coin", cc, 0f, 0.5f, 34f, 34f, 22f, 0f, 0f, 0.5f), "knob", AegisTheme.Gold);
            var coinTxt = Tx(Rt("Amount", cc, 0f, 0f, 1f, 1f), "1250", 30, AegisTheme.Gold, TextAnchor.MiddleLeft, FontStyle.Bold);
            coinTxt.rectTransform.offsetMin = new Vector2(66f, 0f);
            var shTabs = MakeTopTabs(shp, new[] { "Skin quân cờ", "Bàn cờ", "Hiệu ứng" }, 0.19f, 0.18f, 0.775f, 0.865f, null);
            string[] sn = { "Knight of Light", "Dark Legion", "Cyber Future", "Forest Spirit" };
            string[] sg = { "K", "D", "C", "F" };
            Color[] scol = { new Color(0.85f, 0.75f, 0.40f), new Color(0.61f, 0.30f, 0.79f), new Color(0.20f, 0.78f, 1f), new Color(0.24f, 0.75f, 0.42f) };
            var cards = new AegisShopScreen.Card[4];
            for (int i = 0; i < 4; i++)
            {
                float x0 = 0.03f + i * 0.24f;
                var cr = Rt("Card_" + i, shp, x0, 0.08f, x0 + 0.22f, 0.74f);
                var cb = Btn(cr, "panel", Color.white, null, 0, Color.white);
                var art = Rt("Art", cr, 0.06f, 0.34f, 0.94f, 0.95f);
                var artImg = Im(art, "fill", Color.Lerp(scol[i], new Color(0.03f, 0.06f, 0.12f), 0.55f));
                var gl = Tx(Rt("Glyph", art, 0f, 0f, 1f, 1f), sg[i], 110, scol[i], TextAnchor.MiddleCenter, FontStyle.Bold);
                var nm = Tx(Rt("Name", cr, 0.05f, 0.19f, 0.95f, 0.33f), sn[i], 30, AegisTheme.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
                var pc = Rt("Price", cr, 0.16f, 0.04f, 0.84f, 0.17f);
                Im(pc, "chip", Color.white);
                var cd = Im(Pt("Coin", pc, 0f, 0.5f, 30f, 30f, 24f, 0f, 0f, 0.5f), "knob", AegisTheme.Gold);
                var pt = Tx(Rt("Text", pc, 0f, 0f, 1f, 1f), "1000", 28, AegisTheme.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
                pt.rectTransform.offsetMin = new Vector2(50f, 0f);
                cards[i] = new AegisShopScreen.Card { button = cb, art = artImg, glyph = gl, nameText = nm, priceText = pt, coinDot = cd };
            }
            sh.cards = cards;
            sh.tabs = shTabs;
            sh.coinText = coinTxt;

            // ============ POPUP (14) + TOAST (15) ============
            var pr = Rt("Popup", root, 0f, 0f, 1f, 1f);
            Im(pr, "solid", new Color(0f, 0f, 0f, 0.65f), true);
            var modal = Pt("Modal", pr, 0.5f, 0.5f, 820f, 440f);
            Im(modal, "panel_hi", Color.white, true);
            var pTitle = Tx(Rt("Title", modal, 0.05f, 0.72f, 0.95f, 0.95f), "Thông báo", 44, AegisTheme.CyanBright, TextAnchor.MiddleCenter, FontStyle.Bold);
            var pMsg = Tx(Rt("Message", modal, 0.08f, 0.36f, 0.92f, 0.72f), "Bạn có muốn thoát trò chơi?", 32, AegisTheme.Ink, TextAnchor.MiddleCenter);
            var pCancel = Btn(Rt("Cancel", modal, 0.06f, 0.07f, 0.47f, 0.28f), "panel", Color.white, "Hủy", 34, AegisTheme.Ink);
            var pConfirm = Btn(Rt("Confirm", modal, 0.53f, 0.07f, 0.94f, 0.28f), "button", Color.white, "Xác nhận", 34, Color.white);
            var popup = pr.gameObject.AddComponent<AegisPopup>();
            popup.title = pTitle;
            popup.message = pMsg;
            popup.confirm = pConfirm;
            popup.cancel = pCancel;
            popup.confirmLabel = pConfirm.GetComponentInChildren<Text>();

            var tr0 = Pt("Toast", root, 0.5f, 1f, 780f, 100f, 0f, -60f, 0.5f, 1f);
            Im(tr0, "panel_hi", Color.white);
            var tcg = tr0.gameObject.AddComponent<CanvasGroup>();
            tcg.blocksRaycasts = false;
            tcg.interactable = false;
            var tIcon = Im(Pt("Icon", tr0, 0f, 0.5f, 60f, 60f, 40f, 0f, 0f, 0.5f), "knob", AegisTheme.Cyan);
            var tIconTxt = Tx(Rt("Glyph", tIcon.rectTransform, 0f, 0f, 1f, 1f), "i", 28, AegisTheme.Navy, TextAnchor.MiddleCenter, FontStyle.Bold);
            var tLbl = Tx(Rt("Label", tr0, 0f, 0f, 1f, 1f), "Thông báo", 30, AegisTheme.Ink, TextAnchor.MiddleLeft);
            tLbl.rectTransform.offsetMin = new Vector2(130f, 0f);
            tLbl.rectTransform.offsetMax = new Vector2(-30f, 0f);
            var toast = tr0.gameObject.AddComponent<AegisToast>();
            toast.group = tcg;
            toast.label = tLbl;
            toast.icon = tIcon;
            toast.iconText = tIconTxt;

            // ============ MANAGER ============
            var mgr = cgo.AddComponent<AegisUIManager>();
            mgr.screens = Screens.ToArray();
            mgr.popup = popup;
            mgr.toast = toast;
            mgr.startScreen = AegisScreenId.Splash;

            // editing layout: only the splash is visible, everything else is hidden
            for (int i = 1; i < 5; i++) contents[i].gameObject.SetActive(false);
            foreach (var s in Screens) if (s.id != AegisScreenId.Splash) s.gameObject.SetActive(false);
            popup.gameObject.SetActive(false);
            toast.gameObject.SetActive(false);

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            return "OK: built " + Screens.Count + " screens -> " + ScenePath;
        }
    }
}
