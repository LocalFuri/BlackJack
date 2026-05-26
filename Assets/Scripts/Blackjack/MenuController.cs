using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Blackjack
{
    /// <summary>
    /// Toggles the developer menu panel with F2.
    /// Controls visibility of the three test buttons and the master volume.
    /// All settings are session-only and live in an in-memory <see cref="OptionsSettings"/> instance.
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
        [SerializeField] private Toggle martingaleAutoPlayToggle;

        [Header("Volume")]
        [SerializeField] private Slider volumeSlider;
        [SerializeField] private AudioMixer audioMixer;

        [Header("Martingale Threshold")]
        [SerializeField] private Slider martingaleThresholdSlider;
        [Tooltip("Starting Martingale loss-streak threshold. Overrides the persisted value every time the scene loads.")]
        [SerializeField] [Min(1)] private int defaultMartingaleThreshold = 4;

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
        private CanvasGroup     _menuCanvasGroup;
        private bool            _menuVisible;

        /// <summary>Guard flag to prevent re-entrant callback processing when programmatically setting toggle values.</summary>
        private bool _suppressToggleCallbacks;

        // ──────────────────────────────────────────────────────────────────────────
        // Unity lifecycle
        // ──────────────────────────────────────────────────────────────────────────

        private void Awake()
        {
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();

            // Cache the CanvasGroup used to show/hide the panel without SetActive.
            if (menuPanel != null)
                _menuCanvasGroup = menuPanel.GetComponent<CanvasGroup>();

            _settings = new OptionsSettings();

            // Always start from the Inspector-configured default so the designer controls the threshold.
            _settings.martingaleThreshold = defaultMartingaleThreshold;

            // Martingale and strategy table are session-only — always start off.
            _settings.martingaleActive    = false;
            _settings.martingaleAutoPlay  = false;
            _settings.showStrategyEnabled = false;

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
            martingaleAutoPlayToggle?.onValueChanged.AddListener(OnMartingaleAutoPlayToggled);
            volumeSlider?.onValueChanged.AddListener(OnVolumeChanged);
            martingaleThresholdSlider?.onValueChanged.AddListener(OnMartingaleThresholdChanged);
            testSplitRankSlider?.onValueChanged.AddListener(OnTestSplitRankChanged);

            // Play toggle sound whenever any checkbox is turned on.
            foreach (var toggle in new[] { blackjackTestToggle, bjAllToggle, ddTestToggle,
                                           testSplitToggle, overrideStrategyToggle,
                                           alwaysLoseToggle, showStrategyToggle,
                                           martingaleActiveToggle, martingaleAutoPlayToggle })
            {
                if (toggle != null)
                    toggle.onValueChanged.AddListener(OnToggleSoundPlay);
            }

            // Hide the panel via CanvasGroup — keeps the GameObject active so listeners survive.
            SetMenuVisible(false);
        }

        private void Start()
        {
            if (blackjackGame != null)
                blackjackGame.OnAlwaysLoseDisabled += DisableAlwaysLose;
        }

        private void Update()
        {
            if (controls != null && controls.ToggleMenuPressed)
                ToggleMenu();

            if (controls != null && controls.ShowStrategyPressed)
                ToggleStrategyTable();

            if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
                TryCloseStrategyTable();
        }

        /// <summary>Toggles the strategy table visibility and keeps the toggle and settings in sync.</summary>
        private void ToggleStrategyTable()
        {
            if (strategyTableUI == null) return;

            bool isCurrentlyVisible = strategyTableUI.gameObject.activeSelf;
            bool newValue = !isCurrentlyVisible;
            _settings.showStrategyEnabled = newValue;
            showStrategyToggle?.SetIsOnWithoutNotify(newValue);
            strategyTableUI.SetVisible(newValue);

            uiSounds?.toggleSound.Play(audioSource);
        }

        /// <summary>Closes the strategy table on right-click, unchecks the toggle, and plays the exit sound.</summary>
        private void TryCloseStrategyTable()
        {
            if (strategyTableUI == null || !strategyTableUI.gameObject.activeSelf) return;

            strategyTableUI.SetVisible(false);

            _settings.showStrategyEnabled = false;
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
        }

        private void OnBjAllToggled(bool value)
        {
            bjAllButton?.SetActive(value);
            _settings.bjAllEnabled = value;
        }

        private void OnDdTestToggled(bool value)
        {
            ddTestButton?.SetActive(value);
            _settings.ddTestEnabled = value;
        }

        private void OnTestSplitToggled(bool value)
        {
            testSplitButton?.SetActive(value);
            _settings.testSplitEnabled = value;
        }

        private void OnOverrideStrategyToggled(bool value)
        {
            _settings.overrideStrategyEnabled = value;
        }

        /// <summary>Forces the player to lose every round when enabled. Used for Martingale testing.</summary>
        private void OnAlwaysLoseToggled(bool value)
        {
            if (blackjackGame != null)
                blackjackGame.AlwaysLose = value;
            _settings.alwaysLoseEnabled = value;
        }

        /// <summary>Called by BlackjackGame when it automatically turns off Always Lose upon entering Martingale mode.</summary>
        private void DisableAlwaysLose()
        {
            _settings.alwaysLoseEnabled = false;
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
                alwaysLoseToggle?.SetIsOnWithoutNotify(false);
                if (blackjackGame != null)
                    blackjackGame.AlwaysLose = false;
            }
        }

        public void SetOverrideStrategyInteractable(bool interactable)
        {
            if (overrideStrategyToggle == null) return;
            overrideStrategyToggle.interactable = interactable;
            if (!interactable)
            {
                overrideStrategyToggle.SetIsOnWithoutNotify(false);
                _settings.overrideStrategyEnabled = false;
            }
        }

        private void OnVolumeChanged(float linear)
        {
            if (audioMixer != null)
                audioMixer.SetFloat(MasterVolumeParam, LinearToDb(linear));

            _settings.volume = linear;
        }

        private void OnMartingaleThresholdToggled(bool value) { }

        public void OnMartingaleActiveToggled(bool value)
        {
            if (_suppressToggleCallbacks) return;

            _settings.martingaleActive = value;

            if (value)
            {
                blackjackGame?.TryStartMartingaleFromToggle();
            }
            else
            {
                // Deactivating "Martingale is Active" also forces "Martingale automatically plays" off.
                _settings.martingaleAutoPlay = false;
                martingaleAutoPlayToggle?.SetIsOnWithoutNotify(false);

                blackjackGame?.CancelMartingale();
            }
        }

        public void OnMartingaleAutoPlayToggled(bool value)
        {
            if (_suppressToggleCallbacks) return;

            _settings.martingaleAutoPlay = value;
        }

    /// <summary>
    /// Programmatically checks a toggle without firing its onValueChanged callbacks.
    /// Works reliably because the affected toggles use ToggleTransition.None, which
    /// makes SetIsOnWithoutNotify update the checkmark graphic instantly.
    /// </summary>
    private void ForceToggleChecked(Toggle toggle)
        {
            if (toggle == null) return;

            _suppressToggleCallbacks = true;
            toggle.SetIsOnWithoutNotify(true);
            _suppressToggleCallbacks = false;
        }

        private void OnShowStrategyToggled(bool value)
        {
            _settings.showStrategyEnabled = value;
            strategyTableUI?.SetVisible(value);
        }

        private void OnMartingaleThresholdChanged(float value)
        {
            _settings.martingaleThreshold = Mathf.RoundToInt(value);
        }

        /// <summary>Persists the selected test-split rank (2–14, matching the Rank enum).</summary>
        public void OnTestSplitRankChanged(float value)
        {
            if (_settings == null) return;

            _settings.testSplitRank = Mathf.RoundToInt(value);
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
        public bool IsMenuOpen => _menuVisible;

        /// <summary>When true, strategy deviation popup is bypassed and the player's action executes immediately.</summary>
        public bool IsStrategyOverrideEnabled => _settings.overrideStrategyEnabled;

        /// <summary>Returns the Martingale streak threshold from the menu slider.</summary>
        public int MartingaleThreshold => _settings.martingaleThreshold;

        /// <summary>When true, the Martingale suggestion popup is enabled.</summary>
        public bool IsMartingaleActive => _settings.martingaleActive;

        /// <summary>When true, Martingale mode activates and doubles the bet automatically without showing the confirmation popup.</summary>
        public bool IsMartingaleAutoPlay => _settings.martingaleAutoPlay;

        /// <summary>
        /// Programmatically activates the "Martingale is Active" checkbox.
        /// Used when the threshold is exceeded and the popup is about to be shown.
        /// </summary>
        public void ActivateMartingale()
        {
            if (_settings.martingaleActive) return;
            _settings.martingaleActive = true;

            ForceToggleChecked(martingaleActiveToggle);
        }

        /// <summary>
        /// Deactivates the "Override Strategy" checkbox.
        /// Called whenever the game enters Martingale mode.
        /// </summary>
        public void DisableOverrideStrategy()
        {
            if (!_settings.overrideStrategyEnabled) return;
            _settings.overrideStrategyEnabled = false;
            overrideStrategyToggle?.SetIsOnWithoutNotify(false);
        }

        public void DeactivateMartingale()
        {
            if (!_settings.martingaleActive) return;
            _settings.martingaleActive = false;
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

            // Only allow opening when no round is in progress.
            if (!_menuVisible && blackjackGame != null && !blackjackGame.IsBettingAllowed && !blackjackGame.IsRoundOver)
                return;

            SetMenuVisible(!_menuVisible);
            if (!_menuVisible) blackjackGame?.PlayCloseSound();
        }

        /// <summary>Closes the menu panel if it is currently open.</summary>
        /// <param name="playSound">When false, suppresses the close sound. Defaults to true.</param>
        public void CloseMenu(bool playSound = true)
        {
            if (_menuVisible)
            {
                SetMenuVisible(false);
                if (playSound) blackjackGame?.PlayCloseSound();
            }
        }

        /// <summary>Shows or hides the menu via CanvasGroup, keeping the GameObject always active so listeners survive.</summary>
        private void SetMenuVisible(bool visible)
        {
            _menuVisible = visible;
            if (_menuCanvasGroup == null) return;
            _menuCanvasGroup.alpha          = visible ? 1f : 0f;
            _menuCanvasGroup.interactable   = visible;
            _menuCanvasGroup.blocksRaycasts = visible;
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

            if (martingaleAutoPlayToggle != null)
                martingaleAutoPlayToggle.SetIsOnWithoutNotify(_settings.martingaleAutoPlay);

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
