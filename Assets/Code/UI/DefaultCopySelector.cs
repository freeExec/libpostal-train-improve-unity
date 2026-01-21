using UnityEngine;
using UnityEngine.UI;

namespace LP.UI
{
    public enum CopySelector
    {
        None = 0,
        Source,
        SourceAndSplit,
        Libpostal,
    }

    public class DefaultCopySelector : MonoBehaviour
    {
        [SerializeField] Toggle tgSource;
        [SerializeField] Toggle tgSourceAndSplit;
        [SerializeField] Toggle tgLibpostal;

        public CopySelector CopySelector { get; private set; }

        void Start()
        {
            tgSource.onValueChanged.AddListener(UpdateStatus);
            tgSourceAndSplit.onValueChanged.AddListener(UpdateStatus);
            tgLibpostal.onValueChanged.AddListener(UpdateStatus);
            UpdateStatus(false);
        }

        void UpdateStatus(bool misc)
        {
            if (tgSource.isOn) CopySelector = CopySelector.Source;
            else if (tgSourceAndSplit.isOn) CopySelector = CopySelector.SourceAndSplit;
            else CopySelector = CopySelector.Libpostal;
        }
    }
}