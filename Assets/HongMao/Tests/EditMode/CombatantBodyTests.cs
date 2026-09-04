using NUnit.Framework;
using UnityEngine;

namespace HongMao.Tests
{
    public sealed class CombatantBodyTests
    {
        GameObject m_Object;
        CombatantBody m_Body;

        [SetUp]
        public void SetUp()
        {
            m_Object = new GameObject("BodyTest");
            m_Body = m_Object.AddComponent<CombatantBody>();
            m_Body.Configure(100, 20);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(m_Object);

        [Test]
        public void Parry_OnlyRejectsParryableAttack()
        {
            m_Body.IsParrying = true;
            HitResult parried = m_Body.ReceiveHit(Hit(1, true));
            HitResult damaged = m_Body.ReceiveHit(Hit(2, false));
            Assert.That(parried.Outcome, Is.EqualTo(HitOutcome.Parried));
            Assert.That(damaged.Outcome, Is.EqualTo(HitOutcome.Damaged));
        }

        [Test]
        public void Invulnerability_ReportsRealDodgeWithoutDamage()
        {
            m_Body.IsInvulnerable = true;
            HitResult result = m_Body.ReceiveHit(Hit(1, false));
            Assert.That(result.Outcome, Is.EqualTo(HitOutcome.Dodged));
            Assert.That(m_Body.Health, Is.EqualTo(100));
        }

        [Test]
        public void PoiseBreak_EnablesLaunchAndCanRecover()
        {
            HitResult result = m_Body.ReceiveHit(new HitRequest(1, 1, 20, new Vector2(0, 8), false, true, null));
            Assert.That(result.BrokePoise, Is.True);
            Assert.That(m_Body.IsPoiseBroken, Is.True);
            m_Body.RecoverPoise();
            Assert.That(m_Body.Poise, Is.EqualTo(20));
            Assert.That(m_Body.IsPoiseBroken, Is.False);
        }

        static HitRequest Hit(int id, bool parryable) => new(id, 10, 5, Vector2.zero, parryable, false, null);
    }
}
