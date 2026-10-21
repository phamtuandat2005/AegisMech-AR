using UnityEngine;
using UnityEngine.UI;

namespace AegisMech.UI
{
    public class AegisAIDifficultyScreen : AegisScreen
    {
        public Button[] levelButtons;
        public Image[] levelBgs;
        public Image[] levelDots;
        public Sprite spriteOn, spriteOff;
        public AegisChoice timeChoice;
        public Toggle whiteToggle;
        public Button startButton;

        static readonly int[] Times = { 5, 10, 15, 30, 0 };
        int _level = 1;
        bool _wired;

        public override void OnShown()
        {
            if (!_wired) { Wire(); _wired = true; }
            _level = Mathf.Clamp(AegisPrefs.GetInt("aiLevel", 1), 0, levelButtons.Length - 1);
            Refresh();
        }

        void Wire()
        {
            for (int i = 0; i < levelButtons.Length; i++)
            {
                int c = i;
                levelButtons[i].onClick.AddListener(() => SetLevel(c));
            }
            startButton.onClick.AddListener(StartGame);
        }

        void SetLevel(int i)
        {
            _level = i;
            AegisPrefs.SetInt("aiLevel", i);
            Refresh();
        }

        void Refresh()
        {
            for (int i = 0; i < levelButtons.Length; i++)
            {
                bool on = i == _level;
                levelBgs[i].sprite = on ? spriteOn : spriteOff;
                levelDots[i].enabled = on;
            }
        }

        void StartGame()
        {
            AegisGameConfig.Mode = AegisGameMode.VsAI;
            AegisGameConfig.AiLevel = _level;
            AegisGameConfig.TimeMinutes = Times[Mathf.Clamp(timeChoice.index, 0, Times.Length - 1)];
            AegisGameConfig.PlayerIsWhite = whiteToggle.isOn;
            AegisUIManager.Toast("Bắt đầu ván đấu với AI...", AegisToast.Kind.Success);
            AegisGameConfig.RequestStart();
        }
    }
}
