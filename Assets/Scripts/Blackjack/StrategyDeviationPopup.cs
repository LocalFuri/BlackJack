using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Blackjack
{
    /// <summary>
    /// Popup shown when the player deviates from basic strategy.
    /// Displays the strategy recommendation and lets the player keep their decision or reconsider.
    /// </summary>
    public class StrategyDeviationPopup : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI messageLabel;
        [SerializeField] private Button keepButton;
        [SerializeField] private Button reconsiderButton;

        private Action _onKeep;
        private Action _onReconsider;

        private void Awake()
        {
            keepButton.onClick.AddListener(OnKeepClicked);
            reconsiderButton.onClick.AddListener(OnReconsiderClicked);
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Shows the popup with the strategy recommendation.
        /// <paramref name="onKeep"/> executes the player's original action.
        /// <paramref name="onReconsider"/> closes the popup and returns control to the player.
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
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
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
            gameObject.SetActive(false);
        }
    }
}
