using System;
using UnityEngine;

namespace HongMao
{
    [DisallowMultipleComponent]
    public sealed class CombatantBody : MonoBehaviour, IHitReceiver
    {
        Rigidbody2D m_Body;

        public event Action Changed;
        public event Action<HitRequest, HitResult> HitResolved;
        public event Action PoiseBroken;
        public event Action Died;

        public int MaxHealth { get; private set; }
        public int Health { get; private set; }
        public int MaxPoise { get; private set; }
        public int Poise { get; private set; }
        public bool IsDead => Health <= 0;
        public bool IsPoiseBroken { get; private set; }
        public bool IsInvulnerable { get; set; }
        public bool IsParrying { get; set; }

        public void Configure(int health, int poise)
        {
            MaxHealth = Mathf.Max(1, health);
            Health = MaxHealth;
            MaxPoise = Mathf.Max(0, poise);
            Poise = MaxPoise;
            IsPoiseBroken = false;
            IsInvulnerable = false;
            IsParrying = false;
            m_Body = GetComponent<Rigidbody2D>();
            Changed?.Invoke();
        }

        public HitResult ReceiveHit(HitRequest request)
        {
            if (IsDead)
                return Resolve(request, new HitResult(HitOutcome.Invulnerable, 0, 0, false));

            if (IsParrying && request.CanBeParried)
                return Resolve(request, new HitResult(HitOutcome.Parried, 0, 0, false));

            if (IsInvulnerable)
                return Resolve(request, new HitResult(HitOutcome.Dodged, 0, 0, false));

            int appliedHealth = Mathf.Min(Health, Mathf.Max(0, request.HealthDamage));
            int appliedPoise = IsPoiseBroken ? 0 : Mathf.Min(Poise, Mathf.Max(0, request.PoiseDamage));
            Health -= appliedHealth;
            Poise -= appliedPoise;

            bool brokePoise = MaxPoise > 0 && !IsPoiseBroken && Poise <= 0;
            if (brokePoise)
            {
                IsPoiseBroken = true;
                PoiseBroken?.Invoke();
            }

            if (m_Body != null && m_Body.bodyType == RigidbodyType2D.Dynamic)
            {
                Vector2 force = request.Knockback;
                if (request.Launches && (IsPoiseBroken || brokePoise)) force.y = Mathf.Max(force.y, 8f);
                m_Body.linearVelocity = new Vector2(force.x, Mathf.Max(m_Body.linearVelocity.y, force.y));
            }

            HitOutcome outcome = Health <= 0 ? HitOutcome.Killed : HitOutcome.Damaged;
            var result = new HitResult(outcome, appliedHealth, appliedPoise, brokePoise);
            Changed?.Invoke();
            if (Health <= 0) Died?.Invoke();
            return Resolve(request, result);
        }

        public void RecoverPoise()
        {
            if (MaxPoise <= 0 || IsDead) return;
            Poise = MaxPoise;
            IsPoiseBroken = false;
            Changed?.Invoke();
        }

        public void ResetBody()
        {
            Health = MaxHealth;
            Poise = MaxPoise;
            IsPoiseBroken = false;
            IsInvulnerable = false;
            IsParrying = false;
            Changed?.Invoke();
        }

        HitResult Resolve(HitRequest request, HitResult result)
        {
            HitResolved?.Invoke(request, result);
            return result;
        }
    }
}
