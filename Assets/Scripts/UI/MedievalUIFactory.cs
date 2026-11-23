using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MechChess.UI
{
    /// <summary>
    /// MedievalUIFactory: Standardized design system generator for all AegisMech-AR UI.
    /// Provides consistent, responsive, and beautiful Medieval Manuscript / Fantasy Chess
    /// visual components: Grimoires, Crowned Modals, Parchment Cards, Wood & Gold Buttons,
    /// HUD capsules, and alerts.
    /// </summary>
    public static class MedievalUIFactory
    {
        // Theme Color Palette
        public static readonly Color ParchmentLight = new Color(0.96f, 0.91f, 0.83f, 1.0f);
        public static readonly Color ParchmentDark = new Color(0.85f, 0.77f, 0.65f, 1.0f);
        public static readonly Color DarkOak = new Color(0.12f, 0.08f, 0.05f, 0.96f);
        public static readonly Color DeepBronze = new Color(0.24f, 0.17f, 0.11f, 1.0f);
        public static readonly Color AntiqueGold = new Color(0.82f, 0.66f, 0.24f, 1.0f);
        public static readonly Color GoldBright = new Color(1.0f, 0.91f, 0.62f, 1.0f);
        public static readonly Color RoyalCrimson = new Color(0.68f, 0.14f, 0.16f, 1.0f);
        public static readonly Color InkDark = new Color(0.16f, 0.11f, 0.06f, 1.0f);
        public static readonly Color InkMuted = new Color(0.48f, 0.40f, 0.32f, 1.0f);

        // Global Default TMP Font (Loaded dynamically from Resources or default TMP)
        private static TMP_FontAsset _medievalFont;
        public static TMP_FontAsset GetFont()
        {
            if (_medievalFont == null)
            {
                _medievalFont = Resources.Load<TMP_FontAsset>("Fonts/Cinzel-Bold SDF") ??
                               Resources.Load<TMP_FontAsset>("Fonts/LiberationSans SDF") ??
                               TMP_Settings.defaultFontAsset;
            }
            return _medievalFont;
        }

        /// <summary>
        /// Creates a base GameObject with a RectTransform stretched or sized to target parameters.
        /// </summary>
        public static GameObject CreateUIObject(Transform parent, string name, Vector2 size, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.sizeDelta = size;
            rt.anchoredPosition = Vector2.zero;
            return go;
        }

        /// <summary>
        /// Creates a full-screen semi-transparent backdrop blocker for modals.
        /// </summary>
        public static GameObject CreateModalBackdrop(Transform parent, string name = "ModalBackdrop", Action onClick = null)
        {
            GameObject backdrop = CreateUIObject(parent, name, Vector2.zero, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
            Image img = backdrop.AddComponent<Image>();
            img.color = new Color(0.04f, 0.03f, 0.02f, 0.85f); // Deep dimming medieval vignette
            img.raycastTarget = true;

            if (onClick != null)
            {
                Button btn = backdrop.AddComponent<Button>();
                btn.transition = Selectable.Transition.None;
                btn.onClick.AddListener(() => onClick());
            }

            return backdrop;
        }

        /// <summary>
        /// Creates a Royal Crowned Baroque modal window with parchment interior and close button.
        /// </summary>
        public static GameObject CreateCrownedModal(Transform parent, string name, Vector2 size, string title, Action onClose)
        {
            GameObject modal = CreateUIObject(parent, name, size, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            
            // Outer Frame Image
            Image frameImg = modal.AddComponent<Image>();
            frameImg.sprite = MedievalUIAssetLoader.GetFrameSprite(MedievalFrameStyle.ModalCrowned);
            frameImg.type = Image.Type.Sliced;
            frameImg.color = Color.white;
            frameImg.raycastTarget = true;

            // Optional Drop Shadow / Glow
            Shadow shadow = modal.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.65f);
            shadow.effectDistance = new Vector2(4, -6);

            // Title Header
            if (!string.IsNullOrEmpty(title))
            {
                GameObject titleObj = CreateUIObject(modal.transform, "ModalTitle", new Vector2(size.x - 120, 50), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
                RectTransform titleRt = titleObj.GetComponent<RectTransform>();
                titleRt.anchoredPosition = new Vector2(0, -25);

                TextMeshProUGUI titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
                titleTmp.font = GetFont();
                titleTmp.text = title.ToUpperInvariant();
                titleTmp.fontSize = 24;
                titleTmp.fontStyle = FontStyles.Bold;
                titleTmp.alignment = TextAlignmentOptions.Center;
                titleTmp.color = GoldBright;
                titleTmp.raycastTarget = false;

                // Title Shadow
                Shadow tShadow = titleObj.AddComponent<Shadow>();
                tShadow.effectColor = new Color(0.1f, 0.05f, 0.02f, 0.9f);
                tShadow.effectDistance = new Vector2(2, -2);
            }

            // Close Button
            if (onClose != null)
            {
                GameObject closeBtnObj = CreateButton(
                    modal.transform,
                    "BtnCloseModal",
                    "ĐÓNG",
                    MedievalButtonStyle.WoodSmall,
                    () => onClose(),
                    new Vector2(100, 38)
                );
                RectTransform closeRt = closeBtnObj.GetComponent<RectTransform>();
                closeRt.anchorMin = new Vector2(0.5f, 0f);
                closeRt.anchorMax = new Vector2(0.5f, 0f);
                closeRt.pivot = new Vector2(0.5f, 0f);
                closeRt.anchoredPosition = new Vector2(0, 20);
            }

            return modal;
        }

        /// <summary>
        /// Creates an Open Grimoire 2-page book panel for extensive lore, tutorial, and review screens.
        /// </summary>
        public static GameObject CreateGrimoirePanel(Transform parent, string name, Vector2 size, Action onClose = null)
        {
            GameObject grimoire = CreateUIObject(parent, name, size, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));

            Image img = grimoire.AddComponent<Image>();
            img.sprite = MedievalUIAssetLoader.GetFrameSprite(MedievalFrameStyle.GrimoireOpen);
            img.type = Image.Type.Simple;
            img.preserveAspect = true;
            img.raycastTarget = true;

            // Close corner X
            if (onClose != null)
            {
                GameObject closeBtn = CreateButton(grimoire.transform, "BtnCloseGrimoire", "X", MedievalButtonStyle.WoodSmall, () => onClose(), new Vector2(40, 40));
                RectTransform cRt = closeBtn.GetComponent<RectTransform>();
                cRt.anchorMin = new Vector2(1f, 1f);
                cRt.anchorMax = new Vector2(1f, 1f);
                cRt.pivot = new Vector2(1f, 1f);
                cRt.anchoredPosition = new Vector2(-15, -15);
            }

            return grimoire;
        }

        /// <summary>
        /// Creates an authentic Medieval Button with 9-slicing, hover states, and audio feedback.
        /// </summary>
        public static GameObject CreateButton(
            Transform parent,
            string name,
            string label,
            MedievalButtonStyle style,
            UnityEngine.Events.UnityAction onClick,
            Vector2? size = null
        )
        {
            Vector2 btnSize = size ?? new Vector2(220, 52);
            GameObject btnObj = CreateUIObject(parent, name, btnSize, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));

            Image img = btnObj.AddComponent<Image>();
            img.sprite = MedievalUIAssetLoader.GetButtonSprite(style);
            img.type = Image.Type.Sliced;
            img.color = Color.white;
            img.raycastTarget = true;

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = img;

            // Button Color Tint Transitions
            ColorBlock cb = btn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.15f, 1.12f, 1.05f, 1.0f);
            cb.pressedColor = new Color(0.78f, 0.72f, 0.65f, 1.0f);
            cb.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.6f);
            cb.colorMultiplier = 1.0f;
            btn.colors = cb;

            if (onClick != null)
            {
                btn.onClick.AddListener(() =>
                {
                    AegisAudioManager.PlayButtonClick();
                    onClick.Invoke();
                });
            }

            // Label Text
            if (!string.IsNullOrEmpty(label))
            {
                GameObject textObj = CreateUIObject(btnObj.transform, "Label", btnSize - new Vector2(20, 8), Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
                RectTransform tRt = textObj.GetComponent<RectTransform>();
                tRt.offsetMin = new Vector2(10, 4);
                tRt.offsetMax = new Vector2(-10, -4);

                TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
                tmp.font = GetFont();
                tmp.text = label;
                tmp.fontSize = style == MedievalButtonStyle.WoodSmall ? 15 : 19;
                tmp.fontStyle = FontStyles.Bold;
                tmp.alignment = TextAlignmentOptions.Center;

                // Text Color based on Button Style
                if (style == MedievalButtonStyle.GoldChampion || style == MedievalButtonStyle.GoldBlank)
                {
                    tmp.color = InkDark; // Ink on gold parchment
                }
                else if (style == MedievalButtonStyle.WoodCarved || style == MedievalButtonStyle.BlackBanner)
                {
                    tmp.color = GoldBright; // Gold text on dark wood
                }
                else
                {
                    tmp.color = GoldBright;
                }

                tmp.raycastTarget = false;

                // Subtle shadow
                Shadow sh = textObj.AddComponent<Shadow>();
                sh.effectColor = (style == MedievalButtonStyle.GoldChampion) ? new Color(1f, 1f, 1f, 0.4f) : new Color(0f, 0f, 0f, 0.8f);
                sh.effectDistance = new Vector2(1, -1);
            }

            return btnObj;
        }

        /// <summary>
        /// Creates a text component with standardized medieval font styling and colors.
        /// </summary>
        public static TextMeshProUGUI CreateText(
            Transform parent,
            string name,
            string content,
            float fontSize = 20,
            Color? color = null,
            TextAlignmentOptions alignment = TextAlignmentOptions.Center,
            FontStyles fontStyle = FontStyles.Normal
        )
        {
            GameObject obj = CreateUIObject(parent, name, new Vector2(200, 40), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.font = GetFont();
            tmp.text = content;
            tmp.fontSize = fontSize;
            tmp.fontStyle = fontStyle;
            tmp.color = color ?? GoldBright;
            tmp.alignment = alignment;
            tmp.raycastTarget = false;
            return tmp;
        }

        /// <summary>
        /// Creates a Parchment card container for content panels, settings rows, or piece info.
        /// </summary>
        public static GameObject CreateParchmentCard(Transform parent, string name, Vector2 size, MedievalFrameStyle style = MedievalFrameStyle.ParchmentCardMid)
        {
            GameObject card = CreateUIObject(parent, name, size, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            Image img = card.AddComponent<Image>();
            img.sprite = MedievalUIAssetLoader.GetFrameSprite(style);
            img.type = Image.Type.Sliced;
            img.color = Color.white;
            img.raycastTarget = true;
            return card;
        }

        /// <summary>
        /// Creates an antique Banner for Tips or Warnings (Manuscript Ribbon).
        /// </summary>
        public static GameObject CreateAlertBanner(Transform parent, string name, string message, bool isWarning, Vector2 size)
        {
            GameObject banner = CreateUIObject(parent, name, size, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            Image img = banner.AddComponent<Image>();
            img.sprite = MedievalUIAssetLoader.GetSprite(isWarning ? "banner_warning_full" : "banner_tip_full");
            img.type = Image.Type.Sliced;
            img.raycastTarget = false;

            // Text inside banner
            GameObject textObj = CreateUIObject(banner.transform, "BannerText", size - new Vector2(50, 10), Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
            RectTransform tRt = textObj.GetComponent<RectTransform>();
            tRt.offsetMin = new Vector2(40, 5);
            tRt.offsetMax = new Vector2(-15, -5);

            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.font = GetFont();
            tmp.text = message;
            tmp.fontSize = 15;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Left;
            tmp.color = isWarning ? new Color(0.45f, 0.08f, 0.08f, 1f) : InkDark;
            tmp.raycastTarget = false;

            return banner;
        }

        /// <summary>
        /// Creates an obsidian leather ribbon banner with antique gold rims for section titles or headers.
        /// </summary>
        public static GameObject CreateRibbonHeader(Transform parent, string name, string text, Vector2 size)
        {
            GameObject headerObj = CreateUIObject(parent, name, size, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
            Image img = headerObj.AddComponent<Image>();
            Sprite sp = MedievalUIAssetLoader.GetFrameSprite(MedievalFrameStyle.BannerBlackGold);
            if (sp != null)
            {
                img.sprite = sp;
                img.type = Image.Type.Sliced;
            }
            img.raycastTarget = false;

            if (!string.IsNullOrEmpty(text))
            {
                GameObject txtObj = new GameObject("Text");
                txtObj.transform.SetParent(headerObj.transform, false);
                RectTransform trt = txtObj.AddComponent<RectTransform>();
                trt.anchorMin = Vector2.zero;
                trt.anchorMax = Vector2.one;
                trt.offsetMin = new Vector2(30, 4);
                trt.offsetMax = new Vector2(-30, -4);

                TextMeshProUGUI tmp = txtObj.AddComponent<TextMeshProUGUI>();
                tmp.font = GetFont();
                tmp.text = text;
                tmp.fontSize = 20;
                tmp.fontStyle = FontStyles.Bold;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = GoldBright;
                tmp.raycastTarget = false;

                Shadow sh = txtObj.AddComponent<Shadow>();
                sh.effectColor = new Color(0f, 0f, 0f, 0.85f);
                sh.effectDistance = new Vector2(1, -1);
            }

            return headerObj;
        }

        /// <summary>
        /// Creates an antique brass slider with dark wood groove.
        /// </summary>
        public static GameObject CreateSlider(
            Transform parent,
            string name,
            float minValue,
            float maxValue,
            float currentValue,
            Action<float> onValueChanged,
            float width = 280,
            float height = 30
        )
        {
            GameObject sliderObj = CreateUIObject(parent, name, new Vector2(width, height), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            Slider slider = sliderObj.AddComponent<Slider>();

            // Background track
            GameObject bgObj = CreateUIObject(sliderObj.transform, "Background", Vector2.zero, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
            Image bgImg = bgObj.AddComponent<Image>();
            bgImg.sprite = MedievalUIAssetLoader.GetSprite("slider_bar_long");
            bgImg.type = Image.Type.Sliced;
            bgImg.color = new Color(0.35f, 0.28f, 0.20f, 1.0f);

            // Fill area
            GameObject fillArea = CreateUIObject(sliderObj.transform, "Fill Area", Vector2.zero, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
            RectTransform fillAreaRt = fillArea.GetComponent<RectTransform>();
            fillAreaRt.offsetMin = new Vector2(10, 5);
            fillAreaRt.offsetMax = new Vector2(-10, -5);

            GameObject fill = CreateUIObject(fillArea.transform, "Fill", Vector2.zero, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
            Image fillImg = fill.AddComponent<Image>();
            fillImg.color = AntiqueGold;
            fillImg.type = Image.Type.Sliced;

            // Handle slide area & knob
            GameObject handleArea = CreateUIObject(sliderObj.transform, "Handle Slide Area", Vector2.zero, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
            RectTransform handleAreaRt = handleArea.GetComponent<RectTransform>();
            handleAreaRt.offsetMin = new Vector2(10, 0);
            handleAreaRt.offsetMax = new Vector2(-10, 0);

            GameObject handle = CreateUIObject(handleArea.transform, "Handle", new Vector2(24, 24), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f));
            Image handleImg = handle.AddComponent<Image>();
            handleImg.sprite = MedievalUIAssetLoader.GetSprite("avatar_frame_gold");
            handleImg.color = GoldBright;

            slider.targetGraphic = handleImg;
            slider.fillRect = fill.GetComponent<RectTransform>();
            slider.handleRect = handle.GetComponent<RectTransform>();
            slider.minValue = minValue;
            slider.maxValue = maxValue;
            slider.value = currentValue;

            if (onValueChanged != null)
            {
                slider.onValueChanged.AddListener((val) => onValueChanged(val));
            }

            return sliderObj;
        }

        /// <summary>
        /// Creates a Medieval Toggle switch with authentic On/Off lever sprite states.
        /// </summary>
        public static GameObject CreateToggle(
            Transform parent,
            string name,
            string label,
            bool initialValue,
            Action<bool> onToggle,
            float width = 280
        )
        {
            GameObject toggleObj = CreateUIObject(parent, name, new Vector2(width, 42), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            Toggle toggle = toggleObj.AddComponent<Toggle>();

            // Label on the left
            GameObject labelObj = CreateUIObject(toggleObj.transform, "Label", new Vector2(width - 60, 40), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f));
            RectTransform lRt = labelObj.GetComponent<RectTransform>();
            lRt.anchoredPosition = new Vector2(10, 0);

            TextMeshProUGUI tmp = labelObj.AddComponent<TextMeshProUGUI>();
            tmp.font = GetFont();
            tmp.text = label;
            tmp.fontSize = 18;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Left;
            tmp.color = GoldBright;
            tmp.raycastTarget = false;

            // Switch Graphic on the right
            GameObject switchObj = CreateUIObject(toggleObj.transform, "Switch", new Vector2(58, 26), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f));
            RectTransform sRt = switchObj.GetComponent<RectTransform>();
            sRt.anchoredPosition = new Vector2(-10, 0);

            Image switchImg = switchObj.AddComponent<Image>();
            switchImg.sprite = MedievalUIAssetLoader.GetSprite(initialValue ? "toggle_switch_on" : "toggle_switch_off");
            switchImg.preserveAspect = true;
            switchImg.raycastTarget = true;

            toggle.targetGraphic = switchImg;
            toggle.isOn = initialValue;

            toggle.onValueChanged.AddListener((isOn) =>
            {
                switchImg.sprite = MedievalUIAssetLoader.GetSprite(isOn ? "toggle_switch_on" : "toggle_switch_off");
                AegisAudioManager.PlayButtonClick();
                onToggle?.Invoke(isOn);
            });

            return toggleObj;
        }

        /// <summary>
        /// Creates a Medieval Checkbox Field with antique carved frame and golden checkmark.
        /// </summary>
        public static GameObject CreateCheckboxField(Transform parent, string name, string label, bool initialValue, Action<bool> onToggle)
        {
            GameObject toggleObj = CreateUIObject(parent, name, new Vector2(280, 40), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            Toggle toggle = toggleObj.AddComponent<Toggle>();

            // Box Graphic on the left
            GameObject boxObj = CreateUIObject(toggleObj.transform, "Box", new Vector2(32, 32), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f));
            RectTransform bRt = boxObj.GetComponent<RectTransform>();
            bRt.anchoredPosition = new Vector2(5, 0);

            Image boxImg = boxObj.AddComponent<Image>();
            boxImg.sprite = MedievalUIAssetLoader.GetSprite(initialValue ? "checkbox_checked" : "checkbox_unchecked");
            boxImg.preserveAspect = true;
            boxImg.raycastTarget = true;

            // Label on the right
            GameObject labelObj = CreateUIObject(toggleObj.transform, "Label", new Vector2(230, 36), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f));
            RectTransform lRt = labelObj.GetComponent<RectTransform>();
            lRt.anchoredPosition = new Vector2(45, 0);

            TextMeshProUGUI tmp = labelObj.AddComponent<TextMeshProUGUI>();
            tmp.font = GetFont();
            tmp.text = label;
            tmp.fontSize = 18;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Left;
            tmp.color = GoldBright;
            tmp.raycastTarget = false;

            toggle.targetGraphic = boxImg;
            toggle.isOn = initialValue;

            toggle.onValueChanged.AddListener((isOn) =>
            {
                boxImg.sprite = MedievalUIAssetLoader.GetSprite(isOn ? "checkbox_checked" : "checkbox_unchecked");
                AegisAudioManager.PlayButtonClick();
                onToggle?.Invoke(isOn);
            });

            return toggleObj;
        }

        /// <summary>
        /// Creates an ornate Gold Medallion Avatar Frame.
        /// </summary>
        public static GameObject CreateAvatarMedallion(Transform parent, string name, Sprite avatarSprite, float size = 80)
        {
            GameObject medallion = CreateUIObject(parent, name, new Vector2(size, size), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));

            // Avatar photo/art underneath
            if (avatarSprite != null)
            {
                GameObject innerArt = CreateUIObject(medallion.transform, "AvatarArt", new Vector2(size * 0.62f, size * 0.62f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
                Image artImg = innerArt.AddComponent<Image>();
                artImg.sprite = avatarSprite;
                artImg.preserveAspect = true;
                artImg.raycastTarget = false;
            }

            // Gilded Frame on top
            GameObject frame = CreateUIObject(medallion.transform, "Frame", new Vector2(size, size), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            Image fImg = frame.AddComponent<Image>();
            fImg.sprite = (avatarSprite != null)
                ? (MedievalUIAssetLoader.GetSprite("avatar_frame_gold_hollow") ?? MedievalUIAssetLoader.GetSprite("avatar_frame_gold"))
                : (MedievalUIAssetLoader.GetSprite("avatar_frame_gold") ?? MedievalUIAssetLoader.GetSprite("avatar_medallion_gold"));
            fImg.preserveAspect = true;
            fImg.raycastTarget = false;

            return medallion;
        }

        /// <summary>
        /// Creates an antique Currency Capsule for Coins or Gems.
        /// </summary>
        public static GameObject CreateCurrencyPill(Transform parent, string name, bool isGem, int amount)
        {
            Vector2 pillSize = new Vector2(120, 44);
            GameObject pill = CreateUIObject(parent, name, pillSize, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));

            Image img = pill.AddComponent<Image>();
            img.sprite = MedievalUIAssetLoader.GetSprite(isGem ? "counter_gem_capsule" : "counter_gold_capsule");
            img.preserveAspect = true;
            img.raycastTarget = false;

            // Value text
            Vector2 textDim = new Vector2(65, 30);
            GameObject textObj = CreateUIObject(pill.transform, "Value", textDim, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            RectTransform tRt = textObj.GetComponent<RectTransform>();
            tRt.anchoredPosition = new Vector2(4, 0);

            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.font = GetFont();
            tmp.text = amount.ToString("N0");
            tmp.fontSize = 15;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = GoldBright;
            tmp.raycastTarget = false;

            return pill;
        }
    }
}
