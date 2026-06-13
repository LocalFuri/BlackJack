using Blackjack;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;

namespace UI
{
    /// <summary>
    /// Plays a random female-speech sound when the dealer image is clicked with the left or right mouse button.
    /// Rapid clicks are ignored while the previous sound is still playing.
    /// </summary>
    [RequireComponent(typeof(UnityEngine.UI.Image))]
    public class DealerImageClick : MonoBehaviour, IPointerClickHandler
    {
        [Tooltip("Female speech clips to pick from randomly on click. Assign from Assets/Sounds/Speech/Female.")]
        [SerializeField] private SoundEntry[] femaleSpeechSounds = System.Array.Empty<SoundEntry>();

        [Tooltip("Assign the same AudioMixerGroup used by the game so master volume applies.")]
        [SerializeField] private AudioMixerGroup mixerGroup;

        private AudioSource _audioSource;
        private int _lastPlayedIndex = -1;

        private void Awake()
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake           = false;
            _audioSource.outputAudioMixerGroup = mixerGroup;
        }

        /// <summary>Handles left and right mouse button clicks on the dealer image.</summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left &&
                eventData.button != PointerEventData.InputButton.Right)
                return;

            if (femaleSpeechSounds.Length == 0)
                return;

            if (_audioSource.isPlaying)
                return;

            int index = femaleSpeechSounds.Length > 1
                ? (Random.Range(0, femaleSpeechSounds.Length - 1) + _lastPlayedIndex + 1) % femaleSpeechSounds.Length
                : 0;
            SoundEntry chosen = femaleSpeechSounds[index];
            if (!chosen.HasClip)
                return;

            _lastPlayedIndex = index;
            _audioSource.clip   = chosen.clip;
            _audioSource.volume = chosen.volume;
            _audioSource.Play();
        }
    }
}
