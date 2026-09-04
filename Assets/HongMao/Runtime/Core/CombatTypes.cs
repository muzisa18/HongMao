using System;
using UnityEngine;

namespace HongMao
{
    public enum ColorKind
    {
        None,
        Red
    }

    public enum AttackKind
    {
        GroundOne,
        GroundTwo,
        GroundThree,
        Air,
        DashSlash,
        Uppercut,
        DiveSlam,
        MaskGroundOne,
        MaskGroundTwo,
        MaskGroundThree,
        MaskAir
    }

    public enum HitOutcome
    {
        Missed,
        Damaged,
        Parried,
        Dodged,
        Invulnerable,
        Killed
    }

    public readonly struct HitRequest
    {
        public HitRequest(int attackId, int healthDamage, int poiseDamage, Vector2 knockback,
            bool canBeParried, bool launches, GameObject attacker)
        {
            AttackId = attackId;
            HealthDamage = healthDamage;
            PoiseDamage = poiseDamage;
            Knockback = knockback;
            CanBeParried = canBeParried;
            Launches = launches;
            Attacker = attacker;
        }

        public int AttackId { get; }
        public int HealthDamage { get; }
        public int PoiseDamage { get; }
        public Vector2 Knockback { get; }
        public bool CanBeParried { get; }
        public bool Launches { get; }
        public GameObject Attacker { get; }
    }

    public readonly struct HitResult
    {
        public HitResult(HitOutcome outcome, int healthDamage, int poiseDamage, bool brokePoise)
        {
            Outcome = outcome;
            HealthDamage = healthDamage;
            PoiseDamage = poiseDamage;
            BrokePoise = brokePoise;
        }

        public HitOutcome Outcome { get; }
        public int HealthDamage { get; }
        public int PoiseDamage { get; }
        public bool BrokePoise { get; }
    }

    public interface IHitReceiver
    {
        HitResult ReceiveHit(HitRequest request);
    }

    public interface IColorSource
    {
        ColorKind AvailableColor { get; }
        bool IsAvailable { get; }
        bool TryExtractColor(out ColorKind color);
        void SetHighlighted(bool highlighted);
    }
}
