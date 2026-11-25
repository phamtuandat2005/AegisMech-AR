using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MechChess.UI
{
    /// <summary>
    /// InGameMusicSelectorUI: In-game Music Selector & Jukebox modal.
    /// Provides both a compact HUD widget (track name + quick play/next)
    /// and a comprehensive Playlist Modal where players can select background music.
    /// </summary>
    public class InGameMusicSelectorUI : MonoBehaviour
    {
        public static InGameMusicSelectorUI Instance { get; private set; }

        private static GameObject _modalInstance;
        private static GameObject _hudWidgetInstance;

        // Colors matching Aegis Arcane sci-fi palette
        private static readonly Color ColCyanGlow = new Color(0.0f, 0.92f, 1.0f);
        private static readonly Color ColGold = new Color(0.96f, 0.80f, 0.32f);
        private static readonly Color ColTextWhite = new Color(0.92f, 0.95f, 1.0f);
        private static readonly Color ColTextMuted = new Color(0.60f, 0.65f, 0.75f);
        private static readonly Color ColBgDark = new Color(0.04f, 0.06f, 0.10f, 0.95f);
        private static readonly Color ColCardBg = new Color(0.08f, 0.12f, 0.18f, 0.90f);
        private static readonly Color ColBtnCyan = new Color(0.0f, 0.55f, 0.85f, 0.95f);

        private Text _nowPlayingTitleTxt;
        private Text _nowPlayingArtistTxt;
        private Text _playPauseBtnTxt;
        private Text _volValueTxt;
        private Slider _volSlider;
        private Transform _playlistContent;
        private Font _cachedFont;

        public static void ToggleSelector(Transform parent = null)
        {
            if (_modalInstance != null && _modalInstance.activeSelf)
            {
                CloseModal();
            }
            else
            {
                OpenModal(parent);
            }
        }

        public static void OpenModal(Transform parent = null)
        {
            AegisMusicManager.EnsureInitialized();

            if (_modalInstance != null)
            {
                _modalInstance.SetActive(true);
                InGameMusicSelectorUI ui = _modalInstance.GetComponent<InGameMusicSelectorUI>();
                if (ui != null) ui.RefreshUI();
                return;
            }

            if (parent == null)
            {
                Canvas canvas = FindAnyObjectByType<Canvas>();
                if (canvas != null) parent = canvas.transform;
            }

            if (parent == null) return;

            GameObject modal = new GameObject("InGameMusicSelector_Modal");
            modal.transform.SetParent(parent, false);
            _modalInstance = modal;

            InGameMusicSelectorUI comp = modal.AddComponent<InGameMusicSelectorUI>();
            comp.BuildModalUI();
        }

        public static void CloseModal()
        {
            if (_modalInstance != null)
            {
                _modalInstance.SetActive(false);
            }
        }

        /// <summary>
        /// Attaches a compact floating HUD widget to the in-game canvas.
        /// Anchored by default to the bottom-right corner to prevent any collision with top clocks or sidebars.
        /// </summary>
        public static GameObject CreateHUDWidget(Transform hudParent, Vector2 anchoredPos)
        {
            AegisMusicManager.EnsureInitialized();

            GameObject widget = new GameObject("MusicHUD_Widget");
            widget.transform.SetParent(hudParent, false);
            _hudWidgetInstance = widget;

            RectTransform rt = widget.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(1f, 0f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = new Vector2(290f, 40f);

            // Capsule background with sleek cyber frosted border
            Image bg = widget.AddComponent<Image>();
            bg.sprite = ArcaneAssetLoader.LoadArcaneSprite("hud_ar_capsule_clean.png") ?? ArcaneAssetLoader.GetPixelSprite();
            bg.color = new Color(0.04f, 0.08f, 0.14f, 0.92f);
            bg.type = Image.Type.Sliced;

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.Load<Font>("Fonts/AegisFont");

            // Music note icon
            GameObject iconObj = new GameObject("NoteIcon");
            iconObj.transform.SetParent(widget.transform, false);
            RectTransform iconRt = iconObj.AddComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0f, 0.5f);
            iconRt.anchorMax = new Vector2(0f, 0.5f);
            iconRt.pivot = new Vector2(0f, 0.5f);
            iconRt.anchoredPosition = new Vector2(12f, 0f);
            iconRt.sizeDelta = new Vector2(22f, 22f);
            Text iconTxt = iconObj.AddComponent<Text>();
            iconTxt.font = font;
            iconTxt.text = "🎵";
            iconTxt.fontSize = 16;
            iconTxt.alignment = TextAnchor.MiddleCenter;
            iconTxt.color = ColCyanGlow;

            // Song Title Button (Clicking opens playlist modal)
            GameObject titleBtnObj = new GameObject("TitleBtn");
            titleBtnObj.transform.SetParent(widget.transform, false);
            RectTransform tbRt = titleBtnObj.AddComponent<RectTransform>();
            tbRt.anchorMin = new Vector2(0f, 0f);
            tbRt.anchorMax = new Vector2(1f, 1f);
            tbRt.offsetMin = new Vector2(36f, 0f);
            tbRt.offsetMax = new Vector2(-98f, 0f);
            Button titleBtn = titleBtnObj.AddComponent<Button>();
            titleBtn.onClick.AddListener(() => OpenModal(hudParent));

            Text titleTxt = titleBtnObj.AddComponent<Text>();
            titleTxt.font = font;
            titleTxt.fontSize = 13;
            titleTxt.fontStyle = FontStyle.Bold;
            titleTxt.alignment = TextAnchor.MiddleLeft;
            titleTxt.color = ColTextWhite;
            titleTxt.horizontalOverflow = HorizontalWrapMode.Wrap;
            titleTxt.verticalOverflow = VerticalWrapMode.Truncate;

            void UpdateTitle()
            {
                if (titleTxt == null) return;
                var cur = AegisMusicManager.Instance != null ? AegisMusicManager.Instance.CurrentTrack : null;
                titleTxt.text = cur != null ? cur.title : "Chọn Nhạc...";
            }
            UpdateTitle();
            AegisMusicManager.OnTrackChanged += UpdateTitle;

            // Play/Pause button
            GameObject ppObj = new GameObject("BtnPlayPause");
            ppObj.transform.SetParent(widget.transform, false);
            RectTransform ppRt = ppObj.AddComponent<RectTransform>();
            ppRt.anchorMin = new Vector2(1f, 0.5f);
            ppRt.anchorMax = new Vector2(1f, 0.5f);
            ppRt.pivot = new Vector2(1f, 0.5f);
            ppRt.anchoredPosition = new Vector2(-54f, 0f);
            ppRt.sizeDelta = new Vector2(38f, 32f);

            Button ppBtn = ppObj.AddComponent<Button>();
            Text ppTxt = ppObj.AddComponent<Text>();
            ppTxt.font = font;
            ppTxt.fontSize = 15;
            ppTxt.alignment = TextAnchor.MiddleCenter;
            ppTxt.color = ColCyanGlow;
            ppTxt.text = (AegisMusicManager.Instance != null && AegisMusicManager.Instance.IsPlaying) ? "⏸" : "▶";

            ppBtn.onClick.AddListener(() =>
            {
                if (AegisMusicManager.Instance != null)
                {
                    AegisMusicManager.Instance.TogglePlayPause();
                    ppTxt.text = AegisMusicManager.Instance.IsPlaying ? "⏸" : "▶";
                }
            });

            // Next button
            GameObject nextObj = new GameObject("BtnNext");
            nextObj.transform.SetParent(widget.transform, false);
            RectTransform nRt = nextObj.AddComponent<RectTransform>();
            nRt.anchorMin = new Vector2(1f, 0.5f);
            nRt.anchorMax = new Vector2(1f, 0.5f);
            nRt.pivot = new Vector2(1f, 0.5f);
            nRt.anchoredPosition = new Vector2(-12f, 0f);
            nRt.sizeDelta = new Vector2(38f, 32f);

            Button nextBtn = nextObj.AddComponent<Button>();
            Text nextTxt = nextObj.AddComponent<Text>();
            nextTxt.font = font;
            nextTxt.fontSize = 15;
            nextTxt.alignment = TextAnchor.MiddleCenter;
            nextTxt.color = ColCyanGlow;
            nextTxt.text = "⏭";
            nextBtn.onClick.AddListener(() =>
            {
                if (AegisMusicManager.Instance != null) AegisMusicManager.Instance.NextTrack();
            });

            return widget;
        }

        private void Awake()
        {
            Instance = this;
            _cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.Load<Font>("Fonts/AegisFont");
        }

        private void OnEnable()
        {
            AegisMusicManager.OnTrackChanged += RefreshUI;
            AegisMusicManager.OnPlayStateChanged += OnPlayState;
            AegisMusicManager.OnVolumeChanged += OnVolumeChange;
        }

        private void OnDisable()
        {
            AegisMusicManager.OnTrackChanged -= RefreshUI;
            AegisMusicManager.OnPlayStateChanged -= OnPlayState;
            AegisMusicManager.OnVolumeChanged -= OnVolumeChange;
        }

        private void BuildModalUI()
        {
            RectTransform rootRt = gameObject.AddComponent<RectTransform>();
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.offsetMin = Vector2.zero;
            rootRt.offsetMax = Vector2.zero;

            // 1. Dark Blur Backdrop
            GameObject backdrop = new GameObject("Backdrop");
            backdrop.transform.SetParent(transform, false);
            RectTransform bdRt = backdrop.AddComponent<RectTransform>();
            bdRt.anchorMin = Vector2.zero;
            bdRt.anchorMax = Vector2.one;
            bdRt.offsetMin = Vector2.zero;
            bdRt.offsetMax = Vector2.zero;
            Image bdImg = backdrop.AddComponent<Image>();
            bdImg.color = new Color(0f, 0f, 0f, 0.70f);
            Button bdBtn = backdrop.AddComponent<Button>();
            bdBtn.onClick.AddListener(CloseModal);

            // 2. Central Modal Dialog Card
            GameObject card = new GameObject("MusicPlayer_Card");
            card.transform.SetParent(transform, false);
            RectTransform cRt = card.AddComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0.5f, 0.5f);
            cRt.anchorMax = new Vector2(0.5f, 0.5f);
            cRt.pivot = new Vector2(0.5f, 0.5f);
            cRt.anchoredPosition = Vector2.zero;
            cRt.sizeDelta = new Vector2(800f, 620f);

            Image cardBg = card.AddComponent<Image>();
            cardBg.sprite = ArcaneAssetLoader.LoadArcaneSprite("shop_card_frame_default.png") ?? ArcaneAssetLoader.GetPixelSprite();
            cardBg.color = ColBgDark;
            cardBg.type = Image.Type.Sliced;

            // Header Title
            GameObject titleObj = new GameObject("HeaderTitle");
            titleObj.transform.SetParent(card.transform, false);
            RectTransform htRt = titleObj.AddComponent<RectTransform>();
            htRt.anchorMin = new Vector2(0f, 1f);
            htRt.anchorMax = new Vector2(1f, 1f);
            htRt.pivot = new Vector2(0.5f, 1f);
            htRt.anchoredPosition = new Vector2(0f, -24f);
            htRt.sizeDelta = new Vector2(760f, 40f);
            Text htTxt = titleObj.AddComponent<Text>();
            htTxt.font = _cachedFont;
            htTxt.fontSize = 22;
            htTxt.fontStyle = FontStyle.Bold;
            htTxt.alignment = TextAnchor.MiddleCenter;
            htTxt.color = ColCyanGlow;
            htTxt.text = AegisLocalization.T("🎵 TRÌNH CHỌN NHẠC BẮT ĐẦU VÁN ĐẤU", "🎵 IN-GAME BGM SELECTOR");

            // Close (X) button
            GameObject closeBtnObj = new GameObject("BtnClose");
            closeBtnObj.transform.SetParent(card.transform, false);
            RectTransform cbRt = closeBtnObj.AddComponent<RectTransform>();
            cbRt.anchorMin = new Vector2(1f, 1f);
            cbRt.anchorMax = new Vector2(1f, 1f);
            cbRt.pivot = new Vector2(1f, 1f);
            cbRt.anchoredPosition = new Vector2(-20f, -20f);
            cbRt.sizeDelta = new Vector2(36f, 36f);
            Button closeBtn = closeBtnObj.AddComponent<Button>();
            closeBtn.onClick.AddListener(CloseModal);
            Text cbTxt = closeBtnObj.AddComponent<Text>();
            cbTxt.font = _cachedFont;
            cbTxt.fontSize = 20;
            cbTxt.fontStyle = FontStyle.Bold;
            cbTxt.alignment = TextAnchor.MiddleCenter;
            cbTxt.color = Color.white;
            cbTxt.text = "✕";

            // 3. Now Playing Controller Card
            GameObject npCard = new GameObject("NowPlaying_Card");
            npCard.transform.SetParent(card.transform, false);
            RectTransform npRt = npCard.AddComponent<RectTransform>();
            npRt.anchorMin = new Vector2(0.5f, 1f);
            npRt.anchorMax = new Vector2(0.5f, 1f);
            npRt.pivot = new Vector2(0.5f, 1f);
            npRt.anchoredPosition = new Vector2(0f, -75f);
            npRt.sizeDelta = new Vector2(740f, 120f);

            Image npBg = npCard.AddComponent<Image>();
            npBg.sprite = ArcaneAssetLoader.LoadArcaneSprite("shop_card_frame_cyan.png") ?? ArcaneAssetLoader.GetPixelSprite();
            npBg.color = ColCardBg;
            npBg.type = Image.Type.Sliced;

            // Title
            GameObject nptObj = new GameObject("TrackTitle");
            nptObj.transform.SetParent(npCard.transform, false);
            RectTransform nptRt = nptObj.AddComponent<RectTransform>();
            nptRt.anchorMin = new Vector2(0f, 1f);
            nptRt.anchorMax = new Vector2(0.6f, 1f);
            nptRt.anchoredPosition = new Vector2(25f, -22f);
            nptRt.sizeDelta = new Vector2(400f, 30f);
            _nowPlayingTitleTxt = nptObj.AddComponent<Text>();
            _nowPlayingTitleTxt.font = _cachedFont;
            _nowPlayingTitleTxt.fontSize = 18;
            _nowPlayingTitleTxt.fontStyle = FontStyle.Bold;
            _nowPlayingTitleTxt.alignment = TextAnchor.MiddleLeft;
            _nowPlayingTitleTxt.color = ColGold;

            // Artist
            GameObject npaObj = new GameObject("TrackArtist");
            npaObj.transform.SetParent(npCard.transform, false);
            RectTransform npaRt = npaObj.AddComponent<RectTransform>();
            npaRt.anchorMin = new Vector2(0f, 1f);
            npaRt.anchorMax = new Vector2(0.6f, 1f);
            npaRt.anchoredPosition = new Vector2(25f, -48f);
            npaRt.sizeDelta = new Vector2(400f, 25f);
            _nowPlayingArtistTxt = npaObj.AddComponent<Text>();
            _nowPlayingArtistTxt.font = _cachedFont;
            _nowPlayingArtistTxt.fontSize = 13;
            _nowPlayingArtistTxt.alignment = TextAnchor.MiddleLeft;
            _nowPlayingArtistTxt.color = ColTextMuted;

            // Control Buttons Container
            BuildPlaybackControls(npCard.transform);

            // Volume Section
            BuildVolumeSection(npCard.transform);

            // 4. Playlist View (Scrollable list of tracks)
            BuildPlaylistView(card.transform);

            // 5. Bottom Instructions Box
            BuildFooterHelp(card.transform);

            RefreshUI();
        }

        private void BuildPlaybackControls(Transform parent)
        {
            GameObject controls = new GameObject("PlaybackControls");
            controls.transform.SetParent(parent, false);
            RectTransform rt = controls.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.62f, 0.5f);
            rt.anchorMax = new Vector2(0.98f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, 15f);
            rt.sizeDelta = new Vector2(240f, 44f);

            // Prev Button
            CreateSquareBtn(controls.transform, "BtnPrev", "⏮", new Vector2(-70f, 0f), () =>
            {
                if (AegisMusicManager.Instance != null) AegisMusicManager.Instance.PreviousTrack();
            });

            // Play / Pause Button
            GameObject ppBtnObj = CreateSquareBtn(controls.transform, "BtnPlayPause", "▶", new Vector2(0f, 0f), () =>
            {
                if (AegisMusicManager.Instance != null) AegisMusicManager.Instance.TogglePlayPause();
            });
            _playPauseBtnTxt = ppBtnObj.GetComponentInChildren<Text>();

            // Next Button
            CreateSquareBtn(controls.transform, "BtnNext", "⏭", new Vector2(70f, 0f), () =>
            {
                if (AegisMusicManager.Instance != null) AegisMusicManager.Instance.NextTrack();
            });
        }

        private GameObject CreateSquareBtn(Transform parent, string name, string label, Vector2 pos, Action onClick)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(50f, 38f);

            Image img = obj.AddComponent<Image>();
            img.color = ColBtnCyan;
            Button btn = obj.AddComponent<Button>();
            btn.onClick.AddListener(() => onClick?.Invoke());

            GameObject txtObj = new GameObject("Txt");
            txtObj.transform.SetParent(obj.transform, false);
            RectTransform tRt = txtObj.AddComponent<RectTransform>();
            tRt.anchorMin = Vector2.zero;
            tRt.anchorMax = Vector2.one;
            tRt.offsetMin = Vector2.zero;
            tRt.offsetMax = Vector2.zero;

            Text txt = txtObj.AddComponent<Text>();
            txt.font = _cachedFont;
            txt.fontSize = 18;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
            txt.text = label;

            return obj;
        }

        private void BuildVolumeSection(Transform parent)
        {
            GameObject volContainer = new GameObject("VolumeSection");
            volContainer.transform.SetParent(parent, false);
            RectTransform rt = volContainer.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.62f, 0f);
            rt.anchorMax = new Vector2(0.98f, 0f);
            rt.anchoredPosition = new Vector2(0f, 22f);
            rt.sizeDelta = new Vector2(250f, 24f);

            // Icon
            GameObject iconObj = new GameObject("VolIcon");
            iconObj.transform.SetParent(volContainer.transform, false);
            RectTransform iRt = iconObj.AddComponent<RectTransform>();
            iRt.anchorMin = new Vector2(0f, 0.5f);
            iRt.anchorMax = new Vector2(0f, 0.5f);
            iRt.anchoredPosition = new Vector2(10f, 0f);
            iRt.sizeDelta = new Vector2(24f, 24f);
            Text iTxt = iconObj.AddComponent<Text>();
            iTxt.font = _cachedFont;
            iTxt.fontSize = 14;
            iTxt.alignment = TextAnchor.MiddleCenter;
            iTxt.color = ColTextMuted;
            iTxt.text = "🔊";

            // Value text
            GameObject valObj = new GameObject("VolValue");
            valObj.transform.SetParent(volContainer.transform, false);
            RectTransform vRt = valObj.AddComponent<RectTransform>();
            vRt.anchorMin = new Vector2(1f, 0.5f);
            vRt.anchorMax = new Vector2(1f, 0.5f);
            vRt.anchoredPosition = new Vector2(-20f, 0f);
            vRt.sizeDelta = new Vector2(40f, 24f);
            _volValueTxt = valObj.AddComponent<Text>();
            _volValueTxt.font = _cachedFont;
            _volValueTxt.fontSize = 13;
            _volValueTxt.alignment = TextAnchor.MiddleRight;
            _volValueTxt.color = ColTextWhite;
            _volValueTxt.text = "65%";
        }

        private void BuildPlaylistView(Transform parent)
        {
            GameObject listContainer = new GameObject("PlaylistView");
            listContainer.transform.SetParent(parent, false);
            RectTransform rt = listContainer.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, -40f);
            rt.sizeDelta = new Vector2(740f, 270f);

            Image bg = listContainer.AddComponent<Image>();
            bg.color = new Color(0.02f, 0.04f, 0.07f, 0.85f);

            // Scroll View
            ScrollRect scroll = listContainer.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;

            GameObject viewport = new GameObject("Viewport");
            viewport.transform.SetParent(listContainer.transform, false);
            RectTransform vpRt = viewport.AddComponent<RectTransform>();
            vpRt.anchorMin = Vector2.zero;
            vpRt.anchorMax = Vector2.one;
            vpRt.offsetMin = new Vector2(6f, 6f);
            vpRt.offsetMax = new Vector2(-6f, -6f);
            viewport.AddComponent<RectMask2D>();
            scroll.viewport = vpRt;

            GameObject content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            RectTransform cRt = content.AddComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0f, 1f);
            cRt.anchorMax = new Vector2(1f, 1f);
            cRt.pivot = new Vector2(0f, 1f);
            cRt.sizeDelta = new Vector2(0f, 300f);
            scroll.content = cRt;

            VerticalLayoutGroup vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 6f;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlHeight = false;
            vlg.childControlWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth = true;

            ContentSizeFitter csf = content.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _playlistContent = content.transform;
        }

        private void BuildFooterHelp(Transform parent)
        {
            GameObject footer = new GameObject("FooterHelp");
            footer.transform.SetParent(parent, false);
            RectTransform rt = footer.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, 18f);
            rt.sizeDelta = new Vector2(740f, 54f);

            // Instructions text
            GameObject txtObj = new GameObject("HelpTxt");
            txtObj.transform.SetParent(footer.transform, false);
            RectTransform tRt = txtObj.AddComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0f, 0f);
            tRt.anchorMax = new Vector2(0.72f, 1f);
            tRt.offsetMin = Vector2.zero;
            tRt.offsetMax = Vector2.zero;
            Text hTxt = txtObj.AddComponent<Text>();
            hTxt.font = _cachedFont;
            hTxt.fontSize = 12;
            hTxt.alignment = TextAnchor.MiddleLeft;
            hTxt.color = new Color(0.75f, 0.85f, 0.95f, 0.90f);
            hTxt.text = AegisLocalization.T(
                "📁 Thả file nhạc (.mp3, .wav, .ogg) vào: Assets/Resources/Audio/Music/\nsẽ tự động cập nhật ngay vào danh sách trên!",
                "📁 Drop music files (.mp3, .wav, .ogg) into Assets/Resources/Audio/Music/\nto automatically add your custom songs!"
            );

            // Rescan / Refresh Button
            GameObject rescanBtnObj = new GameObject("BtnRescan");
            rescanBtnObj.transform.SetParent(footer.transform, false);
            RectTransform rbRt = rescanBtnObj.AddComponent<RectTransform>();
            rbRt.anchorMin = new Vector2(0.74f, 0.1f);
            rbRt.anchorMax = new Vector2(0.98f, 0.9f);
            rbRt.offsetMin = Vector2.zero;
            rbRt.offsetMax = Vector2.zero;

            Image rbImg = rescanBtnObj.AddComponent<Image>();
            rbImg.color = new Color(0.12f, 0.35f, 0.55f, 0.95f);
            Button rbBtn = rescanBtnObj.AddComponent<Button>();
            rbBtn.onClick.AddListener(() =>
            {
                if (AegisMusicManager.Instance != null)
                {
                    AegisMusicManager.Instance.ReloadTracks();
                    RefreshUI();
                }
            });

            GameObject rbTxtObj = new GameObject("Txt");
            rbTxtObj.transform.SetParent(rescanBtnObj.transform, false);
            RectTransform rbtRt = rbTxtObj.AddComponent<RectTransform>();
            rbtRt.anchorMin = Vector2.zero;
            rbtRt.anchorMax = Vector2.one;
            rbtRt.offsetMin = Vector2.zero;
            rbtRt.offsetMax = Vector2.zero;
            Text rbtTxt = rbTxtObj.AddComponent<Text>();
            rbtTxt.font = _cachedFont;
            rbtTxt.fontSize = 14;
            rbtTxt.fontStyle = FontStyle.Bold;
            rbtTxt.alignment = TextAnchor.MiddleCenter;
            rbtTxt.color = ColCyanGlow;
            rbtTxt.text = AegisLocalization.T("🔄 Quét Lại", "🔄 Refresh");
        }

        public void RefreshUI()
        {
            if (AegisMusicManager.Instance == null) return;

            var cur = AegisMusicManager.Instance.CurrentTrack;
            if (cur != null)
            {
                if (_nowPlayingTitleTxt != null) _nowPlayingTitleTxt.text = cur.title;
                if (_nowPlayingArtistTxt != null) _nowPlayingArtistTxt.text = $"{cur.artist} • {(int)(cur.duration / 60)}:{(int)(cur.duration % 60):D2}";
            }

            if (_playPauseBtnTxt != null)
            {
                _playPauseBtnTxt.text = AegisMusicManager.Instance.IsPlaying ? "⏸" : "▶";
            }

            if (_volValueTxt != null)
            {
                _volValueTxt.text = $"{(int)(AegisMusicManager.Instance.Volume * 100)}%";
            }

            PopulatePlaylistCards();
        }

        private void PopulatePlaylistCards()
        {
            if (_playlistContent == null || AegisMusicManager.Instance == null) return;

            for (int i = _playlistContent.childCount - 1; i >= 0; i--)
            {
                Destroy(_playlistContent.GetChild(i).gameObject);
            }

            var tracks = AegisMusicManager.Instance.Tracks;
            int curIdx = AegisMusicManager.Instance.CurrentTrackIndex;

            for (int i = 0; i < tracks.Count; i++)
            {
                int trackIdx = i;
                var t = tracks[i];
                bool isCurrent = (i == curIdx);

                GameObject row = new GameObject($"TrackRow_{i}");
                row.transform.SetParent(_playlistContent, false);
                RectTransform rRt = row.AddComponent<RectTransform>();
                rRt.sizeDelta = new Vector2(720f, 48f);

                Image rBg = row.AddComponent<Image>();
                rBg.color = isCurrent ? new Color(0.08f, 0.28f, 0.45f, 0.95f) : new Color(0.06f, 0.10f, 0.16f, 0.85f);

                Button btn = row.AddComponent<Button>();
                btn.onClick.AddListener(() =>
                {
                    AegisMusicManager.Instance.Play(trackIdx);
                    RefreshUI();
                });

                // Index #
                GameObject idxObj = new GameObject("Idx");
                idxObj.transform.SetParent(row.transform, false);
                RectTransform idxRt = idxObj.AddComponent<RectTransform>();
                idxRt.anchorMin = new Vector2(0f, 0.5f);
                idxRt.anchorMax = new Vector2(0f, 0.5f);
                idxRt.anchoredPosition = new Vector2(25f, 0f);
                idxRt.sizeDelta = new Vector2(30f, 30f);
                Text idxTxt = idxObj.AddComponent<Text>();
                idxTxt.font = _cachedFont;
                idxTxt.fontSize = 14;
                idxTxt.alignment = TextAnchor.MiddleCenter;
                idxTxt.color = isCurrent ? ColCyanGlow : ColTextMuted;
                idxTxt.text = $"#{i + 1}";

                // Title
                GameObject titleObj = new GameObject("Title");
                titleObj.transform.SetParent(row.transform, false);
                RectTransform tRt = titleObj.AddComponent<RectTransform>();
                tRt.anchorMin = new Vector2(0f, 0.5f);
                tRt.anchorMax = new Vector2(0.70f, 0.5f);
                tRt.anchoredPosition = new Vector2(65f, 0f);
                tRt.sizeDelta = new Vector2(400f, 30f);
                Text tTxt = titleObj.AddComponent<Text>();
                tTxt.font = _cachedFont;
                tTxt.fontSize = 15;
                tTxt.fontStyle = isCurrent ? FontStyle.Bold : FontStyle.Normal;
                tTxt.alignment = TextAnchor.MiddleLeft;
                tTxt.color = isCurrent ? ColGold : ColTextWhite;
                tTxt.text = t.title;

                // Status Badge
                if (isCurrent)
                {
                    GameObject badgeObj = new GameObject("PlayingBadge");
                    badgeObj.transform.SetParent(row.transform, false);
                    RectTransform bRt = badgeObj.AddComponent<RectTransform>();
                    bRt.anchorMin = new Vector2(1f, 0.5f);
                    bRt.anchorMax = new Vector2(1f, 0.5f);
                    bRt.anchoredPosition = new Vector2(-75f, 0f);
                    bRt.sizeDelta = new Vector2(120f, 26f);
                    Image bBg = badgeObj.AddComponent<Image>();
                    bBg.color = new Color(0.0f, 0.85f, 1.0f, 0.25f);

                    GameObject btObj = new GameObject("Txt");
                    btObj.transform.SetParent(badgeObj.transform, false);
                    RectTransform btRt = btObj.AddComponent<RectTransform>();
                    btRt.anchorMin = Vector2.zero;
                    btRt.anchorMax = Vector2.one;
                    Text btTxt = btObj.AddComponent<Text>();
                    btTxt.font = _cachedFont;
                    btTxt.fontSize = 12;
                    btTxt.fontStyle = FontStyle.Bold;
                    btTxt.alignment = TextAnchor.MiddleCenter;
                    btTxt.color = ColCyanGlow;
                    btTxt.text = "✦ ĐANG PHÁT ✦";
                }
            }
        }

        private void OnPlayState(bool playing)
        {
            if (_playPauseBtnTxt != null) _playPauseBtnTxt.text = playing ? "⏸" : "▶";
        }

        private void OnVolumeChange(float vol)
        {
            if (_volValueTxt != null) _volValueTxt.text = $"{(int)(vol * 100)}%";
        }
    }
}
