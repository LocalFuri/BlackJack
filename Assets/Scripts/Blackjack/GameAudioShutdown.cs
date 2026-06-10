using UnityEngine;

namespace Blackjack
{
    /// <summary>
    /// Stops every active <see cref="AudioSource"/> so interrupted clips do not bleed into the next Play session or build launch.
    /// </summary>
    public static class GameAudioShutdown
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void UnregisterQuitHook()
        {
            Application.quitting -= StopAll;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void RegisterQuitHookAndStop()
        {
            Application.quitting += StopAll;
            StopAll();
        }

        /// <summary>Stops all playing audio in the current scene, including inactive objects.</summary>
        public static void StopAll()
        {
            AudioSource[] sources = Object.FindObjectsByType<AudioSource>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            for (int i = 0; i < sources.Length; i++)
            {
                if (sources[i] != null)
                    sources[i].Stop();
            }
        }
    }
}
