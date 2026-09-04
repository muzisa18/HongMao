using NUnit.Framework;

namespace HongMao.Tests
{
    public sealed class AttackSelectionPolicyTests
    {
        [TestCase(true, 0f, AttackKind.DashSlash, true)]
        [TestCase(true, 1f, AttackKind.Uppercut, true)]
        [TestCase(false, -1f, AttackKind.DiveSlam, true)]
        [TestCase(true, -1f, AttackKind.GroundOne, false)]
        public void EmpoweredSelection_RespectsDirectionAndGroundedState(bool grounded, float vertical,
            AttackKind expected, bool consumes)
        {
            AttackChoice choice = AttackSelectionPolicy.Select(false, true, grounded, vertical, 0);
            Assert.That(choice.Kind, Is.EqualTo(expected));
            Assert.That(choice.ConsumesEmpowered, Is.EqualTo(consumes));
        }

        [Test]
        public void MaskSelection_OverridesEmpoweredToken()
        {
            AttackChoice choice = AttackSelectionPolicy.Select(true, true, true, 1f, 2);
            Assert.That(choice.Kind, Is.EqualTo(AttackKind.MaskGroundThree));
            Assert.That(choice.ConsumesEmpowered, Is.False);
        }
    }
}
