using System.Collections;
using UnityEngine;

namespace AegisMech.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public class AegisScreen : MonoBehaviour
    {
        public AegisScreenId id;
        CanvasGroup _cg;
        Coroutine _fade;

        public CanvasGroup Group
        {
            get { if (_cg == null) _cg = GetComponent<CanvasGroup>(); return _cg; }
        }

        public virtual void OnShown() { }

        public void Show(bool instant = false)
        {
            gameObject.SetActive(true);
            Group.blocksRaycasts = true;
            Group.interactable = true;
            Fade(1f, instant ? 0f : 0.25f, false);
            OnShown();
        }

        public void Hide(bool instant = false)
        {
            if (!gameObject.activeSelf) return;
            Group.blocksRaycasts = false;
            Group.interactable = false;
            Fade(0f, instant ? 0f : 0.18f, true);
        }

        void Fade(float to, float dur, bool deactivate)
        {
            if (_fade != null) { StopCoroutine(_fade); _fade = null; }
            if (dur <= 0f || !isActiveAndEnabled)
            {
                Group.alpha = to;
                if (deactivate) gameObject.SetActive(false);
                return;
            }
            _fade = StartCoroutine(FadeRoutine(to, dur, deactivate));
        }

        IEnumerator FadeRoutine(float to, float dur, bool deactivate)
        {
            float from = Group.alpha, t = 0f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                Group.alpha = Mathf.Lerp(from, to, t / dur);
                yield return null;
            }
            Group.alpha = to;
            _fade = null;
            if (deactivate) gameObject.SetActive(false);
        }
    }
}
