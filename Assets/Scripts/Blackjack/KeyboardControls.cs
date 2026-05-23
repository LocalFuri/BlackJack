using UnityEngine;
using UnityEngine.InputSystem;

namespace Blackjack
{
    /// <summary>
    /// ScriptableObject that centralises every keyboard binding used in the game.
    /// Assign one shared instance to all components that need keyboard input.
    /// </summary>
    [CreateAssetMenu(fileName = "KeyboardControls", menuName = "Blackjack/Keyboard Controls")]
    public class KeyboardControls : ScriptableObject
    {
        [Header("Game Actions")]
        [Tooltip("Deal a new round or confirm Hit when the action buttons are visible.")]
        public Key dealOrHitKey = Key.Space;

        [Header("Menu")]
        [Tooltip("Open / close the developer menu panel.")]
        public Key toggleMenuKey = Key.F2;

        [Header("Application")]
        [Tooltip("Close menu if open, otherwise quit / stop Play Mode.")]
        public Key quitKey = Key.Escape;

        // ── Helpers ───────────────────────────────────────────────────────────────

        /// <summary>Returns true if the deal-or-hit key was pressed this frame.</summary>
        public bool DealOrHitPressed =>
            Keyboard.current != null &&
            Keyboard.current[dealOrHitKey].wasPressedThisFrame;

        /// <summary>Returns true if the menu toggle key was pressed this frame.</summary>
        public bool ToggleMenuPressed =>
            Keyboard.current != null &&
            Keyboard.current[toggleMenuKey].wasPressedThisFrame;

        /// <summary>Returns true if the quit key was pressed this frame.</summary>
        public bool QuitPressed =>
            Keyboard.current != null &&
            Keyboard.current[quitKey].wasPressedThisFrame;
    }
}
