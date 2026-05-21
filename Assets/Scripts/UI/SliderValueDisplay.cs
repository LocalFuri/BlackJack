using UnityEngine;
using UnityEngine.UI;

namespace Blackjack.UI
{
    /// <summary>Displays the current integer value of a linked Slider on a Text label.</summary>
    public class SliderValueDisplay : MonoBehaviour
    {
        [SerializeField] private Text valueLabel;

        /// <summary>
        /// When enabled, values 11–14 are rendered as face-card labels (J, Q, K, A)
        /// instead of their numeric equivalents.
        /// </summary>
        [SerializeField] private bool useRankLabels;

        private void Start()
        {
            var slider = GetComponent<Slider>();
            if (slider == null || valueLabel == null) return;

            UpdateLabel(slider.value);
            slider.onValueChanged.AddListener(UpdateLabel);
        }

        private void UpdateLabel(float value)
        {
            valueLabel.text = FormatValue(Mathf.RoundToInt(value));
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
