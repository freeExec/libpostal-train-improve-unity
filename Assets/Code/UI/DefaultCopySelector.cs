using UnityEngine;
using UnityEngine.UI;

namespace LP.UI
{
    public enum CopySelector
    {
        None = 0,
        Source,
        Libpostal,
    }

    public class DefaultCopySelector : MonoBehaviour
    {
        [SerializeField] Toggle tgSource;
        [SerializeField] Toggle tgLibpostal;

        public CopySelector CopySelector { get; private set; }

        void Start()
        {
            tgSource.onValueChanged.AddListener(UpdateStatus);
            tgLibpostal.onValueChanged.AddListener(UpdateStatus);
            UpdateStatus(false);
        }

        void UpdateStatus(bool misc)
        {
            if (tgSource.isOn) CopySelector = CopySelector.Source;
            else CopySelector = CopySelector.Libpostal;
        }
    }
}