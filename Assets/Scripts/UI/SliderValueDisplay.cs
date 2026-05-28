using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Blackjack.UI
{
    /// <summary>Displays the current integer value of a linked Slider on a Text label.</summary>
    public class SliderValueDisplay : MonoBehaviour
    {
        [SerializeField] private Text valueLabel;

        [SerializeField] private UnityEvent<float> onValueChanged;

        /// <summary>
        /// When enabled, values 11–14 are rendered as face-card labels (J, Q, K, A)
        /// instead of their numeric equivalents.
        /// </summary>
        [SerializeField] private bool useRankLabels;

        /// <summary>
        /// When greater than or equal to 0, the label is tinted with <see cref="aboveThresholdColor"/>
        /// whenever the slider value exceeds this threshold, and reverts to <see cref="defaultColor"/> otherwise.
        /// Set to -1 to disable color tinting entirely.
        /// </summary>
        [SerializeField] private int colorThreshold = -1;

        [SerializeField] private Color aboveThresholdColor = Color.red;
        [SerializeField] private Color defaultColor        = Color.white;

        private void Start()
        {
            var slider = GetComponent<Slider>();
            if (slider == null || valueLabel == null) return;

            UpdateLabel(slider.value);
            slider.onValueChanged.AddListener(UpdateLabel);
        }

        private void UpdateLabel(float value)
        {
            int intValue = Mathf.RoundToInt(value);
            valueLabel.text = FormatValue(intValue);

            if (colorThreshold >= 0 && valueLabel != null)
                valueLabel.color = intValue > colorThreshold ? aboveThresholdColor : defaultColor;

            onValueChanged?.Invoke(value);
        }

        private string FormatValue(int intValue)
        {
            if (useRankLabels)
            {
                return intValue switch
                {
                    11 => "J",
                    12 => "Q",
                    13 => "K",
                    14 => "A",
                    _  => intValue.ToString()
                };
            }

            return intValue.ToString();
        }
    }
}
