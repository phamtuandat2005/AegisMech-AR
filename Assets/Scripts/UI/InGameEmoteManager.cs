using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MechChess.Multiplayer;
using MechChess.Chess3D;

namespace MechChess.UI
{
    /// <summary>
    /// In-game Emote & Reaction System:
    /// Allows players to send real-time emote reactions during 3D/AR chess matches.
    /// Emotes are loaded from high-resolution GitHub open-source assets (Twemoji/Fluent).
    /// Supports:
    /// - Interactive Emote Picker Wheel/Popup on the tactical toolbar.
    /// - Animated pop-and-float bubble over local player and opponent avatars.
    /// - Real-time network synchronization over WebSocket in online 2P matches.
    /// - Smart AI counter-reactions in single-player vs AI mode.
    /// </summary>
    public class InGameEmoteManager : MonoBehaviour
    {
        public static InGameEmoteManager Instance { get; private set; }

        [Serializable]
        public class EmoteEntry
        {
            public string id;
            public string nameVi;
            public string nameEn;
            public string spriteName;
        }

        public static readonly EmoteEntry[] AllEmotes = new EmoteEntry[]
        {
            new EmoteEntry { id = "emote_fire",     nameVi = "Lửa Chiến",    nameEn = "Fire",      spriteName = "emote_fire" },
            new EmoteEntry { id = "emote_cool",     nameVi = "Tự Tin",      nameEn = "Cool",      spriteName = "emote_cool" },
            new EmoteEntry { id = "emote_shocked",  nameVi = "Bất Ngờ",     nameEn = "Shocked",   spriteName = "emote_shocked" },
            new EmoteEntry { id = "emote_laugh",    nameVi = "Cười Lớn",    nameEn = "Laugh",     spriteName = "emote_laugh" },
            new EmoteEntry { id = "emote_thinking", nameVi = "Suy Nghĩ",    nameEn = "Thinking",  spriteName = "emote_thinking" },
            new EmoteEntry { id = "emote_crown",    nameVi = "Vương Miện",  nameEn = "Crown",     spriteName = "emote_crown" },
            new EmoteEntry { id = "emote_clap",     nameVi = "Vỗ Tay",      nameEn = "Clap",      spriteName = "emote_clap" },
            new EmoteEntry { id = "emote_cry",      nameVi = "Khóc Ròng",   nameEn = "Cry",       spriteName = "emote_cry" },
            new EmoteEntry { id = "emote_swords",   nameVi = "Chiến Đấu",   nameEn = "Swords",    spriteName = "emote_swords" },
            new EmoteEntry { id = "emote_strong",   nameVi = "Quyết Thắng", nameEn = "Strong",    spriteName = "emote_strong" },
        };

        private GameObject _pickerModal;
        private GameObject _playerBubbleObj;
        private Image _playerEmoteImg;
        private Coroutine _playerBubbleRoutine;

        private GameObject _opponentBubbleObj;
        private Image _opponentEmoteImg;
        private Coroutine _opponentBubbleRoutine;

        private readonly Dictionary<string, Sprite> _cachedSprites = new Dictionary<string, Sprite>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            PreloadEmoteSprites();
        }

        private void PreloadEmoteSprites()
        {
            foreach (var entry in AllEmotes)
            {
                var sp = Resources.Load<Sprite>("Textures/Emotes/" + entry.spriteName);
                if (sp != null)
                {
                    _cachedSprites[entry.id] = sp;
                }
            }
        }

        public Sprite GetEmoteSprite(string emoteId)
        {
            if (string.IsNullOrEmpty(emoteId)) return null;
            if (_cachedSprites.TryGetValue(emoteId, out Sprite sp) && sp != null) return sp;

            sp = Resources.Load<Sprite>("Textures/Emotes/" + emoteId);
            if (sp != null) _cachedSprites[emoteId] = sp;
            return sp;
        }

        // =========================================================================
        // UI CONSTRUCTION
        // =========================================================================
        public void EnsureUIBuilt(Transform hudRoot, Font font)
        {
            if (hudRoot == null) return;

            // 1. Build Player & Opponent Floating Reaction Bubbles
            if (_playerBubbleObj == null)
            {
                _playerBubbleObj = CreateReactionBubble(hudRoot, "PlayerEmoteBubble", new Vector2(210f, -80f), new Color(0.0f, 0.85f, 1.0f), out _playerEmoteImg);
            }
            if (_opponentBubbleObj == null)
            {
                _opponentBubbleObj = CreateReactionBubble(hudRoot, "OpponentEmoteBubble", new Vector2(-210f, -80f), new Color(1.0f, 0.35f, 0.35f), out _opponentEmoteImg, isRightAligned: true);
            }

            // 2. Build Emote Picker Modal
            if (_pickerModal == null)
            {
                BuildPickerModal(hudRoot, font);
            }
        }

        private GameObject CreateReactionBubble(Transform parent, string name, Vector2 pos, Color borderColor, out Image emoteImg, bool isRightAligned = false)
        {
            GameObject bubble = new GameObject(name);
            bubble.transform.SetParent(parent, false);

            RectTransform rt = bubble.AddComponent<RectTransform>();
            rt.anchorMin = isRightAligned ? new Vector2(1f, 1f) : new Vector2(0f, 1f);
            rt.anchorMax = isRightAligned ? new Vector2(1f, 1f) : new Vector2(0f, 1f);
            rt.pivot = isRightAligned ? new Vector2(1f, 1f) : new Vector2(0f, 1f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(110f, 110f);

            Image bg = bubble.AddComponent<Image>();
            bg.sprite = ArcaneAssetLoader.GetCircleSprite(110, new Color(0.04f, 0.08f, 0.16f, 0.95f), borderColor, 3.0f);

            CanvasGroup cg = bubble.AddComponent<CanvasGroup>();
            cg.alpha = 0f;

            // Inner Emote Icon
            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(bubble.transform, false);
            emoteImg = iconObj.AddComponent<Image>();
            emoteImg.preserveAspect = true;

            RectTransform irt = iconObj.GetComponent<RectTransform>();
            irt.anchorMin = new Vector2(0.5f, 0.5f);
            irt.anchorMax = new Vector2(0.5f, 0.5f);
            irt.pivot = new Vector2(0.5f, 0.5f);
            irt.anchoredPosition = Vector2.zero;
            irt.sizeDelta = new Vector2(72f, 72f);

            bubble.SetActive(false);
            return bubble;
        }

        private void BuildPickerModal(Transform parent, Font font)
        {
            _pickerModal = new GameObject("EmotePickerModal");
            _pickerModal.transform.SetParent(parent, false);

            RectTransform pRt = _pickerModal.AddComponent<RectTransform>();
            pRt.anchorMin = new Vector2(0f, 0.5f);
            pRt.anchorMax = new Vector2(0f, 0.5f);
            pRt.pivot = new Vector2(0f, 0.5f);
            pRt.anchoredPosition = new Vector2(130f, -140f);
            pRt.sizeDelta = new Vector2(430f, 210f);

            Image bg = _pickerModal.AddComponent<Image>();
            bg.sprite = ArcaneAssetLoader.GetGlassBoxSprite(430, 210, 16f, new Color(0.03f, 0.07f, 0.15f, 0.96f), new Color(0.0f, 0.85f, 1.0f), 2.0f);
            bg.type = Image.Type.Sliced;

            // Header title: "✦ BIỂU CẢM TRẬN ĐẤU ✦"
            GameObject headerObj = new GameObject("Header");
            headerObj.transform.SetParent(_pickerModal.transform, false);
            Text titleTxt = headerObj.AddComponent<Text>();
            titleTxt.text = AegisLocalization.T("✦ BIỂU CẢM ✦", "✦ EMOTES ✦");
            titleTxt.font = font;
            titleTxt.fontSize = 15;
            titleTxt.fontStyle = FontStyle.Bold;
            titleTxt.alignment = TextAnchor.MiddleCenter;
            titleTxt.color = new Color(0.92f, 0.80f, 0.45f);

            RectTransform hRt = headerObj.GetComponent<RectTransform>();
            hRt.anchorMin = new Vector2(0.5f, 1f);
            hRt.anchorMax = new Vector2(0.5f, 1f);
            hRt.pivot = new Vector2(0.5f, 1f);
            hRt.anchoredPosition = new Vector2(0f, -10f);
            hRt.sizeDelta = new Vector2(300f, 24f);

            // Close button [×]
            GameObject closeBtnObj = new GameObject("BtnClose");
            closeBtnObj.transform.SetParent(_pickerModal.transform, false);
            Image cbImg = closeBtnObj.AddComponent<Image>();
            cbImg.sprite = ArcaneAssetLoader.GetGlassBoxSprite(28, 28, 6f, new Color(0.45f, 0.12f, 0.15f, 0.9f), new Color(0.95f, 0.35f, 0.35f), 1.2f);
            Button cBtn = closeBtnObj.AddComponent<Button>();
            cBtn.onClick.AddListener(ClosePicker);

            RectTransform cbRt = closeBtnObj.GetComponent<RectTransform>();
            cbRt.anchorMin = new Vector2(1f, 1f);
            cbRt.anchorMax = new Vector2(1f, 1f);
            cbRt.pivot = new Vector2(1f, 1f);
            cbRt.anchoredPosition = new Vector2(-12f, -10f);
            cbRt.sizeDelta = new Vector2(28f, 28f);

            GameObject cbTxtObj = new GameObject("Txt");
            cbTxtObj.transform.SetParent(closeBtnObj.transform, false);
            Text cbt = cbTxtObj.AddComponent<Text>();
            cbt.text = "×";
            cbt.font = font;
            cbt.fontSize = 18;
            cbt.fontStyle = FontStyle.Bold;
            cbt.alignment = TextAnchor.MiddleCenter;
            cbt.color = Color.white;
            RectTransform cbtRt = cbTxtObj.GetComponent<RectTransform>();
            cbtRt.anchorMin = Vector2.zero;
            cbtRt.anchorMax = Vector2.one;
            cbtRt.offsetMin = Vector2.zero;
            cbtRt.offsetMax = Vector2.zero;

            // Grid of 10 Emotes (2 rows x 5 columns)
            float itemSize = 64f;
            float gapX = 14f;
            float startX = -((5 * itemSize + 4 * gapX) / 2f) + itemSize / 2f;
            float row0Y = 22f;
            float row1Y = -54f;

            for (int i = 0; i < AllEmotes.Length && i < 10; i++)
            {
                var emote = AllEmotes[i];
                int col = i % 5;
                int row = i / 5;
                float posX = startX + col * (itemSize + gapX);
                float posY = (row == 0) ? row0Y : row1Y;

                GameObject eBtnObj = new GameObject("Emote_" + emote.id);
                eBtnObj.transform.SetParent(_pickerModal.transform, false);

                RectTransform ebRt = eBtnObj.AddComponent<RectTransform>();
                ebRt.anchorMin = new Vector2(0.5f, 0.5f);
                ebRt.anchorMax = new Vector2(0.5f, 0.5f);
                ebRt.pivot = new Vector2(0.5f, 0.5f);
                ebRt.anchoredPosition = new Vector2(posX, posY);
                ebRt.sizeDelta = new Vector2(itemSize, itemSize);

                Image ebBg = eBtnObj.AddComponent<Image>();
                ebBg.sprite = ArcaneAssetLoader.GetGlassBoxSprite((int)itemSize, (int)itemSize, 12f, new Color(0.06f, 0.12f, 0.22f, 0.85f), new Color(0f, 0.75f, 0.95f), 1.2f);

                // Emote Sprite Icon
                GameObject icoObj = new GameObject("EmoteIcon");
                icoObj.transform.SetParent(eBtnObj.transform, false);
                Image icoImg = icoObj.AddComponent<Image>();
                icoImg.sprite = GetEmoteSprite(emote.id);
                icoImg.preserveAspect = true;

                RectTransform icoRt = icoObj.GetComponent<RectTransform>();
                icoRt.anchorMin = Vector2.zero;
                icoRt.anchorMax = Vector2.one;
                icoRt.offsetMin = new Vector2(6f, 6f);
                icoRt.offsetMax = new Vector2(-6f, -6f);

                Button btn = eBtnObj.AddComponent<Button>();
                string capturedId = emote.id;
                btn.onClick.AddListener(() =>
                {
                    SendEmote(capturedId);
                    ClosePicker();
                });
                eBtnObj.AddComponent<MenuButtonEffect>();
            }

            _pickerModal.SetActive(false);
        }

        // =========================================================================
        // CONTROLLER ACTIONS
        // =========================================================================
        public void TogglePicker()
        {
            if (_pickerModal == null)
            {
                if (UIManager2D.Instance != null && UIManager2D.Instance.StandardHudRoot != null)
                {
                    EnsureUIBuilt(UIManager2D.Instance.StandardHudRoot.transform, UIManager2D.GetDefaultFont());
                }
            }

            if (_pickerModal != null)
            {
                bool nextState = !_pickerModal.activeSelf;
                _pickerModal.SetActive(nextState);
                if (nextState) AegisAudioManager.PlayModalOpen();
                else AegisAudioManager.PlayModalClose();
            }
        }

        public void OpenPicker()
        {
            if (_pickerModal != null)
            {
                _pickerModal.SetActive(true);
                AegisAudioManager.PlayModalOpen();
            }
        }

        public void ClosePicker()
        {
            if (_pickerModal != null && _pickerModal.activeSelf)
            {
                _pickerModal.SetActive(false);
                AegisAudioManager.PlayModalClose();
            }
        }

        /// <summary>
        /// Sends a reaction emote locally and broadcasts it to the remote opponent if in an online match.
        /// </summary>
        public void SendEmote(string emoteId)
        {
            if (string.IsNullOrEmpty(emoteId)) return;

            // 1. Show local reaction bubble over Player avatar
            ShowLocalPlayerEmote(emoteId);

            // 2. Transmit to remote peer if online
            if (OnlineMultiplayerManager.Instance != null && OnlineMultiplayerManager.Instance.IsOnlineMatch)
            {
                OnlineMultiplayerManager.Instance.SendEmote(emoteId);
            }
            // 3. Fun AI counter-reaction in vs AI mode (50% chance after 1.2s - 2.0s)
            else if (ChessBoard3DManager.Instance != null && ChessBoard3DManager.Instance.isVsAI)
            {
                StartCoroutine(AICounterReactionRoutine(emoteId));
            }
        }

        /// <summary>
        /// Displays an incoming remote emote received from WebSocket network or AI.
        /// </summary>
        public void ShowRemoteEmote(string emoteId, bool isOpponent = true)
        {
            if (string.IsNullOrEmpty(emoteId)) return;

            if (isOpponent)
            {
                ShowOpponentEmote(emoteId);
            }
            else
            {
                ShowLocalPlayerEmote(emoteId);
            }
        }

        private void ShowLocalPlayerEmote(string emoteId)
        {
            if (_playerBubbleObj == null) return;
            Sprite sp = GetEmoteSprite(emoteId);
            if (sp == null) return;

            if (_playerEmoteImg != null) _playerEmoteImg.sprite = sp;

            if (_playerBubbleRoutine != null) StopCoroutine(_playerBubbleRoutine);
            _playerBubbleRoutine = StartCoroutine(AnimateBubbleRoutine(_playerBubbleObj, new Vector2(210f, -80f)));
            AegisAudioManager.PlayButtonClick();
        }

        private void ShowOpponentEmote(string emoteId)
        {
            if (_opponentBubbleObj == null) return;
            Sprite sp = GetEmoteSprite(emoteId);
            if (sp == null) return;

            if (_opponentEmoteImg != null) _opponentEmoteImg.sprite = sp;

            if (_opponentBubbleRoutine != null) StopCoroutine(_opponentBubbleRoutine);
            _opponentBubbleRoutine = StartCoroutine(AnimateBubbleRoutine(_opponentBubbleObj, new Vector2(-210f, -80f)));
            AegisAudioManager.PlayButtonClick();
        }

        private IEnumerator AnimateBubbleRoutine(GameObject bubble, Vector2 basePos)
        {
            bubble.SetActive(true);
            CanvasGroup cg = bubble.GetComponent<CanvasGroup>();
            RectTransform rt = bubble.GetComponent<RectTransform>();

            float elapsed = 0f;
            float totalDuration = 2.6f;

            while (elapsed < totalDuration)
            {
                elapsed += Time.deltaTime;

                // Scale pop-in: 0 -> 1.25 -> 1.0 (over 0.25s)
                float scale;
                if (elapsed < 0.25f)
                {
                    float p = elapsed / 0.25f;
                    scale = Mathf.Lerp(0.2f, 1.25f, Mathf.SmoothStep(0f, 1f, p));
                }
                else if (elapsed < 0.40f)
                {
                    float p = (elapsed - 0.25f) / 0.15f;
                    scale = Mathf.Lerp(1.25f, 1.0f, Mathf.SmoothStep(0f, 1f, p));
                }
                else
                {
                    // Gentle breathing wobble
                    scale = 1.0f + Mathf.Sin((elapsed - 0.40f) * 6.5f) * 0.04f;
                }
                rt.localScale = new Vector3(scale, scale, 1.0f);

                // Gentle upward float
                float floatY = Mathf.Sin(elapsed * 2.0f) * 6.0f + (elapsed * 8.0f);
                rt.anchoredPosition = basePos + new Vector2(0f, floatY);

                // Alpha fade-out near the end
                if (elapsed < 2.0f)
                {
                    if (cg != null) cg.alpha = Mathf.Min(1.0f, elapsed * 5.0f);
                }
                else
                {
                    float fade = 1.0f - ((elapsed - 2.0f) / 0.6f);
                    if (cg != null) cg.alpha = Mathf.Clamp01(fade);
                }

                yield return null;
            }

            bubble.SetActive(false);
            if (cg != null) cg.alpha = 0f;
        }

        private IEnumerator AICounterReactionRoutine(string playerEmoteId)
        {
            yield return new WaitForSeconds(UnityEngine.Random.Range(1.2f, 2.0f));

            // Select an intelligent counter-emote
            string aiEmote = "emote_cool";
            switch (playerEmoteId)
            {
                case "emote_fire":
                    aiEmote = UnityEngine.Random.value > 0.5f ? "emote_swords" : "emote_cool";
                    break;
                case "emote_shocked":
                    aiEmote = UnityEngine.Random.value > 0.5f ? "emote_laugh" : "emote_crown";
                    break;
                case "emote_laugh":
                    aiEmote = UnityEngine.Random.value > 0.5f ? "emote_thinking" : "emote_strong";
                    break;
                case "emote_clap":
                    aiEmote = "emote_cool";
                    break;
                case "emote_cry":
                    aiEmote = "emote_crown";
                    break;
                default:
                    aiEmote = AllEmotes[UnityEngine.Random.Range(0, AllEmotes.Length)].id;
                    break;
            }

            ShowOpponentEmote(aiEmote);
        }
    }
}
