using UnityEngine;
using UnityEngine.UI;

namespace Blackjack
{
    /// <summary>
    /// Lives on a Martingale toggle GameObject. When threshold is 0, keeps this toggle
    /// unchecked, non-interactable, and hides its checkmark every frame.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Toggle))]
    public class MartingaleThresholdToggleGate : MonoBehaviour
    {
        [SerializeField] private Slider thresholdSlider;

        private Toggle _toggle;

        private void Awake()
        {
            _toggle = GetComponent<Toggle>();
            ResolveThresholdSlider();
        }

        private void LateUpdate()
        {
            if (_toggle == null)
                _toggle = GetComponent<Toggle>();

            ResolveThresholdSlider();
            if (_toggle == null || thresholdSlider == null) return;

            if (Mathf.RoundToInt(thresholdSlider.value) <= 0)
            {
                _toggle.interactable = false;

                if (_toggle.isOn)
                    _toggle.SetIsOnWithoutNotify(false);

                HideCheckmark(_toggle);
                return;
            }

            _toggle.interactable = true;
            SyncCheckmark(_toggle);
        }

        internal static void HideCheckmark(Toggle toggle)
        {
            if (toggle == null) return;

            if (toggle.graphic != null)
            {
                toggle.graphic.enabled = false;
                toggle.graphic.gameObject.SetActive(false);
            }

            var checkmark = toggle.transform.Find("Background/Checkmark");
            if (checkmark != null)
                checkmark.gameObject.SetActive(false);
        }

        internal static void SyncCheckmark(Toggle toggle)
        {
            if (toggle == null) return;

            if (toggle.graphic != null)
            {
                toggle.graphic.enabled = toggle.isOn;
                toggle.graphic.gameObject.SetActive(toggle.isOn);
            }

            var checkmark = toggle.transform.Find("Background/Checkmark");
            if (checkmark != null)
                checkmark.gameObject.SetActive(toggle.isOn);
        }

        private void ResolveThresholdSlider()
        {
            if (thresholdSlider != null) return;

            Transform panel = transform;
            while (panel != null && panel.name != "MenuPanel")
                panel = panel.parent;

            if (panel == null) return;

            foreach (var slider in panel.GetComponentsInChildren<Slider>(true))
            {
                if (slider.name == "MartingaleThresholdSlider")
                {
                    thresholdSlider = slider;
                    return;
                }
            }
        }
    }
}
