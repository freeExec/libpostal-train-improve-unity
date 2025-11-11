using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LP.UI
{
    public enum MessageBoxAnswer
    {
        Ok,
        Close,
    }

    public class MessageWindow : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI _message = default;
        [SerializeField] Button _buttonOk = default;
        [SerializeField] Button _buttonClose = default;

        public event Action<MessageBoxAnswer> OnFinish = delegate(MessageBoxAnswer answer) { };

        private void Start()
        {
            _buttonOk.onClick.AddListener(OnClickOk);
            _buttonClose.onClick.AddListener(OnClickClose);
        }

        public void Setup(string message)
        {
            _message.text = message;
        }

        private void OnClickOk()
        {
            OnFinish(MessageBoxAnswer.Ok);
            gameObject.SetActive(false);
        }

        private void OnClickClose()
        {
            OnFinish(MessageBoxAnswer.Close);
            gameObject.SetActive(false);
        }
    }
}