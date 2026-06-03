using System;
using UnityEngine;

namespace Blackjack
{
    public interface ICardDisplay
    {
        void Setup(Sprite faceSprite, Sprite backSprite, bool faceUp = true);
        void Flip(bool toFaceUp, Action onComplete = null);
        bool IsFaceUp { get; }
        void SetGlow(bool enabled);
        void StartGlowPulse();
        void StopGlowPulse();
    }
}
