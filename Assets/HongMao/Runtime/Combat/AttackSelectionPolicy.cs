namespace HongMao
{
    public readonly struct AttackChoice
    {
        public AttackChoice(AttackKind kind, bool consumesEmpowered)
        {
            Kind = kind;
            ConsumesEmpowered = consumesEmpowered;
        }

        public AttackKind Kind { get; }
        public bool ConsumesEmpowered { get; }
    }

    public static class AttackSelectionPolicy
    {
        public static AttackChoice Select(bool maskActive, bool hasEmpowered, bool grounded, float vertical, int comboIndex)
        {
            if (maskActive)
            {
                AttackKind maskKind = grounded
                    ? (AttackKind)((int)AttackKind.MaskGroundOne + comboIndex % 3)
                    : AttackKind.MaskAir;
                return new AttackChoice(maskKind, false);
            }

            if (hasEmpowered && vertical > 0.3f)
                return new AttackChoice(AttackKind.Uppercut, true);
            if (hasEmpowered && !grounded && vertical < -0.3f)
                return new AttackChoice(AttackKind.DiveSlam, true);
            if (hasEmpowered && vertical >= -0.3f)
                return new AttackChoice(AttackKind.DashSlash, true);

            AttackKind normal = grounded
                ? (AttackKind)((int)AttackKind.GroundOne + comboIndex % 3)
                : AttackKind.Air;
            return new AttackChoice(normal, false);
        }
    }
}
