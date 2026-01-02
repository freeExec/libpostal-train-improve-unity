using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LP.UI.SimpleWindow
{
    public abstract class AbstractSimpleWindow : MonoBehaviour
    {
        [SerializeField] Button _buttonOk = default;
        [SerializeField] Button _buttonClose = default;

        protected abstract void OnClickOkInternal();
        protected abstract void OnClickCloseInternal();

        protected virtual void Start()
        {
            _buttonOk.onClick.AddListener(OnClickOk);
            _buttonClose.onClick.AddListener(OnClickClose);
        }

        private void OnClickOk()
        {
            OnClickOkInternal();
            gameObject.SetActive(false);
        }

        private void OnClickClose()
        {
            OnClickCloseInternal();
            gameObject.SetActive(false);
        }
    }
}