using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LP.UI
{
    public class ProgressBarGradientColor : MonoBehaviour
    {
        [SerializeField] Slider slider;
        [SerializeField] Gradient gradient;
        [SerializeField] TMP_Text label;
        [SerializeField] Image foreground;
        [SerializeField] string textFormat = "{0:P2}";

        public float Value
        {
            get => slider.value;
            set
            {
                slider.value = value;
                foreground.color = gradient.Evaluate(value);
                label.text = string.Format(textFormat, value);
            }
        }

        public void SetLabel(string text) => label.text = text;
    }
}