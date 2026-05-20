using UnityEngine;
using UnityEngine.UI;

namespace Blackjack.UI
{
    /// <summary>Displays the current integer value of a linked Slider on a Text label.</summary>
    public class SliderValueDisplay : MonoBehaviour
    {
        [SerializeField] private Text valueLabel;

        private void Start()
        {
            var slider = GetComponent<Slider>();
            if (slider == null || valueLabel == null) return;

            UpdateLabel(slider.value);
            slider.onValueChanged.AddListener(UpdateLabel);
        }

        private void UpdateLabel(float value)
        {
            valueLabel.text = Mathf.RoundToInt(value).ToString();
        }
    }
}
