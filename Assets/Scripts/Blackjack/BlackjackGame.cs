//CodeRed Soft 2026-05-29
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.UI;
using TMPro;

namespace Blackjack
{
    /// <summary>
    /// Central game controller. Manages game state, deal flow, dealer AI, and UI updates.
    /// </summary>
    public class BlackjackGame : MonoBehaviour
    {
        // ──────────────────────────────────────────────────────────────────────────
        // Inspector References
        // ──────────────────────────────────────────────────────────────────────────

        [Header("Registry")]
        [SerializeField] private CardSpriteRegistry spriteRegistry;

        [Header("Layout - Player")]
        [SerializeField] private Transform playerCardArea;
        [SerializeField] private Transform splitCardArea;

        [Header("Layout - Dealer")]
        [SerializeField] private Transform dealerCardArea;

        [Header("Card Prefab")]
        [SerializeField] private GameObject cardViewPrefab;
        [SerializeField] private bool useWorldSpaceCards = true;
        [SerializeField] private GameObject worldCardPrefab;

        [Header("Buttons")]
        [Tooltip("Parent row for Deal / Hit / Stand / etc. Hidden during autoplay at max speed.")]
        [SerializeField] private GameObject buttonRow;
        [SerializeField] private Button dealButton;
        [SerializeField] private Button hitButton;
        [SerializeField] private Button standButton;
        [SerializeField] private Button surrenderButton;
        [SerializeField] private Button splitButton;
        [SerializeField] private Button doubleDownButton;
        [SerializeField] private Button ddTestButton;

        [Header("Button Sprites")]
        [SerializeField] private Sprite splitAvailableSprite;

        [Header("Menu")]
        [SerializeField] private MenuController menuController;

        [Header("Strategy Table")]
        [SerializeField] private Blackjack.UI.StrategyTableUI strategyTableUI;
        [SerializeField] private bool showStrategyTable = true;

        [Header("Score Labels")]
        [SerializeField] private TextMeshProUGUI playerScoreLabel;
        [SerializeField] private TextMeshProUGUI dealerScoreLabel;

        [Header("Status")]
        [SerializeField] private TextMeshProUGUI statusLabel;
        [SerializeField] private TextMeshProUGUI streakLabel;
        [SerializeField] private TextMeshProUGUI martingaleModeLabel;
        [SerializeField] private TextMeshProUGUI handsDealtLabel;
        [Tooltip("Fixed distance below the gap center for the status text bottom (about half the font size).")]
        [SerializeField] private float statusLabelBottomBelowGapCenter = 20f;
        [Tooltip("Extra vertical nudge in canvas pixels (negative = down).")]
        [SerializeField] private float statusLabelVerticalOffset;

        [Header("Player Info Labels")]
        [Tooltip("Equal vertical gap in pixels between name, money, streak, and Martingale mode labels.")]
        [SerializeField] private float playerInfoLabelGap = 8f;
        [Tooltip("Anchored Y of the top player name label; the other three labels stack below it.")]
        [SerializeField] private float playerInfoTopY = 303f;
        [Tooltip("Font size for the Martingale mode label (uses the status label font at this size).")]
        [SerializeField] private float martingaleModeFontSize = 22f;

        [Header("Money")]
        [SerializeField] private TextMeshProUGUI playerMoneyLabel;
        [SerializeField] private ChipBetting chipBetting;
        [SerializeField] private int startingMoney = 0;

        [Header("Basic Strategy")]
        [SerializeField] private StrategyDeviationPopup deviationPopup;

        [Header("Martingale")]
        [SerializeField] private MartingalePopup martingalePopup;

    [Header("Fireworks")]
    [SerializeField] private GameObject fireworksPrefab;
    [SerializeField] private float fireworksDuration = 4f;


    [Header("Audio")] //mark audio
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private SoundEntry ahdamnitSound;
    [SerializeField] private SoundEntry areyouseriousSound;
    [SerializeField] private SoundEntry bullshitSound;
    [SerializeField] private SoundEntry cardSlideSound;
    [SerializeField] private SoundEntry cantbelievethatSndSound;
    [SerializeField] private SoundEntry carefullSound;
    [SerializeField] private SoundEntry cheaterSound;
    [SerializeField] private SoundEntry chipSound;
    [SerializeField] private SoundEntry comeonSound;
    [SerializeField] private SoundEntry damnitSound;
    [SerializeField] private SoundEntry dealCardSound;
    [SerializeField] private SoundEntry ddSound;
    [SerializeField] private SoundEntry doitagainSound;
    [SerializeField] private SoundEntry donttouchmeSound;
    [SerializeField] private SoundEntry exitSound;
    [SerializeField] private SoundEntry fuckSound;
    [SerializeField] private SoundEntry helltopaySound;
    [SerializeField] private SoundEntry hmhSound;
    [SerializeField] private SoundEntry isthatyourbasicstrategySound;
    [SerializeField] private SoundEntry jesusSound;
    [SerializeField] private SoundEntry knockSound;
    [SerializeField] private SoundEntry loseSound;
    [SerializeField] private SoundEntry naturalBlackjackSound;
    [SerializeField] private SoundEntry nowaySound;
    [SerializeField] private SoundEntry resetSound;
    [SerializeField] private SoundEntry seriouslySound;
    [SerializeField] private SoundEntry startupSound;
    [SerializeField] private SoundEntry surrenderSound;
    [SerializeField] private SoundEntry thegameisriggedSound;
    [SerializeField] private SoundEntry thissucksSound;
    [SerializeField] private SoundEntry tieSound;
    [SerializeField] private SoundEntry touchmeandiwill16Sound;
    [SerializeField] private SoundEntry trythatagainSound;
    [SerializeField] private SoundEntry unbelievableSound;
    [SerializeField] private SoundEntry whatthefuckSound;
    [SerializeField] private SoundEntry youcannotaffordmeSound;
    [SerializeField] private SoundEntry youowemeSound;
    [SerializeField] private SoundEntry yourcarisparkedSound;
    [SerializeField] private SoundEntry yourecheatingSound;
    [SerializeField] private SoundEntry yourekiddingmeSound;
    [SerializeField] private SoundEntry youwillfryinhellSound;
    [SerializeField] private SoundEntry youwillpayforthatSound;
    [SerializeField] private SoundEntry winSound;
    [SerializeField] private SoundEntry yuhuSound;

    private SoundEntry? _lastDoubleBJSound;
    private SoundEntry[] _maleSpeechPool;
    private int _lastDealerNaturalBJReactionIndex = -1;
    private bool _doubleBJSoundPlaying;
    private bool _dealerNaturalBJPlaying;
    private float _fireworksEndTime;
    private GameObject _fireworksInstance;
        private float _winSoundEndTime;
        private bool _winPresentationComplete;
        private bool _deferredWinPayoutPending;
        private PayoutResult _deferredWinPayoutResult;
        private int _deferredWinPayoutBet;

    private float _dealerNaturalBJEndTime;

        [Header("Timing")]

        [Tooltip("dealDelay ist set to 0.45 in code, you can not change it in the inspector!")]
        [SerializeField] private float dealDelay          = 0.45f; //default is 0.45
        [SerializeField] private float dealerPauseDelay   = 0.7f;
        [Tooltip("Seconds to wait after doubling the bet before dealing the third card.")]
        [SerializeField] private float doubleDownBetPause = 0.6f;
        [SerializeField] private float endRoundDelay      = 3.0f;
        [SerializeField] private float newRoundPause      = 0.5f;

        [Header("Controls")]
        [SerializeField] private KeyboardControls controls;

        // ──────────────────────────────────────────────────────────────────────────
        // Constants mark auto
        // ──────────────────────────────────────────────────────────────────────────

        private const int AutoStandHard      = 17;
        private const int AutoStandSoft      = 19;
        private const int AutoHitMaxScore    = 0; //disable
        private const int DealerSoft17       = 17;
        private const int BlackjackValue     = 21;
        public  const int BetLimit           = 1000;

        private static readonly Color WinStatusColor = new Color(1f, 0f, 0f, 1f);        //gold
        private static readonly Color WinColor = new Color(0f, 1f, 0f, 1f);              //green
        private static readonly Color LoseColor = new Color(1f, 0f, 0f, 1f);             //red
        private static readonly Color PushColor = new Color(0.7f, 0f, 0.7f, 1f);        //magenta
        private static readonly Color SurrenderColor = new Color(0f, 0.6666667f, 1f, 1f);
    // ──────────────────────────────────────────────────────────────────────────
    // State
    // ──────────────────────────────────────────────────────────────────────────

      private readonly Deck _deck          = new();
        private readonly Hand _playerHand    = new();
        private readonly Hand _dealerHand    = new();
        private readonly Hand _splitHand     = new();

        private readonly List<ICardDisplay> _playerCardViews = new();
        private readonly List<ICardDisplay> _splitCardViews  = new();
        private readonly List<ICardDisplay> _dealerCardViews = new();

        private ICardDisplay _dealerHoleCardView;

        private bool _forcePlayerBlackjack;
        private bool _forceBothBlackjack;
        private bool _forceSplitHand;
        private bool _forceDoubleDownTest;
        private bool _forceDealerBlackjack;
        private bool _isSplitRound;
        private int  _activeHandIndex; // 0 = player, 1 = split

        private int _doubleDownExtraBet; // extra bet deducted when doubling down
        // Tracks which split hand (index 0 / 1) was doubled down; used to compute per-hand loss count.
        private readonly bool[] _splitHandDoubledDown = new bool[2];
        private int _savedBetBeforeAction; // bet amount before split/double-down, restored next round
        private int _betBeforeMartingale;  // bet placed before Martingale doubling; restored on a Martingale win
        private int _initialBet;           // player's chosen base stake (1–1000); restored after a normal win
        private bool _martingaleDoubledLastPrepare; // skip initial-bet capture after an auto Martingale double

        // Snapshot of the dealer's visible upcard taken at the start of the player's turn.
        private CardData _dealerUpcardSnapshot;

        private readonly BasicStrategyAdvisor _strategyAdvisor = new();

        private decimal _playerMoney; //decimal need for 5 chips / 2 surrendering = 2.5 chips

        // Delayed Martingale detection
        private decimal _consecutiveLosses;
        private int _totalLosses;
        private decimal _totalAmountLost;
        private int _lastRoundBet;
        private bool _martingaleWin;
        private bool _martingaleBetRestored;
        private bool _doubleDownBetRestored;
        private bool _standardBetRestored;
        private bool _playerWon;
        private bool _martingalePopupShown;
        // True once the player has accepted the Martingale popup. Cleared only on a win. Suppresses the popup while active.
        private bool _inMartingaleMode;
        // True when the player is in active Martingale mode and lost the last round — bet should be doubled on next betting screen.
        private bool _pendingMartingaleDouble;
        // True after the player explicitly declines the Martingale popup. Suppresses re-prompting on Deal until the streak resets.
        private bool _martingaleDeclined;

        // True while auto-play is active (dealer rules + auto-deal next round).
        private bool _autoPlayEnabled;

        private static readonly Color MartingaleModeGoldColor = new Color(1f, 0.85f, 0.3f, 1f);

        // Running score: +1 per win, -1 per loss, 0 for push or surrender.
        private int _playerScore;

        // Lifetime hands dealt — loaded from settings on Start, saved back on every deal.
        private int _handsDealt;

        // Fallback used only when MenuController is not available. The authoritative value lives on MenuController.defaultMartingaleThreshold.
        private const int DelayedMartingaleThreshold = 1;

        // Returns the effective Martingale trigger threshold from the menu slider, falling back to the hardcoded constant.
        private int EffectiveMartingaleThreshold =>
            menuController != null
                ? menuController.MartingaleThreshold
                : DelayedMartingaleThreshold;

    private TextMeshProUGUI _splitScoreLabel;
        private Vector2 _defaultPlayerScorePosition;
        private ScoreLabelPulse _playerScorePulse;
        private ScoreLabelPulse _splitScorePulse;
        private Color _defaultStatusColor;

        private const float StatusLabelHeight = 50f;

        private const float DefaultCardWidth  = 120f;
        private const float DefaultCardHeight = 168f;

    private Hand           ActiveHand  => _activeHandIndex == 0 ? _playerHand  : _splitHand;
        private List<ICardDisplay> ActiveViews => _activeHandIndex == 0 ? _playerCardViews : _splitCardViews;

        private enum GameState { Idle, PlayerTurn, DealerTurn, RoundOver }
        private GameState _state = GameState.Idle;

        /// <summary>True when the player is allowed to place or remove bets (before a round begins).</summary>
        public bool IsBettingAllowed => _state == GameState.Idle;

        /// <summary>True when the current round has ended and the table is showing results.</summary>
        public bool IsRoundOver => _state == GameState.RoundOver;

        /// <summary>True while auto-play mode is active.</summary>
        public bool IsAutoPlayEnabled => _autoPlayEnabled;

        /// <summary>True while the developer menu is open. Used by <see cref="ChipBetting"/> to suppress chip input.</summary>
        public bool IsMenuOpen => menuController != null && menuController.IsMenuOpen;

        /// <summary>True while an active Martingale run is in progress (after popup confirm until a win).</summary>
        public bool IsInMartingaleMode => _inMartingaleMode;

        /// <summary>Closes the menu panel. Used by <see cref="ChipBetting"/> when a bet action is taken during the betting phase.</summary>
        /// <param name="playSound">When false, suppresses the close sound. Defaults to true.</param>
        public void CloseMenu(bool playSound = true) => menuController?.CloseMenu(playSound);

        /// <summary>
        /// Syncs the menu Current Bet from chip-tray changes during the betting phase.
        /// Skipped while Martingale has raised the table stake above the saved base bet.
        /// </summary>
        public void CapturePlayerInitialBet(int totalBet)
        {
            if (_martingaleDoubledLastPrepare)
                _martingaleDoubledLastPrepare = false;

            if (!IsBettingAllowed || totalBet <= 0)
                return;

            if (_inMartingaleMode)
            {
                int baseBet = menuController != null && menuController.SavedInitialBet > 0
                    ? menuController.SavedInitialBet
                    : (_initialBet > 0 ? _initialBet : chipBetting?.SmallestChipValue ?? 1);

                if (totalBet > baseBet)
                    return;
            }

            menuController?.UpdateSavedInitialBetFromGameplay(totalBet);
            _initialBet = Mathf.Clamp(totalBet, chipBetting != null ? chipBetting.SmallestChipValue : 1, BetLimit);
        }

        /// <summary>
        /// Player's chosen base stake (1–<see cref="BetLimit"/>), from the menu or chip tray.
        /// </summary>
        public int InitialBet
        {
            get => ResolveTargetInitialBet();
            set => _initialBet = Mathf.Clamp(value, chipBetting != null ? chipBetting.SmallestChipValue : 1, BetLimit);
        }

        /// <summary>
        /// Rebuilds the chip tray from the menu Current Bet after the area was cleared.
        /// Falls back to the smallest chip when no saved stake exists.
        /// </summary>
        private void RestoreInitialBetToBetAreaAfterClear()
        {
            if (chipBetting == null) return;

            int savedInitialBet = menuController != null ? menuController.SavedInitialBet : 0;
            if (savedInitialBet > 0)
            {
                InitialBet = savedInitialBet;
                ApplySavedInitialBetToBetArea();
            }
            else
            {
                chipBetting.ResetToMinimumBet();
                _initialBet = chipBetting.SmallestChipValue;
            }
        }

        /// <summary>
        /// Rebuilds the bet area to <see cref="InitialBet"/> when betting is allowed.
        /// Used when restoring a saved stake from the menu settings file.
        /// </summary>
        public void ApplySavedInitialBetToBetArea()
        {
            if (chipBetting == null) return;
            if (_state != GameState.Idle && _state != GameState.RoundOver) return;

            SyncBetAreaToInitialBetIfNeeded(force: true);
        }

        /// <summary>
        /// Returns the menu Current Bet when available; otherwise the cached or minimum stake.
        /// </summary>
        private int ResolveTargetInitialBet()
        {
            if (menuController != null && menuController.SavedInitialBet > 0)
                return menuController.SavedInitialBet;

            if (_initialBet > 0)
                return _initialBet;

            return chipBetting != null ? chipBetting.SmallestChipValue : 1;
        }

        /// <summary>
        /// Aligns the chip area with the chosen initial stake before a new round is dealt.
        /// Skipped after a Martingale auto-double or while an escalated Martingale bet is active.
        /// </summary>
        /// <param name="force">When true, sync even if Martingale mode has raised the table stake.</param>
        private void SyncBetAreaToInitialBetIfNeeded(bool force = false)
        {
            if (chipBetting == null) return;
            if (!force)
            {
                if (_martingaleDoubledLastPrepare) return;

                int menuBet = ResolveTargetInitialBet();
                if (_inMartingaleMode && CurrentBet > menuBet) return;
            }

            int targetBet = ResolveTargetInitialBet();
            if (targetBet <= 0 || CurrentBet == targetBet) return;

            chipBetting.SetBet(targetBet, playSound: false);
        }

        /// <summary>Saves all menu settings to disk immediately. Called before the application quits.</summary>
        public void SaveMenuSettings() => menuController?.SaveSettings();

        /// <summary>Inspector-configured default for whether the strategy table starts visible.</summary>
        public bool ShowStrategyTable => showStrategyTable;

        /// <summary>When true, the player always loses the round regardless of card values. Used for Martingale testing.</summary>
        public bool AlwaysLose { get; set; }

        /// <summary>
        /// Called when the player manually enables the "Martingale is Active" checkbox.
        /// Clears any previous decline so the popup can fire again.
        /// If the loss threshold is already met, shows the popup immediately regardless of game state.
        /// Does NOT double the bet or start a round — the popup handles that.
        /// </summary>
        public void TryStartMartingaleFromToggle()
        {
            // Clear a previous decline — the player is explicitly re-enabling Martingale.
            _martingaleDeclined = false;

            bool thresholdMet = EffectiveMartingaleThreshold > 0 && _consecutiveLosses >= EffectiveMartingaleThreshold;

            Debug.Log($"[Martingale] TryStartMartingaleFromToggle: losses={_consecutiveLosses} threshold={EffectiveMartingaleThreshold} thresholdMet={thresholdMet} inMode={_inMartingaleMode}");

            if (thresholdMet && !_inMartingaleMode)
                ShowMartingalePopup();

            RefreshStreakLabel();
        }

        /// <summary>
        /// Arms Martingale after the player confirms the popup, then starts the next round
        /// (bet is doubled in <see cref="PrepareForBetting"/>).
        /// </summary>
        private void EnterMartingaleFromPopupConfirm()
        {
            if (AlwaysLose)
            {
                AlwaysLose = false;
                OnAlwaysLoseDisabled?.Invoke();
            }

            if (chipBetting != null && _betBeforeMartingale <= 0)
                _betBeforeMartingale = chipBetting.TotalBet;

            _inMartingaleMode        = true;
            _pendingMartingaleDouble = true;
            menuController?.DisableOverrideStrategy();
            menuController?.ActivateMartingale();
            RefreshStreakLabel();
            OnDeal();

            // Play chip sound after OnDeal so that StopBlackjackCelebration's audioSource.Stop()
            // — called inside OnDeal — does not cancel it.
            PlayGameSound(chipSound);
        }

        private void ShowMartingalePopup()
        {
            Debug.Log($"[Martingale] ShowMartingalePopup: autoPlay={menuController?.IsMartingaleAutoPlay} popupNull={martingalePopup == null}");

            // Auto-play / Martingale-auto: skip popup, enter Martingale mode and deal immediately.
            if (ShouldAutoConfirmMartingalePopup())
            {
                _inMartingaleMode        = true;
                _betBeforeMartingale     = chipBetting != null ? chipBetting.TotalBet : 0;
                _pendingMartingaleDouble = true;
                menuController.DisableOverrideStrategy();
                menuController.ActivateMartingale();
                RefreshStreakLabel();
                OnDeal();
                return;
            }

            // Manual: show the popup and wait for the player's choice.
            if (martingalePopup != null)
            {
                martingalePopup.Show(
                    "Play Martingale ?",
                    onDoIt: EnterMartingaleFromPopupConfirm,
                    onReconsider: OnMartingaleDeclined);
            }
        }

        /// <summary>
        /// Decline on the Martingale popup: skip Martingale and deal the next round.
        /// </summary>
        private void OnMartingaleDeclined()
        {
            _inMartingaleMode        = false;
            _pendingMartingaleDouble = false;
            _martingaleDeclined      = true;
            menuController?.DeactivateMartingale();
            RefreshStreakLabel();
            StartCoroutine(DealAfterMartingaleDeclined());
        }

        private IEnumerator DealAfterMartingaleDeclined()
        {
            yield return null;

            if (_state != GameState.Idle && _state != GameState.RoundOver)
                _state = GameState.RoundOver;

            OnDeal();
        }

        /// <summary>
        /// Cancels Martingale mode immediately.
        /// Called when the player unchecks the "Martingale is Active" checkbox in the menu.
        /// Clears all Martingale flags so no doubling or popup will occur in subsequent rounds.
        /// The consecutive-loss streak counter is preserved so the player can see their history.
        /// </summary>
        public void CancelMartingale()
        {
            _inMartingaleMode        = false;
            _pendingMartingaleDouble = false;
            _martingalePopupShown    = false;
            _martingaleDeclined      = false;
            RefreshStreakLabel();
        }

        /// <summary>Fired when the game automatically disables "Always Lose" upon entering Martingale mode.</summary>
        public event System.Action OnAlwaysLoseDisabled;

        /// <summary>
        /// Resets the game to its initial state: sets the player's money to zero,
        /// clears all cards and the bet area, and returns to the Idle state.
        /// </summary>
        public void ResetGame()
        {
            ClearDeferredWinPayout();
            StopAllCoroutines();
            _doubleBJSoundPlaying = false;

            ClearBetLimitStatus();
            SetAutoplayEnabled(false);

            StopBlackjackCelebration();
            martingalePopup?.Hide();

            DestroyCardViews(_playerCardViews);
            DestroyCardViews(_splitCardViews);
            DestroyCardViews(_dealerCardViews);

            _playerHand.Clear();
            _splitHand.Clear();
            _dealerHand.Clear();
            _dealerHoleCardView   = null;
            _isSplitRound         = false;
            _activeHandIndex      = 0;
            _savedBetBeforeAction = 0;
            _betBeforeMartingale  = 0;
            _initialBet           = 0;

            chipBetting?.ClearBetArea();
            RestoreInitialBetToBetAreaAfterClear();

            _playerMoney = 0;
            RefreshMoneyLabel();

            _consecutiveLosses        = 0;
            _totalLosses              = 0;
            _totalAmountLost          = 0;
            _playerScore              = 0;
            _lastRoundBet             = 0;
            _martingaleWin            = false;
            _martingaleBetRestored    = false;
            _doubleDownBetRestored    = false;
            _standardBetRestored      = false;
            _playerWon                = false;
            _martingalePopupShown    = false;
            _inMartingaleMode        = false;
            _pendingMartingaleDouble = false;
            RefreshStreakLabel();

            _handsDealt = 0;
            if (menuController != null) menuController.HandsDealt = 0;
            RefreshHandsDealtLabel();

            // Restore the Override Strategy option in case Martingale mode was active.

            StopAllScorePulses();
            ResetPlayerScoreLabelPosition();
            SetScoreLabelsVisible(false);

            _state = GameState.Idle;

            SetButtonState(dealEnabled: true, actionEnabled: false, splitEnabled: false);
            SetStatus("Press Deal to start");

            if (resetSound.HasClip && audioSource != null)
                PlayGameSound(resetSound);
        }

        /// <summary>Returns true when the menu is open and a round is already in progress — input should be suppressed.</summary>
        private bool IsMenuBlocking => menuController != null && menuController.IsMenuOpen
            && (_state == GameState.PlayerTurn || _state == GameState.DealerTurn);

        /// <summary>
        /// Transitions from RoundOver back to Idle, clearing the table and prompting the player to bet.
        /// Called by ChipBetting when the player clicks a chip after a round ends.
        /// </summary>
        public void PrepareForBetting()
        {
            if (_state != GameState.RoundOver) return;

            DestroyCardViews(_playerCardViews);
            DestroyCardViews(_splitCardViews);
            DestroyCardViews(_dealerCardViews);

            _playerHand.Clear();
            _splitHand.Clear();
            _dealerHand.Clear();
            _dealerHoleCardView = null;
            _isSplitRound       = false;
            _activeHandIndex    = 0;

            // If the player lost while in Martingale mode, double the bet for the next round.
            bool martingaleBetLimitExceeded = false;
            if (_pendingMartingaleDouble && chipBetting != null)
            {
                if (_betBeforeMartingale <= 0)
                    _betBeforeMartingale = chipBetting.TotalBet;

                _pendingMartingaleDouble = false;
                _martingaleDoubledLastPrepare = true;
                martingaleBetLimitExceeded = !chipBetting.DoubleBetChips(
                    playSound: true, enforceMaxBet: true, notifyLimitExceeded: false);
            }

     
            StopAllScorePulses();
            ResetPlayerScoreLabelPosition();
            SetScoreLabelsVisible(false);
            if (martingaleBetLimitExceeded)
                SetStatus("Press Deal to start");
            else
                SetStatus("Place your bet");

            _state = GameState.Idle;
        }


        // ──────────────────────────────────────────────────────────────────────────

        private void Start()
        {
            Application.runInBackground = true;

            GameAudioShutdown.StopAll();

            // Ensure the game starts in a fully clean visual and logical state
            // regardless of how the previous session ended (crash, force-quit, etc.).
            StopBlackjackCelebration();
            martingalePopup?.Hide();

            DestroyCardViews(_playerCardViews);
            DestroyCardViews(_splitCardViews);
            DestroyCardViews(_dealerCardViews);

            _playerHand.Clear();
            _splitHand.Clear();
            _dealerHand.Clear();
            _dealerHoleCardView      = null;
            _isSplitRound            = false;
            _activeHandIndex         = 0;
            _savedBetBeforeAction    = 0;
            _betBeforeMartingale     = 0;
            _initialBet              = 0;
            _doubleDownExtraBet      = 0;
            _splitHandDoubledDown[0] = false;
            _splitHandDoubledDown[1] = false;

            chipBetting?.ClearBetArea();
            RestoreInitialBetToBetAreaAfterClear();

            _playerMoney = startingMoney;
            RefreshMoneyLabel();

            _consecutiveLosses       = 0;
            _totalLosses             = 0;
            _totalAmountLost         = 0;
            _playerScore             = 0;
            _lastRoundBet            = 0;
            _martingaleWin           = false;
            _martingaleBetRestored   = false;
            _doubleDownBetRestored   = false;
            _standardBetRestored     = false;
            _playerWon               = false;
            _martingalePopupShown    = false;
            _inMartingaleMode        = false;
            _pendingMartingaleDouble = false;
            _autoPlayEnabled         = false;

            _state = GameState.Idle;

            if (chipBetting != null)
                chipBetting.OnBetChanged += OnBetChangedHandler;

            _deck.Build();
            if (statusLabel != null)
                _defaultStatusColor = statusLabel.color;
            AlignStatusLabelToCardArea();
            InitSplitScoreLabel();
            InitMartingaleModeLabel();
            AlignPlayerInfoLabels();
            SetScoreLabelsVisible(false);
            RefreshStreakLabel();
            _handsDealt = menuController != null ? menuController.HandsDealt : 0;
            RefreshHandsDealtLabel();
            SetButtonState(dealEnabled: true, actionEnabled: false, splitEnabled: false);
            SetStatus("Press Deal to start");

            StartCoroutine(FinishSessionStart());

            _maleSpeechPool = new[]
            {
                ahdamnitSound, areyouseriousSound, bullshitSound, cantbelievethatSndSound,
                cheaterSound, comeonSound, doitagainSound, fuckSound, helltopaySound,
                jesusSound, nowaySound, seriouslySound, thegameisriggedSound, thissucksSound,
                trythatagainSound, unbelievableSound, whatthefuckSound, yourecheatingSound,
                yourekiddingmeSound, youwillfryinhellSound, youwillpayforthatSound
            };
        }

        /// <summary>Runs after all Start() methods so stray audio from a prior session cannot overlap startup sounds or autoplay.</summary>
        private IEnumerator FinishSessionStart()
        {
            yield return null;
            GameAudioShutdown.StopAll();

            if (startupSound.HasClip && audioSource != null)
                PlayGameSound(startupSound);

            if (menuController != null && menuController.IsAutoplayMenuEnabled)
                StartAutoplayFromMenu();
        }

        private void OnDestroy()
        {
            GameAudioShutdown.StopAll();
            Time.timeScale = 1f;
            if (chipBetting != null)
                chipBetting.OnBetChanged -= OnBetChangedHandler;
        }

        private void OnApplicationQuit()
        {
            GameAudioShutdown.StopAll();

            // Stop all running coroutines so nothing fires after shutdown begins.
            StopAllCoroutines();
            _doubleBJSoundPlaying = false;

            // Clear all hand and card state.
            DestroyCardViews(_playerCardViews);
            DestroyCardViews(_splitCardViews);
            DestroyCardViews(_dealerCardViews);

            _playerHand.Clear();
            _splitHand.Clear();
            _dealerHand.Clear();
            _dealerHoleCardView   = null;
            _isSplitRound         = false;
            _activeHandIndex      = 0;
            _savedBetBeforeAction = 0;
            _betBeforeMartingale  = 0;
            _initialBet           = 0;
            _doubleDownExtraBet        = 0;
            _splitHandDoubledDown[0]   = false;
            _splitHandDoubledDown[1]   = false;

            // Clear the bet area.
            chipBetting?.ClearBetArea();

            // Reset all counters and mode flags.
            _playerMoney             = 0;
            _consecutiveLosses       = 0;
            _totalLosses             = 0;
            _totalAmountLost         = 0;
            _playerScore             = 0;
            _lastRoundBet            = 0;
            _martingaleWin           = false;
            _martingaleBetRestored   = false;
            _doubleDownBetRestored   = false;
            _standardBetRestored     = false;
            _playerWon               = false;
            _martingalePopupShown    = false;
            _inMartingaleMode        = false;
            _pendingMartingaleDouble = false;

            _state = GameState.Idle;
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Public Audio API
        // ──────────────────────────────────────────────────────────────────────────

        /// <summary>Plays the exit sound and returns its length in seconds.</summary>
        public float PlayExitSound()
        {
            if (SkipAutoplayDelays)
                return exitSound.Length;

            exitSound.Play(audioSource);
            return exitSound.Length;
        }

        /// <summary>Plays the exit sound without returning a duration. Used by menu close actions.</summary>
        public void PlayCloseSound()
        {
            if (SkipAutoplayDelays) return;
            exitSound.Play(audioSource);
        }

        /// <summary>Plays the knock sound.</summary>
        public void PlayKnockSound()
        {
            PlayGameSound(knockSound);
        }

        /// <summary>
        /// Pulses <see cref="BetLimitStatusMessage"/> three times, then keeps it visible until the next round is dealt.
        /// </summary>
        public void NotifyBetLimitExceeded()
        {
            if (_betLimitStatusLocked || IsLimitPulsing) return;

            bool alreadyAtMax = chipBetting != null && chipBetting.TotalBet == chipBetting.MaxBet;

            if (alreadyAtMax)
            {
                PlayGameSound(knockSound);
                SetStatus("Press Deal to start");
                return;
            }

            IsLimitPulsing = true;
            chipBetting?.SetBet(chipBetting.MaxBet);
            PlayGameSound(resetSound);
            _limitPulseCoroutine = StartCoroutine(PulseLimitExceeded());
        }

        /// <summary>Clears the bet-limit status lock and pulse so the player can change chips again.</summary>
        public void ClearBetLimitStatus()
        {
            if (_limitPulseCoroutine != null)
            {
                StopCoroutine(_limitPulseCoroutine);
                _limitPulseCoroutine = null;
            }

            IsLimitPulsing        = false;
            _betLimitStatusLocked = false;

            if (_state == GameState.Idle || _state == GameState.RoundOver)
                SetStatus(CurrentBet > 0 ? "Press Deal to start" : "Place your bet");
        }

        private Coroutine _limitPulseCoroutine;

        private const int LimitPulseCount = 3;
        private const float LimitPulseDelay = 0.5f;
        private const string BetLimitStatusMessage = "Exceeding Limit, setting to Max";
        private bool _betLimitStatusLocked;

        /// <summary>True while the bet-limit pulse animation is running.</summary>
        public bool IsLimitPulsing { get; private set; }

        /// <summary>True while the bet-limit pulse is running or its message is locked until the next deal.</summary>
        public bool IsBetLimitStatusActive => IsLimitPulsing || _betLimitStatusLocked;

        private IEnumerator PulseLimitExceeded()
        {
            IsLimitPulsing = true;

            for (int i = 0; i < LimitPulseCount; i++)
            {
                SetStatus(BetLimitStatusMessage, LoseColor);
                yield return WaitForGameDelay(LimitPulseDelay);
                SetStatus(string.Empty, LoseColor);
                yield return WaitForGameDelay(LimitPulseDelay);
            }

            SetStatus("Press Deal to start");
            IsLimitPulsing        = false;
            _betLimitStatusLocked = true;
            _limitPulseCoroutine  = null;
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Input
        // ──────────────────────────────────────────────────────────────────────────

        private void Update()
        {
            if (controls != null && controls.DealOrHitPressed)
            {
                if (hitButton != null && hitButton.gameObject.activeSelf)
                    OnHit();
                else if (dealButton != null && dealButton.gameObject.activeSelf)
                    OnDeal();
                else if (_state == GameState.RoundOver)
                {
                    if (_playerWon && !_winPresentationComplete)
                        return;

                    OnDeal(); // Skip any ongoing end-of-round animation and deal the next round.
                }
            }
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Button Handlers
        // ──────────────────────────────────────────────────────────────────────────

        /// <summary>Starts a new round. Ensures a minimum bet of 1 chip and deducts the total bet from the player's balance.</summary>
        public void OnDeal()
        {
            if (IsLimitPulsing) return;
            if (_playerWon && !_winPresentationComplete) return;
            if (_state != GameState.Idle && _state != GameState.RoundOver) return;
            StopBlackjackCelebration();
            StartNewRound();
        }

        /// <summary>
        /// Ensures a minimum bet is placed, deducts it from the player's balance, and starts <see cref="DealRound"/>.
        /// All new-game entry points funnel through here.
        /// </summary>
        private void StartNewRound()
        {
            ApplyDeferredWinPayoutIfPending();

            _doubleBJSoundPlaying = false;
            StopAllCoroutines();
            StopDoubleDownLayout();
            IsLimitPulsing = false;
            _dealerNaturalBJPlaying = false;
            _dealerNaturalBJEndTime = 0f;

            strategyTableUI?.ResetToCanonical();

            // EndRound may have been interrupted before it could reset the bet after a win.
            ApplyWinBetAreaRestore();

            _martingaleWin         = false;
            _martingaleBetRestored = false;
            _doubleDownBetRestored = false;
            _standardBetRestored   = false;
            _playerWon             = false;
            dealButton.gameObject.SetActive(false);
            menuController?.CloseMenu();
            _savedBetBeforeAction = 0;
            EnsureMinimumBet();

            if (_martingaleDoubledLastPrepare)
                _martingaleDoubledLastPrepare = false;

            // Martingale doubling may have hit the bet cap — let the limit pulse finish first.
            if (IsLimitPulsing)
            {
                dealButton.gameObject.SetActive(true);
                return;
            }

            _betLimitStatusLocked = false;

            chipBetting?.SnapshotBet();
            _playerMoney -= CurrentBet;
            RefreshMoneyLabel();
            _state = GameState.PlayerTurn;
            _handsDealt++;
            if (menuController != null) menuController.HandsDealt = _handsDealt;
            RefreshHandsDealtLabel();
            if (_inMartingaleMode)
                RefreshMartingaleModeLabel();
            StartCoroutine(DealRound());
        }

        /// <summary>
        /// Transitions to Idle if needed, then ensures the bet area matches the chosen initial stake.
        /// </summary>
        private void EnsureMinimumBet()
        {
            if (chipBetting == null) return;

            if (_state == GameState.RoundOver)
            {
                // Capture the pre-double stake before Martingale raises the bet for the next round.
                if (_pendingMartingaleDouble && _betBeforeMartingale <= 0)
                    _betBeforeMartingale = chipBetting.TotalBet;

                PrepareForBetting();
            }

            if (chipBetting.TotalBet <= 0)
                chipBetting.SetBet(ResolveTargetInitialBet(), playSound: false);
            else
                SyncBetAreaToInitialBetIfNeeded();
        }

        /// <summary>Player draws another card.</summary>
        public void OnHit()
        {
            if (IsMenuBlocking) return;
            if (_state != GameState.PlayerTurn) return;
            ConfirmOrExecute(PlayerAction.Hit, () => StartCoroutine(PlayerHit()));
        }

        /// <summary>Player ends their turn; advances to split hand or dealer turn.</summary>
        public void OnStand()
        {
            if (IsMenuBlocking) return;
            if (_state != GameState.PlayerTurn) return;
            ConfirmOrExecute(PlayerAction.Stand, () => StartCoroutine(AdvanceOrDealerTurn()));
        }

        /// <summary>Player surrenders — forfeits half their bet and ends the round immediately.</summary>
        public void OnSurrender()
        {
            if (IsMenuBlocking) return;
            if (_state != GameState.PlayerTurn) return;
            if (ActiveHand.Cards.Count != 2) return;
            ConfirmOrExecute(PlayerAction.Surrender, () => StartCoroutine(PlayerSurrender()));
        }

        private IEnumerator PlayerSurrender()
        {
            _state = GameState.RoundOver;
            SetButtonState(dealEnabled: false, actionEnabled: false, splitEnabled: false);

            yield return StartCoroutine(RevealHoleCard());
            UpdateScoreLabels(revealDealer: true);

            PlayGameSound(chipSound); //mark1 
            RecordRoundOutcome(true, lostAmount: CurrentBet * 0.5m);
            SetStatus("Surrender returns 1/2 of bet", SurrenderColor);
            ApplyPayout(PayoutResult.Surrender, CurrentBet);

            yield return StartCoroutine(EndRound());
        }

        /// <summary>
        /// Splits the current two-card hand. Only available when both initial cards share the same rank.
        /// The split card moves to splitCardArea; each hand then receives one additional card stacked on top.
        /// </summary>
        public void OnSplit()
        {
            if (IsMenuBlocking) return;
            if (_state != GameState.PlayerTurn) return;
            if (!CanSplit()) return;
            ConfirmOrExecute(PlayerAction.Split, ExecuteSplit);
        }

        /// <summary>Performs all split setup (bet deduction, chip UI) and starts the split coroutine.</summary>
        private void ExecuteSplit()
        {
            _savedBetBeforeAction = CurrentBet;
            _playerMoney -= CurrentBet;
            RefreshMoneyLabel();
            StartCoroutine(PerformSplit());
        }

        /// <summary>
        /// Doubles down: player receives exactly one more card, then automatically stands.
        /// Only available on the initial two-card hand.
        /// </summary>
        public void OnDoubleDown()
        {
            if (IsMenuBlocking) return;
            if (_state != GameState.PlayerTurn) return;
            if (ActiveHand.Cards.Count != 2) return;
            ConfirmOrExecute(PlayerAction.Double, () => StartCoroutine(PerformDoubleDown()));
        }

        // ─── Strategy confirmation ─────────────────────────────────────────────────

        /// <summary>
        /// Evaluates <paramref name="chosenAction"/> against basic strategy.
        /// Correct actions execute immediately. Deviations show the popup:
        /// "Do Strategy" executes the recommended action; "Override" executes the player's chosen action.
        /// </summary>
        private void ConfirmOrExecute(PlayerAction chosenAction, Action executeChosen)
        {
            StrategyEvaluation evaluation = _strategyAdvisor.Evaluate(
                chosenAction, ActiveHand, _dealerUpcardSnapshot,
                canSplit: CanSplit(), canDouble: CanDoubleDown(), canSurrender: CanSurrender());

            if (evaluation.IsCorrect || deviationPopup == null || (menuController != null && menuController.IsStrategyOverrideEnabled))
            {
                executeChosen();
                return;
            }

            StrategyAction recommendation = evaluation.Recommendation;
            deviationPopup.Show(
                recommendation: recommendation.ToString(),
                onKeep:         () => ExecuteRecommendedAction(recommendation),
                onReconsider:   executeChosen);
        }

        private void ExecuteRecommendedAction(StrategyAction recommendation)
        {
            switch (recommendation)
            {
                case StrategyAction.Hit:
                    StartCoroutine(PlayerHit());
                    break;
                case StrategyAction.Stand:
                    StartCoroutine(AdvanceOrDealerTurn());
                    break;
                case StrategyAction.Double:
                    StartCoroutine(PerformDoubleDown());
                    break;
                case StrategyAction.Split:
                    ExecuteSplit();
                    break;
                case StrategyAction.Surrender:
                    StartCoroutine(PlayerSurrender());
                    break;
            }
        }

        /// <summary>Forces the next deal to give the player a natural blackjack, then starts the round.</summary>
        public void OnBlackjackTest()
        {
            if (_state != GameState.Idle && _state != GameState.RoundOver) return;
            StopBlackjackCelebration();
            DeactivateTestCheckboxes();
            _state = GameState.Idle;
            _forcePlayerBlackjack = true;
            StartNewRound();
        }

        /// <summary>Forces the next deal to give the player a matching pair of the rank chosen in the options slider.</summary>
        public void OnSplitTest()
        {
            if (_state != GameState.Idle && _state != GameState.RoundOver) return;
            StopBlackjackCelebration();
            DeactivateTestCheckboxes();
            _state = GameState.Idle;
            _forceSplitHand = true;
            StartNewRound();
        }

        /// <summary>Forces the next deal to give both player and dealer a natural blackjack, then starts the round.</summary>
        public void OnBothBlackjackTest()
        {
            if (_state != GameState.Idle && _state != GameState.RoundOver) return;
            StopBlackjackCelebration();
            DeactivateTestCheckboxes();
            _state = GameState.Idle;
            _forceBothBlackjack = true;
            StartNewRound();
        }

        /// <summary>Forces the next deal to give the dealer a natural blackjack, then starts the round.</summary>
        public void OnDealerBlackjackTest()
        {
            if (_state != GameState.Idle && _state != GameState.RoundOver) return;
            StopBlackjackCelebration();
            DeactivateTestCheckboxes();
            _forceBothBlackjack   = false;
            _forcePlayerBlackjack = false;
            _forceSplitHand       = false;
            _forceDoubleDownTest  = false;
            _state                = GameState.Idle;
            _forceDealerBlackjack = true;
            StartNewRound();
        }

        /// <summary>Forces the next deal to give the player a hard-11 two-card hand (random pair, e.g. 5+6 or 4+7), then starts the round.</summary>
        public void OnDoubleDownTest()
        {
            if (_state != GameState.Idle && _state != GameState.RoundOver) return;
            StopBlackjackCelebration();
            DeactivateTestCheckboxes();
            _state = GameState.Idle;
            _forceDoubleDownTest = true;
            StartNewRound();
        }

        /// <summary>Turns off "Always Lose" and "Override Strategy" before any test-button round.</summary>
        private void DeactivateTestCheckboxes() => menuController?.DisableTestCheckboxes();

        /// <summary>Enables or disables table auto-play without starting a round.</summary>
        public void SetAutoplayEnabled(bool enabled)
        {
            _autoPlayEnabled = enabled;
            SetStatus(_autoPlayEnabled ? "Auto-play ON" : "Auto-play OFF");
            Time.timeScale = (_autoPlayEnabled && _autoplayMaxSpeed) ? MaxAutoplayTimeScale : 1f;
            UpdateButtonRowForAutoplay();
        }

        private const float MaxAutoplayTimeScale = 8f;
        private bool _autoplayMaxSpeed;

        /// <summary>When true, timed waits and sound-length waits are skipped during auto-play.</summary>
        private bool SkipAutoplayDelays => _autoPlayEnabled && _autoplayMaxSpeed;

        /// <summary>Yields for <paramref name="seconds"/> unless max-speed auto-play is active.</summary>
        private IEnumerator WaitForGameDelay(float seconds)
        {
            if (SkipAutoplayDelays || seconds <= 0f)
                yield break;

            yield return new WaitForSeconds(seconds);
        }

        /// <summary>Plays a gameplay sound unless max-speed auto-play is active.</summary>
        private void PlayGameSound(SoundEntry sound)
        {
            if (SkipAutoplayDelays || !sound.HasClip || audioSource == null)
                return;

            sound.Play(audioSource);
        }

        /// <summary>Stores the max-speed flag and applies Time.timeScale immediately when autoplay is active.</summary>
        public void SetAutoplayMaxSpeed(bool enabled)
        {
            _autoplayMaxSpeed = enabled;
            Time.timeScale = (enabled && _autoPlayEnabled) ? MaxAutoplayTimeScale : 1f;
            UpdateButtonRowForAutoplay();
        }

        /// <summary>Enables auto-play when the menu Autoplay option is on at game load or menu close.</summary>
        public void StartAutoplayFromMenu()
        {
            SetAutoplayEnabled(true);

            if (_state == GameState.Idle || _state == GameState.RoundOver)
                OnDeal();
        }

        /// <summary>True when the Martingale popup should be skipped and Martingale entered automatically.</summary>
        private bool ShouldAutoConfirmMartingalePopup() =>
            menuController != null && menuController.IsMartingaleAutoPlay;

        /// <summary>Executes the basic-strategy recommendation for the active hand during auto-play.</summary>
        private IEnumerator RunAutoplayDecision()
        {
            if (!_autoPlayEnabled || _state != GameState.PlayerTurn)
                yield break;

            // Hold if the menu is open — resume once it closes.
            yield return new WaitUntil(() => !IsMenuOpen);
            yield return WaitForGameDelay(0.4f);

            StrategyAction recommendation = BasicStrategyTable.GetRecommendation(
                ActiveHand,
                _dealerUpcardSnapshot,
                canSplit: CanSplit(),
                canDouble: CanDoubleDown(),
                canSurrender: CanSurrender());

            switch (recommendation)
            {
                case StrategyAction.Hit:
                    yield return StartCoroutine(PlayerHit());
                    break;
                case StrategyAction.Stand:
                    PlayGameSound(knockSound);
                    yield return WaitForGameDelay(0.25f);
                    yield return StartCoroutine(AdvanceOrDealerTurn());
                    break;
                case StrategyAction.Double:
                    yield return StartCoroutine(PerformDoubleDown());
                    break;
                case StrategyAction.Split:
                    _savedBetBeforeAction = CurrentBet;
                    _playerMoney -= CurrentBet;
                    RefreshMoneyLabel();
                    chipBetting?.DoubleBetChips();
                    yield return StartCoroutine(PerformSplit());
                    break;
                case StrategyAction.Surrender:
                    yield return StartCoroutine(PlayerSurrender());
                    break;
            }
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Round Flow
        // ──────────────────────────────────────────────────────────────────────────

        private IEnumerator DealRound()
        {
            _state = GameState.PlayerTurn;
            SetButtonState(dealEnabled: false, actionEnabled: false, splitEnabled: false);

            _deck.Build();

            if (_forceBothBlackjack)   { _deck.ForceBothBlackjack();   _forceBothBlackjack   = false; }
            if (_forcePlayerBlackjack) { _deck.ForcePlayerBlackjack(); _forcePlayerBlackjack = false; }
            if (_forceSplitHand)
            {
                Rank selectedRank = (menuController != null)
                    ? (Rank)menuController.TestSplitRank
                    : Rank.Five;
                _deck.ForceSplitHandWithRank(selectedRank);
                _forceSplitHand = false;
            }
            if (_forceDoubleDownTest)  { _deck.ForceDoubleDownTest();  _forceDoubleDownTest  = false; }
            if (_forceDealerBlackjack) { _deck.ForceDealerBlackjack(); _forceDealerBlackjack = false; }

            ClearTable();
            _winPresentationComplete = false;
            ClearDeferredWinPayout();
            SetStatus("");
            _doubleDownExtraBet = 0;
            yield return WaitForGameDelay(newRoundPause); //mark1
            yield return new WaitUntil(() => !IsMenuOpen); // Pause before the first card is dealt
            //SetStatus("Dealing...");

            yield return StartCoroutine(DealCardTo(_playerHand, _playerCardViews, playerCardArea, faceUp: true));
            yield return StartCoroutine(DealCardTo(_dealerHand, _dealerCardViews, dealerCardArea, faceUp: true));
            yield return StartCoroutine(DealCardTo(_playerHand, _playerCardViews, playerCardArea, faceUp: true));
            yield return StartCoroutine(DealCardTo(_dealerHand, _dealerCardViews, dealerCardArea, faceUp: false));

            _dealerHoleCardView = _dealerCardViews[^1];
            UpdateScoreLabels(revealDealer: false);

            if (ShouldDealerPeek())
                yield return StartCoroutine(DealerPeekHoleCardCheck());

            // ── Natural blackjack check (both dealer cards are already in the hand) ──
            bool playerNatural = IsNaturalBlackjack(_playerHand);
            bool dealerNatural = IsNaturalBlackjack(_dealerHand);

            if (playerNatural && dealerNatural)
            {
                yield return StartCoroutine(RevealDealerHoleForNaturalBlackjack());

                if (AlwaysLose)
                {
                    // Always Lose: treat double-BJ push as a loss.
                    RecordRoundOutcome(true, lostAmount: CurrentBet, scoreDelta: -1);
                    SetStatus("You lose", LoseColor);
                    ApplyPayout(PayoutResult.Lose, CurrentBet);
                    yield return StartCoroutine(PlayLoseSoundAndWait());
                }
                else
                {
                    StartCoroutine(PlayDoubleBJSoundRoutine());
                    RecordRoundOutcome(false, scoreDelta: 0, isPush: true);
                    SetStatus("Push", PushColor);
                    ApplyPayout(PayoutResult.Push, CurrentBet);
                }

                yield return StartCoroutine(EndRound());
                yield break;
            }

            if (playerNatural)
            {
                yield return StartCoroutine(RevealDealerHoleForNaturalBlackjack());

                if (AlwaysLose)
                {
                    // Always Lose: treat a natural blackjack as a loss.
                    RecordRoundOutcome(true, lostAmount: CurrentBet, scoreDelta: -1);
                    SetStatus("You lose", LoseColor);
                    ApplyPayout(PayoutResult.Lose, CurrentBet);
                    yield return StartCoroutine(PlayLoseSoundAndWait());
                }
                else
                {
                    // Capture before RecordRoundOutcome clears _inMartingaleMode.
                    bool isMartingaleNaturalBJ = _inMartingaleMode;
                    StartCoroutine(PlayWinAndChipRoutine(
                        useCelebration: true,
                        playResetSound: isMartingaleNaturalBJ,
                        deferPayout: true,
                        deferredPayout: PayoutResult.BlackjackWin,
                        deferredBet: CurrentBet));
                    RecordRoundOutcome(false, scoreDelta: +1);
                    _playerWon = true;
                    SetStatus(isMartingaleNaturalBJ ? "Won with Martingale" : "You win", WinColor);
                }

                yield return StartCoroutine(EndRound());
                yield break;
            }

            if (dealerNatural)
            {
                if (_dealerHoleCardView != null && !_dealerHoleCardView.IsFaceUp)
                    yield return StartCoroutine(RevealDealerHoleForNaturalBlackjack());
                else
                {
                    PlayCardSlideSound();
                    UpdateScoreLabels(revealDealer: true);
                }

                yield return StartCoroutine(ApplyDealerNaturalBlackjackLossRoutine(CurrentBet, revealHole: false));
                yield return StartCoroutine(EndRound());
                yield break;
            }

            // ── Player turn ──
            SetButtonState(dealEnabled: false, actionEnabled: true, splitEnabled: CanSplit(), doubleDownEnabled: CanDoubleDown(), surrenderEnabled: true);
            SetStatus($"Your turn");

            _dealerUpcardSnapshot = _dealerHand.Cards[0];
            bool hasPair = CanSplit();

            RefreshStrategyHighlight();

            // Auto-split pairs of Eights or Aces — always the correct basic strategy play.
            if (CanSplit() && IsAutoSplitHand(_playerHand))
            {
                ExecuteSplit();
                yield break;
            }

            if (_autoPlayEnabled)
            {
                yield return StartCoroutine(RunAutoplayDecision());
                yield break;
            }

            if (!hasPair && _playerHand.BestValue() <= AutoHitMaxScore)
            {
                yield return WaitForGameDelay(0.3f);
                yield return StartCoroutine(AutoHitLoop());
                yield break;
            }

            if (ShouldAutoStand(_playerHand))
            {
                PlayGameSound(knockSound);
                yield return WaitForGameDelay(0.3f);
                yield return StartCoroutine(DealerTurn());
            }
        }

        // ── Split ─────────────────────────────────────────────────────────────────

        private bool CanSplit() =>
            !_isSplitRound
            && _playerHand.Count == 2
            && _playerHand.Cards[0].Rank == _playerHand.Cards[1].Rank;

        /// <summary>Returns true when the pair should always be split automatically (8-8 or A-A).</summary>
        private static bool IsAutoSplitHand(Hand hand) =>
            hand.Cards[0].Rank is Rank.Eight or Rank.Ace;

        private bool CanSurrender() =>
            ActiveHand.Count == 2 && !_isSplitRound;

        private IEnumerator PerformSplit()
        {
            _isSplitRound = true;
            SetButtonState(dealEnabled: false, actionEnabled: false, splitEnabled: false);
            RefreshStrategyHighlight();

            // Move card[1] from player hand to split hand
            CardData movedCard = _playerHand.Cards[1];
            _playerHand.RemoveAt(1);

            ICardDisplay movedView = _playerCardViews[1];
            _playerCardViews.RemoveAt(1);

            Transform splitParent = ResolveCardSpawnParent(splitCardArea);
            Transform movedTransform = GetTransform(movedView);
            if (movedTransform != null && splitParent != null)
                movedTransform.SetParent(splitParent, false);

            if (useWorldSpaceCards)
            {
                splitParent?.GetComponent<WorldCardRowLayout>()?.RefreshLayout();
                ResolveCardSpawnParent(playerCardArea)?.GetComponent<WorldCardRowLayout>()?.RefreshLayout();
            }

            _splitCardViews.Add(movedView);
            _splitHand.AddCard(movedCard);

            PlayCardSlideSound();
            yield return WaitForGameDelay(0.5f);
            PlayGameSound(chipSound);
            if (chipSound.Length > 0f)
                yield return WaitForGameDelay(chipSound.Length);
            chipBetting?.DoubleBetChips();

            // Deal second card to each hand
            yield return StartCoroutine(DealCardTo(_playerHand,  _playerCardViews, playerCardArea, faceUp: true));
            yield return StartCoroutine(DealCardTo(_splitHand,   _splitCardViews,  splitCardArea,  faceUp: true));

            UpdateScoreLabels(revealDealer: false);
            _activeHandIndex = 0;

            SetButtonState(dealEnabled: false, actionEnabled: true, splitEnabled: false, doubleDownEnabled: CanDoubleDown());
            SetStatus($"Players turn Hand 1");
            RefreshStrategyHighlight();

            if (_autoPlayEnabled)
            {
                yield return StartCoroutine(RunAutoplayDecision());
                yield break;
            }

            if (ActiveHand.BestValue() <= AutoHitMaxScore)
            {
                yield return WaitForGameDelay(0.3f);
                yield return StartCoroutine(AutoHitLoop());
                yield break;
            }

            if (ShouldAutoStand(ActiveHand))
            {
                PlayGameSound(knockSound);
                yield return WaitForGameDelay(0.3f);
                yield return StartCoroutine(AdvanceOrDealerTurn());
            }
        }

        // ── Double Down ───────────────────────────────────────────────────────────

        private bool CanDoubleDown() =>
            ActiveHand.Cards.Count == 2;

        private IEnumerator PerformDoubleDown()
        {
            SetButtonState(dealEnabled: false, actionEnabled: false, splitEnabled: false);
            SetStatus("Doubling down");

            int extraBet = _isSplitRound ? CurrentBet / 2 : CurrentBet;
            _savedBetBeforeAction = CurrentBet;
            _doubleDownExtraBet = extraBet;
            if (_isSplitRound)
                _splitHandDoubledDown[_activeHandIndex] = true;
            _playerMoney -= extraBet;
            RefreshMoneyLabel();
            PlayGameSound(ddSound);
            if (ddSound.Length > 0f)
                yield return WaitForGameDelay(ddSound.Length);
            else
                yield return WaitForGameDelay(0.5f);
            if (_isSplitRound)
                chipBetting?.SetBet(CurrentBet + extraBet, playSound: false);
            else
                chipBetting?.DoubleBetChips();
            yield return StartCoroutine(
                DealCardTo(ActiveHand, ActiveViews,
                           _activeHandIndex == 0 ? playerCardArea : splitCardArea,
                           faceUp: true));

            SetStatus("Double Down!");

            UpdateScoreLabels(revealDealer: false);

            if (ActiveHand.IsBust())
            {
                if (_isSplitRound)
                {
                    // In a split round, advance to hand 2 (or dealer turn if this is already hand 2).
                    // ResolveRound() handles all accounting via _splitHandDoubledDown.
                    yield return StartCoroutine(PlayLoseSoundAndWait());
                    yield return StartCoroutine(AdvanceOrDealerTurn());
                    yield break;
                }

                yield return StartCoroutine(RevealHoleCard());
                UpdateScoreLabels(revealDealer: true);
                if (IsDealerNaturalBlackjackLoss(_playerHand))
                {
                    yield return StartCoroutine(
                        ApplyDealerNaturalBlackjackLossRoutine(TotalStakedBet));
                    SetStatus("Busted");
                    yield return StartCoroutine(EndRound());
                    yield break;
                }

                RecordRoundOutcome(true, lostAmount: TotalStakedBet, scoreDelta: -1, lossCount: 2);
                SetStatus($"Busted");
                yield return StartCoroutine(PlayLoseSoundAndWait());
                yield return StartCoroutine(EndRound());
                yield break;
            }

            SetStatus($"Double Down stands at {ActiveHand.BestValue()}");
            yield return WaitForGameDelay(dealerPauseDelay);
            yield return StartCoroutine(AdvanceOrDealerTurn());
        }

        private IEnumerator AdvanceOrDealerTurn()
        {
            if (_isSplitRound && _activeHandIndex == 0)
            {
                _activeHandIndex = 1;
                UpdateScoreLabels(revealDealer: false);
                SetStatus($"Players turn Hand 2");
                SetButtonState(dealEnabled: false, actionEnabled: true, splitEnabled: false, doubleDownEnabled: CanDoubleDown());
                RefreshStrategyHighlight();

                if (_autoPlayEnabled)
                    yield return StartCoroutine(RunAutoplayDecision());
                else if (ActiveHand.BestValue() <= AutoHitMaxScore)
                {
                    yield return WaitForGameDelay(0.3f);
                    yield return StartCoroutine(AutoHitLoop());
                }
                else if (ShouldAutoStand(ActiveHand))
                {
                    PlayGameSound(knockSound);
                    yield return WaitForGameDelay(0.3f);
                    yield return StartCoroutine(DealerTurn());
                }
            }
            else
            {
                yield return StartCoroutine(DealerTurn());
            }
        }

        /// <summary>Automatically hits until the score exceeds AutoHitMaxScore, then returns control to the player or proceeds with auto-stand logic.</summary>
        private IEnumerator AutoHitLoop()
        {
            while (ActiveHand.BestValue() <= AutoHitMaxScore)
            {
                yield return StartCoroutine(PlayerHit());

                int score = ActiveHand.BestValue();
                if (score > BlackjackValue || score == BlackjackValue || ShouldAutoStand(ActiveHand))
                    yield break;
            }
        }

        private IEnumerator PlayerHit()
        {
            SetButtonState(dealEnabled: false, actionEnabled: false, splitEnabled: false);

            Transform area = (_isSplitRound && _activeHandIndex == 1) ? splitCardArea : playerCardArea;
            yield return StartCoroutine(DealCardTo(ActiveHand, ActiveViews, area, faceUp: true));
            UpdateScoreLabels(revealDealer: false);

            RefreshStrategyHighlight();

            int score = ActiveHand.BestValue();

            if (score > BlackjackValue)
            {
                if (!_isSplitRound)
                    SetStatus("Bust!", LoseColor);
                yield return StartCoroutine(PlayLoseSoundAndWait());

                if (_isSplitRound)
                {
                    // Always advance to next hand or dealer turn so both hands get resolved.
                    yield return StartCoroutine(AdvanceOrDealerTurn());
                }
                else
                {
                    RecordRoundOutcome(true, lostAmount: CurrentBet, scoreDelta: -1);
                    yield return StartCoroutine(RevealHoleCard());
                    UpdateScoreLabels(revealDealer: true);
                    if (IsDealerNaturalBlackjackLoss(_playerHand))
                        yield return StartCoroutine(ApplyDealerNaturalBlackjackPresentationRoutine());
                    yield return StartCoroutine(EndRound());
                }
                yield break;
            }

            if (score == BlackjackValue || ShouldAutoStand(ActiveHand))
            {
                if (score != BlackjackValue)
                    PlayGameSound(knockSound);
                yield return WaitForGameDelay(0.25f);
                yield return StartCoroutine(AdvanceOrDealerTurn());
                yield break;
            }

            if (score <= AutoHitMaxScore)
            {
                yield return WaitForGameDelay(0.3f);
                yield return StartCoroutine(PlayerHit());
                yield break;
            }

            SetButtonState(dealEnabled: false, actionEnabled: true, splitEnabled: false);
            SetStatus(_isSplitRound
                ? $"Players turn Hand {_activeHandIndex + 1}"
                : "Your turn");

            if (_autoPlayEnabled)
                yield return StartCoroutine(RunAutoplayDecision());
        }

        private IEnumerator DealerTurn()
        {
            _state = GameState.DealerTurn;
            SetButtonState(dealEnabled: false, actionEnabled: false, splitEnabled: false);
            StopAllScorePulses();

            yield return new WaitUntil(() => !IsMenuOpen); // Pause before hole card reveal
            yield return StartCoroutine(RevealHoleCard());
            UpdateScoreLabels(revealDealer: true);

            if (IsDealerNaturalBlackjackLoss(_playerHand))
            {
                yield return StartCoroutine(
                    ApplyDealerNaturalBlackjackLossRoutine(TotalStakedBet));
                yield return StartCoroutine(EndRound());
                yield break;
            }

            // If both split hands busted, skip dealer drawing.
            bool allPlayerHandsBusted = _isSplitRound
                ? _playerHand.IsBust() && _splitHand.IsBust()
                : _playerHand.IsBust();

            if (!allPlayerHandsBusted)
            {
                SetStatus("Dealer's turn");
                yield return WaitForGameDelay(dealerPauseDelay);

                while (ShouldDealerHit())
                {
                    yield return StartCoroutine(DealCardTo(_dealerHand, _dealerCardViews, dealerCardArea, faceUp: true));
                    UpdateScoreLabels(revealDealer: true);
                    yield return new WaitUntil(() => !IsMenuOpen); // Pause between dealer hits
                    yield return WaitForGameDelay(dealerPauseDelay);
                }
            }

            yield return StartCoroutine(ResolveRound());
        }

        /// <summary>
        /// Dealer hits below 17 and stands on 17 or higher (including soft 17).
        /// </summary>
        private bool ShouldDealerHit()
        {
            return _dealerHand.BestValue() < DealerSoft17;
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Money / Payout
        // ──────────────────────────────────────────────────────────────────────────

        private static readonly System.Globalization.CultureInfo GermanCulture =
            System.Globalization.CultureInfo.GetCultureInfo("de-DE");

        private enum PayoutResult { Win, BlackjackWin, Lose, Push, Surrender }

        /// <summary>
        /// Receives live bet delta from ChipBetting. Money is deducted at deal time,
        /// so this handler only refreshes the label to reflect any UI-only changes.
        /// </summary>
        private void OnBetChangedHandler(int delta)
        {
            RefreshMoneyLabel();

            if (!IsBettingAllowed) return;

            if (IsLimitPulsing || _betLimitStatusLocked) return;

            if (CurrentBet == 0)
            {
                SetStatus("Place your bet");
                return;
            }

            if (_consecutiveLosses >= DelayedMartingaleThreshold && CurrentBet > _lastRoundBet)
            {
                SetStatus("Doing Delayed Martingale", WinColor);
                return;
            }

            // A chip was just added — prompt the player to deal.
            if (delta > 0 && statusLabel.text == "Place your bet")
                SetStatus("Press Deal to start");
        }

        /// <summary>
        /// Applies end-of-round payout. The bet was deducted at deal time,
        /// so payouts return the appropriate amount to the balance.
        /// Win → bet×2 | BJ → bet×2.5 | Push → bet | Surrender → bet×0.5 | Lose → 0
        /// </summary>
        private void ApplyPayout(PayoutResult result, int bet, bool refreshLabel = true)
        {
            _playerMoney += result switch
            {
                PayoutResult.Win          => bet * 2m,
                PayoutResult.BlackjackWin => bet * 2.5m,
                PayoutResult.Push         => bet,
                PayoutResult.Surrender    => bet * 0.5m,
                _                         => 0,                   // Lose — bet already gone
            };

            if (refreshLabel)
                RefreshMoneyLabel();
        }

        private void RegisterDeferredWinPayout(PayoutResult result, int bet)
        {
            if (bet <= 0) return;

            _deferredWinPayoutPending = true;
            _deferredWinPayoutResult  = result;
            _deferredWinPayoutBet     = bet;
        }

        private void ClearDeferredWinPayout()
        {
            _deferredWinPayoutPending = false;
            _deferredWinPayoutBet     = 0;
        }

        /// <summary>
        /// Applies a natural-blackjack payout that was deferred for presentation.
        /// Called before the next deal so max-speed auto-play cannot skip the credit.
        /// </summary>
        private void ApplyDeferredWinPayoutIfPending()
        {
            if (!_deferredWinPayoutPending || _deferredWinPayoutBet <= 0)
                return;

            ApplyPayout(_deferredWinPayoutResult, _deferredWinPayoutBet);
            ClearDeferredWinPayout();
        }

        private void RefreshMoneyLabel()
        {
            if (playerMoneyLabel == null) return;
            playerMoneyLabel.text = $"€ {((decimal)_playerMoney).ToString("N2", GermanCulture)}";
        }

        private int CurrentBet => chipBetting != null ? chipBetting.TotalBet : 0;

        /// <summary>Total stake in the bet area — matches the amount deducted from balance this round.</summary>
        private int TotalStakedBet => CurrentBet;

        /// <summary>Per-hand wager in a split round, accounting for an optional double-down on one hand.</summary>
        private int GetSplitHandBet(int handIndex)
        {
            int baseHandBet = (CurrentBet - _doubleDownExtraBet) / 2;
            return _splitHandDoubledDown[handIndex] ? baseHandBet + _doubleDownExtraBet : baseHandBet;
        }

        /// <summary>
        /// Records whether the round was a net loss (bust, lose, surrender) or not.
        /// Maintains <see cref="_consecutiveLosses"/>, <see cref="_totalLosses"/>, <see cref="_totalAmountLost"/>,
        /// <see cref="_playerScore"/>, and snapshots the bet for Delayed Martingale detection.
        /// <paramref name="lostAmount"/> is the monetary amount forfeited this round (full bet for a loss, half for surrender, 0 for win/push).
        /// <paramref name="scoreDelta"/> is +1 for a win, -1 for a loss, 0 for push or surrender.
        /// <paramref name="isPush"/> when true, leaves the loss streak and all Martingale state completely unchanged — a push is neutral.
        /// <paramref name="isMartingaleNeutral"/> when true, leaves all Martingale and streak state completely unchanged (used for split rounds with no net score change).
        /// <paramref name="lossCount"/> number of losses to count toward the streak — 2 when both split hands are lost.
        /// </summary>
        private void RecordRoundOutcome(bool isLoss, decimal lostAmount = 0, int scoreDelta = 0, bool isPush = false, bool isMartingaleNeutral = false, int lossCount = 1)
        {
            Debug.Log($"[Martingale] RecordRoundOutcome called: isLoss={isLoss} isPush={isPush} isMartingaleNeutral={isMartingaleNeutral} AlwaysLose={AlwaysLose}");

            _lastRoundBet  = CurrentBet;
            _playerScore  += scoreDelta;

            // Split rounds where the player's score is unchanged leave the Martingale counter exactly as-is.
            if (isMartingaleNeutral)
            {
                RefreshStreakLabel();
                return;
            }

            // When AlwaysLose is active every round counts as a loss for Martingale tracking,
            // regardless of the actual card outcome.
            if (AlwaysLose)
            {
                isLoss    = true;
                isPush    = false;
                if (lostAmount == 0) lostAmount = _lastRoundBet;
            }

            if (isLoss)
            {
                _consecutiveLosses += lossCount;
                _totalLosses       += lossCount;
                _totalAmountLost   += lostAmount;

                bool thresholdReached = EffectiveMartingaleThreshold > 0 && _consecutiveLosses >= EffectiveMartingaleThreshold;

                Debug.Log($"[Martingale] Loss recorded: losses={_consecutiveLosses} threshold={EffectiveMartingaleThreshold} thresholdReached={thresholdReached} inMode={_inMartingaleMode} declined={_martingaleDeclined}");

                if (_inMartingaleMode)
                {
                    // Already in Martingale — schedule a bet double for the next betting phase.
                    _pendingMartingaleDouble = true;
                }
                else if (thresholdReached && !_martingaleDeclined)
                {
                    // Threshold just reached — arm the popup for EndRound.
                    _martingalePopupShown = true;
                }

                _savedBetBeforeAction = 0;
                _doubleDownExtraBet   = 0;
            }
            else if (isPush)
            {
                // A push is neutral — the loss streak and all Martingale state are left unchanged.
            }
            else
            {
                // Player won — flag Martingale / double-down bet restore for PlayWinAndChipRoutine.
                if (_inMartingaleMode)
                    _martingaleWin = true;

                if (_doubleDownExtraBet <= 0)
                    _savedBetBeforeAction = 0;

                _consecutiveLosses       = 0;
                _totalAmountLost         = 0;
                _totalLosses             = 0;
                _martingalePopupShown    = false;
                _inMartingaleMode        = false;
                _martingaleDeclined      = false;
                _pendingMartingaleDouble = false;
            }
            RefreshStreakLabel();
        }

        /// <summary>
        /// Keeps player info labels on the same horizontal axis with equal vertical spacing between all four rows.
        /// </summary>
        private void AlignPlayerInfoLabels()
        {
            if (playerMoneyLabel == null) return;

            Transform canvas = playerMoneyLabel.transform.parent;
            if (canvas == null) return;

            EnsureMartingaleModeLabel();
            if (martingaleModeLabel != null)
            {
                martingaleModeLabel.text = "Martingale Mode";
                ApplyMartingaleModeLabelTypography(martingaleModeLabel);
            }

            TextMeshProUGUI playerLabel = canvas.Find("PlayerLabel")?.GetComponent<TextMeshProUGUI>();
            AlignLabelToPlayerMoney(playerLabel);
            AlignLabelToPlayerMoney(playerMoneyLabel);
            AlignLabelToPlayerMoney(streakLabel);
            AlignLabelToPlayerMoney(martingaleModeLabel);
            AlignLabelToPlayerMoney(canvas.Find("DealerLabel")?.GetComponent<TextMeshProUGUI>());

            float width = playerMoneyLabel.rectTransform.sizeDelta.x;
            List<TextMeshProUGUI> rows = CollectVisiblePlayerInfoRows(playerLabel);
            if (rows.Count == 0) return;

            float rowHeight = ComputePlayerInfoRowHeight(rows);
            for (int i = 0; i < rows.Count; i++)
                FitPlayerInfoRow(rows[i], width, rowHeight);

            Vector2 topPos = rows[0].rectTransform.anchoredPosition;
            topPos.y       = playerInfoTopY;
            rows[0].rectTransform.anchoredPosition = topPos;

            for (int i = 1; i < rows.Count; i++)
                StackLabelBelow(rows[i], rows[i - 1], playerInfoLabelGap);

            martingaleModeLabel?.rectTransform.SetAsLastSibling();
        }

        private List<TextMeshProUGUI> CollectVisiblePlayerInfoRows(TextMeshProUGUI playerLabel)
        {
            var rows = new List<TextMeshProUGUI>(4);
            if (playerLabel != null) rows.Add(playerLabel);
            if (playerMoneyLabel != null) rows.Add(playerMoneyLabel);
            if (streakLabel != null && streakLabel.gameObject.activeSelf) rows.Add(streakLabel);
            if (martingaleModeLabel != null && IsMartingaleModeActive) rows.Add(martingaleModeLabel);
            return rows;
        }

        private static float ComputePlayerInfoRowHeight(IReadOnlyList<TextMeshProUGUI> rows)
        {
            float height = 0f;
            for (int i = 0; i < rows.Count; i++)
                height = Mathf.Max(height, MeasureLabelHeight(rows[i]));
            return height;
        }

        private static void FitPlayerInfoRow(TextMeshProUGUI label, float width, float rowHeight)
        {
            if (label == null) return;

            label.rectTransform.sizeDelta = new Vector2(width, rowHeight);
            label.verticalAlignment       = VerticalAlignmentOptions.Bottom;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Application.isPlaying || playerMoneyLabel == null)
                return;

            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this == null || playerMoneyLabel == null)
                    return;

                AlignPlayerInfoLabels();
            };
        }
#endif

        private static void StackLabelBelow(TextMeshProUGUI lower, TextMeshProUGUI upper, float gap)
        {
            if (lower == null || upper == null) return;

            RectTransform lowerRT = lower.rectTransform;
            RectTransform upperRT = upper.rectTransform;

            lowerRT.anchorMin = upperRT.anchorMin;
            lowerRT.anchorMax = upperRT.anchorMax;
            lowerRT.pivot     = upperRT.pivot;

            Vector2 pos = lowerRT.anchoredPosition;
            pos.y       = upperRT.anchoredPosition.y - gap - lowerRT.sizeDelta.y;
            lowerRT.anchoredPosition = pos;
        }

        private static float MeasureLabelHeight(TextMeshProUGUI label)
        {
            if (label == null) return 0f;
            label.ForceMeshUpdate(true, true);
            Bounds bounds = label.textBounds;
            if (bounds.size.y > 0.01f)
                return bounds.size.y;
            return label.preferredHeight;
        }

        private void AlignLabelToPlayerMoney(TextMeshProUGUI label)
        {
            if (label == null || playerMoneyLabel == null) return;

            RectTransform moneyRT = playerMoneyLabel.rectTransform;
            RectTransform labelRT = label.rectTransform;

            label.horizontalAlignment = HorizontalAlignmentOptions.Center;
            label.margin              = playerMoneyLabel.margin;

            Vector2 pos = labelRT.anchoredPosition;
            pos.x       = moneyRT.anchoredPosition.x;
            labelRT.anchoredPosition = pos;

            Vector2 size = labelRT.sizeDelta;
            size.x       = moneyRT.sizeDelta.x;
            labelRT.sizeDelta = size;
        }

        /// <summary>Updates the streak label to show the current loss streak, total amount lost, and when in Martingale mode, the chip amount being added each round.</summary>
        private void RefreshStreakLabel()
        {
            if (streakLabel == null) return;
            if (_consecutiveLosses > 0)
            {
                //string text = $"Lost: {_consecutiveLosses} times in row  |  -€ {_totalAmountLost:N2}";
                string text = $"Lost: {(int)System.Math.Ceiling(_consecutiveLosses)} times in row";
                streakLabel.text = text;
                streakLabel.gameObject.SetActive(true);
            }
            else
            {
                streakLabel.gameObject.SetActive(false);
            }

            RefreshMartingaleModeLabel();
        }

        /// <summary>Updates the hands-dealt counter label with the current lifetime count.</summary>
        private void RefreshHandsDealtLabel()
        {
            if (handsDealtLabel != null)
                handsDealtLabel.text = $"Hands: {_handsDealt.ToString("N0", GermanCulture)}";
        }

        private void RefreshMartingaleModeLabel()
        {
            EnsureMartingaleModeLabel();
            if (martingaleModeLabel == null) return;

            bool showLabel = _inMartingaleMode;
            if (!showLabel)
            {
                martingaleModeLabel.gameObject.SetActive(false);
                AlignPlayerInfoLabels();
                return;
            }

            martingaleModeLabel.gameObject.SetActive(true);
            martingaleModeLabel.text = "Martingale Mode";
            ApplyMartingaleModeLabelTypography(martingaleModeLabel);
            AlignPlayerInfoLabels();
            martingaleModeLabel.alpha = 1f;
            martingaleModeLabel.ForceMeshUpdate(true, true);
        }

        private bool IsMartingaleModeActive => _inMartingaleMode;

        private void EnsureMartingaleModeLabel()
        {
            if (martingaleModeLabel != null || streakLabel == null) return;

            GameObject labelObj = new("Martingale Mode", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelObj.transform.SetParent(streakLabel.transform.parent, false);

            martingaleModeLabel = labelObj.GetComponent<TextMeshProUGUI>();
            ApplyMartingaleModeLabelTypography(martingaleModeLabel);
            martingaleModeLabel.text                = "Martingale Mode";
            martingaleModeLabel.raycastTarget       = false;
            martingaleModeLabel.horizontalAlignment = HorizontalAlignmentOptions.Center;
            martingaleModeLabel.verticalAlignment   = VerticalAlignmentOptions.Middle;
            martingaleModeLabel.enableWordWrapping  = false;
            martingaleModeLabel.margin              = streakLabel.margin;

            labelObj.SetActive(false);
        }

        private void ApplyMartingaleModeGoldColor(TextMeshProUGUI target)
        {
            if (target == null) return;

            target.color = MartingaleModeGoldColor;
            if (statusLabel != null)
            {
                target.outlineWidth = statusLabel.outlineWidth;
                target.outlineColor = statusLabel.outlineColor;
            }
        }

        private void ApplyMartingaleModeLabelTypography(TextMeshProUGUI target)
        {
            if (target == null || statusLabel == null) return;

            target.font = statusLabel.font;
            if (statusLabel.fontSharedMaterial != null)
                target.fontSharedMaterial = statusLabel.fontSharedMaterial;
            target.fontSize  = martingaleModeFontSize;
            target.fontStyle = statusLabel.fontStyle;

            target.enableVertexGradient  = false;
            target.horizontalAlignment = HorizontalAlignmentOptions.Center;
            target.verticalAlignment   = VerticalAlignmentOptions.Bottom;

            ApplyMartingaleModeGoldColor(target);

            if (target.font != null)
                target.UpdateFontAsset();
        }

        private void ApplyStatusLabelTypography(TextMeshProUGUI target)
        {
            target.font               = statusLabel.font;
            target.fontSharedMaterial = statusLabel.fontSharedMaterial;
            target.fontSize           = statusLabel.fontSize;
            target.fontStyle          = statusLabel.fontStyle;
            target.color              = statusLabel.color;
            target.UpdateFontAsset();
        }

        /// <summary>Creates the Martingale mode indicator beneath the streak label, using StatusLabel typography.</summary>
        private void InitMartingaleModeLabel()
        {
            EnsureMartingaleModeLabel();
            if (martingaleModeLabel != null)
                martingaleModeLabel.gameObject.SetActive(false);
        }

        private IEnumerator ResolveRound()
        {
            if (IsNaturalBlackjack(_dealerHand))
            {
                yield return StartCoroutine(ResolveDealerNaturalBlackjackRound());
                yield break;
            }

            int dealerScore = _dealerHand.BestValue();
            bool dealerBust = dealerScore > BlackjackValue;

            // When AlwaysLose is enabled, override the outcome: treat it as a dealer win.
            if (AlwaysLose)
            {
                dealerBust  = false;
                dealerScore = BlackjackValue; // dealer at 21 — beats any non-bust player hand
            }

            if (_isSplitRound)
            {
                var    results = new List<string>();
                bool   anyWin  = false;
                bool   anyLoss = false;
                bool   anyPush = false;
                int    splitLostAmount = 0;

                Hand[]   hands  = { _playerHand, _splitHand };
                string[] labels = { "Hand 1", "Hand 2" };

                int splitLossCount = 0;

                for (int i = 0; i < hands.Length; i++)
                {
                    int s = hands[i].BestValue();
                    int handBet = GetSplitHandBet(i);

          if (s > BlackjackValue)
                    {
                        results.Add(ColorizeText($"{labels[i]}: Bust", LoseColor));
                        anyLoss = true;
                        splitLostAmount += handBet;
                        splitLossCount  += 1 + (_splitHandDoubledDown[i] ? 1 : 0);
                        ApplyPayout(PayoutResult.Lose, handBet);
                    }
                    else if (dealerBust || s > dealerScore)
                    {
                        results.Add(ColorizeText($"{labels[i]}: Win", WinColor));
                        anyWin = true;
                        ApplyPayout(PayoutResult.Win, handBet, refreshLabel: false);
                    }
                    else if (s < dealerScore)
                    {
                        results.Add(ColorizeText($"{labels[i]}: Lose", LoseColor));
                        anyLoss = true;
                        splitLostAmount += handBet;
                        splitLossCount  += 1 + (_splitHandDoubledDown[i] ? 1 : 0);
                        ApplyPayout(PayoutResult.Lose, handBet);
                    }
                    else
                    {
                        if (AlwaysLose)
                        {
                            // Always Lose: treat per-hand push as a loss.
                            results.Add(ColorizeText($"{labels[i]}: Lose", LoseColor));
                            anyLoss = true;
                            splitLostAmount += handBet;
                            splitLossCount  += 1 + (_splitHandDoubledDown[i] ? 1 : 0);
                            ApplyPayout(PayoutResult.Lose, handBet);
                        }
                        else
                        {
                            results.Add(ColorizeText($"{labels[i]}: Push", PushColor));
                            anyPush = true;
                            ApplyPayout(PayoutResult.Push, handBet);
                        }
                    }
                }

                if (anyWin && !anyLoss) { StartCoroutine(PlayWinAndChipRoutine(useCelebration: _inMartingaleMode, playResetSound: _inMartingaleMode)); _playerWon = true; }
                else if (anyWin && anyLoss) { PlayTieSound(); RefreshMoneyLabel(); }
                else if (anyLoss)           yield return StartCoroutine(PlayLoseSoundAndWait());
                else                        PlayTieSound();

                // Split 1W/1L or 1W/1Push counts as a push for the Martingale counter — streak is neither incremented nor reset.
                bool splitPush = (anyWin && anyLoss) || (anyWin && anyPush && !anyLoss);
                int  splitScoreDelta = anyWin && !anyLoss ? +1 : anyLoss && !anyWin ? -1 : 0;
                // If the split produced no net score change, leave the Martingale counter completely untouched.
                bool splitNeutral = splitScoreDelta == 0;
                // Each losing hand contributes 1 base loss + 1 extra if it was doubled down.
                RecordRoundOutcome(isLoss: anyLoss && !anyWin, lostAmount: splitLostAmount,
                    scoreDelta: splitScoreDelta,
                    isPush: !anyWin && !anyLoss || splitPush,
                    isMartingaleNeutral: splitNeutral,
                    lossCount: splitLossCount > 0 ? splitLossCount : 1);
                SetStatus(string.Join("  |  ", results));
            }
            else
            {
                int p = _playerHand.BestValue();
                int stakedBet = TotalStakedBet;
                int winBet = stakedBet;
                // Capture before RecordRoundOutcome clears _inMartingaleMode.
                bool isMartingaleWin = _inMartingaleMode;
                if      (IsNaturalBlackjack(_playerHand)) { StartCoroutine(PlayWinAndChipRoutine(useCelebration: true, playResetSound: isMartingaleWin, deferPayout: true, deferredPayout: PayoutResult.BlackjackWin, deferredBet: winBet)); RecordRoundOutcome(false, scoreDelta: +1); _playerWon = true; SetStatus(isMartingaleWin ? "Won with Martingale" : "You win", WinColor); }
                else if (dealerBust)                    { StartCoroutine(PlayWinAndChipRoutine(useCelebration: isMartingaleWin, playResetSound: isMartingaleWin));  RecordRoundOutcome(false, scoreDelta: +1); _playerWon = true; SetStatus(isMartingaleWin ? "Won with Martingale" : "You win", WinColor);  ApplyPayout(PayoutResult.Win,  winBet, refreshLabel: false); }
                else if (p > dealerScore)               { StartCoroutine(PlayWinAndChipRoutine(useCelebration: isMartingaleWin, playResetSound: isMartingaleWin));  RecordRoundOutcome(false, scoreDelta: +1); _playerWon = true; SetStatus(isMartingaleWin ? "Won with Martingale" : "You win", WinColor);  ApplyPayout(PayoutResult.Win,  winBet, refreshLabel: false); }
                else if (dealerScore > p)
                {
                    RecordRoundOutcome(true, lostAmount: stakedBet, scoreDelta: -1, lossCount: _doubleDownExtraBet > 0 ? 2 : 1);
                    SetStatus($"You lose", LoseColor);
                    ApplyPayout(PayoutResult.Lose, stakedBet);
                    yield return StartCoroutine(PlayLoseSoundAndWait());
                }
                else
                {
                    if (AlwaysLose)
                    {
                        // Always Lose: treat push as a loss.
                        RecordRoundOutcome(true, lostAmount: stakedBet, scoreDelta: -1, lossCount: _doubleDownExtraBet > 0 ? 2 : 1);
                        SetStatus("You lose", LoseColor);
                        ApplyPayout(PayoutResult.Lose, stakedBet);
                        yield return StartCoroutine(PlayLoseSoundAndWait());
                    }
                    else
                    {
                        PlayTieSound();
                        RecordRoundOutcome(false, scoreDelta: 0, isPush: true);
                        SetStatus("Push", PushColor);
                        ApplyPayout(PayoutResult.Push, stakedBet);
                    }
                }
            }

            yield return StartCoroutine(EndRound());
        }

        /// <summary>
        /// After a win in Martingale mode, resets the bet area to the stake from before Martingale began.
        /// Also clears <see cref="_savedBetBeforeAction"/> so that a chip click during RoundOver
        /// (which calls <see cref="PrepareForBetting"/>) cannot override the restored amount with
        /// a stale split or double-down value. Returns true when the bet was restored.
        /// </summary>
        private bool ApplyPendingWinBetRestore()
        {
            if (chipBetting == null)
                return false;

            if (_martingaleBetRestored)
                return true;

            if (!_martingaleWin)
                return false;

            int targetBet = ResolveTargetInitialBet();

            chipBetting.SetBet(targetBet, playSound: false);
            chipBetting.SnapshotBet();
            _betBeforeMartingale   = 0;
            _savedBetBeforeAction  = 0;
            _doubleDownExtraBet    = 0;
            _martingaleWin         = false;
            _playerWon             = false;
            _martingaleBetRestored = true;
            return true;
        }

        /// <summary>
        /// After a win following a double down, resets the bet area to the stake before the double.
        /// Returns true when the bet was restored (or was already restored this round).
        /// </summary>
        private bool ApplyDoubleDownWinBetRestore()
        {
            if (chipBetting == null)
                return false;

            if (_doubleDownBetRestored)
                return true;

            if (_doubleDownExtraBet <= 0 || _savedBetBeforeAction <= 0)
                return false;

            chipBetting.SetBet(ResolveTargetInitialBet(), playSound: false);
            chipBetting.SnapshotBet();
            _savedBetBeforeAction  = 0;
            _doubleDownExtraBet    = 0;
            _doubleDownBetRestored = true;
            return true;
        }

        /// <summary>
        /// After a normal win, resets the bet area to the player's chosen initial stake.
        /// Returns true when the bet was restored (or was already restored this round).
        /// </summary>
        private bool ApplyStandardWinBetRestore()
        {
            if (chipBetting == null)
                return false;

            if (_standardBetRestored)
                return true;

            if (!_playerWon)
                return false;

            chipBetting.SetBet(ResolveTargetInitialBet(), playSound: false);
            chipBetting.SnapshotBet();
            _savedBetBeforeAction = 0;
            _doubleDownExtraBet   = 0;
            _standardBetRestored  = true;
            return true;
        }

        /// <summary>Tries Martingale restore, then double-down restore, then the player's initial bet.</summary>
        private bool ApplyWinBetAreaRestore()
        {
            if (ApplyPendingWinBetRestore())
                return true;

            if (ApplyDoubleDownWinBetRestore())
                return true;

            return ApplyStandardWinBetRestore();
        }

        private IEnumerator EndRound()
        {
            _state = GameState.RoundOver;
            SetButtonState(dealEnabled: false, actionEnabled: false, splitEnabled: false);
            strategyTableUI?.ClearHighlight();

            yield return WaitForGameDelay(endRoundDelay);
            chipBetting?.ResetMaxBet();
            chipBetting?.ClampBetToMaxBet();

            if (_playerWon)
            {
                while (!_winPresentationComplete)
                    yield return null;

                ApplyWinBetAreaRestore();

                float chipRemaining = _winSoundEndTime - Time.time;
                if (chipRemaining > 0f)
                    yield return WaitForGameDelay(chipRemaining);
            }
            else
            {
                chipBetting?.RestoreBetFromSnapshot();
                if (!_inMartingaleMode || CurrentBet <= ResolveTargetInitialBet())
                    ApplySavedInitialBetToBetArea();
                else
                    SyncBetAreaToInitialBetIfNeeded();
            }

            // If fireworks or dealer natural-blackjack presentation is still playing, wait before
            // letting the player interact again.
            float remaining = _fireworksEndTime - Time.time;
            if (remaining > 0f)
                yield return WaitForGameDelay(remaining);

            remaining = _dealerNaturalBJEndTime - Time.time;
            if (remaining > 0f)
                yield return WaitForGameDelay(remaining);

            if (!_doubleBJSoundPlaying && !_dealerNaturalBJPlaying && _state == GameState.RoundOver)
                SetButtonState(dealEnabled: true, actionEnabled: false, splitEnabled: false);

            if (_inMartingaleMode)
                RefreshMartingaleModeLabel();

            // Show Martingale popup immediately when the threshold was just reached.
            if (_martingalePopupShown && !_inMartingaleMode && !_martingaleDeclined)
            {
                Debug.Log("[Martingale] EndRound: showing popup");
                _martingalePopupShown = false;
                ShowMartingalePopup();
                yield break;
            }

            Debug.Log($"[Martingale] EndRound: popup NOT shown. shown={_martingalePopupShown} inMode={_inMartingaleMode} declined={_martingaleDeclined}");

            // Auto-deal the next round when Martingale auto-play or table auto-play is active.
            if (_state == GameState.RoundOver)
            {
                if (_inMartingaleMode && _pendingMartingaleDouble && (menuController?.IsMartingaleAutoPlay ?? false))
                    OnDeal();
                else if (_autoPlayEnabled)
                {
                    yield return WaitForGameDelay(0.6f);
                    yield return new WaitUntil(() => !IsMenuOpen);
                    OnDeal();
                }
            }
            // State stays RoundOver — chip click or Deal press drives the next transition.
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Card Dealing
        // ──────────────────────────────────────────────────────────────────────────

        private IEnumerator DealCardTo(
            Hand hand, List<ICardDisplay> views, Transform area, bool faceUp)
        {
            // Pause all card dealing (initial deal and dealer hits) while the menu is open.
            yield return new WaitUntil(() => !IsMenuOpen);
            yield return WaitForGameDelay(dealDelay);

            CardData card = _deck.Draw();

            PlayGameSound(dealCardSound);

            ICardDisplay view = SpawnCardView(card, area, faceUp: SkipAutoplayDelays && faceUp);
            if (view == null)
                yield break;

            hand.AddCard(card);
            views.Add(view);

            if (faceUp && !SkipAutoplayDelays)
            {
                bool flipDone = false;
                view.Flip(toFaceUp: true, () => flipDone = true);
                yield return new WaitUntil(() => flipDone);
            }
        }

        private void StopDoubleDownLayout()
        {
            DestroyLegacyDoubleDownAnchors(playerCardArea);
            DestroyLegacyDoubleDownAnchors(splitCardArea);
        }

        private static void DestroyLegacyDoubleDownAnchors(Transform area)
        {
            if (area == null)
                return;

            Transform anchor = area.Find("DoubleDownAnchor");
            if (anchor == null)
                return;

            var areaRt = (RectTransform)area;
            for (int i = anchor.childCount - 1; i >= 0; i--)
                anchor.GetChild(i).SetParent(areaRt, true);

            UnityEngine.Object.Destroy(anchor.gameObject);
        }

        private static void DestroyCardViews(System.Collections.Generic.List<ICardDisplay> views)
        {
            for (int i = views.Count - 1; i >= 0; i--)
            {
                ICardDisplay view = views[i];
                if (view is MonoBehaviour behaviour && behaviour != null)
                    UnityEngine.Object.Destroy(behaviour.gameObject);
            }
            views.Clear();
        }

        private ICardDisplay SpawnCardView(CardData card, Transform area, bool faceUp)
        {
            GameObject prefab = ResolveCardPrefab();
            Transform parent = ResolveCardSpawnParent(area);

            if (prefab == null || parent == null)
            {
                Debug.LogError("BlackjackGame: card prefab or card area is not assigned.");
                return null;
            }

            GameObject go = Instantiate(prefab, parent, false);
            go.SetActive(true);
            go.name = "CardView";

            if (useWorldSpaceCards)
            {
                WorldCardRowLayout layout = parent.GetComponent<WorldCardRowLayout>();
                if (layout != null)
                {
                    if (area is RectTransform areaRt)
                        WorldCardAreaBootstrap.EnsureWorldArea(area, area.name + "_World");
                    layout.RefreshLayout();
                }
            }

            ICardDisplay view = go.GetComponent<ICardDisplay>();
            if (view == null)
            {
                Debug.LogError("BlackjackGame: card prefab is missing ICardDisplay (WorldCardView or CardView).");
                Destroy(go);
                return null;
            }

            if (useWorldSpaceCards && view is WorldCardView worldCard)
            {
                WorldCardRowLayout rowLayout = parent.GetComponent<WorldCardRowLayout>();
                if (rowLayout != null)
                    worldCard.SetCardWorldWidth(rowLayout.CardWorldWidth);
            }

            view.Setup(
                spriteRegistry.GetSprite(card),
                spriteRegistry.GetBackSprite(),
                faceUp);
            return view;
        }

        private GameObject ResolveCardPrefab()
        {
            if (useWorldSpaceCards && worldCardPrefab != null)
                return worldCardPrefab;
            return cardViewPrefab;
        }

        private Transform ResolveCardSpawnParent(Transform area)
        {
            if (!useWorldSpaceCards)
                return area;

            return WorldCardAreaBootstrap.EnsureWorldArea(area, area.name + "_World");
        }

        private static Transform GetTransform(ICardDisplay card)
        {
            return card is MonoBehaviour behaviour ? behaviour.transform : null;
        }

        private static RectTransform GetRectTransform(ICardDisplay card)
        {
            return card is MonoBehaviour behaviour ? behaviour.GetComponent<RectTransform>() : null;
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Hole Card
        // ──────────────────────────────────────────────────────────────────────────

        private bool ShouldDealerPeek()
        {
            if (_dealerHand.Count == 0)
                return false;

            Rank upcard = _dealerHand.Cards[0].Rank;
            return upcard == Rank.Ace || (int)upcard >= (int)Rank.Ten;
        }

        private IEnumerator DealerPeekHoleCardCheck()
        {
            if (SkipAutoplayDelays)
                yield break;

            if (_dealerHoleCardView == null || _dealerHoleCardView.IsFaceUp)
                yield break;
      

            if (_dealerHoleCardView is WorldCardView worldCard)
            {
                yield return worldCard.DealerPeekHoleCardAnimation();
                yield break;
            }

            DealerPeekAnimation legacyPeek = GetTransform(_dealerHoleCardView)?.GetComponent<DealerPeekAnimation>();
            if (legacyPeek != null)
                yield return legacyPeek.DealerPeekHoleCardAnimation();
        }

        private IEnumerator RevealDealerHoleForNaturalBlackjack()
        {
            yield return StartCoroutine(RevealHoleCard());
            UpdateScoreLabels(revealDealer: true);
        }

        private IEnumerator RevealHoleCard()
        {
            if (_dealerHoleCardView == null || _dealerHoleCardView.IsFaceUp)
                yield break;

            if (SkipAutoplayDelays)
            {
                _dealerHoleCardView.SetFaceUpImmediate(true);
                yield break;
            }

            bool done = false;
            _dealerHoleCardView.Flip(toFaceUp: true, () => done = true);
            yield return new WaitUntil(() => done);
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Glow Effect
        // ──────────────────────────────────────────────────────────────────────────

        private static bool IsNaturalBlackjack(Hand hand)
        {
            if (hand.Cards.Count != 2) return false;

            bool hasAce = false;
            bool hasTenValue = false;
            foreach (CardData card in hand.Cards)
            {
                if (card.Rank == Rank.Ace) hasAce = true;
                else if (card.BlackjackValue == 10) hasTenValue = true;
            }

            return hasAce && hasTenValue;
        }

        private bool IsDealerNaturalBlackjackLoss(Hand playerHand) =>
            IsNaturalBlackjack(_dealerHand) && !IsNaturalBlackjack(playerHand);

        private IEnumerator ResolveDealerNaturalBlackjackRound()
        {
            yield return StartCoroutine(RevealDealerHoleForNaturalBlackjack());

            int stakedBet = TotalStakedBet;

            if (_isSplitRound)
            {
                var    results   = new List<string>();
                int    lostAmount = 0;
                Hand[] hands     = { _playerHand, _splitHand };
                string[] labels  = { "Hand 1", "Hand 2" };

                for (int i = 0; i < hands.Length; i++)
                {
                    int handBet = GetSplitHandBet(i);
                    if (IsNaturalBlackjack(hands[i]))
                    {
                        results.Add(ColorizeText($"{labels[i]}: Push", PushColor));
                        ApplyPayout(PayoutResult.Push, handBet);
                    }
                    else
                    {
                        results.Add(ColorizeText($"{labels[i]}: Lose", LoseColor));
                        lostAmount += handBet;
                        ApplyPayout(PayoutResult.Lose, handBet);
                    }
                }

                RecordRoundOutcome(
                    isLoss: lostAmount > 0,
                    lostAmount: lostAmount,
                    scoreDelta: lostAmount > 0 ? -1 : 0,
                    isPush: lostAmount == 0);
                SetStatus(string.Join("  |  ", results));
                yield return StartCoroutine(ApplyDealerNaturalBlackjackPresentationRoutine());
            }
            else if (IsNaturalBlackjack(_playerHand))
            {
                if (AlwaysLose)
                {
                    yield return StartCoroutine(ApplyDealerNaturalBlackjackLossRoutine(stakedBet));
                }
                else
                {
                    StartCoroutine(PlayDoubleBJSoundRoutine());
                    RecordRoundOutcome(false, scoreDelta: 0, isPush: true);
                    SetStatus("Push", PushColor);
                    ApplyPayout(PayoutResult.Push, stakedBet);
                }
            }
            else
            {
                yield return StartCoroutine(ApplyDealerNaturalBlackjackLossRoutine(stakedBet));
            }

            yield return StartCoroutine(EndRound());
        }

        /// <summary>
        /// Reveals the hole if needed, plays dealer card bloom + lose/damnit sounds, records the loss, and waits for the presentation to finish.
        /// </summary>
        private IEnumerator ApplyDealerNaturalBlackjackLossRoutine(int lostAmount, bool revealHole = false)
        {
            _dealerNaturalBJPlaying = true;

            if (revealHole)
                yield return StartCoroutine(RevealDealerHoleForNaturalBlackjack());

            ApplyBlackjackGlow(_dealerCardViews);
            float glowDuration = PlayDealerBlackjackLoseSound();
            _dealerNaturalBJEndTime = Time.time + glowDuration;

            RecordRoundOutcome(true, lostAmount: lostAmount, scoreDelta: -1);
            SetStatus("You lose", LoseColor);
            ApplyPayout(PayoutResult.Lose, lostAmount);

            yield return WaitForGameDelay(glowDuration);
            StopBlackjackGlow(_dealerCardViews);
            _dealerNaturalBJPlaying = false;
            _dealerNaturalBJEndTime = 0f;
        }

        /// <summary>Bloom + lose/damnit sounds only; caller handles payout/outcome.</summary>
        private IEnumerator ApplyDealerNaturalBlackjackPresentationRoutine()
        {
            _dealerNaturalBJPlaying = true;

            ApplyBlackjackGlow(_dealerCardViews);
            float glowDuration = PlayDealerBlackjackLoseSound();
            _dealerNaturalBJEndTime = Time.time + glowDuration;

            yield return WaitForGameDelay(glowDuration);
            StopBlackjackGlow(_dealerCardViews);
            _dealerNaturalBJPlaying = false;
            _dealerNaturalBJEndTime = 0f;
        }

        private void ApplyBlackjackGlow() => ApplyBlackjackGlow(_playerCardViews);

        private void ApplyBlackjackGlow(IReadOnlyList<ICardDisplay> cardViews)
        {
            if (SkipAutoplayDelays)
                return;

            foreach (ICardDisplay v in cardViews)
                v?.StartGlowPulse();
        }

        private void StopBlackjackGlow(IReadOnlyList<ICardDisplay> cardViews)
        {
            foreach (ICardDisplay v in cardViews)
                v?.StopGlowPulse();
        }

        /// <summary>Stops fireworks, all audio, and card glow pulses from a blackjack celebration.</summary>
        private void StopBlackjackCelebration()
        {
            _dealerNaturalBJPlaying = false;
            _dealerNaturalBJEndTime = 0f;

            if (_fireworksInstance != null)
            {
                Destroy(_fireworksInstance);
                _fireworksInstance = null;
            }
            _fireworksEndTime = 0f;

            if (audioSource != null)
                audioSource.Stop();

            foreach (ICardDisplay v in _playerCardViews)
                v?.StopGlowPulse();
            foreach (ICardDisplay v in _dealerCardViews)
                v?.StopGlowPulse();
        }

        // ──────────────────────────────────────────────────────────────────────────
        // UI Helpers
        // ──────────────────────────────────────────────────────────────────────────

        private void RefreshStrategyHighlight()
        {
            if (strategyTableUI == null || !showStrategyTable)
                return;

            Hand hand = _isSplitRound ? ActiveHand : _playerHand;
            strategyTableUI.HighlightRecommendation(
                hand,
                _dealerUpcardSnapshot,
                canSplit: CanSplit(),
                canDouble: CanDoubleDown(),
                canSurrender: CanSurrender());
        }

        private void UpdateScoreLabels(bool revealDealer)
        {
            SetScoreLabelsVisible(true);

            if (_isSplitRound)
            {
                int p1 = _playerHand.BestValue();
                int p2 = _splitHand.BestValue();
                string s1 = p1 > BlackjackValue ? "Bust" : p1.ToString();
                string s2 = p2 > BlackjackValue ? "Bust" : p2.ToString();

                PositionLabelLeftOfArea(playerScoreLabel, playerCardArea);
                playerScoreLabel.text = s1;

                PositionLabelLeftOfArea(_splitScoreLabel, splitCardArea);
                _splitScoreLabel.text = s2;

                if (_state == GameState.PlayerTurn)
                    UpdateSplitScorePulse();
                else
                    StopAllScorePulses();
            }
            else
            {
                ResetPlayerScoreLabelPosition();
                StopAllScorePulses();
                playerScoreLabel.text = $"{_playerHand.BestValue()}";
            }

            if (revealDealer)
                dealerScoreLabel.text = $"{_dealerHand.BestValue()}";
            else
            {
                int visibleValue = _dealerHand.Cards.Count > 0
                    ? _dealerHand.Cards[0].BlackjackValue
                    : 0;
                dealerScoreLabel.text = $"{visibleValue}";
            }
        }

        /// <summary>
        /// Pins the status label between dealer and player card areas: left edge aligned with
        /// the dealer area, rendered text bottom on a fixed line in the gap.
        /// </summary>
        private void AlignStatusLabelToCardArea()
        {
            if (statusLabel == null || dealerCardArea == null || playerCardArea == null)
                return;

            RectTransform dealerRT = dealerCardArea as RectTransform
                                     ?? dealerCardArea.GetComponent<RectTransform>();
            RectTransform playerRT = playerCardArea as RectTransform
                                     ?? playerCardArea.GetComponent<RectTransform>();
            if (dealerRT == null || playerRT == null)
                return;

            RectTransform statusRT = statusLabel.rectTransform;
            Transform parent = statusRT.parent;
            if (parent == null)
                return;

            statusLabel.useMaxVisibleDescender = false;
            statusLabel.horizontalAlignment = HorizontalAlignmentOptions.Left;
            statusLabel.verticalAlignment   = VerticalAlignmentOptions.Bottom;

            statusRT.anchorMin = new Vector2(0.5f, 0.5f);
            statusRT.anchorMax = new Vector2(0.5f, 0.5f);
            statusRT.pivot = new Vector2(0f, 0f);

            Vector2 size = statusRT.sizeDelta;
            size.y = StatusLabelHeight;
            statusRT.sizeDelta = size;

            float targetBottomY = GetStatusLabelBottomY(dealerRT, playerRT, parent);
            float leftEdge      = GetRectLeftXInParent(dealerRT, parent);

            statusRT.anchoredPosition = new Vector2(leftEdge, targetBottomY);
            SnapStatusLabelRenderedBottomTo(parent, targetBottomY);
        }

        private float GetStatusGapMidY(RectTransform dealerRT, RectTransform playerRT, Transform parent)
        {
            float dealerBottom = GetRectEdgeYInParent(dealerRT, parent, top: false);
            float playerTop    = GetRectEdgeYInParent(playerRT, parent, top: true);
            return (dealerBottom + playerTop) * 0.5f + statusLabelVerticalOffset;
        }

        private float GetStatusLabelBottomY(RectTransform dealerRT, RectTransform playerRT, Transform parent) =>
            GetStatusGapMidY(dealerRT, playerRT, parent) - statusLabelBottomBelowGapCenter;

        /// <summary>Shifts the status label so TMP rendered bounds share the same bottom Y.</summary>
        private void SnapStatusLabelRenderedBottomTo(Transform parent, float targetBottomY)
        {
            if (statusLabel == null || parent == null || string.IsNullOrEmpty(statusLabel.text))
                return;

            Canvas.ForceUpdateCanvases();
            statusLabel.ForceMeshUpdate();

            Bounds bounds = statusLabel.textBounds;
            if (bounds.size.sqrMagnitude <= 0f)
                return;

            Vector3 localBottom = new Vector3(bounds.min.x, bounds.min.y, 0f);
            float currentBottom = parent.InverseTransformPoint(statusLabel.transform.TransformPoint(localBottom)).y;
            float delta = targetBottomY - currentBottom;

            RectTransform statusRT = statusLabel.rectTransform;
            statusRT.anchoredPosition += new Vector2(0f, delta);
        }

        private static float GetRectEdgeYInParent(RectTransform rt, Transform parent, bool top)
        {
            Vector3[] corners = new Vector3[4];
            rt.GetWorldCorners(corners);

            float edge = top ? float.NegativeInfinity : float.PositiveInfinity;
            for (int i = 0; i < corners.Length; i++)
            {
                float y = parent.InverseTransformPoint(corners[i]).y;
                edge = top ? Mathf.Max(edge, y) : Mathf.Min(edge, y);
            }

            return edge;
        }

        private static float GetRectLeftXInParent(RectTransform rt, Transform parent)
        {
            Vector3[] corners = new Vector3[4];
            rt.GetWorldCorners(corners);

            float left = float.PositiveInfinity;
            for (int i = 0; i < corners.Length; i++)
                left = Mathf.Min(left, parent.InverseTransformPoint(corners[i]).x);

            return left;
        }

        private void ApplyStatusLabelAlignment()
        {
            if (statusLabel == null) return;

            statusLabel.horizontalAlignment = HorizontalAlignmentOptions.Left;
            statusLabel.verticalAlignment   = VerticalAlignmentOptions.Bottom;
            RefreshStatusLabelVerticalPosition();
        }

        private void RefreshStatusLabelVerticalPosition()
        {
            if (statusLabel == null || dealerCardArea == null || playerCardArea == null)
                return;

            RectTransform dealerRT = dealerCardArea as RectTransform
                                     ?? dealerCardArea.GetComponent<RectTransform>();
            RectTransform playerRT = playerCardArea as RectTransform
                                     ?? playerCardArea.GetComponent<RectTransform>();
            RectTransform statusRT = statusLabel.rectTransform;
            Transform parent = statusRT != null ? statusRT.parent : null;
            if (dealerRT == null || playerRT == null || parent == null)
                return;

            float targetBottomY = GetStatusLabelBottomY(dealerRT, playerRT, parent);
            float leftEdge      = GetRectLeftXInParent(dealerRT, parent);

            statusRT.anchoredPosition = new Vector2(leftEdge, targetBottomY);
            SnapStatusLabelRenderedBottomTo(parent, targetBottomY);
        }

        /// <summary>Sets the status label text and resets its color to the default.</summary>
        private void SetStatus(string message)
        {
            if (ShouldBlockStatusUpdate(message)) return;

            statusLabel.text = message;
            statusLabel.color = _defaultStatusColor;
            ApplyStatusLabelAlignment();
        }

        /// <summary>Sets the status label text with a specific color.</summary>
        private void SetStatus(string message, Color color)
        {
            if (ShouldBlockStatusUpdate(message)) return;

            statusLabel.text = message;
            statusLabel.color = color;
            ApplyStatusLabelAlignment();
        }

        private bool ShouldBlockStatusUpdate(string message)
        {
            if (IsLimitPulsing)
                return message != BetLimitStatusMessage && message != string.Empty;

            if (_betLimitStatusLocked)
                return message != BetLimitStatusMessage;

            return false;
        }

        /// <summary>Wraps text in TMP rich text color tags.</summary>
        private static string ColorizeText(string text, Color color)
        {
            string hex = ColorUtility.ToHtmlStringRGBA(color);
            return $"<color=#{hex}>{text}</color>";
        }

        /// <summary>Shows or hides all score labels including the split label.</summary>
        private void SetScoreLabelsVisible(bool visible)
        {
            playerScoreLabel.gameObject.SetActive(visible);
            dealerScoreLabel.gameObject.SetActive(visible);

            if (_splitScoreLabel != null)
                _splitScoreLabel.gameObject.SetActive(visible && _isSplitRound);
        }

        /// <summary>Returns true when the given hand should automatically stand (hard 17+ or soft 19+).</summary>
        private bool ShouldAutoStand(Hand hand)
        {
            int score = hand.BestValue();
            if (hand.IsSoft())
                return score >= AutoStandSoft;
            return score >= AutoStandHard;
        }

        /// <summary>Creates the split score label by cloning the player score label and adds pulse components.</summary>
        private void InitSplitScoreLabel()
        {
            RectTransform playerScoreRT = playerScoreLabel.GetComponent<RectTransform>();
            _defaultPlayerScorePosition = playerScoreRT.anchoredPosition;

            // Instantiate with the source active so TMP's internal material/font
            // state is fully initialized on the clone before we deactivate it.
            // Deactivating immediately after prevents any Canvas rebuild pass from
            // running on the clone before it is intentionally shown.
            GameObject splitLabelObj = Instantiate(playerScoreLabel.gameObject, playerScoreLabel.transform.parent);
            splitLabelObj.name = "SplitScoreLabel";

            _splitScoreLabel = splitLabelObj.GetComponent<TextMeshProUGUI>();
            _splitScoreLabel.text = "";

            splitLabelObj.SetActive(false);

            _playerScorePulse = playerScoreLabel.gameObject.AddComponent<ScoreLabelPulse>();
            _splitScorePulse  = splitLabelObj.AddComponent<ScoreLabelPulse>();
        }

        /// <summary>Positions a score label to the left of the given card area, vertically centered.</summary>
        private void PositionLabelLeftOfArea(TextMeshProUGUI label, Transform cardArea)
        {
            RectTransform labelRT = label.GetComponent<RectTransform>();
            RectTransform areaRT  = cardArea.GetComponent<RectTransform>();

            // areaCenterY is the Y of the card area's centre in parent space.
            // The label pivot is (0.5, 0.5), so anchoredPosition.y represents the label's
            // own centre — assign areaCenterY directly to align them.
            float areaCenterY = areaRT.anchoredPosition.y + areaRT.sizeDelta.y * 0.5f;

            labelRT.anchorMin = areaRT.anchorMin;
            labelRT.anchorMax = areaRT.anchorMax;
            labelRT.anchoredPosition = new Vector2(
                _defaultPlayerScorePosition.x,
                areaCenterY
            );
        }

        /// <summary>Resets the player score label to its original position.</summary>
        private void ResetPlayerScoreLabelPosition()
        {
            RectTransform playerScoreRT = playerScoreLabel.GetComponent<RectTransform>();
            playerScoreRT.anchoredPosition = _defaultPlayerScorePosition;
        }

        /// <summary>Pulses the active hand's score label and dims the inactive one.</summary>
        private void UpdateSplitScorePulse()
        {
            if (_activeHandIndex == 0)
            {
                _playerScorePulse.StartPulse();
                _splitScorePulse.StopPulse();
            }
            else
            {
                _playerScorePulse.StopPulse();
                _splitScorePulse.StartPulse();
            }
        }

        /// <summary>Stops all score label pulses and resets their alpha.</summary>
        private void StopAllScorePulses()
        {
            _playerScorePulse.StopPulse();
            _splitScorePulse.StopPulse();
        }

        /// <summary>Plays the win sound if both clip and source are assigned.</summary>
        private void PlayWinSound() => PlayGameSound(winSound);

        /// <summary>
        /// Plays the win audio and card glow.
        /// When the current round was entered as a Delayed Martingale, triggers the full
        /// natural-blackjack celebration instead of the regular win sound, then plays
        /// <see cref="resetSound"/> once the celebration has finished.
        /// </summary>
        private void PlayWinRoutine()
        {
            if (_inMartingaleMode)
            {
                ApplyBlackjackGlow();
                float celebrationDuration = PlayNaturalBlackjackSound();
                SpawnFireworks(celebrationDuration);
                StartCoroutine(PlayResetSoundAfterDelay(celebrationDuration));
            }
            else
            {
                PlayWinSound();
            }
        }

        /// <summary>
        /// Plays win audio, waits for it to finish, then updates balance (immediately or via deferred payout),
        /// restores the bet area, and plays chip sound.
        /// Pass <paramref name="useCelebration"/> for Martingale wins and natural blackjacks.
        /// Pass <paramref name="playResetSound"/> only for Martingale wins.
        /// When <paramref name="deferPayout"/> is true, <see cref="ApplyPayout"/> runs after celebration audio/fireworks.
        /// </summary>
        private IEnumerator PlayWinAndChipRoutine(
            bool useCelebration = false,
            bool playResetSound = false,
            bool playChipSound = true,
            bool deferPayout = false,
            PayoutResult deferredPayout = PayoutResult.Win,
            int deferredBet = 0)
        {
            _winPresentationComplete = false;

            if (deferPayout && deferredBet > 0)
                RegisterDeferredWinPayout(deferredPayout, deferredBet);

            if (SkipAutoplayDelays)
                ApplyDeferredWinPayoutIfPending();

            if (!SkipAutoplayDelays)
            {
                if (useCelebration)
                {
                    ApplyBlackjackGlow();
                    float celebrationDuration = PlayNaturalBlackjackSound();
                    SpawnFireworks(celebrationDuration);
                    if (playResetSound)
                        StartCoroutine(PlayResetSoundAfterDelay(celebrationDuration));

                    if (celebrationDuration > 0f)
                        yield return WaitForGameDelay(celebrationDuration);
                    else
                        yield return null;
                }
                else
                {
                    PlayWinSound();
                    if (winSound.Length > 0f)
                        yield return WaitForGameDelay(winSound.Length);
                    else
                        yield return null;
                }
            }
            else
            {
                yield return null;
            }

            if (playChipSound)
                PlayGameSound(chipSound);

            ApplyWinBetAreaRestore();

            if (playChipSound && chipSound.Length > 0f && !SkipAutoplayDelays)
                yield return WaitForGameDelay(chipSound.Length);

            if (deferPayout && deferredBet > 0)
                ApplyDeferredWinPayoutIfPending();
            else
                RefreshMoneyLabel();

            _winSoundEndTime = Time.time;
            _winPresentationComplete = true;
        }
        private IEnumerator PlayResetSoundAfterDelay(float delay)
        {
            // Always yield at least one frame so RecordRoundOutcome can set _martingaleWin before we read it.
            if (delay > 0f)
                yield return WaitForGameDelay(delay);
            else
                yield return null;

            PlayGameSound(resetSound);
        }

        /// <summary>
        /// Instantiates <see cref="fireworksPrefab"/> at the world origin and auto-destroys it
        /// after <see cref="fireworksDuration"/> seconds.
        /// </summary>
        private void SpawnFireworks(float duration)
        {
            if (fireworksPrefab == null) return;
            if (SkipAutoplayDelays) return;
            Vector3 spawnPosition = Vector3.zero;
            if (playerCardArea is RectTransform cardRect)
            {
                Vector3[] corners = new Vector3[4];
                cardRect.GetWorldCorners(corners);
                // corners: 0=bottom-left, 1=top-left, 2=top-right, 3=bottom-right
                // Cards are left-aligned in the area, so bias toward the left side
                float x = Mathf.Lerp(corners[0].x, corners[2].x, 0.2f);
                float y = (corners[0].y + corners[2].y) * 0.5f;
                spawnPosition = new Vector3(x, y, corners[0].z);
            }
            else if (playerCardArea != null)
            {
                spawnPosition = playerCardArea.position;
            }
            _fireworksInstance = Instantiate(fireworksPrefab, spawnPosition, Quaternion.identity);
            float actualDuration = duration > 0f ? duration : fireworksDuration;
            Destroy(_fireworksInstance, actualDuration);
            _fireworksEndTime = Time.time + actualDuration;
        }

        /// <summary>Plays the natural blackjack sound if assigned, otherwise falls back to win sound.
        /// Also plays the yuhu sound simultaneously. Stops all player card glow pulses once the longest clip finishes.
        /// Returns the duration of the longest clip played, so callers can chain additional sounds.</summary>
        private float PlayNaturalBlackjackSound()
        {
            if (SkipAutoplayDelays)
                return 0f;

            SoundEntry primary = naturalBlackjackSound.HasClip ? naturalBlackjackSound : winSound;
            float longestDuration = 0f;

            if (primary.HasClip && audioSource != null)
            {
                PlayGameSound(primary);
                longestDuration = primary.Length;
            }

            if (yuhuSound.HasClip && audioSource != null)
            {
                PlayGameSound(yuhuSound);
                if (yuhuSound.Length > longestDuration)
                    longestDuration = yuhuSound.Length;
            }

            if (longestDuration > 0f)
                StartCoroutine(StopGlowAfterClip(longestDuration, _playerCardViews));

            return longestDuration;
        }

        private IEnumerator StopGlowAfterClip(float duration, IReadOnlyList<ICardDisplay> cardViews)
        {
            yield return WaitForGameDelay(duration);
            StopBlackjackGlow(cardViews);
        }

    /// <summary>Plays the lose sound if both clip and source are assigned.</summary>
      private void PlayCardSlideSound() => PlayGameSound(cardSlideSound);

        private void PlayLoseSound() => PlayGameSound(loseSound);

        /// <summary>Plays the lose sound and waits for it to finish before returning.</summary>
        private IEnumerator PlayLoseSoundAndWait()
        {
            if (SkipAutoplayDelays)
                yield break;

            PlayGameSound(loseSound);
            if (loseSound.HasClip)
                yield return WaitForGameDelay(loseSound.Length);
        }

        /// <summary>Plays a random non-repeating male reaction sound when the dealer has a natural blackjack.</summary>
        private float PlayDealerBlackjackLoseSound()
        {
            if (SkipAutoplayDelays)
                return 0f;

            float longestDuration = 0f;

            if (damnitSound.HasClip && audioSource != null)
            {
                PlayGameSound(damnitSound);
                if (damnitSound.Length > longestDuration)
                    longestDuration = damnitSound.Length;
            }

            SoundEntry? reaction = PickNoDuplicateReactionSound();
            if (reaction.HasValue && reaction.Value.HasClip && audioSource != null)
            {
                PlayGameSound(reaction.Value);
                if (reaction.Value.Length > longestDuration)
                    longestDuration = reaction.Value.Length;
            }

            return longestDuration > 0f ? longestDuration : 3f;
        }

        /// <summary>Picks a random male reaction sound, never repeating the last played clip.</summary>
        private SoundEntry? PickNoDuplicateReactionSound()
        {
            if (_maleSpeechPool == null || _maleSpeechPool.Length == 0)
                return null;

            int index = _maleSpeechPool.Length > 1
                ? (UnityEngine.Random.Range(0, _maleSpeechPool.Length - 1) + _lastDealerNaturalBJReactionIndex + 1) % _maleSpeechPool.Length
                : 0;

            SoundEntry chosen = _maleSpeechPool[index];
            if (!chosen.HasClip)
                return null;

            _lastDealerNaturalBJReactionIndex = index;
            return chosen;
        }
        /// <summary>Plays the tie sound if both clip and source are assigned.</summary>
        private void PlayTieSound() => PlayGameSound(tieSound);

        /// <summary>
        /// Triggered on a double natural blackjack push. Plays the tie sound once,
        /// then plays one randomly chosen sound from cheaterSound, damnitSound, and hmhSound.
        /// The same sound is never played twice in a row. Only assigned clips are included.
        /// The deal button stays locked until the random sound has finished playing.
        /// </summary>
        private IEnumerator PlayDoubleBJSoundRoutine()
        {
            _doubleBJSoundPlaying = true;

            if (SkipAutoplayDelays)
            {
                _doubleBJSoundPlaying = false;
                yield break;
            }

            PlayGameSound(tieSound);
            yield return WaitForGameDelay(tieSound.Length);

            List<SoundEntry> pool = new List<SoundEntry>();
            if (cheaterSound.HasClip) pool.Add(cheaterSound);
            if (damnitSound.HasClip)  pool.Add(damnitSound);
            if (hmhSound.HasClip)     pool.Add(hmhSound);

            if (_lastDoubleBJSound.HasValue && pool.Count > 1)
                pool.RemoveAll(s => s.clip == _lastDoubleBJSound.Value.clip);

            if (pool.Count == 0)
            {
                _doubleBJSoundPlaying = false;
                if (_state == GameState.RoundOver)
                    SetButtonState(dealEnabled: true, actionEnabled: false, splitEnabled: false);
                yield break;
            }

            SoundEntry chosen = pool[UnityEngine.Random.Range(0, pool.Count)];
            _lastDoubleBJSound = chosen;

            PlayGameSound(chosen);
            yield return WaitForGameDelay(chosen.Length);
            _doubleBJSoundPlaying = false;

            if (_state == GameState.RoundOver)
                SetButtonState(dealEnabled: true, actionEnabled: false, splitEnabled: false);
        }

        private void SetButtonState(bool dealEnabled, bool actionEnabled, bool splitEnabled, bool doubleDownEnabled = false, bool surrenderEnabled = false)
        {
            if (SkipAutoplayDelays)
            {
                SetButtonRowVisible(false);
                return;
            }

            SetButtonRowVisible(true);
            ApplyButtonVisibility(dealEnabled, actionEnabled, splitEnabled, doubleDownEnabled, surrenderEnabled);
        }

        private GameObject ResolveButtonRow()
        {
            if (buttonRow != null)
                return buttonRow;

            if (dealButton != null)
                buttonRow = dealButton.transform.parent.gameObject;

            return buttonRow;
        }

        private void SetButtonRowVisible(bool visible)
        {
            GameObject row = ResolveButtonRow();
            if (row != null)
                row.SetActive(visible);
        }

        /// <summary>Hides ButtonRow during max-speed autoplay; restores buttons when it ends.</summary>
        private void UpdateButtonRowForAutoplay()
        {
            if (SkipAutoplayDelays)
            {
                SetButtonRowVisible(false);
                return;
            }

            SetButtonRowVisible(true);
            ApplyButtonStateForCurrentGameState();
        }

        private void ApplyButtonStateForCurrentGameState()
        {
            switch (_state)
            {
                case GameState.Idle:
                case GameState.RoundOver:
                    ApplyButtonVisibility(dealEnabled: true, actionEnabled: false, splitEnabled: false);
                    break;
                case GameState.PlayerTurn:
                    ApplyButtonVisibility(
                        dealEnabled: false,
                        actionEnabled: true,
                        splitEnabled: CanSplit(),
                        doubleDownEnabled: CanDoubleDown(),
                        surrenderEnabled: CanSurrender());
                    break;
                case GameState.DealerTurn:
                    ApplyButtonVisibility(dealEnabled: false, actionEnabled: false, splitEnabled: false);
                    break;
            }
        }

        private void ApplyButtonVisibility(
            bool dealEnabled,
            bool actionEnabled,
            bool splitEnabled,
            bool doubleDownEnabled = false,
            bool surrenderEnabled = false)
        {
            dealButton.interactable = dealEnabled;
            dealButton.gameObject.SetActive(dealEnabled);

            hitButton.interactable = actionEnabled;
            hitButton.gameObject.SetActive(actionEnabled);

            standButton.interactable = actionEnabled;
            standButton.gameObject.SetActive(actionEnabled);

            if (surrenderButton != null)
            {
                surrenderButton.interactable = surrenderEnabled;
                surrenderButton.gameObject.SetActive(surrenderEnabled);
            }

            if (splitButton != null)
            {
                splitButton.interactable = splitEnabled;
                splitButton.gameObject.SetActive(splitEnabled);

                if (splitEnabled && splitAvailableSprite != null)
                {
                    Image splitImage = splitButton.GetComponent<Image>();
                    if (splitImage != null)
                        splitImage.sprite = splitAvailableSprite;
                }
            }

            if (doubleDownButton != null)
            {
                doubleDownButton.interactable = doubleDownEnabled;
                doubleDownButton.gameObject.SetActive(doubleDownEnabled);
            }
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Table Clear
        // ──────────────────────────────────────────────────────────────────────────

        private void ClearTable()
        {
            StopDoubleDownLayout();

            _playerHand.Clear();
            _splitHand.Clear();
            _dealerHand.Clear();
            _dealerHoleCardView = null;
            _isSplitRound    = false;
            _activeHandIndex = 0;
            StopAllScorePulses();
            ResetPlayerScoreLabelPosition();
            SetScoreLabelsVisible(false);

            DestroyCardViews(_playerCardViews);
            DestroyCardViews(_splitCardViews);
            DestroyCardViews(_dealerCardViews);
        }
    }
}
