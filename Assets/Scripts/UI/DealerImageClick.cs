using Blackjack;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;

namespace UI
{
    /// <summary>
    /// Plays a "don't touch me" sound when the dealer image is clicked.
    /// Rapid clicks are ignored while the previous sound is still playing.
    /// </summary>
    [RequireComponent(typeof(UnityEngine.UI.Image))]
    public class DealerImageClick : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private UISoundsConfig uiSounds;

        [Tooltip("Assign the same AudioMixerGroup used by the game so master volume applies.")]
        [SerializeField] private AudioMixerGroup mixerGroup;

        private AudioSource _audioSource;

        private void Awake()
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake           = false;
            _audioSource.outputAudioMixerGroup = mixerGroup;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (uiSounds == null || !uiSounds.dontTouchMeSound.HasClip)
                return;

            if (_audioSource.isPlaying)
                return;

            _audioSource.clip   = uiSounds.dontTouchMeSound.clip;
            _audioSource.volume = uiSounds.dontTouchMeSound.volume;
            _audioSource.Play();
        }
    }
}
