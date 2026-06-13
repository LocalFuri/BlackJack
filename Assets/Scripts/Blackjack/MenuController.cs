using Blackjack.UI;
using System.Collections;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Blackjack
{
    /// <summary>
    /// Toggles the developer menu panel with F2.
    /// Controls visibility of the three test buttons and the master volume.
    /// Settings are persisted via <see cref="SettingsRepository"/> (<c>options.json</c> next to the
    /// build executable, with fallback to <c>Application.persistentDataPath</c>).
    /// Inspector defaults are used as the initial values; the JSON file overrides them on load.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class MenuController : MonoBehaviour
    {
        private const string LegacySettingsFileName = "settings.json";

        [Header("Menu Panel")]
        [SerializeField] private GameObject menuPanel;

        [Header("Test Button GameObjects")]
        [SerializeField] private GameObject blackjackTestButton;
        [SerializeField] private GameObject bjAllButton;
        [SerializeField] private GameObject ddTestButton;
        [SerializeField] private GameObject testSplitButton;
        [SerializeField] private GameObject dealerBlackjackTestButton;

        [Header("Checkboxes")]
        [SerializeField] private Toggle autoplayToggle;
        [SerializeField] private Toggle autoplayMaxSpeedToggle;
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

        [Header("Current Bet")]
        [SerializeField] private InputField currentBetInputField;

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
        private float           _preFocusVolume = 1f;

        /// <summary>Guard flag to prevent re-entrant callback processing when programmatically setting toggle values.</summary>
        private bool _suppressToggleCallbacks;

        /// <summary>Guard flag when programmatically updating the current-bet input field.</summary>
        private bool _suppressCurrentBetInputCallbacks;

        /// <summary>Cached lock state so we only refresh Martingale menu controls when mode toggles.</summary>
        private bool _lastMartingaleMenuLocked;

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

            EnsureAutoplayMaxSpeedRow();
            EnsureCurrentBetRow();
            BindRowToggleReferences();
            EnsureToggleRowPlacement();
            EnsureTestMenuRowOrder();

            if (strategyTableUI == null)
                strategyTableUI = FindObjectOfType<StrategyTableUI>();

            _settings = new OptionsSettings
            {
                martingaleThreshold = defaultMartingaleThreshold,
                showStrategyEnabled = blackjackGame != null && blackjackGame.ShowStrategyTable
            };

            LoadSettingsFromFile();
            ApplySettings();

            Application.quitting += HandleApplicationQuitting;

            // Register callbacks after applying so initial apply does not trigger saves.
            autoplayToggle?.onValueChanged.AddListener(OnAutoplayToggled);
            autoplayMaxSpeedToggle?.onValueChanged.AddListener(OnAutoplayMaxSpeedToggled);
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
            currentBetInputField?.onEndEdit.AddListener(OnCurrentBetInputChanged);
            WireCurrentBetInputValidation();

            // Play toggle sound whenever any checkbox is turned on.
            foreach (var toggle in new[] { autoplayToggle, autoplayMaxSpeedToggle, blackjackTestToggle, bjAllToggle, ddTestToggle,
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
            HandleCurrentBetInputTabFocus();
            RefreshMartingaleMenuLockIfChanged();
        }

        private void RefreshMartingaleMenuLockIfChanged()
        {
            if (!_menuVisible) return;

            bool locked = IsMartingaleMenuLocked;
            if (locked == _lastMartingaleMenuLocked) return;

            RefreshMartingaleMenuLock();
        }

        private void HandleCurrentBetInputTabFocus()
        {
            if (!_menuVisible || Keyboard.current == null || IsMartingaleMenuLocked)
                return;

            if (!Keyboard.current[Key.Tab].wasPressedThisFrame)
                return;

            InputField input = GetCurrentBetInputField();
            if (input == null)
                return;

            // Let Tab finish editing and move on when the field is already active.
            if (input.isFocused)
                return;

            FocusCurrentBetInputField();
        }

        private void FocusCurrentBetInputField()
        {
            InputField input = GetCurrentBetInputField();
            if (input == null || !input.interactable)
                return;

            CurrentBetInputClickForwarder forwarder = input.GetComponent<CurrentBetInputClickForwarder>();
            if (forwarder != null)
            {
                forwarder.FocusInput();
                return;
            }

            EventSystem.current?.SetSelectedGameObject(input.gameObject);
            input.Select();
            input.ActivateInputField();
            CurrentBetInputClickForwarder.SelectEntireText(input);
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (audioMixer == null) return;

            if (!hasFocus)
            {
                // Store current volume and silence the mixer.
                audioMixer.GetFloat(MasterVolumeParam, out float currentDb);
                _preFocusVolume = DbToLinear(currentDb);
                audioMixer.SetFloat(MasterVolumeParam, LinearToDb(0f));
            }
            else
            {
                // Restore the volume that was set before losing focus.
                audioMixer.SetFloat(MasterVolumeParam, LinearToDb(_preFocusVolume));
            }
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
            autoplayToggle        ??= FindMenuToggle("AutoplayToggle");
            autoplayMaxSpeedToggle ??= FindMenuToggle("AutoplayMaxSpeedToggle");
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
            currentBetInputField ??= GetCurrentBetInputField();
        }

        private InputField GetCurrentBetInputField()
        {
            if (currentBetInputField != null) return currentBetInputField;
            if (menuPanel == null) return null;

            Transform row = menuPanel.transform.Find("CurrentBetRow");
            if (row != null)
            {
                currentBetInputField = row.GetComponentInChildren<InputField>(true);
                if (currentBetInputField != null)
                    return currentBetInputField;
            }

            foreach (var input in menuPanel.GetComponentsInChildren<InputField>(true))
            {
                if (input.name == "CurrentBetInputField")
                {
                    currentBetInputField = input;
                    return currentBetInputField;
                }
            }

            return null;
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
            ReparentToggleToRow("AutoplayToggle", "AutoplayRow");
            ReparentToggleToRow("AutoplayMaxSpeedToggle", "AutoplayMaxSpeedRow");
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
                         "AutoplayRow",
                         "AutoplayMaxSpeedRow",
                         "DealerBJRow",
                         "BJTestRow",
                         "BJAllRow",
                         "DDTestRow",
                         "TestSplitRow",
                         "CurrentBetRow",
                     })
            {
                Transform row = menuPanel.transform.Find(rowName);
                if (row != null)
                    row.SetSiblingIndex(index++);
            }
        }

        /// <summary>
        /// Clones AutoplayRow to create AutoplayMaxSpeedRow directly below it if it does not already exist.
        /// This keeps the row visually consistent without requiring scene edits.
        /// </summary>
        private void EnsureAutoplayMaxSpeedRow()
        {
            if (menuPanel == null) return;

            Transform autoplayRow = menuPanel.transform.Find("AutoplayRow");
            if (autoplayRow == null) return;

            const string rowName   = "AutoplayMaxSpeedRow";
            const string labelText = "Autoplay at max speed";

            Transform existing = menuPanel.transform.Find(rowName);
            if (existing != null)
                return; // Already present; EnsureTestMenuRowOrder will position it.

            // Clone AutoplayRow as a template.
            GameObject newRow = Instantiate(autoplayRow.gameObject, menuPanel.transform);
            newRow.name = rowName;
            newRow.transform.SetSiblingIndex(autoplayRow.GetSiblingIndex() + 1);

            // Rename the toggle and clear any cloned persistent listeners.
            Transform toggleTf = newRow.transform.Find("AutoplayToggle");
            if (toggleTf != null)
            {
                toggleTf.name = "AutoplayMaxSpeedToggle";
                Toggle t = toggleTf.GetComponent<Toggle>();
                if (t != null)
                {
                    t.isOn = false;
                    t.onValueChanged.RemoveAllListeners();
                }
            }

            // Rename the label and update its text.
            Transform labelTf = newRow.transform.Find("AutoplayLabel");
            if (labelTf != null)
            {
                labelTf.name = "AutoplayMaxSpeedLabel";
                Text txt = labelTf.GetComponent<Text>();
                if (txt != null)
                    txt.text = labelText;
            }
        }

        /// <summary>
        /// Clones MartingaleThresholdRow to create CurrentBetRow below Test Split if it does not already exist.
        /// </summary>
        private void EnsureCurrentBetRow()
        {
            if (menuPanel == null) return;

            const string rowName   = "CurrentBetRow";
            const string labelText = "Current Bet";

            Transform existing = menuPanel.transform.Find(rowName);
            if (existing != null)
            {
                currentBetInputField ??= existing.GetComponentInChildren<InputField>(true);
                Text existingLabel = existing.Find("CurrentBetLabel")?.GetComponent<Text>();

                // Replace legacy DefaultControls fields (Text Area child) with the simpler layout.
                if (currentBetInputField != null && currentBetInputField.transform.Find("Text Area") != null)
                {
                    Destroy(currentBetInputField.gameObject);
                    currentBetInputField = null;
                }

                if (currentBetInputField == null)
                    currentBetInputField = CreateSimpleBetInputField(existing, existingLabel?.font);

                ApplyCurrentBetInputFieldStyle(currentBetInputField, existingLabel);
                return;
            }

            Transform templateRow = menuPanel.transform.Find("MartingaleThresholdRow");
            if (templateRow == null) return;

            GameObject newRow = Instantiate(templateRow.gameObject, menuPanel.transform);
            newRow.name = rowName;

            Text labelTextComponent = null;
            Transform labelTf = newRow.transform.Find("MartingaleThresholdLabel");
            if (labelTf != null)
            {
                labelTf.name = "CurrentBetLabel";
                labelTextComponent = labelTf.GetComponent<Text>();
                if (labelTextComponent != null)
                    labelTextComponent.text = labelText;
            }

            Transform sliderTf = newRow.transform.Find("MartingaleThresholdSlider");
            if (sliderTf != null)
                Destroy(sliderTf.gameObject);

            currentBetInputField = CreateSimpleBetInputField(newRow.transform, labelTextComponent?.font);
            ApplyCurrentBetInputFieldStyle(currentBetInputField, labelTextComponent);
        }

        private static InputField CreateSimpleBetInputField(Transform parent, Font font)
        {
            var inputGo = new GameObject(
                "CurrentBetInputField",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(InputField),
                typeof(LayoutElement),
                typeof(CurrentBetInputClickForwarder));

            inputGo.transform.SetParent(parent, false);

            Font fieldFont = GetCurrentBetInputFont(font);
            float inputWidth = MeasureCurrentBetInputWidth(font);
            RectTransform inputRect = inputGo.GetComponent<RectTransform>();
            inputRect.sizeDelta = new Vector2(inputWidth, 28f);

            Image background = inputGo.GetComponent<Image>();
            ApplyCurrentBetInputBackground(background);

            Text text = CreateBetInputTextChild(inputGo.transform, "Text", fieldFont, string.Empty, CurrentBetInputTextColor);
            text.alignment = TextAnchor.MiddleRight;
            Text placeholder = CreateBetInputTextChild(
                inputGo.transform,
                "Placeholder",
                fieldFont,
                FormatCurrentBetGerman(1),
                CurrentBetInputPlaceholderColor);
            placeholder.alignment = TextAnchor.MiddleRight;

            InputField input = inputGo.GetComponent<InputField>();
            input.targetGraphic = background;
            input.textComponent = text;
            input.placeholder = placeholder;
            input.contentType = InputField.ContentType.Standard;
            input.characterLimit = FormatCurrentBetGerman(BlackjackGame.BetLimit).Length;
            input.lineType = InputField.LineType.SingleLine;
            ApplyCurrentBetInputColors(input);

            return input;
        }

        private static Text CreateBetInputTextChild(
            Transform parent,
            string objectName,
            Font font,
            string value,
            Color color)
        {
            var textGo = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textGo.transform.SetParent(parent, false);

            RectTransform textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(CurrentBetInputHorizontalPadding, 2f);
            textRect.offsetMax = new Vector2(-CurrentBetInputHorizontalPadding, -2f);

            Text text = textGo.GetComponent<Text>();
            ApplyCurrentBetInputTypography(text, font);
            text.text = value;
            text.color = color;
            text.supportRichText = false;
            text.raycastTarget = false;
            return text;
        }

        private static void ApplyCurrentBetInputColors(InputField input)
        {
            input.selectionColor = CurrentBetInputSelectionColor;
            input.customCaretColor = true;
            input.caretColor = Color.black;

            ColorBlock colors = input.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.white;
            colors.pressedColor = Color.white;
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            input.colors = colors;

            if (input.textComponent != null)
                input.textComponent.color = CurrentBetInputTextColor;

            if (input.placeholder is Text placeholder)
                placeholder.color = CurrentBetInputPlaceholderColor;

            if (input.targetGraphic is Image background)
                ApplyCurrentBetInputBackground(background);
        }

        private static void ApplyCurrentBetInputBackground(Image background)
        {
            background.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
            background.type = Image.Type.Sliced;
            background.color = CurrentBetInputBackgroundColor;
            background.raycastTarget = true;
        }

        private static void ApplyCurrentBetInputFieldStyle(InputField input, Text labelReference)
        {
            if (input == null) return;

            input.interactable = true;

            Font menuFont = labelReference != null ? labelReference.font : null;

            if (input.textComponent != null)
            {
                ApplyCurrentBetInputTypography(input.textComponent, menuFont);
                input.textComponent.alignment = TextAnchor.MiddleRight;
                input.textComponent.raycastTarget = false;
            }

            if (input.placeholder is Text placeholder)
            {
                ApplyCurrentBetInputTypography(placeholder, menuFont);
                placeholder.text = FormatCurrentBetGerman(1);
                placeholder.alignment = TextAnchor.MiddleRight;
                placeholder.raycastTarget = false;
            }

            ApplyCurrentBetInputColors(input);

            float inputWidth = MeasureCurrentBetInputWidth(menuFont);
            RectTransform inputRect = input.GetComponent<RectTransform>();
            if (inputRect != null)
                inputRect.sizeDelta = new Vector2(inputWidth, 28f);

            foreach (var graphic in input.GetComponentsInChildren<Graphic>(true))
            {
                if (graphic != input.targetGraphic)
                    graphic.raycastTarget = false;
            }

            LayoutElement layout = input.GetComponent<LayoutElement>();
            if (layout == null)
                layout = input.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = inputWidth;
            layout.preferredHeight = 28f;
            layout.minWidth = inputWidth;
            layout.minHeight = 28f;

            CurrentBetInputClickForwarder forwarder = input.GetComponent<CurrentBetInputClickForwarder>();
            if (forwarder == null)
                forwarder = input.gameObject.AddComponent<CurrentBetInputClickForwarder>();
            forwarder.Bind(input);

            if (labelReference != null)
                labelReference.raycastTarget = false;
        }

        private const int CurrentBetInputFontSize = 18;
        private const FontStyle CurrentBetInputFontStyle = FontStyle.Bold;
        private const float CurrentBetInputHorizontalPadding = 4f;
        private static readonly Color CurrentBetInputBackgroundColor = new Color(0.95f, 0.95f, 0.95f, 1f);
        private static readonly Color CurrentBetInputTextColor = Color.black;
        private static readonly Color CurrentBetInputPlaceholderColor = new Color(0.4f, 0.4f, 0.4f, 0.75f);
        private static readonly Color CurrentBetInputSelectionColor = new Color(0.2f, 0.45f, 0.85f, 0.35f);
        private static readonly Color CurrentBetInputLockedBackgroundColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        private static readonly Color CurrentBetInputLockedTextColor = new Color(0.45f, 0.45f, 0.45f, 1f);

        private static Font GetCurrentBetInputFont(Font labelFont) =>
            labelFont != null
                ? labelFont
                : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        private static void ApplyCurrentBetInputTypography(Text text, Font labelFont)
        {
            text.font = GetCurrentBetInputFont(labelFont);
            text.fontSize = CurrentBetInputFontSize;
            text.fontStyle = CurrentBetInputFontStyle;
        }

        private static float MeasureCurrentBetInputWidth(Font labelFont)
        {
            string sample = FormatCurrentBetGerman(BlackjackGame.BetLimit);
            var measureGo = new GameObject("CurrentBetWidthMeasure", typeof(Text));
            try
            {
                Text measureText = measureGo.GetComponent<Text>();
                ApplyCurrentBetInputTypography(measureText, labelFont);
                measureText.text = sample;
                measureText.alignment = TextAnchor.MiddleRight;

                float textWidth = measureText.preferredWidth;
                return textWidth + (CurrentBetInputHorizontalPadding * 2f);
            }
            finally
            {
                Object.Destroy(measureGo);
            }
        }

        private static int ClampCurrentBet(int bet) =>
            Mathf.Clamp(bet, 1, BlackjackGame.BetLimit);

        private static readonly CultureInfo CurrentBetCulture =
            CultureInfo.GetCultureInfo("de-DE");

        private static string FormatCurrentBetGerman(int bet) =>
            ClampCurrentBet(bet).ToString("N0", CurrentBetCulture);

        private static bool TryParseCurrentBetGerman(string text, out int bet, bool allowPartial = false)
        {
            bet = allowPartial ? 0 : 1;
            if (string.IsNullOrWhiteSpace(text))
                return allowPartial;

            string digits = text.Replace(".", string.Empty).Replace(" ", string.Empty).Trim();
            if (digits.Length == 0)
                return allowPartial;

            for (int i = 0; i < digits.Length; i++)
            {
                if (!char.IsDigit(digits[i]))
                    return false;
            }

            if (digits.Length > BlackjackGame.BetLimit.ToString().Length)
                return false;

            if (!int.TryParse(digits, NumberStyles.Integer, CultureInfo.InvariantCulture, out bet))
                return false;

            if (bet > BlackjackGame.BetLimit)
                return false;

            if (!allowPartial)
                bet = ClampCurrentBet(bet);

            return true;
        }

        private void WireCurrentBetInputValidation()
        {
            var input = GetCurrentBetInputField();
            if (input == null) return;

            input.contentType = InputField.ContentType.Standard;
            input.characterLimit = FormatCurrentBetGerman(BlackjackGame.BetLimit).Length;
            input.onValidateInput = ValidateCurrentBetInputCharacter;

            CurrentBetInputClickForwarder forwarder = input.GetComponent<CurrentBetInputClickForwarder>();
            if (forwarder != null)
                forwarder.OnSelected = StripCurrentBetInputFormatting;
        }

        private void StripCurrentBetInputFormatting()
        {
            if (_suppressCurrentBetInputCallbacks) return;

            var input = GetCurrentBetInputField();
            if (input == null || !TryParseCurrentBetGerman(input.text, out int bet))
                return;

            _suppressCurrentBetInputCallbacks = true;
            input.SetTextWithoutNotify(bet.ToString(CultureInfo.InvariantCulture));
            _suppressCurrentBetInputCallbacks = false;
        }

        private char ValidateCurrentBetInputCharacter(string text, int charIndex, char addedChar)
        {
            if (addedChar == '.')
            {
                string withDot = text.Insert(charIndex, addedChar.ToString());
                if (withDot.StartsWith(".") || withDot.Contains(".."))
                    return '\0';

                return TryParseCurrentBetGerman(withDot, out _, allowPartial: true) ? addedChar : '\0';
            }

            if (!char.IsDigit(addedChar))
                return '\0';

            string proposed = text.Insert(charIndex, addedChar.ToString());
            return TryParseCurrentBetGerman(proposed, out _, allowPartial: true) ? addedChar : '\0';
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
            MartingaleThresholdToggleGate.SyncCheckmark(showStrategyToggle);
            strategyTableUI.SetVisible(newValue);

            uiSounds?.toggleSound.Play(audioSource);
            PersistSettingsToFile();
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

            if (!IsPointerOverMenuPanel()) return;

            OpenMenuInternal(playSound: true);
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
            PersistSettingsToFile();
        }

        private void HideStrategyTable()
        {
            strategyTableUI.SetVisible(false);
            _settings.showStrategyEnabled = false;
            showStrategyToggle?.SetIsOnWithoutNotify(false);
            MartingaleThresholdToggleGate.SyncCheckmark(showStrategyToggle);
            blackjackGame?.PlayCloseSound();
            _settingsDirty = true;
            PersistSettingsToFile();
        }

        /// <summary>Plays the toggle click sound whenever any option checkbox changes value.</summary>
        private void OnToggleSoundPlay(bool _) => uiSounds?.toggleSound.Play(audioSource);

        private void OnDestroy()
        {
            Application.quitting -= HandleApplicationQuitting;

            if (blackjackGame != null)
                blackjackGame.OnAlwaysLoseDisabled -= DisableAlwaysLose;
        }

        private void HandleApplicationQuitting() => SaveSettings();

        private void OnApplicationQuit() => SaveSettings();

        // ──────────────────────────────────────────────────────────────────────────
        // Toggle callbacks
        // ──────────────────────────────────────────────────────────────────────────

        private void OnAutoplayToggled(bool value)
        {
            _settings.autoplayEnabled = value;
            _settingsDirty = true;

            if (value)
            {
                if (!_menuVisible)
                    blackjackGame?.StartAutoplayFromMenu();
            }
            else
            {
                if (_settings.autoplayMaxSpeed)
                    SetAutoplayMaxSpeedToggleState(false);

                blackjackGame?.SetAutoplayEnabled(false);
            }
        }

        /// <summary>Enables or disables autoplay at maximum speed via Time.timeScale.</summary>
        private void OnAutoplayMaxSpeedToggled(bool value)
        {
            if (_suppressToggleCallbacks) return;
            _settings.autoplayMaxSpeed = value;
            blackjackGame?.SetAutoplayMaxSpeed(value);
            _settingsDirty = true;

            if (value)
                EnsureAutoplayEnabledForMaxSpeed();
        }

        /// <summary>Max-speed autoplay requires the main Autoplay option to be on.</summary>
        private void EnsureAutoplayEnabledForMaxSpeed()
        {
            if (_settings.autoplayEnabled)
                return;

            SetAutoplayToggleState(true);
            _settingsDirty = true;
        }

        private void SetAutoplayToggleState(bool isOn)
        {
            _settings.autoplayEnabled = isOn;

            if (autoplayToggle == null)
                return;

            _suppressToggleCallbacks = true;
            autoplayToggle.SetIsOnWithoutNotify(isOn);
            _suppressToggleCallbacks = false;
        }

        private void SetAutoplayMaxSpeedToggleState(bool isOn)
        {
            _settings.autoplayMaxSpeed = isOn;
            blackjackGame?.SetAutoplayMaxSpeed(isOn);

            if (autoplayMaxSpeedToggle == null)
                return;

            _suppressToggleCallbacks = true;
            autoplayMaxSpeedToggle.SetIsOnWithoutNotify(isOn);
            _suppressToggleCallbacks = false;
        }

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
            if (IsMartingaleMenuLocked) return;

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

            if (_menuVisible)
                RefreshMartingaleMenuLock();

            _settingsDirty = true;
        }

        public void OnMartingaleAutoPlayToggled(bool value)
        {
            if (_suppressToggleCallbacks) return;
            if (IsMartingaleMenuLocked) return;

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
            _settingsDirty = true;
            ApplyMartingaleThresholdState();
        }

        /// <summary>
        /// Applies threshold state when the slider moves but the UnityEvent did not fire
        /// (e.g. some inspector setups or drag end without callback).
        /// </summary>
        private void SyncMartingaleThresholdIfSliderChanged()
        {
            if (IsMartingaleMenuLocked) return;
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
            if (IsMartingaleMenuLocked) return;

            _settings.martingaleThreshold = Mathf.RoundToInt(value);
            _lastMartingaleThresholdFromSlider = _settings.martingaleThreshold;
            ApplyMartingaleThresholdState();
            _settingsDirty = true;
        }

        /// <summary>Unchecks both Martingale toggles when the threshold is 0; both stay clickable.</summary>
        private void ApplyMartingaleThresholdState()
        {
            if (IsMartingaleMenuLocked)
            {
                RefreshMartingaleMenuLock();
                return;
            }

            int threshold = ReadMartingaleThresholdFromSlider();
            _settings.martingaleThreshold = threshold;
            _lastMartingaleThresholdFromSlider = threshold;

            var activeToggle = GetMartingaleActiveRowToggle();
            var autoPlayToggle = GetMartingaleAutoPlayRowToggle();
            EnsureShowStrategyToggleUnlocked();

            bool togglesInteractable = true;

            if (threshold <= 0)
            {
                _settings.martingaleActive   = false;
                _settings.martingaleAutoPlay = false;
                ApplyMartingaleToggleVisual(activeToggle, isOn: false, interactable: togglesInteractable);
                ApplyMartingaleToggleVisual(autoPlayToggle, isOn: false, interactable: togglesInteractable);
                EnsureShowStrategyToggleUnlocked();
                blackjackGame?.CancelMartingale();
                return;
            }

            _settings.martingaleActive = true;
            ApplyMartingaleToggleVisual(activeToggle, isOn: true, interactable: togglesInteractable);
            ApplyMartingaleToggleVisual(
                autoPlayToggle,
                isOn: _settings.martingaleAutoPlay,
                interactable: togglesInteractable);
            EnsureShowStrategyToggleUnlocked();
        }

        /// <summary>Persists the selected test-split rank (2–14, matching the Rank enum).</summary>
        public void OnTestSplitRankChanged(float value)
        {
            if (_settings == null) return;

            _settings.testSplitRank = Mathf.RoundToInt(value);
            _settingsDirty = true;
        }

        /// <summary>Applies the menu current-bet input to settings and the bet area.</summary>
        public void OnCurrentBetInputChanged(string text)
        {
            if (_settings == null || _suppressCurrentBetInputCallbacks) return;
            if (IsMartingaleMenuLocked) return;

            ApplyCurrentBetFromInputField(text);
            _settingsDirty = true;
        }

        private bool IsMartingaleMenuLocked =>
            blackjackGame != null && blackjackGame.IsInMartingaleMode;

        /// <summary>Locks Martingale-related menu controls while an active run is in progress.</summary>
        private void RefreshMartingaleMenuLock()
        {
            bool locked = IsMartingaleMenuLocked;
            _lastMartingaleMenuLocked = locked;

            InputField input = GetCurrentBetInputField();
            if (input != null)
            {
                input.interactable = !locked;

                if (input.textComponent != null)
                    input.textComponent.color = locked ? CurrentBetInputLockedTextColor : CurrentBetInputTextColor;

                if (input.targetGraphic is Image background)
                    background.color = locked ? CurrentBetInputLockedBackgroundColor : CurrentBetInputBackgroundColor;

                if (locked)
                    ReleaseCurrentBetInputFocus();
            }

            if (martingaleThresholdSlider != null)
                martingaleThresholdSlider.interactable = !locked;

            if (locked)
            {
                ApplyMartingaleToggleVisual(
                    GetMartingaleActiveRowToggle(),
                    _settings.martingaleActive,
                    interactable: false);
                ApplyMartingaleToggleVisual(
                    GetMartingaleAutoPlayRowToggle(),
                    _settings.martingaleAutoPlay,
                    interactable: false);
            }
            else
            {
                ApplyMartingaleThresholdState();
            }
        }

        private void ApplyCurrentBetFromInputField(string text = null, bool skipRefresh = false)
        {
            if (_settings == null) return;
            if (IsMartingaleMenuLocked) return;

            var input = GetCurrentBetInputField();
            if (input == null && text == null) return;

            string raw = text ?? input.text;
            if (!TryParseCurrentBetGerman(raw, out int bet))
                bet = ClampCurrentBet(_settings.initialBet > 0 ? _settings.initialBet : 1);

            bet = ClampCurrentBet(bet);
            SavedInitialBet = bet;

            if (blackjackGame != null)
            {
                blackjackGame.InitialBet = bet;
                blackjackGame.ApplySavedInitialBetToBetArea();
            }

            if (!skipRefresh)
            {
                RefreshCurrentBetInputDisplay();
                ReleaseCurrentBetInputFocus();
            }
        }

        /// <summary>Clears edit focus so the next click or Tab fully re-highlights the field.</summary>
        private void ReleaseCurrentBetInputFocus()
        {
            InputField input = GetCurrentBetInputField();
            if (input == null) return;

            input.DeactivateInputField();

            if (EventSystem.current != null &&
                EventSystem.current.currentSelectedGameObject == input.gameObject)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
        }

        private void RefreshCurrentBetInputDisplay()
        {
            var input = GetCurrentBetInputField();
            if (input == null || _settings == null) return;

            int bet = ClampCurrentBet(_settings.initialBet > 0 ? _settings.initialBet : 1);
            _suppressCurrentBetInputCallbacks = true;
            input.SetTextWithoutNotify(FormatCurrentBetGerman(bet));
            _suppressCurrentBetInputCallbacks = false;
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

        /// <summary>When true, the Autoplay test button is shown and the game should auto-play on load.</summary>
        public bool IsAutoplayMenuEnabled => _settings.autoplayEnabled;

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

        /// <summary>Gets or sets the lifetime hands-dealt counter. Persisted through the settings file.</summary>
        public int HandsDealt
        {
            get => _settings.handsDealt;
            set => _settings.handsDealt = value;
        }

        /// <summary>Gets or sets the player's chosen base stake persisted in the settings file.</summary>
        public int SavedInitialBet
        {
            get => _settings.initialBet;
            set => _settings.initialBet = ClampCurrentBet(value);
        }

        /// <summary>Shows or hides the menu panel. Opens in any game state; coroutine-driven gameplay pauses via WaitUntil checks.</summary>
        private void ToggleMenu()
        {
            if (menuPanel == null) return;

            if (_menuVisible)
                CloseMenuInternal(playSound: true);
            else
                OpenMenuInternal(playSound: false);
        }

        private void OpenMenuInternal(bool playSound)
        {
            _settingsDirty = false;
            SetMenuVisible(true);
            SyncMartingaleThresholdFromSlider();
            ApplyInitialBetFromSettings();
            RefreshCurrentBetInputDisplay();
            RefreshMartingaleMenuLock();
            if (playSound)
                uiSounds?.toggleSound.Play(audioSource);

            if (!IsMartingaleMenuLocked)
                StartCoroutine(FocusCurrentBetInputNextFrame());
        }

        private IEnumerator FocusCurrentBetInputNextFrame()
        {
            yield return null;
            if (IsMartingaleMenuLocked) yield break;
            FocusCurrentBetInputField();
        }

        /// <summary>Closes the menu panel if it is currently open.</summary>
        /// <param name="playSound">When false, suppresses the close sound. Defaults to true.</param>
        public void CloseMenu(bool playSound = true) => CloseMenuInternal(playSound);

        private void CloseMenuInternal(bool playSound)
        {
            if (!_menuVisible) return;

            SaveSettings();
            _settingsDirty = false;

            SetMenuVisible(false);
            if (playSound) blackjackGame?.PlayCloseSound();

            ApplyAutoplaySettingsFromMenu(startIfEnabled: true);
        }

        /// <summary>Restores the saved Current Bet from settings into the game and bet area when allowed.</summary>
        private void ApplyInitialBetFromSettings()
        {
            if (blackjackGame == null || _settings.initialBet <= 0) return;

            blackjackGame.InitialBet = _settings.initialBet;

            if (!blackjackGame.IsInMartingaleMode)
                blackjackGame.ApplySavedInitialBetToBetArea();
        }

        /// <summary>Reads autoplay checkbox state from the UI so menu close matches what the player selected.</summary>
        private void SyncAutoplaySettingsFromToggles()
        {
            if (autoplayToggle != null)
                _settings.autoplayEnabled = autoplayToggle.isOn;

            if (autoplayMaxSpeedToggle != null)
                _settings.autoplayMaxSpeed = autoplayMaxSpeedToggle.isOn;

            if (_settings.autoplayMaxSpeed)
                _settings.autoplayEnabled = true;
        }

        /// <summary>
        /// Pushes autoplay checkbox state into <see cref="BlackjackGame"/>.
        /// When <paramref name="startIfEnabled"/> is true and Autoplay is checked, deals the next round.
        /// </summary>
        private void ApplyAutoplaySettingsFromMenu(bool startIfEnabled)
        {
            if (blackjackGame == null) return;

            blackjackGame.SetAutoplayMaxSpeed(_settings.autoplayMaxSpeed);

            if (!_settings.autoplayEnabled)
            {
                blackjackGame.SetAutoplayEnabled(false);
                return;
            }

            if (!startIfEnabled)
            {
                blackjackGame.SetAutoplayEnabled(true);
                return;
            }

            blackjackGame.SetAutoplayEnabled(false);
            blackjackGame.StartAutoplayFromMenu();
        }

        /// <summary>Immediately flushes the current settings to disk. Called explicitly before the application quits.</summary>
        public void SaveSettings()
        {
            SyncAllSettingsFromUI();
            PersistSettingsToFile();
        }

        /// <summary>Copies all menu UI values into <see cref="OptionsSettings"/> before persisting.</summary>
        private void SyncAllSettingsFromUI()
        {
            if (_settings == null) return;

            if (_menuVisible)
                ApplyCurrentBetFromInputField(skipRefresh: true);

            SyncMartingaleThresholdFromSlider();
            SyncAutoplaySettingsFromToggles();

            if (blackjackTestToggle != null)
                _settings.blackjackTestEnabled = blackjackTestToggle.isOn;
            if (bjAllToggle != null)
                _settings.bjAllEnabled = bjAllToggle.isOn;
            if (ddTestToggle != null)
                _settings.ddTestEnabled = ddTestToggle.isOn;
            if (testSplitToggle != null)
                _settings.testSplitEnabled = testSplitToggle.isOn;
            if (dealerBjTestToggle != null)
                _settings.dealerBjTestEnabled = dealerBjTestToggle.isOn;
            if (overrideStrategyToggle != null)
                _settings.overrideStrategyEnabled = overrideStrategyToggle.isOn;
            if (alwaysLoseToggle != null)
                _settings.alwaysLoseEnabled = alwaysLoseToggle.isOn;
            if (showStrategyToggle != null)
                _settings.showStrategyEnabled = showStrategyToggle.isOn;

            Toggle martingaleActiveToggle = GetMartingaleActiveRowToggle();
            if (martingaleActiveToggle != null)
                _settings.martingaleActive = martingaleActiveToggle.isOn;

            Toggle martingaleAutoPlayToggle = GetMartingaleAutoPlayRowToggle();
            if (martingaleAutoPlayToggle != null)
                _settings.martingaleAutoPlay = martingaleAutoPlayToggle.isOn;

            if (volumeSlider != null)
                _settings.volume = volumeSlider.value;

            Slider splitRankSlider = GetTestSplitRankSlider();
            if (splitRankSlider != null)
                _settings.testSplitRank = Mathf.RoundToInt(splitRankSlider.value);
        }

        /// <summary>Writes the current settings to disk via <see cref="SettingsRepository"/>.</summary>
        private void PersistSettingsToFile()
        {
            try
            {
                SettingsRepository.Save(_settings);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[MenuController] Failed to save settings: {e.Message}");
            }
        }

        /// <summary>
        /// Loads settings from <see cref="SettingsRepository"/>, or migrates legacy
        /// <c>settings.json</c> from <see cref="Application.persistentDataPath"/>.
        /// Inspector defaults remain for any field absent from the file.
        /// </summary>
        private void LoadSettingsFromFile()
        {
            if (SettingsRepository.Exists())
            {
                try
                {
                    OptionsSettings loaded = SettingsRepository.Load();
                    JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(loaded), _settings);
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[MenuController] Failed to load settings: {e.Message}");
                }

                return;
            }

            string legacyPath = Path.Combine(Application.persistentDataPath, LegacySettingsFileName);
            if (!File.Exists(legacyPath)) return;

            try
            {
                JsonUtility.FromJsonOverwrite(File.ReadAllText(legacyPath), _settings);
                PersistSettingsToFile();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[MenuController] Failed to load legacy settings: {e.Message}");
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

            if (autoplayMaxSpeedToggle != null)
                autoplayMaxSpeedToggle.SetIsOnWithoutNotify(_settings.autoplayMaxSpeed);

            if (_settings.autoplayMaxSpeed)
                _settings.autoplayEnabled = true;

            if (autoplayToggle != null)
                autoplayToggle.SetIsOnWithoutNotify(_settings.autoplayEnabled);

            blackjackGame?.SetAutoplayMaxSpeed(_settings.autoplayMaxSpeed);

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
            {
                showStrategyToggle.SetIsOnWithoutNotify(_settings.showStrategyEnabled);
                MartingaleThresholdToggleGate.SyncCheckmark(showStrategyToggle);
            }
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

            if (_settings.initialBet > 0)
                _settings.initialBet = ClampCurrentBet(_settings.initialBet);

            if (blackjackGame != null && _settings.initialBet > 0)
            {
                blackjackGame.InitialBet = _settings.initialBet;
                blackjackGame.ApplySavedInitialBetToBetArea();
            }

            RefreshCurrentBetInputDisplay();
        }

        // Converts a linear [0,1] slider value to decibels for the AudioMixer.
        private static float LinearToDb(float linear) =>
            linear > 0.0001f ? Mathf.Log10(linear) * 20f : -80f;

        // Converts decibels from the AudioMixer back to a linear [0,1] slider value.
        private static float DbToLinear(float dB) =>
            Mathf.Pow(10f, dB / 20f);
    }
}
