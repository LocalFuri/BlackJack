using System;
using UnityEngine;

namespace Blackjack
{
    /// <summary>
    /// Maps game actions to strategy actions so deviations can be detected.
    /// </summary>
    public enum PlayerAction
    {
        Hit,
        Stand,
        Double,
        Split,
        Surrender,
    }

    /// <summary>
    /// The result of evaluating a player's move against basic strategy.
    /// </summary>
    public readonly struct StrategyEvaluation
    {
        /// <summary>Whether the player's action matched basic strategy.</summary>
        public readonly bool IsCorrect;

        /// <summary>The action the player chose.</summary>
        public readonly PlayerAction PlayerAction;

        /// <summary>The action basic strategy recommends.</summary>
        public readonly StrategyAction Recommendation;

        /// <summary>Human-readable description of the deviation, or null when correct.</summary>
        public readonly string DeviationMessage;

        public StrategyEvaluation(
            bool isCorrect,
            PlayerAction playerAction,
            StrategyAction recommendation,
            string deviationMessage)
        {
            IsCorrect        = isCorrect;
            PlayerAction     = playerAction;
            Recommendation   = recommendation;
            DeviationMessage = deviationMessage;
        }
    }

    /// <summary>
    /// Evaluates player decisions against the basic strategy table and raises an
    /// event whenever a deviation is detected.
    /// </summary>
    public class BasicStrategyAdvisor : MonoBehaviour
    {
        [Header("Strategy Table")]
        [Tooltip("Assign the BasicStrategyTable ScriptableObject asset here.")]
        [SerializeField] private BasicStrategyTable strategyTable;

        /// <summary>
        /// Raised after every player action. Carries the full evaluation result,
        /// including whether it was correct and what the recommendation was.
        /// </summary>
        public event Action<StrategyEvaluation> OnActionEvaluated;

        // ─── Public API ───────────────────────────────────────────────────────────

        /// <summary>
        /// Evaluates whether the player's action matches basic strategy, raises
        /// <see cref="OnActionEvaluated"/>, and returns the evaluation result.
        /// </summary>
        /// <param name="playerAction">The action the player took.</param>
        /// <param name="playerHand">The player's hand at the moment of the decision.</param>
        /// <param name="dealerUpcard">The dealer's visible upcard.</param>
        /// <param name="canSplit">Whether splitting was available.</param>
        /// <param name="canDouble">Whether doubling was available.</param>
        /// <param name="canSurrender">Whether surrendering was available.</param>
        public StrategyEvaluation Evaluate(
            PlayerAction playerAction,
            Hand         playerHand,
            CardData     dealerUpcard,
            bool         canSplit,
            bool         canDouble,
            bool         canSurrender)
        {
            if (strategyTable == null)
            {
                Debug.LogError("[BasicStrategyAdvisor] No BasicStrategyTable assigned.");
                StrategyEvaluation empty = new(true, playerAction, StrategyAction.Hit, null);
                return empty;
            }

            StrategyAction recommendation = strategyTable.GetRecommendation(
                playerHand, dealerUpcard, canSplit, canDouble, canSurrender);

            bool isCorrect = ActionMatchesStrategy(playerAction, recommendation);

            string deviationMessage = isCorrect
                ? null
                : BuildDeviationMessage(playerAction, recommendation, playerHand, dealerUpcard);

            StrategyEvaluation evaluation = new(isCorrect, playerAction, recommendation, deviationMessage);

            OnActionEvaluated?.Invoke(evaluation);

            if (!isCorrect)
                Debug.Log($"[BasicStrategy] Deviation — {deviationMessage}");

            return evaluation;
        }

        // ─── Private helpers ──────────────────────────────────────────────────────

        /// <summary>
        /// Returns true when the player's action fulfils the strategy recommendation.
        /// A Double recommendation accepts Hit as a correct fallback (when unavailable),
        /// and Surrender accepts Hit as a correct fallback too — but those cases are
        /// already resolved inside BasicStrategyTable before reaching here.
        /// </summary>
        private static bool ActionMatchesStrategy(PlayerAction action, StrategyAction strategy)
        {
            return strategy switch
            {
                StrategyAction.Hit       => action == PlayerAction.Hit,
                StrategyAction.Stand     => action == PlayerAction.Stand,
                StrategyAction.Double    => action == PlayerAction.Double,
                StrategyAction.Split     => action == PlayerAction.Split,
                StrategyAction.Surrender => action == PlayerAction.Surrender,
                _                        => false,
            };
        }

        private static string BuildDeviationMessage(
            PlayerAction   playerAction,
            StrategyAction recommendation,
            Hand           hand,
            CardData       dealerUpcard)
        {
            string handDescription = hand.IsSoft()
                ? $"soft {hand.BestValue()}"
                : $"hard {hand.BestValue()}";

            string dealer = DealerUpcardLabel(dealerUpcard);

            // Short form for the status label, long form in the log.
            return $"Strategy: {recommendation}  (you chose {playerAction}, {handDescription} vs {dealer})";
        }

        private static string DealerUpcardLabel(CardData card)
        {
            return card.Rank switch
            {
                Rank.Ace   => "Ace",
                Rank.King  => "King (10)",
                Rank.Queen => "Queen (10)",
                Rank.Jack  => "Jack (10)",
                _          => card.BlackjackValue.ToString(),
            };
        }
    }
}
