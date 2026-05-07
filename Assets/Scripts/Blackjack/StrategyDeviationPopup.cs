using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Blackjack
{
    /// <summary>
    /// Modal popup shown when the player deviates from basic strategy.
    /// Presents the deviation and lets the player choose to keep their action
    /// or switch to the basic strategy recommendation.
    /// </summary>
    public class StrategyDeviationPopup : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI messageLabel;
        [SerializeField] private Button keepButton;
        [SerializeField] private TextMeshProUGUI keepButtonLabel;
        [SerializeField] private Button followStrategyButton;
        [SerializeField] private TextMeshProUGUI followStrategyButtonLabel;

        private Action _onKeep;
        private Action _onFollowStrategy;

        private void Awake()
        {
            keepButton.onClick.AddListener(OnKeepClicked);
            followStrategyButton.onClick.AddListener(OnFollowStrategyClicked);
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Shows the popup with the given deviation message and button labels.
        /// Invokes <paramref name="onKeep"/> or <paramref name="onFollowStrategy"/>
        /// depending on which button the player presses.
        /// </summary>
        public void Show(
            string message,
            string keepLabel,
            string followLabel,
            Action onKeep,
            Action onFollowStrategy)
        {
            messageLabel.text              = message;
            keepButtonLabel.text           = keepLabel;
            followStrategyButtonLabel.text = followLabel;
            _onKeep                        = onKeep;
            _onFollowStrategy              = onFollowStrategy;
            gameObject.SetActive(true);
        }

        private void OnKeepClicked()
        {
            Action callback = _onKeep;
            ClearAndHide();
            callback?.Invoke();
        }

        private void OnFollowStrategyClicked()
        {
            Action callback = _onFollowStrategy;
            ClearAndHide();
            callback?.Invoke();
        }

        private void ClearAndHide()
        {
            _onKeep           = null;
            _onFollowStrategy = null;
            gameObject.SetActive(false);
        }
    }
}
