using System;
using UnityEngine;

namespace Blackjack
{
    public interface ICardDisplay
    {
        void Setup(Sprite faceSprite, Sprite backSprite, bool faceUp = true);
        void Flip(bool toFaceUp, Action onComplete = null);
        /// <summary>Sets face up/down immediately with no flip animation.</summary>
        void SetFaceUpImmediate(bool faceUp);
        bool IsFaceUp { get; }
        void SetGlow(bool enabled);
        void StartGlowPulse();
        void StopGlowPulse();
    }
}
