using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Blackjack
{
    /// <summary>
    /// Swaps the Image sprite on pointer enter, exit, down, and up, bypassing the
    /// Button transition system for reliable feedback with the Input System package.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class ButtonSpriteHover : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerDownHandler,
        IPointerUpHandler
    {
        private const string MissingNormalSpriteWarning =
            "ButtonSpriteHover: normalSprite is not assigned on {0}. Hover-out will show nothing.";

        [Tooltip("Sprite displayed in the normal (idle) state.")]
        [SerializeField] private Sprite normalSprite;

        [Tooltip("Sprite displayed while the pointer is over the button.")]
        [SerializeField] private Sprite hoverSprite;

        [Tooltip("Sprite displayed while the button is held down.")]
        [SerializeField] private Sprite pressedSprite;

        private Image _image;
        private bool _isHovered;

        private void Awake()
        {
            _image = GetComponent<Image>();

            if (normalSprite == null)
                Debug.LogWarning(string.Format(MissingNormalSpriteWarning, gameObject.name));
        }

        /// <summary>Swap to the hover sprite when the pointer enters.</summary>
        public void OnPointerEnter(PointerEventData eventData)
        {
            _isHovered = true;
            if (hoverSprite != null)
                _image.sprite = hoverSprite;
        }

        /// <summary>Restore the normal sprite when the pointer exits.</summary>
        public void OnPointerExit(PointerEventData eventData)
        {
            _isHovered = false;
            _image.sprite = normalSprite;
        }

        /// <summary>Swap to the pressed sprite when the pointer is held down.</summary>
        public void OnPointerDown(PointerEventData eventData)
        {
            if (pressedSprite != null)
                _image.sprite = pressedSprite;
        }

        /// <summary>Restore hover or normal sprite when the pointer is released.</summary>
        public void OnPointerUp(PointerEventData eventData)
        {
            _image.sprite = _isHovered && hoverSprite != null ? hoverSprite : normalSprite;
        }
    }
}
