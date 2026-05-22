using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
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

        [Header("Buttons")]
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

        [Header("Score Labels")]
        [SerializeField] private TextMeshProUGUI playerScoreLabel;
        [SerializeField] private TextMeshProUGUI dealerScoreLabel;

        [Header("Status")]
        [SerializeField] private TextMeshProUGUI statusLabel;
        [SerializeField] private TextMeshProUGUI streakLabel;

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

    [SerializeField] private SoundEntry cardSlideSound;
    [SerializeField] private SoundEntry cheaterSound;
    [SerializeField] private SoundEntry chipSound;
    [SerializeField] private SoundEntry damnitSound;
    [SerializeField] private SoundEntry ddSound;
    [SerializeField] private SoundEntry hmhSound;
    [SerializeField] private SoundEntry dealCardSound;
    [SerializeField] private SoundEntry exitSound;
    [SerializeField] private SoundEntry knockSound;
    [SerializeField] private SoundEntry loseSound;
    [SerializeField] private SoundEntry naturalBlackjackSound;
    [SerializeField] private SoundEntry startupSound;
    [SerializeField] private SoundEntry resetSound;
    [SerializeField] private SoundEntry surrenderSound;
    [SerializeField] private SoundEntry tieSound;
    [SerializeField] private SoundEntry winSound;
    [SerializeField] private SoundEntry yuhuSound;

    private SoundEntry? _lastDoubleBJSound;
    private bool _doubleBJSoundPlaying;

        [Header("Timing")]

        [Tooltip("dealDelay ist set to 0.45 in code, you can not change it in the inspector!")]
        [SerializeField] private float dealDelay          = 0.45f; //default is 0.45
        [SerializeField] private float dealerPauseDelay   = 0.7f;
        [SerializeField] private float endRoundDelay      = 3.0f;
        [SerializeField] private float newRoundPause      = 0.5f;

        // ──────────────────────────────────────────────────────────────────────────
        // Constants mark auto
        // ──────────────────────────────────────────────────────────────────────────

        private const int AutoStandHard      = 17;
        private const int AutoStandSoft      = 19;
        private const int AutoHitMaxScore    = 0; //disable
        private const int DealerSoft17       = 17;
        private const int BlackjackValue     = 21;

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

        private readonly List<CardView> _playerCardViews = new();
        private readonly List<CardView> _splitCardViews  = new();
        private readonly List<CardView> _dealerCardViews = new();

        private CardView _dealerHoleCardView;

        private bool _forcePlayerBlackjack;
        private bool _forceBothBlackjack;
        private bool _forceSplitHand;
        private bool _forceDoubleDownTest;
        private bool _isSplitRound;
        private int  _activeHandIndex; // 0 = player, 1 = split

        private int _doubleDownExtraBet; // extra bet deducted when doubling down
        private int _savedBetBeforeAction; // bet amount before split/double-down, restored next round
        private int _betBeforeMartingale;  // bet the player had before entering Martingale mode, restored on win

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
        private bool _martingalePopupShown;
        // True when the player is in active Martingale mode and lost the last round — bet should be doubled on next betting screen.
        private bool _pendingMartingaleDouble;
        // Tracks the current Martingale chip addition: smallest chip on first confirm, doubled after each loss.
        private int _martingaleChipValue;

        // Running score: +1 per win, -1 per loss, 0 for push or surrender.
        private int _playerScore;

        private const int DelayedMartingaleThreshold = 4;

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

        private Hand           ActiveHand  => _activeHandIndex == 0 ? _playerHand  : _splitHand;
        private List<CardView> ActiveViews => _activeHandIndex == 0 ? _playerCardViews : _splitCardViews;

        private enum GameState { Idle, PlayerTurn, DealerTurn, RoundOver }
        private GameState _state = GameState.Idle;

        /// <summary>True when the player is allowed to place or remove bets (before a round begins).</summary>
        public bool IsBettingAllowed => _state == GameState.Idle;

        /// <summary>True when the current round has ended and the table is showing results.</summary>
        public bool IsRoundOver => _state == GameState.RoundOver;

        /// <summary>True while the developer menu is open. Used by <see cref="ChipBetting"/> to suppress chip input.</summary>
        public bool IsMenuOpen => menuController != null && menuController.IsMenuOpen;

        /// <summary>Closes the menu panel. Used by <see cref="ChipBetting"/> when a bet action is taken during the betting phase.</summary>
        public void CloseMenu() => menuController?.CloseMenu();

        /// <summary>When true, the player always loses the round regardless of card values. Used for Martingale testing.</summary>
        public bool AlwaysLose { get; set; }

        /// <summary>Fired when the game automatically disables "Always Lose" upon entering Martingale mode.</summary>
        public event System.Action OnAlwaysLoseDisabled;

        /// <summary>
        /// Resets the game to its initial state: sets the player's money to zero,
        /// clears all cards and the bet area, and returns to the Idle state.
        /// </summary>
        public void ResetGame()
        {
            StopAllCoroutines();
            _doubleBJSoundPlaying = false;

            menuController?.CloseMenu();

            StopBlackjackCelebration();
            martingalePopup?.Hide();

            foreach (CardView v in _playerCardViews) if (v != null) Destroy(v.gameObject);
            _playerCardViews.Clear();

            foreach (CardView v in _splitCardViews) if (v != null) Destroy(v.gameObject);
            _splitCardViews.Clear();

            foreach (CardView v in _dealerCardViews) if (v != null) Destroy(v.gameObject);
            _dealerCardViews.Clear();

            _playerHand.Clear();
            _splitHand.Clear();
            _dealerHand.Clear();
            _dealerHoleCardView   = null;
            _isSplitRound         = false;
            _activeHandIndex      = 0;
            _savedBetBeforeAction = 0;
            _betBeforeMartingale  = 0;

            chipBetting?.ClearBetArea();

            _playerMoney = 0;
            RefreshMoneyLabel();

            _consecutiveLosses        = 0;
            _totalLosses              = 0;
            _totalAmountLost          = 0;
            _playerScore              = 0;
            _lastRoundBet             = 0;
            _martingaleWin            = false;
            _martingalePopupShown     = false;
            _pendingMartingaleDouble  = false;
            _martingaleChipValue      = 0;
            RefreshStreakLabel();

            StopAllScorePulses();
            ResetPlayerScoreLabelPosition();
            SetScoreLabelsVisible(false);

            _state = GameState.Idle;

            SetButtonState(dealEnabled: true, actionEnabled: false, splitEnabled: false);
            SetStatus("Press Deal to start");

            if (resetSound.HasClip && audioSource != null)
                resetSound.Play(audioSource);
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

            foreach (CardView v in _playerCardViews) if (v != null) Destroy(v.gameObject);
            _playerCardViews.Clear();

            foreach (CardView v in _splitCardViews) if (v != null) Destroy(v.gameObject);
            _splitCardViews.Clear();

            foreach (CardView v in _dealerCardViews) if (v != null) Destroy(v.gameObject);
            _dealerCardViews.Clear();

            _playerHand.Clear();
            _splitHand.Clear();
            _dealerHand.Clear();
            _dealerHoleCardView = null;
            _isSplitRound       = false;
            _activeHandIndex    = 0;

            if (_savedBetBeforeAction > 0 && chipBetting != null)
            {
                chipBetting.RestoreBet(_savedBetBeforeAction);
                _savedBetBeforeAction = 0;
            }

            // If the player lost while in Martingale mode, double the bet for the next round.
            if (_pendingMartingaleDouble && chipBetting != null)
            {
                _pendingMartingaleDouble = false;
                chipBetting.DoubleBetChips(playSound: true);
            }

     
            StopAllScorePulses();
            ResetPlayerScoreLabelPosition();
            SetScoreLabelsVisible(false);
            SetStatus("Place your bet");

            _state = GameState.Idle;
        }


        // ──────────────────────────────────────────────────────────────────────────

        private void Start()
        {
            if (startupSound.HasClip && audioSource != null)
                startupSound.Play(audioSource);

            _playerMoney = startingMoney;
            RefreshMoneyLabel();

            if (chipBetting != null)
                chipBetting.OnBetChanged += OnBetChangedHandler;

            _deck.Build();
            _defaultStatusColor = statusLabel.color;
            AlignStatusLabelToCardArea();
            InitSplitScoreLabel();
            SetScoreLabelsVisible(false);
            RefreshStreakLabel();
            SetButtonState(dealEnabled: true, actionEnabled: false, splitEnabled: false);
            SetStatus("Press Deal to start");
        }

        private void OnDestroy()
        {
            if (chipBetting != null)
                chipBetting.OnBetChanged -= OnBetChangedHandler;
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Public Audio API
        // ──────────────────────────────────────────────────────────────────────────

        /// <summary>Plays the exit sound and returns its length in seconds.</summary>
        public float PlayExitSound()
        {
            exitSound.Play(audioSource);
            return exitSound.Length;
        }

        /// <summary>Plays the knock sound.</summary>
        public void PlayKnockSound()
        {
            knockSound.Play(audioSource);
        }

        /// <summary>
        /// Plays the knock sound and pulses "Limit exceeded!" in LoseColor 3 times,
        /// then restores the previous status label text and color.
        /// </summary>
        public void NotifyBetLimitExceeded()
        {
            knockSound.Play(audioSource);
            StartCoroutine(PulseLimitExceeded());
        }

        private const int LimitPulseCount = 3;
        private const float LimitPulseDelay = 0.5f;

        /// <summary>True while the "Limit exceeded!" pulse animation is running. All input should be suppressed during this window.</summary>
        public bool IsLimitPulsing { get; private set; }

        private IEnumerator PulseLimitExceeded()
        {
            IsLimitPulsing = true;

            string previousText  = statusLabel.text;
            Color  previousColor = statusLabel.color;

            for (int i = 0; i < LimitPulseCount; i++)
            {
                SetStatus("Limit exceeded!", LoseColor);
                yield return new WaitForSeconds(LimitPulseDelay);
                SetStatus(string.Empty, LoseColor);
                yield return new WaitForSeconds(LimitPulseDelay);
            }

            SetStatus(previousText, previousColor);

            IsLimitPulsing = false;
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Input
        // ──────────────────────────────────────────────────────────────────────────

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                if (hitButton != null && hitButton.gameObject.activeSelf)
                    OnHit();
                else if (dealButton != null && dealButton.gameObject.activeSelf)
                    OnDeal();
            }
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Button Handlers
        // ──────────────────────────────────────────────────────────────────────────

        /// <summary>Starts a new round. Ensures a minimum bet of 1 chip and deducts the total bet from the player's balance.</summary>
        public void OnDeal()
        {
            if (_state != GameState.Idle && _state != GameState.RoundOver) return;
            StopBlackjackCelebration();

            // Show Martingale suggestion popup only after the player has lost exactly the threshold consecutive rounds.
            bool consecutiveTrigger = EffectiveMartingaleThreshold > 0 && _consecutiveLosses >= EffectiveMartingaleThreshold;

            if (martingalePopup != null
                && consecutiveTrigger
                && !_martingalePopupShown)
            {
                _martingalePopupShown = true;
                martingalePopup.Show(
                    "Play Martingale ?",
                    onDoIt: () =>
                    {
                        // Disable "Always Lose" when entering Martingale mode.
                        if (AlwaysLose)
                        {
                            AlwaysLose = false;
                            OnAlwaysLoseDisabled?.Invoke();
                        }

                        if (chipBetting != null)
                        {
                            // Snapshot the bet placed before Martingale so we can restore it on a win.
                            _betBeforeMartingale = chipBetting.TotalBet;

                            // First entry into Martingale: bet abs(total amount lost) + 1 chip.
                            int chipValue = chipBetting.SmallestChipValue;
                            int nextBet   = (int)_totalAmountLost + chipValue;
                            chipBetting.SetBet(nextBet, playSound: true);
                            _martingaleChipValue = chipValue;
                        }
                        RefreshStreakLabel();
                        StartNewRound();
                    },
                    onReconsider: () => { /* player stays on betting screen */ }
                );
                return;
            }

            StartNewRound();
        }

        /// <summary>
        /// Ensures a minimum bet is placed, deducts it from the player's balance, and starts <see cref="DealRound"/>.
        /// All new-game entry points funnel through here.
        /// </summary>
        private void StartNewRound()
        {
            if (_doubleBJSoundPlaying) return;
            StopAllCoroutines();
            _martingaleWin = false;
            dealButton.gameObject.SetActive(false);
            menuController?.CloseMenu();
            EnsureMinimumBet();
            _savedBetBeforeAction = 0;
            chipBetting?.SnapshotBet();
            _playerMoney -= CurrentBet;
            RefreshMoneyLabel();
            _state = GameState.PlayerTurn;
            StartCoroutine(DealRound());
        }

        /// <summary>
        /// Transitions to Idle if needed and places the smallest chip when <see cref="ChipBetting.TotalBet"/> is zero.
        /// </summary>
        private void EnsureMinimumBet()
        {
            if (chipBetting == null) return;

            if (_state == GameState.RoundOver)
                PrepareForBetting();

            if (chipBetting.TotalBet <= 0)
                chipBetting.PlaceSmallestChip();
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

            surrenderSound.Play(audioSource);
            RecordRoundOutcome(true, lostAmount: CurrentBet * 0.5m);
            SetStatus("<size=40>Surrender returns 1/2 of bet</size>", SurrenderColor);
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
            chipBetting?.DoubleBetChips();
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
        /// "Keep decision" executes the player's choice; "Reconsider" closes the popup
        /// and returns button control so the player can choose again.
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

            deviationPopup.Show(
                recommendation: evaluation.Recommendation.ToString(),
                onKeep:         executeChosen,
                onReconsider:   null);
        }

        /// <summary>Forces the next deal to give the player a natural blackjack, then starts the round.</summary>
        public void OnBlackjackTest()
        {
            if (_state != GameState.Idle && _state != GameState.RoundOver) return;
            StopBlackjackCelebration();
            _state = GameState.Idle;
            _forcePlayerBlackjack = true;
            StartNewRound();
        }

        /// <summary>Forces the next deal to give the player a matching pair of the rank chosen in the options slider.</summary>
        public void OnSplitTest()
        {
            if (_state != GameState.Idle && _state != GameState.RoundOver) return;
            StopBlackjackCelebration();
            _state = GameState.Idle;
            _forceSplitHand = true;
            StartNewRound();
        }

        /// <summary>Forces the next deal to give both player and dealer a natural blackjack, then starts the round.</summary>
        public void OnBothBlackjackTest()
        {
            if (_state != GameState.Idle && _state != GameState.RoundOver) return;
            StopBlackjackCelebration();
            _state = GameState.Idle;
            _forceBothBlackjack = true;
            StartNewRound();
        }

        /// <summary>Forces the next deal to give the player a hard-11 two-card hand (random pair, e.g. 5+6 or 4+7), then starts the round.</summary>
        public void OnDoubleDownTest()
        {
            if (_state != GameState.Idle && _state != GameState.RoundOver) return;
            StopBlackjackCelebration();
            _state = GameState.Idle;
            _forceDoubleDownTest = true;
            StartNewRound();
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

            ClearTable();
            SetStatus("");
            _doubleDownExtraBet = 0;
            yield return new WaitForSeconds(newRoundPause); //mark1
            //SetStatus("Dealing...");

            yield return StartCoroutine(DealCardTo(_playerHand, _playerCardViews, playerCardArea, faceUp: true));
            yield return StartCoroutine(DealCardTo(_dealerHand, _dealerCardViews, dealerCardArea, faceUp: true));
            yield return StartCoroutine(DealCardTo(_playerHand, _playerCardViews, playerCardArea, faceUp: true));
            yield return StartCoroutine(DealCardTo(_dealerHand, _dealerCardViews, dealerCardArea, faceUp: false));

            _dealerHoleCardView = _dealerCardViews[^1];
            UpdateScoreLabels(revealDealer: false);

            // ── Natural blackjack check ──
            bool playerBJ = _playerHand.IsBlackjack();
            bool dealerBJ = _dealerHand.IsBlackjack();

            if (playerBJ || dealerBJ)
            {
                yield return StartCoroutine(RevealHoleCard());
                UpdateScoreLabels(revealDealer: true);

                if (playerBJ && dealerBJ)  { StartCoroutine(PlayDoubleBJSoundRoutine()); RecordRoundOutcome(false, scoreDelta:  0, isPush: true); SetStatus("Push", PushColor); ApplyPayout(PayoutResult.Push, CurrentBet); }
                else if (playerBJ)         { ApplyBlackjackGlow(); SpawnFireworks(PlayNaturalBlackjackSound()); RecordRoundOutcome(false, scoreDelta: +1); SetStatus("You win", WinColor); ApplyPayout(PayoutResult.BlackjackWin, CurrentBet); }
                else                       { PlayLoseSound(); RecordRoundOutcome(true, lostAmount: CurrentBet, scoreDelta: -1); SetStatus("You lose", LoseColor); ApplyPayout(PayoutResult.Lose, CurrentBet); }

                yield return StartCoroutine(EndRound());
                yield break;
            }

            // ── Player turn ──
            SetButtonState(dealEnabled: false, actionEnabled: true, splitEnabled: CanSplit(), doubleDownEnabled: CanDoubleDown(), surrenderEnabled: true);
            SetStatus($"Your turn");

            _dealerUpcardSnapshot = _dealerHand.Cards[0];
            bool hasPair = CanSplit();

            // Highlight the recommended cell in the strategy table (if visible).
            strategyTableUI?.HighlightRecommendation(
                _playerHand, _dealerUpcardSnapshot,
                canSplit: hasPair, canDouble: CanDoubleDown(), canSurrender: true);

            if (!hasPair && _playerHand.BestValue() <= AutoHitMaxScore)
            {
                yield return new WaitForSeconds(0.3f);
                yield return StartCoroutine(AutoHitLoop());
                yield break;
            }

            bool shouldStand = ShouldAutoStand(_playerHand);

            if (shouldStand)
            {
                knockSound.Play(audioSource);
                yield return new WaitForSeconds(0.3f);
                yield return StartCoroutine(DealerTurn());
            }
        }

        // ── Split ─────────────────────────────────────────────────────────────────

        private bool CanSplit() =>
            !_isSplitRound
            && _playerHand.Count == 2
            && _playerHand.Cards[0].Rank == _playerHand.Cards[1].Rank;

        private bool CanSurrender() =>
            ActiveHand.Count == 2 && !_isSplitRound;

        private IEnumerator PerformSplit()
        {
            _isSplitRound = true;
            SetButtonState(dealEnabled: false, actionEnabled: false, splitEnabled: false);

            // Move card[1] from player hand to split hand
            CardData movedCard = _playerHand.Cards[1];
            _playerHand.RemoveAt(1);

            CardView movedView = _playerCardViews[1];
            _playerCardViews.RemoveAt(1);

            // Move card[1] to split card area
            movedView.transform.SetParent(splitCardArea, worldPositionStays: false);
            _splitCardViews.Add(movedView);
            _splitHand.AddCard(movedCard);

            cardSlideSound.Play(audioSource);
            yield return new WaitForSeconds(0.5f);

            bool isAces = _playerHand.Cards[0].Rank == Rank.Ace;

            // Deal second card to each hand
            yield return StartCoroutine(DealCardTo(_playerHand,  _playerCardViews, playerCardArea, faceUp: true));
            yield return StartCoroutine(DealCardTo(_splitHand,   _splitCardViews,  splitCardArea,  faceUp: true));

            UpdateScoreLabels(revealDealer: false);
            _activeHandIndex = 0;

            if (isAces)
            {
                SetStatus("Split Aces — one card each. Standing.");
                yield return new WaitForSeconds(0.5f);
                yield return StartCoroutine(DealerTurn());
                yield break;
            }

            SetButtonState(dealEnabled: false, actionEnabled: true, splitEnabled: false, doubleDownEnabled: CanDoubleDown());
            SetStatus($"Players turn Hand 1");

            if (ActiveHand.BestValue() <= AutoHitMaxScore)
            {
                yield return new WaitForSeconds(0.3f);
                yield return StartCoroutine(AutoHitLoop());
                yield break;
            }

            if (ShouldAutoStand(ActiveHand))
            {
                knockSound.Play(audioSource);
                yield return new WaitForSeconds(0.3f);
                yield return StartCoroutine(AdvanceOrDealerTurn());
            }
        }

        // ── Double Down ───────────────────────────────────────────────────────────

        private bool CanDoubleDown() =>
            ActiveHand.Cards.Count == 2;

        private IEnumerator PerformDoubleDown()
        {
            SetButtonState(dealEnabled: false, actionEnabled: false, splitEnabled: false);

            // Status is set after the deal so a deviation message shown just before
            // this coroutine starts stays visible during the deal animation.
            _savedBetBeforeAction = CurrentBet;
            _doubleDownExtraBet = CurrentBet;
            _playerMoney -= _doubleDownExtraBet;
            RefreshMoneyLabel();
            chipBetting?.DoubleBetChips();
            ddSound.Play(audioSource); //mark dd sound
            yield return StartCoroutine(
                DealCardTo(ActiveHand, ActiveViews,
                           _activeHandIndex == 0 ? playerCardArea : splitCardArea,
                           faceUp: true));

            SetStatus("Double Down!");

            UpdateScoreLabels(revealDealer: false);

            if (ActiveHand.IsBust())
            {
                yield return StartCoroutine(RevealHoleCard());
                UpdateScoreLabels(revealDealer: true);
                PlayLoseSound();
                RecordRoundOutcome(true, lostAmount: CurrentBet + _doubleDownExtraBet, scoreDelta: -1);
                SetStatus($"Busted");
                yield return StartCoroutine(EndRound());
                yield break;
            }

            SetStatus($"Double Down stands at {ActiveHand.BestValue()}");
            yield return new WaitForSeconds(dealerPauseDelay);
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

                if (ActiveHand.BestValue() <= AutoHitMaxScore)
                {
                    yield return new WaitForSeconds(0.3f);
                    yield return StartCoroutine(AutoHitLoop());
                }
                else if (ShouldAutoStand(ActiveHand))
                {
                    knockSound.Play(audioSource);
                    yield return new WaitForSeconds(0.3f);
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

            int score = ActiveHand.BestValue();

            if (score > BlackjackValue)
            {
                
              string label = _isSplitRound ? $"Hand {_activeHandIndex + 1} busts" : "Bust!";
              SetStatus($"{label}", LoseColor);
              PlayLoseSound();

                if (_isSplitRound)
                {
                    // Always advance to next hand or dealer turn so both hands get resolved.
                    yield return new WaitForSeconds(0.5f);
                    yield return StartCoroutine(AdvanceOrDealerTurn());
                }
                else
                {
                    RecordRoundOutcome(true, lostAmount: CurrentBet, scoreDelta: -1);
                    yield return StartCoroutine(RevealHoleCard());
                    yield return StartCoroutine(EndRound());
                }
                yield break;
            }

            if (score == BlackjackValue || ShouldAutoStand(ActiveHand))
            {
                if (score != BlackjackValue)
                    knockSound.Play(audioSource);
                yield return new WaitForSeconds(0.25f);
                yield return StartCoroutine(AdvanceOrDealerTurn());
                yield break;
            }

            if (score <= AutoHitMaxScore)
            {
                yield return new WaitForSeconds(0.3f);
                yield return StartCoroutine(PlayerHit());
                yield break;
            }

            SetButtonState(dealEnabled: false, actionEnabled: true, splitEnabled: false);
            SetStatus(_isSplitRound
                ? $"Players turn Hand 1"
                : $"Your turn");
        }

        private IEnumerator DealerTurn()
        {
            _state = GameState.DealerTurn;
            SetButtonState(dealEnabled: false, actionEnabled: false, splitEnabled: false);
            StopAllScorePulses();

            yield return StartCoroutine(RevealHoleCard());
            UpdateScoreLabels(revealDealer: true);

            // If both split hands busted, skip dealer drawing.
            bool allPlayerHandsBusted = _isSplitRound
                ? _playerHand.IsBust() && _splitHand.IsBust()
                : _playerHand.IsBust();

            if (!allPlayerHandsBusted)
            {
                SetStatus("Dealer's turn");
                yield return new WaitForSeconds(dealerPauseDelay);

                while (ShouldDealerHit())
                {
                    yield return StartCoroutine(DealCardTo(_dealerHand, _dealerCardViews, dealerCardArea, faceUp: true));
                    UpdateScoreLabels(revealDealer: true);
                    yield return new WaitForSeconds(dealerPauseDelay);
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
        private void ApplyPayout(PayoutResult result, int bet)
        {
            _playerMoney += result switch
            {
                PayoutResult.Win          => bet * 2m,
                PayoutResult.BlackjackWin => bet * 2.5m,
                PayoutResult.Push         => bet,
                PayoutResult.Surrender    => bet * 0.5m,
                _                         => 0,                   // Lose — bet already gone
            };
            RefreshMoneyLabel();
        }

        private void RefreshMoneyLabel()
        {
            if (playerMoneyLabel == null) return;
            playerMoneyLabel.text = $"€ {((decimal)_playerMoney).ToString("N2", GermanCulture)}";
        }

        private int CurrentBet => chipBetting != null ? chipBetting.TotalBet : 0;

        /// <summary>
        /// Records whether the round was a net loss (bust, lose, surrender) or not.
        /// Maintains <see cref="_consecutiveLosses"/>, <see cref="_totalLosses"/>, <see cref="_totalAmountLost"/>,
        /// <see cref="_playerScore"/>, and snapshots the bet for Delayed Martingale detection.
        /// <paramref name="lostAmount"/> is the monetary amount forfeited this round (full bet for a loss, half for surrender, 0 for win/push).
        /// <paramref name="scoreDelta"/> is +1 for a win, -1 for a loss, 0 for push or surrender.
        /// <paramref name="isPush"/> when true, counts as half a loss toward the Martingale threshold without resetting streak or Martingale state.
        /// <paramref name="isMartingaleNeutral"/> when true, leaves all Martingale and streak state completely unchanged (used for split rounds with no net score change).
        /// </summary>
        private void RecordRoundOutcome(bool isLoss, decimal lostAmount = 0, int scoreDelta = 0, bool isPush = false, bool isMartingaleNeutral = false)
        {
            _lastRoundBet  = CurrentBet + _doubleDownExtraBet;
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
                _consecutiveLosses++;
                _totalLosses++;
                _totalAmountLost += lostAmount;
                // If the player is already in Martingale mode, schedule a bet double for the next betting phase
                // and reflect the upcoming doubled chip value immediately in the streak label.
                if (_martingalePopupShown)
                {
                    _pendingMartingaleDouble = true;
                    _martingaleChipValue    *= 2;
                }
            }
            else if (isPush)
            {
                // A push counts as half a loss toward the Martingale threshold but does not reset streak or Martingale state.
                _consecutiveLosses += 0.5m;
            }
            else
            {
                // If the player wins while Martingale mode is active, schedule the minimum-bet reset for EndRound.
                if (_martingalePopupShown)
                    _martingaleWin = true;

                _consecutiveLosses       = 0;
                _martingalePopupShown    = false;
                _pendingMartingaleDouble = false;
                _martingaleChipValue     = 0;
            }
            RefreshStreakLabel();
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
        }

        private IEnumerator ResolveRound()
        {
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

                for (int i = 0; i < hands.Length; i++)
                {
                    int s = hands[i].BestValue();
                    int handBet = CurrentBet / 2;

          if (s > BlackjackValue)
                    {
                        results.Add(ColorizeText($"{labels[i]}: Bust", LoseColor));
                        anyLoss = true;
                        splitLostAmount += handBet;
                        ApplyPayout(PayoutResult.Lose, handBet);
                    }
                    else if (dealerBust || s > dealerScore)
                    {
                        results.Add(ColorizeText($"{labels[i]}: Win", WinColor));
                        anyWin = true;
                        ApplyPayout(PayoutResult.Win, handBet);
                    }
                    else if (s < dealerScore)
                    {
                        results.Add(ColorizeText($"{labels[i]}: Lose", LoseColor));
                        anyLoss = true;
                        splitLostAmount += handBet;
                        ApplyPayout(PayoutResult.Lose, handBet);
                    }
                    else
                    {
                        results.Add(ColorizeText($"{labels[i]}: Push", PushColor));
                        anyPush = true;
                        ApplyPayout(PayoutResult.Push, handBet);
                    }
                }

                if (anyWin)       { PlayWinRoutine(); }
                else if (anyLoss) PlayLoseSound();
                else              PlayTieSound();

                // Split 1W/1L or 1W/1Push counts as a push for the Martingale counter — streak is neither incremented nor reset.
                bool splitPush = (anyWin && anyLoss) || (anyWin && anyPush && !anyLoss);
                int  splitScoreDelta = anyWin && !anyLoss ? +1 : anyLoss && !anyWin ? -1 : 0;
                // If the split produced no net score change, leave the Martingale counter completely untouched.
                bool splitNeutral = splitScoreDelta == 0;
                RecordRoundOutcome(isLoss: anyLoss && !anyWin, lostAmount: splitLostAmount,
                    scoreDelta: splitScoreDelta,
                    isPush: !anyWin && !anyLoss || splitPush,
                    isMartingaleNeutral: splitNeutral);
                SetStatus(string.Join("  |  ", results));
            }
            else
            {
                int p = _playerHand.BestValue();
                int totalBet = CurrentBet + _doubleDownExtraBet;
                if      (dealerBust)         { PlayWinRoutine();  RecordRoundOutcome(false, scoreDelta: +1); SetStatus($"You win", WinColor);  ApplyPayout(PayoutResult.Win,  totalBet); }
                else if (p > dealerScore)    { PlayWinRoutine();  RecordRoundOutcome(false, scoreDelta: +1); SetStatus($"You win", WinColor);  ApplyPayout(PayoutResult.Win,  totalBet); }
                else if (dealerScore > p)    { PlayLoseSound(); RecordRoundOutcome(true, lostAmount: totalBet, scoreDelta: -1);  SetStatus($"You lose",LoseColor); ApplyPayout(PayoutResult.Lose, totalBet); }
                else                         { PlayTieSound();  RecordRoundOutcome(false, scoreDelta:  0, isPush: true); SetStatus($"Push",PushColor);     ApplyPayout(PayoutResult.Push, totalBet); }
            }

            yield return StartCoroutine(EndRound());
        }

        private IEnumerator EndRound()
        {
            _state = GameState.RoundOver;
            SetButtonState(dealEnabled: false, actionEnabled: false, splitEnabled: false);
            strategyTableUI?.ClearHighlight();
            yield return new WaitForSeconds(endRoundDelay);
            chipBetting?.ResetMaxBet();
            chipBetting?.ClampBetToMaxBet();
            chipBetting?.RestoreBetFromSnapshot();
            if (!_doubleBJSoundPlaying && _state == GameState.RoundOver)
                SetButtonState(dealEnabled: true, actionEnabled: false, splitEnabled: false);
            // State stays RoundOver — chip click or Deal press drives the next transition.
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Card Dealing
        // ──────────────────────────────────────────────────────────────────────────

        private IEnumerator DealCardTo(Hand hand, List<CardView> views, Transform area, bool faceUp)
        {
            yield return new WaitForSeconds(dealDelay);

            CardData card = _deck.Draw();
            hand.AddCard(card);

            if (dealCardSound.HasClip && audioSource != null)
                dealCardSound.Play(audioSource);

            // Always spawn face-down, then flip to reveal if this card should be face-up
            CardView view = SpawnCardView(card, area, faceUp: false);
            views.Add(view);

            if (faceUp)
            {
                bool flipDone = false;
                view.Flip(toFaceUp: true, () => flipDone = true);
                yield return new WaitUntil(() => flipDone);
            }
        }

        private CardView SpawnCardView(CardData card, Transform parent, bool faceUp)
        {
            GameObject go   = Instantiate(cardViewPrefab, parent);
            CardView   view = go.GetComponent<CardView>();
            view.Setup(
                spriteRegistry.GetSprite(card),
                spriteRegistry.GetBackSprite(),
                faceUp
            );
            return view;
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Hole Card
        // ──────────────────────────────────────────────────────────────────────────

        private IEnumerator RevealHoleCard()
        {
            if (_dealerHoleCardView == null || _dealerHoleCardView.IsFaceUp)
                yield break;

            bool done = false;
            _dealerHoleCardView.Flip(toFaceUp: true, () => done = true);
            yield return new WaitUntil(() => done);
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Glow Effect
        // ──────────────────────────────────────────────────────────────────────────

        private void ApplyBlackjackGlow()
        {
            foreach (CardView v in _playerCardViews)
                v.StartGlowPulse();
        }

        /// <summary>Stops fireworks, all audio, and card glow pulses from a blackjack celebration.</summary>
        private void StopBlackjackCelebration()
        {
            if (audioSource != null)
                audioSource.Stop();

            foreach (CardView v in _playerCardViews)
                v.StopGlowPulse();
        }

        // ──────────────────────────────────────────────────────────────────────────
        // UI Helpers
        // ──────────────────────────────────────────────────────────────────────────

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
        /// Pins the status label's left edge to the left edge of the dealer card area.
        /// Uses the dealer card area's RectTransform to compute the offset at runtime,
        /// so the alignment stays correct regardless of canvas or layout changes.
        /// </summary>
        private void AlignStatusLabelToCardArea()
        {
            if (dealerCardArea == null) return;

            RectTransform dealerRT = dealerCardArea as RectTransform
                                     ?? dealerCardArea.GetComponent<RectTransform>();
            if (dealerRT == null) return;

            RectTransform statusRT = statusLabel.rectTransform;

            // Move pivot to the left edge so anchoredPosition controls the left side.
            Vector2 pivot = statusRT.pivot;
            pivot.x = 0f;
            statusRT.pivot = pivot;

            // Left edge of dealer card area = its anchoredPosition.x - half its width.
            float leftEdge = dealerRT.anchoredPosition.x - dealerRT.rect.width * 0.5f;
            Vector2 pos = statusRT.anchoredPosition;
            pos.x = leftEdge;
            statusRT.anchoredPosition = pos;
        }

        /// <summary>Sets the status label text and resets its color to the default.</summary>
        private void SetStatus(string message)
        {
            statusLabel.text = message;
            statusLabel.color = _defaultStatusColor;
        }

        /// <summary>Sets the status label text with a specific color.</summary>
        private void SetStatus(string message, Color color)
        {
            statusLabel.text = message;
            statusLabel.color = color;
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

            float areaCenterY = areaRT.anchoredPosition.y + areaRT.sizeDelta.y * 0.5f;
            float labelHalfHeight = labelRT.sizeDelta.y * 0.5f;

            labelRT.anchorMin = areaRT.anchorMin;
            labelRT.anchorMax = areaRT.anchorMax;
            labelRT.anchoredPosition = new Vector2(
                _defaultPlayerScorePosition.x,
                areaCenterY - labelHalfHeight
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
        private void PlayWinSound()
        {
            winSound.Play(audioSource);
        }

        /// <summary>
        /// Plays the win audio and card glow.
        /// When the current round was entered as a Delayed Martingale, triggers the full
        /// natural-blackjack celebration instead of the regular win sound, then plays
        /// <see cref="resetSound"/> once the celebration has finished.
        /// </summary>
        private void PlayWinRoutine()
        {
            if (_martingalePopupShown)
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

        /// <summary>Waits for <paramref name="delay"/> seconds, then plays <see cref="resetSound"/>.
        /// If the round was a Martingale win, also replaces the bet area chips with the minimum bet.</summary>
        private IEnumerator PlayResetSoundAfterDelay(float delay)
        {
            // Always yield at least one frame so RecordRoundOutcome can set _martingaleWin before we read it.
            if (delay > 0f)
                yield return new WaitForSeconds(delay);
            else
                yield return null;

            resetSound.Play(audioSource);

            if (_martingaleWin && chipBetting != null)
            {
                int restoreBet = _betBeforeMartingale > 0 ? _betBeforeMartingale : chipBetting.SmallestChipValue;
                chipBetting.SetBet(restoreBet);
                chipBetting.SnapshotBet();
                _betBeforeMartingale = 0;
                _martingaleWin       = false;
            }
        }

        /// <summary>
        /// Instantiates <see cref="fireworksPrefab"/> at the world origin and auto-destroys it
        /// after <see cref="fireworksDuration"/> seconds.
        /// </summary>
        private void SpawnFireworks(float duration)
        {
            if (fireworksPrefab == null) return;
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
            GameObject fx = Instantiate(fireworksPrefab, spawnPosition, Quaternion.identity);
            Destroy(fx, duration > 0f ? duration : fireworksDuration);
        }

        /// <summary>Plays the natural blackjack sound if assigned, otherwise falls back to win sound.
        /// Also plays the yuhu sound simultaneously. Stops all player card glow pulses once the longest clip finishes.
        /// Returns the duration of the longest clip played, so callers can chain additional sounds.</summary>
        private float PlayNaturalBlackjackSound()
        {
            SoundEntry primary = naturalBlackjackSound.HasClip ? naturalBlackjackSound : winSound;
            float longestDuration = 0f;

            if (primary.HasClip && audioSource != null)
            {
                primary.Play(audioSource);
                longestDuration = primary.Length;
            }

            if (yuhuSound.HasClip && audioSource != null)
            {
                yuhuSound.Play(audioSource);
                if (yuhuSound.Length > longestDuration)
                    longestDuration = yuhuSound.Length;
            }

            if (longestDuration > 0f)
                StartCoroutine(StopGlowAfterClip(longestDuration));

            return longestDuration;
        }

        private IEnumerator StopGlowAfterClip(float duration)
        {
            yield return new WaitForSeconds(duration);
            foreach (CardView v in _playerCardViews) v.StopGlowPulse();
            foreach (CardView v in _splitCardViews)  v.StopGlowPulse();
        }

        /// <summary>Plays the lose sound if both clip and source are assigned.</summary>
        private void PlayLoseSound()
        {
            loseSound.Play(audioSource);
        }

        /// <summary>Plays the tie sound if both clip and source are assigned.</summary>
        private void PlayTieSound()
        {
            tieSound.Play(audioSource);
        }

        /// <summary>
        /// Triggered on a double natural blackjack push. Plays the tie sound once,
        /// then plays one randomly chosen sound from cheaterSound, damnitSound, and hmhSound.
        /// The same sound is never played twice in a row. Only assigned clips are included.
        /// The deal button stays locked until the random sound has finished playing.
        /// </summary>
        private IEnumerator PlayDoubleBJSoundRoutine()
        {
            _doubleBJSoundPlaying = true;

            tieSound.Play(audioSource);
            yield return new WaitForSeconds(tieSound.Length);

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

            chosen.Play(audioSource);
            yield return new WaitForSeconds(chosen.Length);
            _doubleBJSoundPlaying = false;

            if (_state == GameState.RoundOver)
                SetButtonState(dealEnabled: true, actionEnabled: false, splitEnabled: false);
        }

        private void SetButtonState(bool dealEnabled, bool actionEnabled, bool splitEnabled, bool doubleDownEnabled = false, bool surrenderEnabled = false)
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
            _playerHand.Clear();
            _splitHand.Clear();
            _dealerHand.Clear();
            _dealerHoleCardView = null;
            _isSplitRound    = false;
            _activeHandIndex = 0;
            StopAllScorePulses();
            ResetPlayerScoreLabelPosition();
            SetScoreLabelsVisible(false);

            foreach (CardView v in _playerCardViews) if (v != null) Destroy(v.gameObject);
            _playerCardViews.Clear();

            foreach (CardView v in _splitCardViews)  if (v != null) Destroy(v.gameObject);
            _splitCardViews.Clear();

            foreach (CardView v in _dealerCardViews) if (v != null) Destroy(v.gameObject);
            _dealerCardViews.Clear();
        }
    }
}
