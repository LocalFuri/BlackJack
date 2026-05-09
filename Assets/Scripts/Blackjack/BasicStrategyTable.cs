using System.Collections.Generic;
using UnityEngine;

namespace Blackjack
{
    /// <summary>The action basic strategy recommends for a given game state.</summary>
    public enum StrategyAction
    {
        Hit,
        Stand,
        Double,     // Double if allowed, otherwise Hit
        Split,
        Surrender,  // Surrender if allowed, otherwise Hit
    }

    /// <summary>
    /// Static lookup table encoding the complete single-deck basic strategy.
    /// Rows are keyed by player total or pair rank; columns by dealer upcard (2–A).
    /// </summary>
    public static class BasicStrategyTable
    {
        // ─── Dealer upcard column order: 2,3,4,5,6,7,8,9,10,A ───────────────────
        //     Index:                       0 1 2 3 4 5 6 7  8 9

        private static readonly StrategyAction[,] HardTable =
        {
            // vs:           2          3          4          5          6          7          8          9         10          A
            /* H5  */ { SA.Hit,    SA.Hit,    SA.Hit,    SA.Hit,    SA.Hit,    SA.Hit,    SA.Hit,    SA.Hit,    SA.Hit,    SA.Hit    },
            /* H6  */ { SA.Hit,    SA.Hit,    SA.Hit,    SA.Hit,    SA.Hit,    SA.Hit,    SA.Hit,    SA.Hit,    SA.Hit,    SA.Hit    },
            /* H7  */ { SA.Hit,    SA.Hit,    SA.Hit,    SA.Hit,    SA.Hit,    SA.Hit,    SA.Hit,    SA.Hit,    SA.Hit,    SA.Hit    },
            /* H8  */ { SA.Hit,    SA.Hit,    SA.Hit,    SA.Hit,    SA.Hit,    SA.Hit,    SA.Hit,    SA.Hit,    SA.Hit,    SA.Hit    },
            /* H9  */ { SA.Hit,    SA.Double, SA.Double, SA.Double, SA.Double, SA.Hit,    SA.Hit,    SA.Hit,    SA.Hit,    SA.Hit    },
            /* H10 */ { SA.Double, SA.Double, SA.Double, SA.Double, SA.Double, SA.Double, SA.Double, SA.Double, SA.Hit,    SA.Hit    },
            /* H11 */ { SA.Double, SA.Double, SA.Double, SA.Double, SA.Double, SA.Double, SA.Double, SA.Double, SA.Double, SA.Hit    },
            /* H12 */ { SA.Hit,    SA.Hit,    SA.Stand,  SA.Stand,  SA.Stand,  SA.Hit,    SA.Hit,    SA.Hit,    SA.Hit,    SA.Hit    },
            /* H13 */ { SA.Stand,  SA.Stand,  SA.Stand,  SA.Stand,  SA.Stand,  SA.Hit,    SA.Hit,    SA.Hit,    SA.Hit,    SA.Hit    },
            /* H14 */ { SA.Stand,  SA.Stand,  SA.Stand,  SA.Stand,  SA.Stand,  SA.Hit,    SA.Hit,    SA.Hit,    SA.Hit,    SA.Hit    },
            /* H15 */ { SA.Stand,  SA.Stand,  SA.Stand,  SA.Stand,  SA.Stand,  SA.Hit,    SA.Hit,    SA.Hit,    SA.Surrender, SA.Hit },
            /* H16 */ { SA.Stand,  SA.Stand,  SA.Stand,  SA.Stand,  SA.Stand,  SA.Hit,    SA.Hit,    SA.Surrender, SA.Surrender, SA.Surrender },
            /* H17 */ { SA.Stand,  SA.Stand,  SA.Stand,  SA.Stand,  SA.Stand,  SA.Stand,  SA.Stand,  SA.Stand,  SA.Stand,  SA.Stand  },
            /* H18 */ { SA.Stand,  SA.Stand,  SA.Stand,  SA.Stand,  SA.Stand,  SA.Stand,  SA.Stand,  SA.Stand,  SA.Stand,  SA.Stand  },
            /* H19 */ { SA.Stand,  SA.Stand,  SA.Stand,  SA.Stand,  SA.Stand,  SA.Stand,  SA.Stand,  SA.Stand,  SA.Stand,  SA.Stand  },
            /* H20 */ { SA.Stand,  SA.Stand,  SA.Stand,  SA.Stand,  SA.Stand,  SA.Stand,  SA.Stand,  SA.Stand,  SA.Stand,  SA.Stand  },
            /* H21 */ { SA.Stand,  SA.Stand,  SA.Stand,  SA.Stand,  SA.Stand,  SA.Stand,  SA.Stand,  SA.Stand,  SA.Stand,  SA.Stand  },
        };

        private static readonly StrategyAction[,] SoftTable =
        {
            // vs:              2          3          4          5          6          7          8          9         10          A
            /* S13 = A2 */ { SA.Hit,    SA.Hit,    SA.Hit,    SA.Double, SA.Double, SA.Hit,    SA.Hit,    SA.Hit,    SA.Hit,    SA.Hit    },
            /* S14 = A3 */ { SA.Hit,    SA.Hit,    SA.Hit,    SA.Double, SA.Double, SA.Hit,    SA.Hit,    SA.Hit,    SA.Hit,    SA.Hit    },
            /* S15 = A4 */ { SA.Hit,    SA.Hit,    SA.Double, SA.Double, SA.Double, SA.Hit,    SA.Hit,    SA.Hit,    SA.Hit,    SA.Hit    },
            /* S16 = A5 */ { SA.Hit,    SA.Hit,    SA.Double, SA.Double, SA.Double, SA.Hit,    SA.Hit,    SA.Hit,    SA.Hit,    SA.Hit    },
            /* S17 = A6 */ { SA.Hit,    SA.Double, SA.Double, SA.Double, SA.Double, SA.Hit,    SA.Hit,    SA.Hit,    SA.Hit,    SA.Hit    },
            /* S18 = A7 */ { SA.Double, SA.Double, SA.Double, SA.Double, SA.Double, SA.Stand,  SA.Stand,  SA.Hit,    SA.Hit,    SA.Hit    },
            /* S19 = A8 */ { SA.Stand,  SA.Stand,  SA.Stand,  SA.Stand,  SA.Stand,  SA.Stand,  SA.Stand,  SA.Stand,  SA.Stand,  SA.Stand  },
            /* S20 = A9 */ { SA.Stand,  SA.Stand,  SA.Stand,  SA.Stand,  SA.Stand,  SA.Stand,  SA.Stand,  SA.Stand,  SA.Stand,  SA.Stand  },
            /* S21 = AA */ { SA.Stand,  SA.Stand,  SA.Stand,  SA.Stand,  SA.Stand,  SA.Stand,  SA.Stand,  SA.Stand,  SA.Stand,  SA.Stand  },
        };

        private static readonly Dictionary<int, StrategyAction[]> PairTable = new()
        {
            //                    vs: 2           3           4           5           6           7           8           9          10           A
            [2]  = new[] { SA.Split,  SA.Split,  SA.Split,  SA.Split,  SA.Split,  SA.Split,  SA.Hit,    SA.Hit,    SA.Hit,    SA.Hit    },
            [3]  = new[] { SA.Split,  SA.Split,  SA.Split,  SA.Split,  SA.Split,  SA.Split,  SA.Hit,    SA.Hit,    SA.Hit,    SA.Hit    },
            [4]  = new[] { SA.Hit,    SA.Hit,    SA.Hit,    SA.Split,  SA.Split,  SA.Hit,    SA.Hit,    SA.Hit,    SA.Hit,    SA.Hit    },
            [5]  = new[] { SA.Double, SA.Double, SA.Double, SA.Double, SA.Double, SA.Double, SA.Double, SA.Double, SA.Hit,    SA.Hit    },
            [6]  = new[] { SA.Split,  SA.Split,  SA.Split,  SA.Split,  SA.Split,  SA.Hit,    SA.Hit,    SA.Hit,    SA.Hit,    SA.Hit    },
            [7]  = new[] { SA.Split,  SA.Split,  SA.Split,  SA.Split,  SA.Split,  SA.Split,  SA.Hit,    SA.Hit,    SA.Hit,    SA.Hit    },
            [8]  = new[] { SA.Split,  SA.Split,  SA.Split,  SA.Split,  SA.Split,  SA.Split,  SA.Split,  SA.Split,  SA.Split,  SA.Split  },
            [9]  = new[] { SA.Split,  SA.Split,  SA.Split,  SA.Split,  SA.Split,  SA.Stand,  SA.Split,  SA.Split,  SA.Stand,  SA.Stand  },
            [10] = new[] { SA.Stand,  SA.Stand,  SA.Stand,  SA.Stand,  SA.Stand,  SA.Stand,  SA.Stand,  SA.Stand,  SA.Stand,  SA.Stand  },
            [11] = new[] { SA.Split,  SA.Split,  SA.Split,  SA.Split,  SA.Split,  SA.Split,  SA.Split,  SA.Split,  SA.Split,  SA.Split  },
        };

        // ─── Public API ───────────────────────────────────────────────────────────

        /// <summary>Returns the basic strategy recommendation for the given hand state.</summary>
        public static StrategyAction GetRecommendation(
            Hand hand,
            CardData dealerUpcard,
            bool canSplit,
            bool canDouble,
            bool canSurrender)
        {
            int col = DealerColumn(dealerUpcard);

            if (canSplit && hand.Count == 2)
            {
                int v0 = hand.Cards[0].BlackjackValue;
                int v1 = hand.Cards[1].BlackjackValue;
                bool isPair = v0 == v1 || (v0 >= 10 && v1 >= 10);
                if (isPair)
                {
                    int key = (v0 >= 10 && v0 != 11) ? 10 : Mathf.Min(v0, 11);
                    if (PairTable.TryGetValue(key, out StrategyAction[] row))
                        return Resolve(row[col], canDouble, canSurrender);
                }
            }

            if (hand.IsSoft())
            {
                int rowIndex = Mathf.Clamp(hand.BestValue(), 13, 21) - 13;
                return Resolve(SoftTable[rowIndex, col], canDouble, canSurrender);
            }

            int hardRow = Mathf.Clamp(hand.BestValue(), 5, 21) - 5;
            return Resolve(HardTable[hardRow, col], canDouble, canSurrender);
        }

        // ─── Helpers ─────────────────────────────────────────────────────────────

        private static int DealerColumn(CardData card) => card.Rank switch
        {
            Rank.Two   => 0,
            Rank.Three => 1,
            Rank.Four  => 2,
            Rank.Five  => 3,
            Rank.Six   => 4,
            Rank.Seven => 5,
            Rank.Eight => 6,
            Rank.Nine  => 7,
            Rank.Ten   => 8,
            Rank.Jack  => 8,
            Rank.Queen => 8,
            Rank.King  => 8,
            Rank.Ace   => 9,
            _          => 8,
        };

        private static StrategyAction Resolve(StrategyAction action, bool canDouble, bool canSurrender) =>
            action switch
            {
                StrategyAction.Double    => canDouble    ? StrategyAction.Double    : StrategyAction.Hit,
                StrategyAction.Surrender => canSurrender ? StrategyAction.Surrender : StrategyAction.Hit,
                _                        => action,
            };

        private static class SA
        {
            public static readonly StrategyAction Hit       = StrategyAction.Hit;
            public static readonly StrategyAction Stand     = StrategyAction.Stand;
            public static readonly StrategyAction Double    = StrategyAction.Double;
            public static readonly StrategyAction Split     = StrategyAction.Split;
            public static readonly StrategyAction Surrender = StrategyAction.Surrender;
        }
    }
}
