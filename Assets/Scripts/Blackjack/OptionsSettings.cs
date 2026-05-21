using System;

namespace Blackjack
{
    /// <summary>
    /// Plain data container for all options menu settings.
    /// Serialized to and from JSON by <see cref="SettingsRepository"/>.
    /// </summary>
    [Serializable]
    public class OptionsSettings
    {
        public bool blackjackTestEnabled    = true;
        public bool bjAllEnabled            = true;
        public bool ddTestEnabled           = true;
        public bool testSplitEnabled        = true;
        public int  testSplitRank           = 2;
        public bool overrideStrategyEnabled        = false;
        public bool alwaysLoseEnabled              = false;
        public float volume                        = 1f;
        public bool martingaleThresholdEnabled     = false;
        public int  martingaleThreshold            = 4;
    }
}
