using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Blackjack
{
    /// <summary>
    /// Popup shown when the player deviates from basic strategy.
    /// Displays the strategy recommendation and lets the player follow strategy or override it.
    /// </summary>
    public class StrategyDeviationPopup : MonoBehaviour
    {
        private const string DoStrategyButtonLabel = "Do Strategy";
        private const string OverrideButtonLabel       = "Override";

        [SerializeField] private TextMeshProUGUI messageLabel;
        [SerializeField] private Button keepButton;
        [SerializeField] private Button reconsiderButton;

        [Header("Audio")]
        [SerializeField] private UISoundsConfig uiSounds;

        [Header("Controls")]
        [SerializeField] private KeyboardControls controls;

        private Action _onKeep;
        private Action _onReconsider;

        private void Awake()
        {
            keepButton.onClick.AddListener(OnKeepClicked);
            reconsiderButton.onClick.AddListener(OnReconsiderClicked);
            ApplyButtonLabels();
            gameObject.SetActive(false);
        }

        private void ApplyButtonLabels()
        {
            SetButtonLabel(keepButton, DoStrategyButtonLabel);
            SetButtonLabel(reconsiderButton, OverrideButtonLabel);
        }

        private static void SetButtonLabel(Button button, string label)
        {
            if (button == null) return;

            var text = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (text != null)
                text.text = label;
        }

        /// <summary>
        /// Shows the popup with the strategy recommendation.
        /// <paramref name="onKeep"/> executes the recommended strategy action.
        /// <paramref name="onReconsider"/> executes the player's chosen action instead of the recommendation.
        /// </summary>
        public void Show(string recommendation, Action onKeep, Action onReconsider)
        {
            messageLabel.text = $"Strategy recommends: {recommendation}";
            _onKeep           = onKeep;
            _onReconsider     = onReconsider;
            gameObject.SetActive(true);
        }

        private void Update()
        {
            if (controls != null && controls.DealOrHitPressed)
                OnKeepClicked();
        }

        private void OnKeepClicked()
        {
            Action callback = _onKeep;
            ClearAndHide();
            callback?.Invoke();
        }

        private void OnReconsiderClicked()
        {
            Action callback = _onReconsider;
            ClearAndHide();
            callback?.Invoke();
        }

        private void ClearAndHide()
        {
            _onKeep       = null;
            _onReconsider = null;
            if (uiSounds != null && uiSounds.closeSound.HasClip)
                AudioSource.PlayClipAtPoint(uiSounds.closeSound.clip, Vector3.zero, uiSounds.closeSound.volume);
            gameObject.SetActive(false);
        }
    }
}
