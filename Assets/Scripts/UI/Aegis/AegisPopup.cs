using System;
using UnityEngine;
using UnityEngine.UI;

namespace AegisMech.UI
{
    public class AegisPopup : MonoBehaviour
    {
        public Text title, message, confirmLabel;
        public Button confirm, cancel;
        Action _onConfirm;
        bool _wired;

        public bool IsOpen { get { return gameObject.activeSelf; } }

        public void Show(string t, string m, Action onConfirm, string confirmText = null)
        {
            gameObject.SetActive(true);
            if (!_wired)
            {
                confirm.onClick.AddListener(OnConfirm);
                cancel.onClick.AddListener(Close);
                _wired = true;
            }
            title.text = t;
            message.text = m;
            _onConfirm = onConfirm;
            if (confirmLabel != null) confirmLabel.text = string.IsNullOrEmpty(confirmText) ? "Xác nhận" : confirmText;
        }

        void OnConfirm()
        {
            var a = _onConfirm;
            Close();
            if (a != null) a();
        }

        public void Close()
        {
            _onConfirm = null;
            gameObject.SetActive(false);
        }
    }
}
