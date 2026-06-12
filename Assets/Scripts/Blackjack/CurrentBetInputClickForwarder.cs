using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Blackjack
{
    /// <summary>
    /// Ensures runtime-created menu InputFields receive focus when clicked
    /// (nested canvas + Input System UI can miss legacy InputField activation).
    /// </summary>
    [RequireComponent(typeof(InputField))]
    public class CurrentBetInputClickForwarder : MonoBehaviour, IPointerClickHandler, IPointerDownHandler
    {
        private InputField _input;

        public void Bind(InputField input) => _input = input;

        private void Awake() => _input ??= GetComponent<InputField>();

        public void OnPointerDown(PointerEventData eventData)
        {
            FocusInput();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            FocusInput();
        }

        private void FocusInput()
        {
            if (_input == null || !_input.isActiveAndEnabled || !_input.interactable)
                return;

            EventSystem.current?.SetSelectedGameObject(_input.gameObject);
            _input.Select();
            _input.ActivateInputField();
        }
    }
}
