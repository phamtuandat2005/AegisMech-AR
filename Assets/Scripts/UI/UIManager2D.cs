using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MechChess.ProceduralMech;

namespace MechChess.UI
{
    /// <summary>
    /// Complete 2D HUD Manager: Turn countdown timers, algebraic move history,
    /// real-time status indicators for AI Server & Adafruit Cloud, and game control buttons.
    /// </summary>
    public class UIManager2D : MonoBehaviour
    {
        public static UIManager2D Instance { get; private set; }

        [Header("State")]
        private float _whiteTimer = 600f; // 10 minutes
        private float _blackTimer = 600f;
        private PieceColor _activeColor = PieceColor.White;
        private bool _isTimerRunning = true;

        private List<string> _moveHistory = new List<string>();

        // UI Element References
        private Text _whiteTimerText;
        private Text _blackTimerText;
        private Text _turnIndicatorText;
        private Text _moveHistoryText;
        private Text _aiServerStatusText;
        private Text _cloudStatusText;
        private Text _lastConfidenceText;

        public Action OnScanCardClicked;
        public Action OnToggleARClicked;
        public Action OnResetGameClicked;
        public Action OnTriggerAIClicked;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            BuildHUD();
        }

        private void BuildHUD()
        {
            // Canvas
            GameObject canvasObj = new GameObject("HUD_Canvas");
            canvasObj.transform.SetParent(transform, false);
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasObj.AddComponent<GraphicRaycaster>();

            Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // 1. TOP HEADER: Status Badges & Timers
            GameObject topBar = CreatePanel(canvasObj.transform, "TopBar", new Vector2(0, 480), new Vector2(1880, 80), new Color(0.05f, 0.08f, 0.12f, 0.85f));

            // Title & Active Turn
            _turnIndicatorText = CreateText(topBar.transform, "TurnIndicator", "CURRENT TURN: [CYBER ALLIANCE - WHITE]", 22, TextAnchor.MiddleLeft, new Vector2(-420, 0), new Vector2(500, 50), new Color(0.0f, 0.95f, 1.0f), defaultFont);

            // White Timer
            _whiteTimerText = CreateText(topBar.transform, "WhiteTimer", "WHITE: 10:00", 22, TextAnchor.MiddleCenter, new Vector2(120, 0), new Vector2(180, 50), new Color(0.85f, 0.9f, 1.0f), defaultFont);

            // Black Timer
            _blackTimerText = CreateText(topBar.transform, "BlackTimer", "BLACK: 10:00", 22, TextAnchor.MiddleCenter, new Vector2(320, 0), new Vector2(180, 50), new Color(1.0f, 0.35f, 0.35f), defaultFont);

            // AI Status Badge
            _aiServerStatusText = CreateText(topBar.transform, "AIStatus", "AI SERVER: [ONLINE]", 18, TextAnchor.MiddleRight, new Vector2(620, 0), new Vector2(240, 50), Color.green, defaultFont);

            // Cloud Status Badge
            _cloudStatusText = CreateText(topBar.transform, "CloudStatus", "AIO CLOUD: [CONNECTED]", 18, TextAnchor.MiddleRight, new Vector2(850, 0), new Vector2(240, 50), new Color(0.2f, 0.8f, 1.0f), defaultFont);

            // 2. LEFT PANEL: Move History & Telemetry
            GameObject leftPanel = CreatePanel(canvasObj.transform, "LeftPanel", new Vector2(-780, 0), new Vector2(320, 800), new Color(0.04f, 0.06f, 0.10f, 0.82f));
            CreateText(leftPanel.transform, "HistoryTitle", "-- ALGEBRAIC MOVE LOG --", 18, TextAnchor.MiddleCenter, new Vector2(0, 360), new Vector2(300, 40), new Color(0.0f, 0.95f, 1.0f), defaultFont);

            _moveHistoryText = CreateText(leftPanel.transform, "HistoryLog", "Match started.\nAwaiting initial moves...", 16, TextAnchor.UpperLeft, new Vector2(0, 0), new Vector2(290, 680), Color.white, defaultFont);

            // Confidence display
            _lastConfidenceText = CreateText(leftPanel.transform, "ConfidenceDisplay", "AI Confidence: N/A", 15, TextAnchor.MiddleCenter, new Vector2(0, -360), new Vector2(300, 30), Color.yellow, defaultFont);

            // 3. BOTTOM RIGHT CONTROLS
            GameObject controlsPanel = CreatePanel(canvasObj.transform, "ControlsPanel", new Vector2(740, -420), new Vector2(400, 180), new Color(0.04f, 0.06f, 0.10f, 0.85f));

            CreateButton(controlsPanel.transform, "BtnScanCard", "SCAN TACTICAL CARD (AI)", new Vector2(-95, 40), new Vector2(190, 50), new Color(0.0f, 0.5f, 0.8f), () => OnScanCardClicked?.Invoke(), defaultFont);
            CreateButton(controlsPanel.transform, "BtnAIMove", "TRIGGER AI MOVE", new Vector2(95, 40), new Vector2(170, 50), new Color(0.7f, 0.2f, 0.2f), () => OnTriggerAIClicked?.Invoke(), defaultFont);
            CreateButton(controlsPanel.transform, "BtnToggleAR", "TOGGLE AR/WEBCAM", new Vector2(-95, -35), new Vector2(190, 50), new Color(0.2f, 0.6f, 0.4f), () => OnToggleARClicked?.Invoke(), defaultFont);
            CreateButton(controlsPanel.transform, "BtnReset", "RESET MATCH", new Vector2(95, -35), new Vector2(170, 50), new Color(0.3f, 0.3f, 0.35f), () => OnResetGameClicked?.Invoke(), defaultFont);

            // 4. BOTTOM CENTER HELPER BANNER
            CreateText(canvasObj.transform, "HelpBanner", "🎮 ĐIỀU KHIỂN: Click CHUỘT TRÁI để chọn quân (hiện ô xanh) & đi quân | Kéo CHUỘT PHẢI xoay 360° | Cuộn chuột để Zoom", 16, TextAnchor.MiddleCenter, new Vector2(0, -490), new Vector2(1400, 35), new Color(0.1f, 0.95f, 1.0f), defaultFont);
        }

        private void Update()
        {
            if (!_isTimerRunning) return;

            if (_activeColor == PieceColor.White)
            {
                _whiteTimer = Mathf.Max(0, _whiteTimer - Time.deltaTime);
            }
            else
            {
                _blackTimer = Mathf.Max(0, _blackTimer - Time.deltaTime);
            }

            _whiteTimerText.text = $"WHITE: {FormatTime(_whiteTimer)}";
            _blackTimerText.text = $"BLACK: {FormatTime(_blackTimer)}";
        }

        private string FormatTime(float seconds)
        {
            int min = (int)(seconds / 60);
            int sec = (int)(seconds % 60);
            return $"{min:00}:{sec:00}";
        }

        public void SetActiveTurn(PieceColor color)
        {
            _activeColor = color;
            if (color == PieceColor.White)
            {
                _turnIndicatorText.text = "CURRENT TURN: [CYBER ALLIANCE - WHITE]";
                _turnIndicatorText.color = new Color(0.0f, 0.95f, 1.0f);
            }
            else
            {
                _turnIndicatorText.text = "CURRENT TURN: [DARK SYNDICATE - BLACK]";
                _turnIndicatorText.color = new Color(1.0f, 0.25f, 0.35f);
            }
        }

        public void RecordMove(string moveNotation)
        {
            _moveHistory.Add(moveNotation);
            if (_moveHistory.Count > 18) _moveHistory.RemoveAt(0);

            _moveHistoryText.text = string.Join("\n", _moveHistory);
        }

        public void UpdateAIServerStatus(bool online, string detail = "")
        {
            if (_aiServerStatusText == null) return;
            if (online)
            {
                _aiServerStatusText.text = $"AI SERVER: [ONLINE]";
                _aiServerStatusText.color = Color.green;
            }
            else
            {
                _aiServerStatusText.text = $"AI SERVER: [OFFLINE]";
                _aiServerStatusText.color = Color.red;
            }
        }

        public void UpdateCloudStatus(bool connected, string detail = "")
        {
            if (_cloudStatusText == null) return;
            if (connected)
            {
                _cloudStatusText.text = "AIO CLOUD: [CONNECTED]";
                _cloudStatusText.color = new Color(0.2f, 0.8f, 1.0f);
            }
            else
            {
                _cloudStatusText.text = "AIO CLOUD: [CONNECTING]";
                _cloudStatusText.color = Color.yellow;
            }
        }

        public void UpdateConfidence(float confidence, string cardName)
        {
            if (_lastConfidenceText != null)
            {
                _lastConfidenceText.text = $"AI: {cardName} ({confidence * 100f:0.1f}%)";
            }
        }

        // UI Helpers
        private GameObject CreatePanel(Transform parent, string name, Vector2 pos, Vector2 size, Color color)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            Image img = panel.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            RectTransform rt = panel.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return panel;
        }

        private Text CreateText(Transform parent, string name, string content, int fontSize, TextAnchor anchor, Vector2 pos, Vector2 size, Color color, Font font)
        {
            GameObject txtObj = new GameObject(name);
            txtObj.transform.SetParent(parent, false);
            Text txt = txtObj.AddComponent<Text>();
            txt.text = content;
            txt.font = font;
            txt.fontSize = fontSize;
            txt.alignment = anchor;
            txt.color = color;
            RectTransform rt = txtObj.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return txt;
        }

        private GameObject CreateButton(Transform parent, string name, string label, Vector2 pos, Vector2 size, Color btnColor, Action onClick, Font font)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);
            Image img = btnObj.AddComponent<Image>();
            img.color = btnColor;

            Button btn = btnObj.AddComponent<Button>();
            btn.onClick.AddListener(() => onClick?.Invoke());

            RectTransform rt = btnObj.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            GameObject txtObj = new GameObject("Text");
            txtObj.transform.SetParent(btnObj.transform, false);
            Text txt = txtObj.AddComponent<Text>();
            txt.text = label;
            txt.font = font;
            txt.fontSize = 13;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;

            RectTransform txtRt = txtObj.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = Vector2.zero;
            txtRt.offsetMax = Vector2.zero;

            return btnObj;
        }
    }
}
