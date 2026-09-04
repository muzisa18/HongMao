using NUnit.Framework;

namespace HongMao.Tests
{
    public sealed class CombatResourceModelTests
    {
        [Test]
        public void Charge_CapsAtThree_WithoutOverflow()
        {
            var model = new CombatResourceModel();
            Assert.That(model.TryAddCharge(2), Is.True);
            Assert.That(model.TryAddCharge(2), Is.True);
            Assert.That(model.Charge, Is.EqualTo(3));
            Assert.That(model.TryAddCharge(1), Is.False);
        }

        [Test]
        public void ParryAndDodgeRewards_CombineToFullCharge()
        {
            var model = new CombatResourceModel();
            model.TryAddCharge(2);
            model.TryAddCharge(1);
            Assert.That(model.Charge, Is.EqualTo(3));
            Assert.That(model.CanAbsorb, Is.True);
        }

        [Test]
        public void Absorb_ClearsCharge_FillsSlot_AndRefreshesSingleEmpoweredToken()
        {
            var model = FullCharge();
            Assert.That(model.TryCommitAbsorb(ColorKind.Red), Is.True);
            Assert.That(model.Charge, Is.Zero);
            Assert.That(model.FilledColorSlots, Is.EqualTo(1));
            Assert.That(model.HasEmpoweredAttack, Is.True);

            model.TryAddCharge(3);
            Assert.That(model.TryCommitAbsorb(ColorKind.Red), Is.True);
            Assert.That(model.FilledColorSlots, Is.EqualTo(2));
            Assert.That(model.HasEmpoweredAttack, Is.True);
        }

        [Test]
        public void CancelledAbsorb_DoesNotMutateState()
        {
            var model = FullCharge();
            Assert.That(model.Charge, Is.EqualTo(3));
            Assert.That(model.FilledColorSlots, Is.Zero);
            Assert.That(model.HasEmpoweredAttack, Is.False);
        }

        [Test]
        public void EmpoweredAttack_DoesNotConsumeColorSlot()
        {
            var model = FullCharge();
            model.TryCommitAbsorb(ColorKind.Red);
            Assert.That(model.TryConsumeEmpoweredAttack(), Is.True);
            Assert.That(model.FilledColorSlots, Is.EqualTo(1));
            Assert.That(model.HasEmpoweredAttack, Is.False);
        }

        [Test]
        public void ThreeRedSlots_ActivateMask_AndMaskBlocksCharge()
        {
            var model = new CombatResourceModel();
            for (int i = 0; i < 3; i++)
            {
                model.TryAddCharge(3);
                Assert.That(model.TryCommitAbsorb(ColorKind.Red), Is.True);
            }
            Assert.That(model.CanAbsorb, Is.False);
            Assert.That(model.CanActivateMask, Is.True);
            Assert.That(model.TryActivateMask(), Is.True);
            Assert.That(model.FilledColorSlots, Is.Zero);
            Assert.That(model.HasEmpoweredAttack, Is.False);
            Assert.That(model.TryAddCharge(3), Is.False);
            model.TickMask(8f);
            Assert.That(model.IsMaskActive, Is.False);
        }

        static CombatResourceModel FullCharge()
        {
            var model = new CombatResourceModel();
            model.TryAddCharge(3);
            return model;
        }
    }
}
