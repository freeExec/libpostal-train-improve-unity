using LP.Data;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace LP.UI.SimpleWindow
{
    public enum SelectRecordMode
    {
        Cancel,
        Normal,
        Completed,
    }

    public class SelectRecordWindow : AbstractSimpleWindow
    {
        [SerializeField] TMPro.TMP_InputField editNumber;
        [SerializeField] Toggle toggleNormal;
        [SerializeField] Toggle toggleCompleted;

        public event Action<SelectRecordMode, int> OnFinish = delegate (SelectRecordMode mode, int number) { };
        public event Action<SelectRecordMode, int> OnChangeNumber = delegate (SelectRecordMode mode, int number) { };

        protected override void Start()
        {
            base.Start();
            editNumber.onValueChanged.AddListener(OnChangeNumberHandler);
        }

        private void OnEnable()
        {
            editNumber.text = string.Empty;
        }

        private void OnChangeNumberHandler(string newValue)
        {
            if (int.TryParse(newValue, out var lineNUmber))
                OnChangeNumber(GetMode(), lineNUmber);
        }

        protected override void OnClickCloseInternal()
        {
            OnFinish(SelectRecordMode.Cancel, 0);
        }

        protected override void OnClickOkInternal()
        {
            if (int.TryParse(editNumber.text, out var lineNUmber)) 
                OnFinish(GetMode(), lineNUmber);
            else 
                OnFinish(SelectRecordMode.Cancel, 0);
        }

        private SelectRecordMode GetMode()
        {
            return toggleNormal.isOn ? SelectRecordMode.Normal : SelectRecordMode.Completed;
        }
    }
}