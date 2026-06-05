using Blackjack.UI;
using System.IO;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Blackjack
{
    /// <summary>
    /// Toggles the developer menu panel with F2.
    /// Controls visibility of the three test buttons and the master volume.
    /// Settings are persisted to <c>settings.json</c> in <c>Application.persistentDataPath</c>.
    /// Inspector defaults are used as the initial values; the JSON file overrides them on load.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class MenuController : MonoBehaviour
    {
        private static string SaveFilePath =>
            Path.Combine(Application.persistentDataPath, "settings.json");
        [Header("Menu Panel")]
        [SerializeField] private GameObject menuPanel;

        [Header("Test Button GameObjects")]
        [SerializeField] private GameObject blackjackTestButton;
        [SerializeField] private GameObject bjAllButton;
        [SerializeField] private GameObject ddTestButton;
        [SerializeField] private GameObject testSplitButton;
        [SerializeField] private GameObject dealerBlackjackTestButton;

        [Header("Checkboxes")]
        [SerializeField] private Toggle blackjackTestToggle;
        [SerializeField] private Toggle bjAllToggle;
        [SerializeField] private Toggle ddTestToggle;
        [SerializeField] private Toggle testSplitToggle;
        [SerializeField] private Toggle dealerBjTestToggle;
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
        [Tooltip("Starting Martingale loss-streak threshold for a new session.")]
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
        private RectTransform   _menuRectTransform;
        private bool            _menuVisible;

        /// <summary>Guard flag to prevent re-entrant callback processing when programmatically setting toggle values.</summary>
        private bool _suppressToggleCallbacks;

        /// <summary>Last threshold read from the slider; used to detect UI changes without relying on slider callbacks alone.</summary>
        private int _lastMartingaleThresholdFromSlider = int.MinValue;

        /// <summary>True when at least one setting has changed since the menu was last opened or saved.</summary>
        private bool _settingsDirty;

        // ──────────────────────────────────────────────────────────────────────────
        // Unity lifecycle
        // ──────────────────────────────────────────────────────────────────────────

        private void Awake()
        {
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();

            // Cache the CanvasGroup used to show/hide the panel without SetActive.
            if (menuPanel != null)
            {
                _menuCanvasGroup   = menuPanel.GetComponent<CanvasGroup>();
                _menuRectTransform = menuPanel.GetComponent<RectTransform>();
                UiOverlaySorting.Apply(menuPanel, UiOverlaySorting.Menu);
            }

            BindRowToggleReferences();
            EnsureToggleRowPlacement();
            EnsureTestMenuRowOrder();

            _settings = new OptionsSettings
            {
                martingaleThreshold = defaultMartingaleThreshold,
                showStrategyEnabled = blackjackGame != null && blackjackGame.ShowStrategyTable
            };

            LoadSettingsFromFile();
            ApplySettings();

            // Register callbacks after applying so initial apply does not trigger saves.
            blackjackTestToggle?.onValueChanged.AddListener(OnBlackjackTestToggled);
            bjAllToggle?.onValueChanged.AddListener(OnBjAllToggled);
            ddTestToggle?.onValueChanged.AddListener(OnDdTestToggled);
            testSplitToggle?.onValueChanged.AddListener(OnTestSplitToggled);
            dealerBjTestToggle?.onValueChanged.AddListener(OnDealerBjTestToggled);
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
                                           testSplitToggle, dealerBjTestToggle, overrideStrategyToggle,
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
            {
                HandleStrategyTableRightClick();
                HandleMenuPanelRightClick();
            }

            SyncMartingaleThresholdIfSliderChanged();
        }

        /// <summary>Sets toggle state and forces the checkmark graphic to match (fixes Unity UI desync).</summary>
        private void ApplyMartingaleToggleVisual(Toggle toggle, bool isOn, bool interactable)
        {
            if (toggle == null) return;

            toggle.interactable = interactable;

            _suppressToggleCallbacks = true;
            toggle.SetIsOnWithoutNotify(isOn);
            _suppressToggleCallbacks = false;

            if (isOn)
                MartingaleThresholdToggleGate.SyncCheckmark(toggle);
            else
                MartingaleThresholdToggleGate.HideCheckmark(toggle);
        }

        /// <summary>
        /// Binds toggles by GameObject name so row layout cannot mis-wire Martingale logic.
        /// </summary>
        private void BindRowToggleReferences()
        {
            dealerBjTestToggle    ??= FindMenuToggle("DealerBJTestToggle");
            blackjackTestToggle   ??= FindMenuToggle("BlackjackTestToggle");
            bjAllToggle           ??= FindMenuToggle("BJAllToggle");
            ddTestToggle          ??= FindMenuToggle("DDTestToggle");
            testSplitToggle       ??= FindMenuToggle("TestSplitToggle");
            showStrategyToggle    = FindMenuToggle("ShowStrategyToggle");
            martingaleActiveToggle = FindMenuToggle("MartingaleActiveToggle");
            martingaleAutoPlayToggle = FindMenuToggle("MartingaleAutoPlayToggle");

            if (martingaleThresholdSlider == null && menuPanel != null)
            {
                foreach (var slider in menuPanel.GetComponentsInChildren<Slider>(true))
                {
                    if (slider.name == "MartingaleThresholdSlider")
                    {
                        martingaleThresholdSlider = slider;
                        break;
                    }
                }
            }

            testSplitRankSlider ??= GetTestSplitRankSlider();
        }

        private Slider GetTestSplitRankSlider()
        {
            if (testSplitRankSlider != null) return testSplitRankSlider;
            if (menuPanel == null) return null;

            foreach (var slider in menuPanel.GetComponentsInChildren<Slider>(true))
            {
                if (slider.name == "TestSplitRankSlider")
                {
                    testSplitRankSlider = slider;
                    return slider;
                }
            }

            return null;
        }

        /// <summary>
        /// Ensures each toggle lives on its named row (fixes swapped checkbox placement in the scene).
        /// </summary>
        private void EnsureToggleRowPlacement()
        {
            ReparentToggleToRow("DealerBJTestToggle", "DealerBJRow");
            ReparentToggleToRow("BlackjackTestToggle", "BJTestRow");
            ReparentToggleToRow("BJAllToggle", "BJAllRow");
            ReparentToggleToRow("DDTestToggle", "DDTestRow");
            ReparentToggleToRow("TestSplitToggle", "TestSplitRow");
            ReparentToggleToRow("ShowStrategyToggle", "ShowStrategyRow");
            ReparentToggleToRow("MartingaleActiveToggle", "MartingaleActiveRow");
            ReparentToggleToRow("MartingaleAutoPlayToggle", "MartingaleAutoPlayRow");
        }

        /// <summary>Keeps test rows in menu order with Dealer BJ above Blackjack Test.</summary>
        private void EnsureTestMenuRowOrder()
        {
            if (menuPanel == null) return;

            int index = 0;
            Transform title = menuPanel.transform.Find("TitleLabel");
            if (title != null)
                title.SetSiblingIndex(index++);

            foreach (string rowName in new[]
                     {
                         "DealerBJRow",
                         "BJTestRow",
                         "BJAllRow",
                         "DDTestRow",
                         "TestSplitRow",
                     })
            {
                Transform row = menuPanel.transform.Find(rowName);
                if (row != null)
                    row.SetSiblingIndex(index++);
            }
        }

        private void ReparentToggleToRow(string toggleName, string rowName)
        {
            if (menuPanel == null) return;

            Transform row = null;
            foreach (Transform child in menuPanel.transform)
            {
                if (child.name == rowName)
                {
                    row = child;
                    break;
                }
            }

            if (row == null) return;

            var toggle = FindMenuToggle(toggleName);
            if (toggle == null || toggle.transform.parent == row) return;

            toggle.transform.SetParent(row, false);
            toggle.transform.SetAsFirstSibling();
        }

        private Toggle FindMenuToggle(string toggleObjectName)
        {
            if (menuPanel == null) return null;

            foreach (var toggle in menuPanel.GetComponentsInChildren<Toggle>(true))
            {
                if (toggle.name == toggleObjectName)
                    return toggle;
            }

            return null;
        }

        /// <summary>Show Strategy must never be grayed out by Martingale threshold logic.</summary>
        private void EnsureShowStrategyToggleUnlocked()
        {
            var toggle = FindMenuToggle("ShowStrategyToggle");
            if (toggle == null) return;

            toggle.interactable = true;
            MartingaleThresholdToggleGate.SyncCheckmark(toggle);
        }

        private Toggle GetShowStrategyRowToggle() => FindMenuToggle("ShowStrategyToggle");
        private Toggle GetMartingaleActiveRowToggle() => FindMenuToggle("MartingaleActiveToggle");
        private Toggle GetMartingaleAutoPlayRowToggle() => FindMenuToggle("MartingaleAutoPlayToggle");

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

        /// <summary>Right-click over the strategy table area toggles it open or closed.</summary>
        private void HandleStrategyTableRightClick()
        {
            if (strategyTableUI == null || Mouse.current == null) return;

            if (!strategyTableUI.ContainsScreenPoint(Mouse.current.position.ReadValue()))
                return;

            if (strategyTableUI.gameObject.activeSelf)
                HideStrategyTable();
            else
                ShowStrategyTable();
        }

        /// <summary>Right-click inside the menu panel area opens the menu when hidden, or closes it when visible.</summary>
        private void HandleMenuPanelRightClick()
        {
            if (Mouse.current == null) return;

            if (_menuVisible)
            {
                if (!IsPointerOverMenuPanel()) return;
                CloseMenuInternal(playSound: true);
                return;
            }

            if (_menuRectTransform == null) return;

            // Honour the same guard as keyboard open: only allow when betting is idle or round is over.
            if (blackjackGame != null && !blackjackGame.IsBettingAllowed && !blackjackGame.IsRoundOver)
                return;

            if (!IsPointerOverMenuPanel()) return;

            SetMenuVisible(true);
            SyncMartingaleThresholdFromSlider();
            uiSounds?.toggleSound.Play(audioSource);
        }

        private bool IsPointerOverMenuPanel()
        {
            if (_menuRectTransform == null || Mouse.current == null) return false;

            Canvas canvas = _menuRectTransform.GetComponentInParent<Canvas>();
            Camera uiCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;

            return RectTransformUtility.RectangleContainsScreenPoint(
                _menuRectTransform,
                Mouse.current.position.ReadValue(),
                uiCamera);
        }

        private void ShowStrategyTable()
        {
            _settings.showStrategyEnabled = true;
            showStrategyToggle?.SetIsOnWithoutNotify(true);
            MartingaleThresholdToggleGate.SyncCheckmark(showStrategyToggle);
            strategyTableUI.SetVisible(true);
            uiSounds?.toggleSound.Play(audioSource);
            _settingsDirty = true;
        }

        private void HideStrategyTable()
        {
            strategyTableUI.SetVisible(false);
            _settings.showStrategyEnabled = false;
            showStrategyToggle?.SetIsOnWithoutNotify(false);
            blackjackGame?.PlayCloseSound();
            _settingsDirty = true;
        }

        /// <summary>Plays the toggle click sound whenever any option checkbox changes value.</summary>
        private void OnToggleSoundPlay(bool _) => uiSounds?.toggleSound.Play(audioSource);

        private void OnDestroy()
        {
            if (blackjackGame != null)
                blackjackGame.OnAlwaysLoseDisabled -= DisableAlwaysLose;
        }

        private void OnApplicationQuit()
        {
            PersistSettingsToFile();
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Toggle callbacks
        // ──────────────────────────────────────────────────────────────────────────

        private void OnBlackjackTestToggled(bool value)
        {
            blackjackTestButton?.SetActive(value);
            _settings.blackjackTestEnabled = value;
            _settingsDirty = true;
        }

        private void OnBjAllToggled(bool value)
        {
            bjAllButton?.SetActive(value);
            _settings.bjAllEnabled = value;
            _settingsDirty = true;
        }

        private void OnDdTestToggled(bool value)
        {
            ddTestButton?.SetActive(value);
            _settings.ddTestEnabled = value;
            _settingsDirty = true;
        }

        private void OnTestSplitToggled(bool value)
        {
            testSplitButton?.SetActive(value);
            _settings.testSplitEnabled = value;
            _settingsDirty = true;
        }

        private void OnDealerBjTestToggled(bool value)
        {
            dealerBlackjackTestButton?.SetActive(value);
            _settings.dealerBjTestEnabled = value;
            _settingsDirty = true;
        }

        private void OnOverrideStrategyToggled(bool value)
        {
            _settings.overrideStrategyEnabled = value;
            _settingsDirty = true;
        }

        /// <summary>Forces the player to lose every round when enabled. Used for Martingale testing.</summary>
        private void OnAlwaysLoseToggled(bool value)
        {
            if (blackjackGame != null)
                blackjackGame.AlwaysLose = value;
            _settings.alwaysLoseEnabled = value;
            _settingsDirty = true;
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
            _settingsDirty = true;
        }

        private void OnMartingaleThresholdToggled(bool value) { }

        public void OnMartingaleActiveToggled(bool value)
        {
            if (_suppressToggleCallbacks) return;

            if (value && ReadMartingaleThresholdFromSlider() <= 0)
                SetMartingaleThreshold(1);

            _settings.martingaleActive = value;

            if (value)
            {
                ApplyMartingaleToggleVisual(GetMartingaleActiveRowToggle(), isOn: true, interactable: true);
                blackjackGame?.TryStartMartingaleFromToggle();
            }
            else
            {
                // Deactivating "Martingale is Active" also forces "Martingale automatically plays" off.
                DeactivateMartingaleAutoPlay();

                blackjackGame?.CancelMartingale();
            }

            _settingsDirty = true;
        }

        public void OnMartingaleAutoPlayToggled(bool value)
        {
            if (_suppressToggleCallbacks) return;

            if (value && ReadMartingaleThresholdFromSlider() <= 0)
                SetMartingaleThreshold(1);

            _settings.martingaleAutoPlay = value;

            ApplyMartingaleToggleVisual(
                GetMartingaleAutoPlayRowToggle(),
                isOn: value,
                interactable: true);

            if (value)
            {
                ActivateMartingale();
                blackjackGame?.TryStartMartingaleFromToggle();
            }

            _settingsDirty = true;
        }

        /// <summary>Reads the Martingale threshold from the slider when present.</summary>
        private int ReadMartingaleThresholdFromSlider() =>
            martingaleThresholdSlider != null
                ? Mathf.RoundToInt(martingaleThresholdSlider.value)
                : _settings.martingaleThreshold;

        /// <summary>Sets the threshold slider and syncs Martingale toggle state.</summary>
        private void SetMartingaleThreshold(int threshold)
        {
            if (martingaleThresholdSlider != null)
            {
                threshold = Mathf.Clamp(
                    threshold,
                    (int)martingaleThresholdSlider.minValue,
                    (int)martingaleThresholdSlider.maxValue);
                martingaleThresholdSlider.value = threshold;
                return;
            }

            _settings.martingaleThreshold = threshold;
            _lastMartingaleThresholdFromSlider = threshold;
            ApplyMartingaleThresholdState();
        }

        /// <summary>Keeps settings and Martingale toggles in sync with the threshold slider value.</summary>
        private void SyncMartingaleThresholdFromSlider()
        {
            _settings.martingaleThreshold = ReadMartingaleThresholdFromSlider();
            _lastMartingaleThresholdFromSlider = _settings.martingaleThreshold;
            ApplyMartingaleThresholdState();
        }

        /// <summary>
        /// Applies threshold state when the slider moves but the UnityEvent did not fire
        /// (e.g. some inspector setups or drag end without callback).
        /// </summary>
        private void SyncMartingaleThresholdIfSliderChanged()
        {
            if (martingaleThresholdSlider == null) return;

            int sliderThreshold = ReadMartingaleThresholdFromSlider();
            if (sliderThreshold == _lastMartingaleThresholdFromSlider) return;

            SyncMartingaleThresholdFromSlider();
        }

        private void OnShowStrategyToggled(bool value)
        {
            _settings.showStrategyEnabled = value;
            MartingaleThresholdToggleGate.SyncCheckmark(showStrategyToggle);
            strategyTableUI?.SetVisible(value);
            _settingsDirty = true;
        }

        public void OnMartingaleThresholdChanged(float value)
        {
            _settings.martingaleThreshold = Mathf.RoundToInt(value);
            _lastMartingaleThresholdFromSlider = _settings.martingaleThreshold;
            ApplyMartingaleThresholdState();
            _settingsDirty = true;
        }

        /// <summary>Unchecks both Martingale toggles when the threshold is 0; both stay clickable.</summary>
        private void ApplyMartingaleThresholdState()
        {
            int threshold = ReadMartingaleThresholdFromSlider();
            _settings.martingaleThreshold = threshold;
            _lastMartingaleThresholdFromSlider = threshold;

            var activeToggle = GetMartingaleActiveRowToggle();
            var autoPlayToggle = GetMartingaleAutoPlayRowToggle();
            EnsureShowStrategyToggleUnlocked();

            if (threshold <= 0)
            {
                _settings.martingaleActive   = false;
                _settings.martingaleAutoPlay = false;
                ApplyMartingaleToggleVisual(activeToggle, isOn: false, interactable: true);
                ApplyMartingaleToggleVisual(autoPlayToggle, isOn: false, interactable: true);
                EnsureShowStrategyToggleUnlocked();
                blackjackGame?.CancelMartingale();
                return;
            }

            _settings.martingaleActive = true;
            ApplyMartingaleToggleVisual(activeToggle, isOn: true, interactable: true);
            ApplyMartingaleToggleVisual(
                autoPlayToggle,
                isOn: _settings.martingaleAutoPlay,
                interactable: true);
            EnsureShowStrategyToggleUnlocked();
        }

        /// <summary>Persists the selected test-split rank (2–14, matching the Rank enum).</summary>
        public void OnTestSplitRankChanged(float value)
        {
            if (_settings == null) return;

            _settings.testSplitRank = Mathf.RoundToInt(value);
            _settingsDirty = true;
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
        public bool IsMartingaleActive =>
            _settings.martingaleThreshold > 0 && _settings.martingaleActive;

        /// <summary>When true, Martingale mode activates and doubles the bet automatically without showing the confirmation popup.</summary>
        public bool IsMartingaleAutoPlay =>
            _settings.martingaleThreshold > 0 && _settings.martingaleAutoPlay;

        /// <summary>
        /// Programmatically activates the "Martingale is Active" checkbox.
        /// Used when the threshold is exceeded and the popup is about to be shown.
        /// </summary>
        public void ActivateMartingale()
        {
            if (ReadMartingaleThresholdFromSlider() <= 0) return;

            _settings.martingaleThreshold = ReadMartingaleThresholdFromSlider();
            _settings.martingaleActive = true;
            ApplyMartingaleToggleVisual(GetMartingaleActiveRowToggle(), isOn: true, interactable: true);
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
            _settings.martingaleActive = false;
            ApplyMartingaleToggleVisual(GetMartingaleActiveRowToggle(), isOn: false, interactable: true);
        }

        private void DeactivateMartingaleAutoPlay()
        {
            _settings.martingaleAutoPlay = false;
            ApplyMartingaleToggleVisual(GetMartingaleAutoPlayRowToggle(), isOn: false, interactable: true);
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

            if (_menuVisible)
                CloseMenuInternal(playSound: true);
            else
            {
                _settingsDirty = false;
                SetMenuVisible(true);
                SyncMartingaleThresholdFromSlider();
            }
        }

        /// <summary>Closes the menu panel if it is currently open.</summary>
        /// <param name="playSound">When false, suppresses the close sound. Defaults to true.</param>
        public void CloseMenu(bool playSound = true) => CloseMenuInternal(playSound);

        private void CloseMenuInternal(bool playSound)
        {
            if (!_menuVisible) return;

            if (_settingsDirty)
            {
                PersistSettingsToFile();
                _settingsDirty = false;
            }
            SetMenuVisible(false);
            if (playSound) blackjackGame?.PlayCloseSound();
        }

        /// <summary>Writes the current settings to <see cref="SaveFilePath"/> as JSON.</summary>
        private void PersistSettingsToFile()
        {
            try
            {
                string json = JsonUtility.ToJson(_settings, prettyPrint: true);
                File.WriteAllText(SaveFilePath, json);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[MenuController] Failed to save settings: {e.Message}");
            }
        }

        /// <summary>
        /// Reads <see cref="SaveFilePath"/> and overlays its values onto <see cref="_settings"/>.
        /// Inspector defaults remain for any field absent from the file.
        /// </summary>
        private void LoadSettingsFromFile()
        {
            if (!File.Exists(SaveFilePath)) return;

            try
            {
                string json = File.ReadAllText(SaveFilePath);
                JsonUtility.FromJsonOverwrite(json, _settings);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[MenuController] Failed to load settings: {e.Message}");
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

            if (dealerBjTestToggle != null)
                dealerBjTestToggle.SetIsOnWithoutNotify(_settings.dealerBjTestEnabled);
            dealerBlackjackTestButton?.SetActive(_settings.dealerBjTestEnabled);

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

            var splitRankSlider = GetTestSplitRankSlider();
            if (splitRankSlider != null)
            {
                splitRankSlider.SetValueWithoutNotify(_settings.testSplitRank);
                splitRankSlider.GetComponent<SliderValueDisplay>()?.RefreshFromSlider();
            }

            _lastMartingaleThresholdFromSlider = _settings.martingaleThreshold;

            if (_settings.martingaleThreshold <= 0)
            {
                _settings.martingaleActive   = false;
                _settings.martingaleAutoPlay = false;
            }

            ApplyMartingaleToggleVisual(
                GetMartingaleActiveRowToggle(),
                _settings.martingaleActive,
                interactable: true);
            ApplyMartingaleToggleVisual(
                GetMartingaleAutoPlayRowToggle(),
                _settings.martingaleAutoPlay,
                interactable: true);
            EnsureShowStrategyToggleUnlocked();
        }

        // Converts a linear [0,1] slider value to decibels for the AudioMixer.
        private static float LinearToDb(float linear) =>
            linear > 0.0001f ? Mathf.Log10(linear) * 20f : -80f;

        // Converts decibels from the AudioMixer back to a linear [0,1] slider value.
        private static float DbToLinear(float dB) =>
            Mathf.Pow(10f, dB / 20f);
    }
}
