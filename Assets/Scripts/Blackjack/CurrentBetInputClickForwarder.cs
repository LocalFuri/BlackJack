using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Blackjack
{
    /// <summary>
    /// Ensures runtime-created menu InputFields receive focus when clicked or tabbed to
    /// (nested canvas + Input System UI can miss legacy InputField activation).
    /// </summary>
    [RequireComponent(typeof(InputField))]
    public class CurrentBetInputClickForwarder : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, ISelectHandler
    {
        private InputField _input;

        public System.Action OnSelected;

        public void Bind(InputField input) => _input = input;

        public static void SelectEntireText(InputField input)
        {
            if (input == null) return;

            int length = input.text.Length;
            input.caretPosition = length;
            input.selectionAnchorPosition = 0;
            input.selectionFocusPosition = length;
        }

        private void Awake() => _input ??= GetComponent<InputField>();

        public void OnSelect(BaseEventData eventData) => PrepareForEditing();

        public void OnPointerDown(PointerEventData eventData) => FocusInput();

        public void OnPointerClick(PointerEventData eventData) => FocusInput();

        public void FocusInput()
        {
            if (_input == null || !_input.isActiveAndEnabled || !_input.interactable)
                return;

            EventSystem.current?.SetSelectedGameObject(_input.gameObject);
            _input.Select();
            PrepareForEditing();
        }

        private void PrepareForEditing()
        {
            if (_input == null || !_input.isActiveAndEnabled || !_input.interactable)
                return;

            OnSelected?.Invoke();
            _input.ActivateInputField();
            SelectEntireText(_input);
        }
    }
}
