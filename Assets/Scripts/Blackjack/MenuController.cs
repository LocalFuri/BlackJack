using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Blackjack
{
    /// <summary>
    /// Toggles the developer menu panel with F2.
    /// Controls visibility of the three test buttons and the master volume.
    /// All changes are persisted to disk via <see cref="SettingsRepository"/>.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class MenuController : MonoBehaviour
    {
        [Header("Menu Panel")]
        [SerializeField] private GameObject menuPanel;

        [Header("Test Button GameObjects")]
        [SerializeField] private GameObject blackjackTestButton;
        [SerializeField] private GameObject bjAllButton;
        [SerializeField] private GameObject ddTestButton;
        [SerializeField] private GameObject testSplitButton;

        [Header("Checkboxes")]
        [SerializeField] private Toggle blackjackTestToggle;
        [SerializeField] private Toggle bjAllToggle;
        [SerializeField] private Toggle ddTestToggle;
        [SerializeField] private Toggle testSplitToggle;
        [SerializeField] private Toggle overrideStrategyToggle;
        [SerializeField] private Toggle alwaysLoseToggle;
        [SerializeField] private Toggle showStrategyToggle;
        [SerializeField] private Toggle martingaleActiveToggle;

        [Header("Volume")]
        [SerializeField] private Slider volumeSlider;
        [SerializeField] private AudioMixer audioMixer;

        [Header("Martingale Threshold")]
        [SerializeField] private Slider martingaleThresholdSlider;

        [Header("Test Split Rank")]
        [SerializeField] private Slider testSplitRankSlider;

        [Header("Game Actions")]
        [SerializeField] private BlackjackGame blackjackGame;

        [Header("Strategy Table")]
        [SerializeField] private Blackjack.UI.StrategyTableUI strategyTableUI;

        [Header("Audio")]
        [SerializeField] private UISoundsConfig uiSounds;
        [SerializeField] private AudioSource audioSource;

        [Header("Controls")]
        [SerializeField] private KeyboardControls controls;

        private const string MasterVolumeParam = "MasterVolume";

        private OptionsSettings _settings;

        // ──────────────────────────────────────────────────────────────────────────
        // Unity lifecycle
        // ──────────────────────────────────────────────────────────────────────────

        private void Awake()
        {
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();

            _settings = SettingsRepository.Load();

            ApplySettings();

            // Register callbacks after applying so initial apply does not trigger saves.
            blackjackTestToggle?.onValueChanged.AddListener(OnBlackjackTestToggled);
            bjAllToggle?.onValueChanged.AddListener(OnBjAllToggled);
            ddTestToggle?.onValueChanged.AddListener(OnDdTestToggled);
            testSplitToggle?.onValueChanged.AddListener(OnTestSplitToggled);
            overrideStrategyToggle?.onValueChanged.AddListener(OnOverrideStrategyToggled);
            alwaysLoseToggle?.onValueChanged.AddListener(OnAlwaysLoseToggled);
            showStrategyToggle?.onValueChanged.AddListener(OnShowStrategyToggled);
            martingaleActiveToggle?.onValueChanged.AddListener(OnMartingaleActiveToggled);
            volumeSlider?.onValueChanged.AddListener(OnVolumeChanged);
            martingaleThresholdSlider?.onValueChanged.AddListener(OnMartingaleThresholdChanged);
            testSplitRankSlider?.onValueChanged.AddListener(OnTestSplitRankChanged);

            // Play toggle sound whenever any checkbox is turned on.
            foreach (var toggle in new[] { blackjackTestToggle, bjAllToggle, ddTestToggle,
                                           testSplitToggle, overrideStrategyToggle,
                                           alwaysLoseToggle, showStrategyToggle, martingaleActiveToggle })
            {
                if (toggle != null)
                    toggle.onValueChanged.AddListener(OnToggleSoundPlay);
            }

            menuPanel?.SetActive(false);
        }

        private void Start()
        {
            // Ensure the panel is hidden even if Awake order caused it to show briefly.
            menuPanel?.SetActive(false);

            if (blackjackGame != null)
                blackjackGame.OnAlwaysLoseDisabled += DisableAlwaysLose;
        }

        private void Update()
        {
            if (controls != null && controls.ToggleMenuPressed)
                ToggleMenu();

            if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
                TryCloseStrategyTable();
        }

        /// <summary>Closes the strategy table on right-click, unchecks the toggle, and plays the exit sound.</summary>
        private void TryCloseStrategyTable()
        {
            if (strategyTableUI == null || !strategyTableUI.gameObject.activeSelf) return;

            strategyTableUI.SetVisible(false);

            _settings.showStrategyEnabled = false;
            SettingsRepository.Save(_settings);
            showStrategyToggle?.SetIsOnWithoutNotify(false);

            blackjackGame?.PlayCloseSound();
        }

        /// <summary>Plays the toggle click sound whenever any option checkbox changes value.</summary>
        private void OnToggleSoundPlay(bool _) => uiSounds?.toggleSound.Play(audioSource);

        private void OnDestroy()
        {
            if (blackjackGame != null)
                blackjackGame.OnAlwaysLoseDisabled -= DisableAlwaysLose;
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Toggle callbacks
        // ──────────────────────────────────────────────────────────────────────────

        private void OnBlackjackTestToggled(bool value)
        {
            blackjackTestButton?.SetActive(value);
            _settings.blackjackTestEnabled = value;
            SettingsRepository.Save(_settings);
        }

        private void OnBjAllToggled(bool value)
        {
            bjAllButton?.SetActive(value);
            _settings.bjAllEnabled = value;
            SettingsRepository.Save(_settings);
        }

        private void OnDdTestToggled(bool value)
        {
            ddTestButton?.SetActive(value);
            _settings.ddTestEnabled = value;
            SettingsRepository.Save(_settings);
        }

        private void OnTestSplitToggled(bool value)
        {
            testSplitButton?.SetActive(value);
            _settings.testSplitEnabled = value;
            SettingsRepository.Save(_settings);
        }

        private void OnOverrideStrategyToggled(bool value)
        {
            _settings.overrideStrategyEnabled = value;
            SettingsRepository.Save(_settings);
        }

        /// <summary>Forces the player to lose every round when enabled. Used for Martingale testing.</summary>
        private void OnAlwaysLoseToggled(bool value)
        {
            if (blackjackGame != null)
                blackjackGame.AlwaysLose = value;
            _settings.alwaysLoseEnabled = value;
            SettingsRepository.Save(_settings);
        }

        /// <summary>Called by BlackjackGame when it automatically turns off Always Lose upon entering Martingale mode.</summary>
        private void DisableAlwaysLose()
        {
            _settings.alwaysLoseEnabled = false;
            SettingsRepository.Save(_settings);
            alwaysLoseToggle?.SetIsOnWithoutNotify(false);
        }

        /// <summary>
        /// Disables the "Always Lose" checkbox.
        /// Called whenever a test button in the TestButtonColumn is pressed.
        /// </summary>
        public void DisableTestCheckboxes()
        {
            // Always Lose
            if (_settings.alwaysLoseEnabled)
            {
                _settings.alwaysLoseEnabled = false;
                SettingsRepository.Save(_settings);
                alwaysLoseToggle?.SetIsOnWithoutNotify(false);
                if (blackjackGame != null)
                    blackjackGame.AlwaysLose = false;
            }
        }

        /// <summary>
        /// Enables or disables the Override Strategy checkbox.
        /// Call with <c>false</c> when entering Martingale mode, <c>true</c> when leaving it.
        /// </summary>
        public void SetOverrideStrategyInteractable(bool interactable)
        {
            if (overrideStrategyToggle == null) return;
            overrideStrategyToggle.interactable = interactable;
            if (!interactable)
            {
                overrideStrategyToggle.SetIsOnWithoutNotify(false);
                _settings.overrideStrategyEnabled = false;
                SettingsRepository.Save(_settings);
            }
        }

        private void OnVolumeChanged(float linear)
        {
            if (audioMixer != null)
                audioMixer.SetFloat(MasterVolumeParam, LinearToDb(linear));

            _settings.volume = linear;
            SettingsRepository.Save(_settings);
        }

        private void OnMartingaleThresholdToggled(bool value) { }

        private void OnMartingaleActiveToggled(bool value)
        {
            _settings.martingaleActive = value;
            SettingsRepository.Save(_settings);

            if (value)
                blackjackGame?.TryStartMartingaleFromToggle();
        }

        private void OnShowStrategyToggled(bool value)
        {
            _settings.showStrategyEnabled = value;
            SettingsRepository.Save(_settings);
            strategyTableUI?.SetVisible(value);
        }

        private void OnMartingaleThresholdChanged(float value)        {
            _settings.martingaleThreshold = Mathf.RoundToInt(value);
            SettingsRepository.Save(_settings);
        }

        /// <summary>Persists the selected test-split rank (2–14, matching the Rank enum).</summary>
        public void OnTestSplitRankChanged(float value)
        {
            if (_settings == null) return;

            _settings.testSplitRank = Mathf.RoundToInt(value);
            SettingsRepository.Save(_settings);
        }

        /// <summary>Resets the game to the initial state. Called by the Reset Game button inside the menu panel.</summary>
        public void OnResetGameClicked()
        {
            blackjackGame?.ResetGame();
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Helpers
        // ──────────────────────────────────────────────────────────────────────────

        /// <summary>True while the menu panel is visible.</summary>
        public bool IsMenuOpen => menuPanel != null && menuPanel.activeSelf;

        /// <summary>When true, strategy deviation popup is bypassed and the player's action executes immediately.</summary>
        public bool IsStrategyOverrideEnabled => _settings.overrideStrategyEnabled;

        /// <summary>Returns the Martingale streak threshold from the menu slider.</summary>
        public int MartingaleThreshold => _settings.martingaleThreshold;

        /// <summary>When true, the Martingale suggestion popup is enabled.</summary>
        public bool IsMartingaleActive => _settings.martingaleActive;

        /// <summary>
        /// Programmatically activates the "Martingale is Active" checkbox.
        /// Used when the threshold is exceeded and the popup is about to be shown.
        /// </summary>
        public void ActivateMartingale()
        {
            if (_settings.martingaleActive) return;
            _settings.martingaleActive = true;
            SettingsRepository.Save(_settings);
            martingaleActiveToggle?.SetIsOnWithoutNotify(true);
        }

        /// <summary>
        /// Deactivates the "Override Strategy" checkbox.
        /// Called whenever the game enters Martingale mode.
        /// </summary>
        public void DisableOverrideStrategy()
        {
            if (!_settings.overrideStrategyEnabled) return;
            _settings.overrideStrategyEnabled = false;
            SettingsRepository.Save(_settings);
            overrideStrategyToggle?.SetIsOnWithoutNotify(false);
        }

        /// <summary>
        /// Programmatically deactivates the "Martingale is Active" checkbox.
        /// Used when the player declines the Martingale popup.
        /// </summary>
        public void DeactivateMartingale()
        {
            if (!_settings.martingaleActive) return;
            _settings.martingaleActive = false;
            SettingsRepository.Save(_settings);
            martingaleActiveToggle?.SetIsOnWithoutNotify(false);
        }

        /// <summary>When true, the strategy table should be visible to the player.</summary>
        public bool IsShowStrategyEnabled => _settings.showStrategyEnabled;

        /// <summary>Returns the Rank integer (2–14) selected by the test-split slider.</summary>
        public int TestSplitRank => _settings.testSplitRank;

        /// <summary>Shows or hides the menu panel.</summary>
        private void ToggleMenu()
        {
            if (menuPanel == null) return;
            bool closing = menuPanel.activeSelf;

            // Only allow opening when no round is in progress.
            if (!closing && blackjackGame != null && !blackjackGame.IsBettingAllowed && !blackjackGame.IsRoundOver)
                return;

            menuPanel.SetActive(!closing);
            if (closing) blackjackGame?.PlayCloseSound();
        }

        /// <summary>Closes the menu panel if it is currently open.</summary>
        /// <param name="playSound">When false, suppresses the close sound. Defaults to true.</param>
        public void CloseMenu(bool playSound = true)
        {
            if (menuPanel != null && menuPanel.activeSelf)
            {
                menuPanel.SetActive(false);
                if (playSound) blackjackGame?.PlayCloseSound();
            }
        }

        /// <summary>Pushes all loaded settings into the UI and the AudioMixer.</summary>
        private void ApplySettings()
        {
            if (blackjackTestToggle != null)
                blackjackTestToggle.SetIsOnWithoutNotify(_settings.blackjackTestEnabled);
            blackjackTestButton?.SetActive(_settings.blackjackTestEnabled);

            if (bjAllToggle != null)
                bjAllToggle.SetIsOnWithoutNotify(_settings.bjAllEnabled);
            bjAllButton?.SetActive(_settings.bjAllEnabled);

            if (ddTestToggle != null)
                ddTestToggle.SetIsOnWithoutNotify(_settings.ddTestEnabled);
            ddTestButton?.SetActive(_settings.ddTestEnabled);

            if (testSplitToggle != null)
                testSplitToggle.SetIsOnWithoutNotify(_settings.testSplitEnabled);
            testSplitButton?.SetActive(_settings.testSplitEnabled);

            if (overrideStrategyToggle != null)
                overrideStrategyToggle.SetIsOnWithoutNotify(_settings.overrideStrategyEnabled);

            if (alwaysLoseToggle != null)
                alwaysLoseToggle.SetIsOnWithoutNotify(_settings.alwaysLoseEnabled);
            if (blackjackGame != null)
                blackjackGame.AlwaysLose = _settings.alwaysLoseEnabled;

            if (showStrategyToggle != null)
                showStrategyToggle.SetIsOnWithoutNotify(_settings.showStrategyEnabled);
            strategyTableUI?.SetVisible(_settings.showStrategyEnabled);

            if (volumeSlider != null)
                volumeSlider.SetValueWithoutNotify(_settings.volume);

            if (audioMixer != null)
                audioMixer.SetFloat(MasterVolumeParam, LinearToDb(_settings.volume));

            if (martingaleThresholdSlider != null)
                martingaleThresholdSlider.SetValueWithoutNotify(_settings.martingaleThreshold);

            if (martingaleActiveToggle != null)
                martingaleActiveToggle.SetIsOnWithoutNotify(_settings.martingaleActive);

            if (testSplitRankSlider != null)
                testSplitRankSlider.SetValueWithoutNotify(_settings.testSplitRank);
        }

        // Converts a linear [0,1] slider value to decibels for the AudioMixer.
        private static float LinearToDb(float linear) =>
            linear > 0.0001f ? Mathf.Log10(linear) * 20f : -80f;

        // Converts decibels from the AudioMixer back to a linear [0,1] slider value.
        private static float DbToLinear(float dB) =>
            Mathf.Pow(10f, dB / 20f);
    }
}
