using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Blackjack
{
    /// <summary>
    /// Toggles the developer menu panel with F2.
    /// Controls visibility of the three test buttons and the master volume.
    /// </summary>
    public class MenuController : MonoBehaviour
    {
        [Header("Menu Panel")]
        [SerializeField] private GameObject menuPanel;

        [Header("Test Button GameObjects")]
        [SerializeField] private GameObject blackjackTestButton;
        [SerializeField] private GameObject bjAllButton;
        [SerializeField] private GameObject ddTestButton;

        [Header("Checkboxes")]
        [SerializeField] private Toggle blackjackTestToggle;
        [SerializeField] private Toggle bjAllToggle;
        [SerializeField] private Toggle ddTestToggle;

        [Header("Volume")]
        [SerializeField] private Slider  volumeSlider;
        [SerializeField] private AudioMixer audioMixer;

        private const string MasterVolumeParam = "MasterVolume";

        // ──────────────────────────────────────────────────────────────────────────
        // Unity lifecycle
        // ──────────────────────────────────────────────────────────────────────────

        private void Awake()
        {
            // Sync toggle initial state with button visibility.
            if (blackjackTestToggle != null && blackjackTestButton != null)
                blackjackTestToggle.SetIsOnWithoutNotify(blackjackTestButton.activeSelf);

            if (bjAllToggle != null && bjAllButton != null)
                bjAllToggle.SetIsOnWithoutNotify(bjAllButton.activeSelf);

            if (ddTestToggle != null && ddTestButton != null)
                ddTestToggle.SetIsOnWithoutNotify(ddTestButton.activeSelf);

            // Sync slider initial state with current audio mixer value.
            if (volumeSlider != null && audioMixer != null)
            {
                if (audioMixer.GetFloat(MasterVolumeParam, out float dB))
                    volumeSlider.SetValueWithoutNotify(DbToLinear(dB));
                else
                    volumeSlider.SetValueWithoutNotify(1f);
            }

            // Register callbacks.
            blackjackTestToggle?.onValueChanged.AddListener(OnBlackjackTestToggled);
            bjAllToggle?.onValueChanged.AddListener(OnBjAllToggled);
            ddTestToggle?.onValueChanged.AddListener(OnDdTestToggled);
            volumeSlider?.onValueChanged.AddListener(OnVolumeChanged);

            menuPanel?.SetActive(false);
        }

        private void Start()
        {
            // Ensure the panel is hidden even if Awake order caused it to show briefly.
            menuPanel?.SetActive(false);
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.f2Key.wasPressedThisFrame)
                ToggleMenu();
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Toggle callbacks
        // ──────────────────────────────────────────────────────────────────────────

        private void OnBlackjackTestToggled(bool value) =>
            blackjackTestButton?.SetActive(value);

        private void OnBjAllToggled(bool value) =>
            bjAllButton?.SetActive(value);

        private void OnDdTestToggled(bool value) =>
            ddTestButton?.SetActive(value);

        private void OnVolumeChanged(float linear)
        {
            if (audioMixer != null)
                audioMixer.SetFloat(MasterVolumeParam, LinearToDb(linear));
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Helpers
        // ──────────────────────────────────────────────────────────────────────────

        /// <summary>Shows or hides the menu panel.</summary>
        private void ToggleMenu()
        {
            if (menuPanel == null) return;
            menuPanel.SetActive(!menuPanel.activeSelf);
        }

        // Converts a linear [0,1] slider value to decibels for the AudioMixer.
        private static float LinearToDb(float linear) =>
            linear > 0.0001f ? Mathf.Log10(linear) * 20f : -80f;

        // Converts decibels from the AudioMixer back to a linear [0,1] slider value.
        private static float DbToLinear(float dB) =>
            Mathf.Pow(10f, dB / 20f);
    }
}
