using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace AegisMech.UI
{
    public class AegisToast : MonoBehaviour
    {
        public enum Kind { Info, Success, Error }

        public CanvasGroup group;
        public Text label;
        public Image icon;
        public Text iconText;
        public float holdSeconds = 2.2f;
        Coroutine _c;

        public void Show(string message, Kind kind = Kind.Info)
        {
            gameObject.SetActive(true);
            label.text = message;
            Color c = kind == Kind.Success ? AegisTheme.Success : kind == Kind.Error ? AegisTheme.Danger : AegisTheme.Cyan;
            icon.color = c;
            iconText.text = kind == Kind.Success ? "OK" : kind == Kind.Error ? "!" : "i";
            if (_c != null) StopCoroutine(_c);
            _c = StartCoroutine(Run());
        }

        IEnumerator Run()
        {
            float t = 0f;
            while (t < 0.2f) { t += Time.unscaledDeltaTime; group.alpha = t / 0.2f; yield return null; }
            group.alpha = 1f;
            yield return new WaitForSecondsRealtime(holdSeconds);
            t = 0f;
            while (t < 0.3f) { t += Time.unscaledDeltaTime; group.alpha = 1f - t / 0.3f; yield return null; }
            group.alpha = 0f;
            _c = null;
            gameObject.SetActive(false);
        }
    }
}
