using UnityEngine;

namespace Blackjack
{
    /// <summary>
    /// Shared audio configuration for UI sounds.
    /// Assign one instance to all UI components so volume and clip changes apply everywhere.
    /// </summary>
    [CreateAssetMenu(fileName = "UISoundsConfig", menuName = "Blackjack/UI Sounds Config")]
    public class UISoundsConfig : ScriptableObject
    {
        [Tooltip("Played whenever any menu or popup is closed.")]
        public SoundEntry closeSound;

        [Tooltip("Played when the dealer image is clicked.")]
        public SoundEntry dontTouchMeSound;

        [Tooltip("Played when any option checkbox is toggled on.")]
        public SoundEntry toggleSound;

        [Tooltip("Played when a popup appears (e.g. strategy deviation warning).")]
        public SoundEntry popupSound;
    }
}
