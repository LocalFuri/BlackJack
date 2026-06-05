using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Blackjack
{
    /// <summary>
    /// Manages chip selection from the chip tray and placement into the bet area.
    /// Same-kind chips stack vertically; different kinds sit in separate columns.
    /// When a stack's total value reaches the next chip's denomination the stack
    /// is automatically converted to one chip of that higher value (cascading).
    /// </summary>
    public class ChipBetting : MonoBehaviour
    {
        // ──────────────────────────────────────────────────────────────────────
        // Nested types
        // ──────────────────────────────────────────────────────────────────────

        [System.Serializable]
        public class ChipType
        {
            [Tooltip("Monetary value of this chip.")]
            public int value;

            [Tooltip("Sprite used when placing this chip in the bet area.")]
            public Sprite sprite;

            [Tooltip("Tray button the player clicks to add this chip.")]
            public Button sourceButton;

            [Tooltip("How many chips of this type are needed to auto-upgrade to the next. -1 = no upgrade.")]
            public int upgradeAt = -1;
        }

        // ──────────────────────────────────────────────────────────────────────
        // Inspector
        // ──────────────────────────────────────────────────────────────────────

        [Header("Chip Types (lowest → highest value)")]
        [SerializeField] private List<ChipType> chipTypes = new();

        [Header("References")]
        [SerializeField] private Transform betArea;
        [SerializeField] private BlackjackGame blackjackGame;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private SoundEntry chipSound;
        [SerializeField] private TextMeshProUGUI betSumLabel;

        [Header("Layout")]
        [Tooltip("Horizontal spacing between chip type columns.")]
        [SerializeField] private float columnSpacing = 70f;

        [Tooltip("Vertical pixel offset per stacked chip within a column.")]
        [SerializeField] private float stackOffsetY = 6f;

        [Tooltip("Uniform scale applied to each bet chip image.")]
        [SerializeField] private float betChipScale = 1.2f;

        [Tooltip("Size of each chip image rect in the bet area.")]
        [SerializeField] private Vector2 chipSize = new(60f, 60f);

        [Tooltip("Horizontal offset applied to all chip columns, allowing space for a label to the left.")]
        [SerializeField] private float chipStartOffsetX = 0f;

        [Header("Limits")]
        [Tooltip("Maximum total bet the player is allowed to place.")]
        [SerializeField] private int maxBet = BlackjackGame.BetLimit;

        private const int DefaultMaxBet = BlackjackGame.BetLimit;

        [Header("Buttons")]
        [Tooltip("Button that clears all chips from the bet area.")]
        [SerializeField] private Button chipResetButton;

        [Tooltip("Sound played when the chip reset button is pressed.")]
        [SerializeField] private SoundEntry chipResetSound;

        [Tooltip("Button that sets the bet to the maximum allowed amount.")]
        [SerializeField] private Button chipMaxButton;


    // ──────────────────────────────────────────────────────────────────────
    // Events
    // ──────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Fired whenever the bet amount changes.
    /// The argument is the signed delta (positive = chip added, negative = chip removed).
    /// </summary>
    public event Action<int> OnBetChanged;

        // ──────────────────────────────────────────────────────────────────────
        // State
        // ──────────────────────────────────────────────────────────────────────

        // Column order — chip type indices in the order they first appeared
        private readonly List<int> _columnOrder = new();

        // Placed GameObjects per chip type index
        private readonly Dictionary<int, List<GameObject>> _stacks = new();

        // Chip count per chip type index
        private readonly Dictionary<int, int> _chipCounts = new();

        // ──────────────────────────────────────────────────────────────────────
        // Unity lifecycle
        // ──────────────────────────────────────────────────────────────────────

        private void Start()
        {
            for (int i = 0; i < chipTypes.Count; i++)
            {
                int index = i;
                if (chipTypes[i].sourceButton == null) continue;

                // Left click — add chip
                chipTypes[i].sourceButton.onClick.AddListener(() => OnChipClicked(index));

                // Right click — remove top chip of this type from the bet area
                EventTrigger trigger = chipTypes[i].sourceButton.gameObject
                    .GetComponent<EventTrigger>() ?? chipTypes[i].sourceButton.gameObject
                    .AddComponent<EventTrigger>();

                var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
                entry.callback.AddListener(data =>
                {
                    var pointerData = (PointerEventData)data;
                    if (pointerData.button == PointerEventData.InputButton.Right)
                        OnChipRightClicked(index);
                });
                trigger.triggers.Add(entry);
            }

            chipResetButton?.onClick.AddListener(OnChipResetClicked);
            chipMaxButton?.onClick.AddListener(OnChipMaxClicked);

            if (betSumLabel != null)
                betSumLabel.gameObject.SetActive(false);
        }

        // ──────────────────────────────────────────────────────────────────────
        // Public API
        // ──────────────────────────────────────────────────────────────────────

        /// <summary>Total monetary value of all chips currently in the bet area.</summary>
        public int TotalBet
        {
            get
            {
                int total = 0;
                foreach (KeyValuePair<int, int> kvp in _chipCounts)
                {
                    if (kvp.Key >= 0 && kvp.Key < chipTypes.Count)
                        total += chipTypes[kvp.Key].value * kvp.Value;
                }
                return total;
            }
        }

        /// <summary>Maximum total bet the player is allowed to place.</summary>
        public int MaxBet => maxBet;

        /// <summary>The monetary value of the lowest-denomination chip type. Returns 1 when no chip types are configured.</summary>
        public int SmallestChipValue => chipTypes.Count > 0 ? chipTypes[0].value : 1;

        /// <summary>Resets the maximum bet back to the default value of <see cref="DefaultMaxBet"/>.</summary>
        public void ResetMaxBet() => maxBet = DefaultMaxBet;

        /// <summary>Overrides the maximum bet to <paramref name="value"/>. Used by Martingale mode to lift the cap.</summary>
        public void SetMaxBet(int value) => maxBet = Mathf.Max(value, DefaultMaxBet);

        /// <summary>
        /// Removes chips from the bet area — highest denomination first — until
        /// <see cref="TotalBet"/> is at or below <see cref="MaxBet"/>.
        /// Fires <see cref="OnBetChanged"/> for each chip removed and refreshes the label.
        /// </summary>
        public void ClampBetToMaxBet()
        {
          if (TotalBet <= maxBet) return;
          int removed = 0;
        
        while (TotalBet > maxBet)
            {
                int highestTypeIndex = -1;
                for (int i = chipTypes.Count - 1; i >= 0; i--)
                {
                    if (_stacks.TryGetValue(i, out List<GameObject> stack) && stack.Count > 0)
                    {
                        highestTypeIndex = i;
                        break;
                    }
                }

                if (highestTypeIndex == -1) break;

                int chipValue = chipTypes[highestTypeIndex].value;
                RemoveTopChips(highestTypeIndex, 1);
                removed += chipValue;
            }

            if (removed != 0)
                OnBetChanged?.Invoke(-removed);

            RefreshBetLabel();
        }

        /// <summary>
        /// Places one chip of the lowest available denomination into the bet area
        /// and fires <see cref="OnBetChanged"/>. Used as a minimum-bet fallback.
        /// </summary>
        public void PlaceSmallestChip()
        {
            if (chipTypes.Count == 0) return;

            int typeIndex = 0;
            chipSound.Play(audioSource);
            PlaceChip(typeIndex);
            CheckUpgrade(typeIndex);
            OnBetChanged?.Invoke(chipTypes[typeIndex].value);
            RefreshBetLabel();
        }

        /// <summary>
        /// Clears the entire bet area and places exactly one chip of the lowest denomination.
        /// Fires <see cref="OnBetChanged"/> to reflect the net change in bet value.
        /// Does not play any sound — callers are responsible for audio.
        /// </summary>
        public void ResetToMinimumBet()
        {
            if (chipTypes.Count == 0) return;

            SetBet(SmallestChipValue);

            if (TotalBet <= 0)
                PlaceSmallestChip();
        }

        /// <summary>
        /// Rebuilds the bet area to represent exactly <paramref name="targetAmount"/> and fires
        /// <see cref="OnBetChanged"/> with the signed delta so all listeners stay in sync.
        /// </summary>
        /// <param name="targetAmount">The desired total bet value.</param>
        /// <param name="playSound">When true, plays <see cref="chipSound"/> if the bet increased (e.g. for Martingale auto-raise).</param>
        public void SetBet(int targetAmount, bool playSound = false)
        {
            int previousBet = TotalBet;
            RestoreBet(targetAmount);
            int delta = TotalBet - previousBet;
            if (delta != 0)
                OnBetChanged?.Invoke(delta);

            if (playSound && delta > 0)
                chipSound.Play(audioSource);
        }

        /// <summary>
        /// Rebuilds the bet area to represent exactly <paramref name="targetAmount"/> using
        /// the available chip denominations (greedy highest-first). Clears the current chips
        /// without firing <see cref="OnBetChanged"/>, then places the new chips silently and
        /// refreshes the bet sum label. Any remainder that cannot be represented exactly is
        /// ignored (e.g. odd amounts when only even denominations are available).
        /// </summary>
        public void RestoreBet(int targetAmount)
        {
            if (chipTypes.Count == 0 || targetAmount <= 0) return;

            ClearAllBetChips();

            // Greedy decomposition: highest denomination first
            int remaining = targetAmount;
            for (int i = chipTypes.Count - 1; i >= 0 && remaining > 0; i--)
            {
                int denomination = chipTypes[i].value;
                if (denomination <= 0) continue;

                int count = remaining / denomination;
                remaining -= count * denomination;

                for (int j = 0; j < count; j++)
                    PlaceChip(i);
            }

            RefreshBetLabel();
        }

        // Saved chip composition from the most recent SnapshotBet call.
        private List<int> _snapshotColumnOrder = new();
        private Dictionary<int, int> _snapshotChipCounts = new();

        /// <summary>
        /// Captures the current chip composition so it can be restored after the round ends.
        /// Call this just before deducting the bet from the player's money.
        /// </summary>
        public void SnapshotBet()
        {
            _snapshotColumnOrder = new List<int>(_columnOrder);
            _snapshotChipCounts  = new Dictionary<int, int>(_chipCounts);
        }

        /// <summary>
        /// Rebuilds the bet area to exactly match the composition captured by the last
        /// <see cref="SnapshotBet"/> call. Clears current chips without firing
        /// <see cref="OnBetChanged"/>, places the snapshotted chips, and refreshes the label.
        /// Does nothing when no snapshot exists or it was empty.
        /// </summary>
        public void RestoreBetFromSnapshot()
        {
            if (_snapshotColumnOrder.Count == 0) return;

            // Clear existing chips silently (no event)
            foreach (KeyValuePair<int, List<GameObject>> kvp in _stacks)
                foreach (GameObject go in kvp.Value)
                    if (go != null) Destroy(go);

            _stacks.Clear();
            _columnOrder.Clear();
            _chipCounts.Clear();

            // Re-place chips in the original column order to preserve visual layout.
            foreach (int typeIndex in _snapshotColumnOrder)
            {
                if (!_snapshotChipCounts.TryGetValue(typeIndex, out int count)) continue;
                for (int i = 0; i < count; i++)
                    PlaceChip(typeIndex);
            }

            RefreshBetLabel();
        }

        /// <summary>
        /// Duplicates every chip currently in the bet area, doubling the visual stack and
        /// <see cref="TotalBet"/>. Fires <see cref="OnBetChanged"/> with the added amount
        /// and refreshes the bet sum label.
        /// </summary>
        /// <param name="playSound">When true, plays <see cref="chipSound"/> after doubling (e.g. for Martingale auto-raise).</param>
        /// <param name="enforceMaxBet">
        /// When true, the resulting bet is clamped to <see cref="MaxBet"/>.
        /// If the current bet already equals or exceeds <see cref="MaxBet"/>, nothing is added
        /// and <see cref="BlackjackGame.NotifyBetLimitExceeded"/> is called instead (unless
        /// <paramref name="notifyLimitExceeded"/> is false).
        /// Pass true for player-initiated doubling (e.g. Martingale); leave false for
        /// game-mechanic doublings such as Double Down and Split.
        /// </param>
        /// <param name="notifyLimitExceeded">When false, limit handling is left to the caller.</param>
        /// <returns>False when <paramref name="enforceMaxBet"/> is true and the bet limit was reached.</returns>
        public bool DoubleBetChips(bool playSound = false, bool enforceMaxBet = false, bool notifyLimitExceeded = true)
        {
            if (_columnOrder.Count == 0) return true;

            if (enforceMaxBet)
            {
                int currentBet = TotalBet;

                if (currentBet >= maxBet)
                {
                    if (notifyLimitExceeded)
                        blackjackGame?.NotifyBetLimitExceeded();
                    return false;
                }

                int doubledBet = currentBet * 2;
                if (doubledBet > maxBet)
                {
                    if (notifyLimitExceeded)
                        blackjackGame?.NotifyBetLimitExceeded();

                    int previousBet = currentBet;
                    RestoreBet(maxBet);
                    int delta = TotalBet - previousBet;
                    if (delta != 0 && notifyLimitExceeded)
                        OnBetChanged?.Invoke(delta);

                    if (playSound && delta > 0 && notifyLimitExceeded)
                        chipSound.Play(audioSource);

                    return false;
                }
            }

            // Snapshot current state before we add anything.
            var snapshotTypes  = new List<int>(_columnOrder);
            var snapshotCounts = new Dictionary<int, int>(_chipCounts);

            int addedValue = 0;

            foreach (int typeIndex in snapshotTypes)
            {
                int count = snapshotCounts[typeIndex];
                for (int i = 0; i < count; i++)
                {
                    PlaceChip(typeIndex);
                    CheckUpgrade(typeIndex);
                    addedValue += chipTypes[typeIndex].value;
                }
            }

            if (addedValue != 0)
                OnBetChanged?.Invoke(addedValue);

            if (playSound)
                chipSound.Play(audioSource);

            RefreshBetLabel();
            return true;
        }

        /// <summary>Removes all chips from the bet area and resets state.</summary>
        public void ClearBetArea()
        {
            int refund = TotalBet;

            ClearAllBetChips();

            if (refund != 0)
                OnBetChanged?.Invoke(-refund);

            RefreshBetLabel();
        }

        // ──────────────────────────────────────────────────────────────────────
        // Chip placement
        // ──────────────────────────────────────────────────────────────────────

        private void OnChipResetClicked()
        {
            if (blackjackGame != null)
            {
                if (blackjackGame.IsMenuOpen)
                {
                    if (!blackjackGame.IsBettingAllowed && !blackjackGame.IsRoundOver)
                        return;
                    blackjackGame.CloseMenu(playSound: false);
                }

                if (blackjackGame.IsRoundOver)
                    blackjackGame.PrepareForBetting();
                else if (!blackjackGame.IsBettingAllowed)
                    return;
            }

            if (TotalBet <= 0)
                blackjackGame?.PlayKnockSound();
            else
                chipResetSound.Play(audioSource);

            blackjackGame?.ClearBetLimitStatus();
            ClearBetArea();
        }

        private void OnChipMaxClicked()
        {
            if (blackjackGame != null)
            {
                if (blackjackGame.IsMenuOpen)
                {
                    if (!blackjackGame.IsBettingAllowed && !blackjackGame.IsRoundOver)
                        return;
                    blackjackGame.CloseMenu();
                }

                if (blackjackGame.IsBetLimitStatusActive)
                    return;

                if (blackjackGame.IsRoundOver)
                    blackjackGame.PrepareForBetting();
                else if (!blackjackGame.IsBettingAllowed)
                    return;
            }

            if (TotalBet >= maxBet)
            {
                blackjackGame?.NotifyBetLimitExceeded();
                return;
            }

            int previousBet = TotalBet;
            chipResetSound.Play(audioSource);
            RestoreBet(maxBet);

            int delta = TotalBet - previousBet;
            if (delta != 0)
                OnBetChanged?.Invoke(delta);
        }

        /// <summary>Returns true when the current total bet exceeds the minimum (one chip of the lowest denomination).</summary>
        private bool HasMoreThanMinimumBet()
        {
          if (chipTypes.Count == 0) return false;
          return TotalBet > chipTypes[0].value;
        }

        private void OnChipClicked(int typeIndex)
        {
            if (typeIndex < 0 || typeIndex >= chipTypes.Count) return;

            if (blackjackGame != null)
            {
                if (blackjackGame.IsMenuOpen)
                {
                    if (!blackjackGame.IsBettingAllowed && !blackjackGame.IsRoundOver)
                        return;
                    blackjackGame.CloseMenu();
                }

                if (blackjackGame.IsBetLimitStatusActive)
                    return;

                if (blackjackGame.IsRoundOver)
                    blackjackGame.PrepareForBetting();
                else if (!blackjackGame.IsBettingAllowed)
                    return;
            }

            int chipValue = chipTypes[typeIndex].value;

            if (TotalBet + chipValue > maxBet)
            {
                blackjackGame?.NotifyBetLimitExceeded();
                return;
            }

            chipSound.Play(audioSource);
            PlaceChip(typeIndex);
            CheckUpgrade(typeIndex);
            OnBetChanged?.Invoke(chipValue);
            RefreshBetLabel();
        }

        private void OnChipRightClicked(int typeIndex)
        {
            if (typeIndex < 0 || typeIndex >= chipTypes.Count) return;

            if (blackjackGame != null)
            {
                if (blackjackGame.IsMenuOpen)
                {
                    if (!blackjackGame.IsBettingAllowed && !blackjackGame.IsRoundOver)
                        return;
                    blackjackGame.CloseMenu();
                }

                if (blackjackGame.IsBetLimitStatusActive)
                    return;

                if (blackjackGame.IsRoundOver)
                    blackjackGame.PrepareForBetting();
                else if (!blackjackGame.IsBettingAllowed)
                    return;
            }

            if (!_stacks.ContainsKey(typeIndex) || _stacks[typeIndex].Count == 0) return;

            int chipValue = chipTypes[typeIndex].value;

            // Never allow the bet to drop below 1 chip (the smallest denomination).
            if (TotalBet - chipValue < SmallestChipValue) return;

            chipSound.Play(audioSource);
            RemoveTopChips(typeIndex, 1);
            OnBetChanged?.Invoke(-chipValue);
            RefreshBetLabel();
        }

        /// <summary>Destroys every chip in the bet area and clears placement tracking.</summary>
        private void ClearAllBetChips()
        {
            if (betArea != null)
            {
                for (int i = betArea.childCount - 1; i >= 0; i--)
                {
                    Transform child = betArea.GetChild(i);
                    if (betSumLabel != null && child == betSumLabel.transform)
                        continue;

                    Destroy(child.gameObject);
                }
            }

            _stacks.Clear();
            _columnOrder.Clear();
            _chipCounts.Clear();
        }

        /// <summary>Places one chip of the given type into the bet area.</summary>
        private void PlaceChip(int typeIndex)
        {
            bool isNewColumn = !_columnOrder.Contains(typeIndex);
            EnsureTracking(typeIndex);

            List<GameObject> stack = _stacks[typeIndex];
            int col = _columnOrder.IndexOf(typeIndex);

            GameObject chipGO = CreateChipGO(typeIndex, col, stack.Count);
            stack.Add(chipGO);
            _chipCounts[typeIndex]++;

            // Always recentre when a new column is added (sort may have shifted others)
            if (isNewColumn)
                RecenterAllColumns();
        }

        // ──────────────────────────────────────────────────────────────────────
        // Auto-upgrade logic
        // ──────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Checks whether the stack for the given chip type has reached its upgrade
        /// threshold. If so, removes those chips and places one of the next type,
        /// then recursively checks the next type for further upgrades.
        /// </summary>
        private void CheckUpgrade(int typeIndex)
        {
            if (typeIndex < 0 || typeIndex >= chipTypes.Count) return;

            ChipType ct = chipTypes[typeIndex];
            if (ct.upgradeAt <= 0) return;

            int nextIndex = typeIndex + 1;
            if (nextIndex >= chipTypes.Count) return;

            if (!_chipCounts.TryGetValue(typeIndex, out int count)) return;
            if (count < ct.upgradeAt) return;

            // Remove the required number of lower chips
            RemoveTopChips(typeIndex, ct.upgradeAt);

            // Place one chip of the next denomination
            PlaceChip(nextIndex);

            // Cascade: the next type might also now be eligible for an upgrade
            CheckUpgrade(nextIndex);
        }

        /// <summary>Destroys the top <paramref name="count"/> chips of a stack and updates tracking.</summary>
        private void RemoveTopChips(int typeIndex, int count)
        {
            if (!_stacks.TryGetValue(typeIndex, out List<GameObject> stack)) return;

            int removeCount = Mathf.Min(count, stack.Count);
            for (int i = 0; i < removeCount; i++)
            {
                int last = stack.Count - 1;
                if (stack[last] != null) Destroy(stack[last]);
                stack.RemoveAt(last);
            }

            _chipCounts[typeIndex] = Mathf.Max(0, _chipCounts[typeIndex] - removeCount);

            // Remove the column entirely if the stack is now empty
            if (stack.Count == 0)
            {
                _columnOrder.Remove(typeIndex);
                _stacks.Remove(typeIndex);
                _chipCounts.Remove(typeIndex);
                RecenterAllColumns();
            }
        }

        // ──────────────────────────────────────────────────────────────────────
        // Helpers
        // ──────────────────────────────────────────────────────────────────────

        private void EnsureTracking(int typeIndex)
        {
            if (_columnOrder.Contains(typeIndex)) return;

            _columnOrder.Add(typeIndex);
            _columnOrder.Sort(); // chipTypes is ordered low→high, so index order = value order
            _stacks[typeIndex] = new List<GameObject>();
            _chipCounts[typeIndex] = 0;
        }

        private GameObject CreateChipGO(int typeIndex, int col, int stackHeight)
        {
            float x = chipStartOffsetX + chipSize.x * 0.5f + col * columnSpacing;
            float y = stackHeight * stackOffsetY;

            GameObject go = new($"BetChip_{chipTypes[typeIndex].value}_{stackHeight}");
            go.transform.SetParent(betArea, worldPositionStays: false);

            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0.5f);
            rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot     = new Vector2(0f, 0.5f);
            rt.sizeDelta = chipSize;
            rt.localScale = Vector3.one * betChipScale;
            rt.anchoredPosition = new Vector2(x, y);

            Image img = go.AddComponent<Image>();
            img.sprite = chipTypes[typeIndex].sprite;
            img.preserveAspect = true;
            img.raycastTarget = false;

            return go;
        }

        private void RecenterAllColumns()
        {
            for (int col = 0; col < _columnOrder.Count; col++)
            {
                int typeIndex = _columnOrder[col];
                if (!_stacks.TryGetValue(typeIndex, out List<GameObject> stack)) continue;

                float x = chipStartOffsetX + chipSize.x * 0.5f + col * columnSpacing;
                for (int s = 0; s < stack.Count; s++)
                {
                    if (stack[s] == null) continue;
                    stack[s].GetComponent<RectTransform>().anchoredPosition = new Vector2(x, s * stackOffsetY);
                }
            }
        }

        // ──────────────────────────────────────────────────────────────────────
        // Label
        // ──────────────────────────────────────────────────────────────────────

        private static readonly System.Globalization.CultureInfo GermanCulture =
            System.Globalization.CultureInfo.GetCultureInfo("de-DE");

        /// <summary>Updates the bet sum label to reflect the current total bet value.</summary>
        private void RefreshBetLabel()
        {
            if (betSumLabel == null) return;

            bool hasBet = TotalBet > 0;
            betSumLabel.gameObject.SetActive(hasBet);

            if (!hasBet) return;

            //betSumLabel.text = $"Bet: € {((decimal)TotalBet).ToString("N2", GermanCulture)}";
            //betSumLabel.text = $"Bet: € {TotalBet}"; //no separators
            betSumLabel.text = $"€ {(TotalBet).ToString("N0", GermanCulture)}"; //N2 = decimal digits
            PositionBetLabelRightOfChips();
        }

        /// <summary>
        /// Positions the bet sum label to the right of the rightmost chip column,
        /// vertically aligned with the lowest (first) chip in that column.
        /// </summary>
        private void PositionBetLabelRightOfChips()
        {
            if (betSumLabel == null) return;

            RectTransform labelRT = betSumLabel.GetComponent<RectTransform>();
            if (labelRT == null) return;

            if (_stacks.Count == 0 || _columnOrder.Count == 0)
            {
                labelRT.anchoredPosition = Vector2.zero;
                return;
            }

            // Find the rightmost chip's right edge across all columns
            float rightmostEdge = float.MinValue;
            float lowestChipY = 0f;

            foreach (int typeIndex in _columnOrder)
            {
                if (!_stacks.TryGetValue(typeIndex, out List<GameObject> stack) || stack.Count == 0) continue;

                RectTransform bottomChipRT = stack[0].GetComponent<RectTransform>();
                // Chip pivot is at left-center; visual right edge accounts for localScale
                float chipRightEdge = bottomChipRT.anchoredPosition.x + chipSize.x * betChipScale;

                if (chipRightEdge > rightmostEdge)
                {
                    rightmostEdge = chipRightEdge;
                    lowestChipY   = bottomChipRT.anchoredPosition.y;
                }
            }

            const float LabelGap = 8f;

            // Left-align the label just after the rightmost chip, centred on the lowest chip's Y
            labelRT.anchorMin = new Vector2(0f, 0.5f);
            labelRT.anchorMax = new Vector2(0f, 0.5f);
            labelRT.pivot     = new Vector2(0f, 0.5f);  // pivot at left edge so it grows rightward
            labelRT.anchoredPosition = new Vector2(rightmostEdge + LabelGap, lowestChipY);
        }

    }
}
