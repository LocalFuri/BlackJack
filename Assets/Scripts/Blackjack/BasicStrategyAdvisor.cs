namespace Blackjack
{
    /// <summary>The action the player chose.</summary>
    public enum PlayerAction { Hit, Stand, Double, Split, Surrender }

    /// <summary>Result of evaluating a player action against basic strategy.</summary>
    public readonly struct StrategyEvaluation
    {
        public readonly bool           IsCorrect;
        public readonly PlayerAction   PlayerAction;
        public readonly StrategyAction Recommendation;

        public StrategyEvaluation(bool isCorrect, PlayerAction playerAction, StrategyAction recommendation)
        {
            IsCorrect      = isCorrect;
            PlayerAction   = playerAction;
            Recommendation = recommendation;
        }
    }

    /// <summary>
    /// Evaluates player decisions against <see cref="BasicStrategyTable"/>.
    /// Instantiate directly — no MonoBehaviour required.
    /// </summary>
    public class BasicStrategyAdvisor
    {
        /// <summary>
        /// Evaluates whether the player's action matches basic strategy and returns the result.
        /// </summary>
        public StrategyEvaluation Evaluate(
            PlayerAction playerAction,
            Hand         playerHand,
            CardData     dealerUpcard,
            bool         canSplit,
            bool         canDouble,
            bool         canSurrender)
        {
            StrategyAction recommendation = BasicStrategyTable.GetRecommendation(
                playerHand, dealerUpcard, canSplit, canDouble, canSurrender);

            bool isCorrect = Matches(playerAction, recommendation);
            return new StrategyEvaluation(isCorrect, playerAction, recommendation);
        }

        private static bool Matches(PlayerAction action, StrategyAction strategy) =>
            strategy switch
            {
                StrategyAction.Hit       => action == PlayerAction.Hit,
                StrategyAction.Stand     => action == PlayerAction.Stand,
                StrategyAction.Double    => action == PlayerAction.Double,
                StrategyAction.Split     => action == PlayerAction.Split,
                StrategyAction.Surrender => action == PlayerAction.Surrender,
                _                        => false,
            };
    }
}
