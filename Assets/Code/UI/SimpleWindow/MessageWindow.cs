using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LP.UI.SimpleWindow
{
    public enum MessageBoxAnswer
    {
        Ok,
        Close,
    }

    public class MessageWindow : AbstractSimpleWindow
    {
        [SerializeField] TextMeshProUGUI _message = default;

        public event Action<MessageBoxAnswer> OnFinish = delegate(MessageBoxAnswer answer) { };

        public void Setup(string message)
        {
            _message.text = message;
        }

        protected override void OnClickOkInternal()
        {
            OnFinish(MessageBoxAnswer.Ok);
        }

        protected override void OnClickCloseInternal()
        {
            OnFinish(MessageBoxAnswer.Close);
        }
    }
}