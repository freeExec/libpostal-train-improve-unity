using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LP.UI
{
    public class MessageWindow : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI _message = default;
        [SerializeField] Button _buttonOk = default;
        [SerializeField] Button _buttonClose = default;

        public event Action OnEditFinish = delegate() { };

        private void Start()
        {
            _buttonOk.onClick.AddListener(OnClickClose);
            _buttonClose.onClick.AddListener(OnClickClose);
        }

        public void Setup(string message)
        {
            _message.text = message;
        }

        private void OnClickClose()
        {
            OnEditFinish();
            gameObject.SetActive(false);
        }
    }
}