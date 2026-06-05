using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Blackjack
{
    /// <summary>
    /// Popup shown when the player has lost enough consecutive rounds to trigger the Martingale suggestion.
    /// Lets the player confirm they want to play Martingale or reconsider before dealing.
    /// </summary>
    public class MartingalePopup : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI messageLabel;
        [SerializeField] private Button doItButton;
        [SerializeField] private Button reconsiderButton;

        [Header("Audio")]
        [SerializeField] private UISoundsConfig uiSounds;

        [Header("Controls")]
        [SerializeField] private KeyboardControls controls;

        private Action _onDoIt;
        private Action _onReconsider;

        private void Awake()
        {
            UiOverlaySorting.Apply(gameObject, UiOverlaySorting.MartingalePopup);

            // Force the card background to be fully opaque, matching the menu panel color.
            var popupCard = transform.Find("PopupCard");
            if (popupCard != null)
            {
                var bg = popupCard.GetComponent<Image>();
                if (bg != null)
                    bg.color = new Color(0.0482093133f, 0.230188608f, 0.0939902738f, 1f);
            }

            doItButton.onClick.AddListener(OnDoItClicked);
            reconsiderButton.onClick.AddListener(OnReconsiderClicked);
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Shows the popup with a custom message.
        /// <paramref name="onDoIt"/> is invoked when the player confirms Martingale.
        /// <paramref name="onReconsider"/> is invoked when the player dismisses the popup.
        /// </summary>
        public void Show(string message, Action onDoIt, Action onReconsider)
        {
            messageLabel.text = message;
            _onDoIt           = onDoIt;
            _onReconsider     = onReconsider;
            gameObject.SetActive(true);
        }

        /// <summary>Hides the popup immediately without invoking any callback.</summary>
        public void Hide() => ClearAndHide();

        private void Update()
        {
            if (controls != null && controls.DealOrHitPressed)
                OnDoItClicked();
        }

        private void OnDoItClicked()
        {
            Action callback = _onDoIt;
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
            _onDoIt       = null;
            _onReconsider = null;
            gameObject.SetActive(false);
        }
    }
}
