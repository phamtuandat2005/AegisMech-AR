using UnityEngine;
using UnityEngine.EventSystems;

namespace AegisMech.UI
{
    public class AegisButtonFx : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        public float hover = 1.04f;
        public float press = 0.96f;
        Vector3 _base = Vector3.one;
        float _target = 1f;
        bool _inside;
        bool _init;

        void OnEnable()
        {
            if (!_init) { _base = transform.localScale; _init = true; }
            transform.localScale = _base;
            _target = 1f;
            _inside = false;
        }

        public void OnPointerEnter(PointerEventData e) { _inside = true; _target = hover; }
        public void OnPointerExit(PointerEventData e) { _inside = false; _target = 1f; }
        public void OnPointerDown(PointerEventData e) { _target = press; }
        public void OnPointerUp(PointerEventData e) { _target = _inside ? hover : 1f; }

        void Update()
        {
            float cur = transform.localScale.x / _base.x;
            if (Mathf.Abs(cur - _target) < 0.0005f) return;
            float n = Mathf.Lerp(cur, _target, 1f - Mathf.Exp(-18f * Time.unscaledDeltaTime));
            transform.localScale = _base * n;
        }
    }
}
