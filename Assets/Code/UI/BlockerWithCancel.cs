using System.Threading;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace LP.UI
{
    public class BlockerWithCancel : MonoBehaviour
    {
        [SerializeField] Button cancelButton;

        public UnityEvent CancelAction => cancelButton.onClick;

        private CancellationTokenSource cancelTokenSource;

        public void Show() => gameObject.SetActive(true);

        public void Hide() => gameObject.SetActive(false);

        public void Show(CancellationTokenSource token)
        {
            cancelTokenSource = token;
            CancelAction.AddListener(OnCancelClickAndTokenCancel);
            Show();
        }

        private void OnCancelClickAndTokenCancel()
        {
            cancelTokenSource?.Cancel();
            cancelTokenSource = null;
            CancelAction.RemoveListener(OnCancelClickAndTokenCancel);

            Hide();
        }
    }
}