using Blackjack;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UI
{
    /// <summary>
    /// Plays a "don't touch me" sound when the dealer image is clicked.
    /// </summary>
    [RequireComponent(typeof(UnityEngine.UI.Image))]
    public class DealerImageClick : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private UISoundsConfig uiSounds;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (uiSounds == null || uiSounds.dontTouchMeSound.clip == null)
                return;

            AudioSource.PlayClipAtPoint(
                uiSounds.dontTouchMeSound.clip,
                Vector3.zero,
                uiSounds.dontTouchMeSound.volume
            );
        }
    }
}
