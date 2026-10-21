using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace AegisMech.UI
{
    public class AegisSplashScreen : AegisScreen
    {
        public Image fill;
        public Text percent;
        public float duration = 2.6f;
        Coroutine _run;

        public override void OnShown()
        {
            if (_run != null) StopCoroutine(_run);
            _run = StartCoroutine(Run());
        }

        IEnumerator Run()
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / duration));
                if (fill != null) fill.fillAmount = p;
                if (percent != null) percent.text = Mathf.RoundToInt(p * 100f) + "%";
                yield return null;
            }
            _run = null;
            var m = AegisUIManager.Instance;
            if (m != null) m.Go(AegisScreenId.MainMenu, false);
        }
    }
}
