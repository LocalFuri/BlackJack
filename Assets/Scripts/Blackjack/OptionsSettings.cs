using System;

namespace Blackjack
{
    /// <summary>
    /// Plain data container for all menu settings.
    /// Inspector defaults are applied first; if a save file exists it overrides those values on load.
    /// </summary>
    [Serializable]
    public class OptionsSettings
    {
        public bool  blackjackTestEnabled    = true;
        public bool  bjAllEnabled            = true;
        public bool  ddTestEnabled           = true;
        public bool  testSplitEnabled        = true;
        public int   testSplitRank           = 2;
        public bool  overrideStrategyEnabled = false;
        public bool  alwaysLoseEnabled       = false;
        public float volume                  = 1f;
        public int   martingaleThreshold     = 4;
        public bool  martingaleActive        = false;
        public bool  martingaleAutoPlay      = false;
        public bool  showStrategyEnabled     = false;
    }
}
