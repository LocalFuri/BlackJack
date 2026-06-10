using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Blackjack
{
    /// <summary>
    /// Quits the application when the quit key is pressed (default: Escape).
    /// Plays an exit sound via BlackjackGame before quitting.
    /// In the Editor, stops Play Mode instead.
    /// </summary>
    public class QuitOnKeyPress : MonoBehaviour
    {
        [SerializeField] private BlackjackGame gameManager;

        [Header("Controls")]
        [SerializeField] private KeyboardControls controls;

        private bool _isQuitting;

        private void Update()
        {
            if (_isQuitting) return;
            if (controls == null || !controls.QuitPressed) return;

            if (gameManager != null && gameManager.IsMenuOpen)
            {
                gameManager.CloseMenu();
                return;
            }

            StartCoroutine(PlaySoundThenQuit());
        }

        private IEnumerator PlaySoundThenQuit()
        {
            _isQuitting = true;

            // Persist any unsaved settings before quitting.
            gameManager?.SaveMenuSettings();

            GameAudioShutdown.StopAll();

            if (gameManager != null)
            {
                float length = gameManager.PlayExitSound();
                yield return new WaitForSeconds(length);
            }

            GameAudioShutdown.StopAll();
            Quit();
        }

        private static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit(); //will only excecute if in build mode 
#endif
        }
    }
}
