using System;

namespace HongMao
{
    public sealed class CombatResourceModel
    {
        public const int MaxCharge = 3;
        public const int MaxColorSlots = 3;
        public const float DefaultMaskDuration = 8f;

        readonly ColorKind[] m_ColorSlots = new ColorKind[MaxColorSlots];

        public event Action Changed;

        public int Charge { get; private set; }
        public bool HasEmpoweredAttack { get; private set; }
        public bool IsMaskActive { get; private set; }
        public float MaskRemaining { get; private set; }

        public int FilledColorSlots
        {
            get
            {
                int count = 0;
                for (int i = 0; i < m_ColorSlots.Length; i++)
                    if (m_ColorSlots[i] != ColorKind.None) count++;
                return count;
            }
        }

        public bool CanAbsorb => !IsMaskActive && Charge == MaxCharge && FilledColorSlots < MaxColorSlots;
        public bool CanActivateMask => !IsMaskActive && FilledColorSlots == MaxColorSlots && AllSlotsAre(ColorKind.Red);

        public ColorKind GetSlot(int index)
        {
            if (index < 0 || index >= MaxColorSlots) throw new ArgumentOutOfRangeException(nameof(index));
            return m_ColorSlots[index];
        }

        public bool TryAddCharge(int amount)
        {
            if (amount <= 0 || IsMaskActive) return false;
            int next = Math.Min(MaxCharge, Charge + amount);
            if (next == Charge) return false;
            Charge = next;
            Changed?.Invoke();
            return true;
        }

        public bool TryCommitAbsorb(ColorKind color)
        {
            if (!CanAbsorb || color == ColorKind.None) return false;
            m_ColorSlots[FilledColorSlots] = color;
            Charge = 0;
            HasEmpoweredAttack = true;
            Changed?.Invoke();
            return true;
        }

        public bool TryConsumeEmpoweredAttack()
        {
            if (!HasEmpoweredAttack || IsMaskActive) return false;
            HasEmpoweredAttack = false;
            Changed?.Invoke();
            return true;
        }

        public bool TryActivateMask(float duration = DefaultMaskDuration)
        {
            if (!CanActivateMask || duration <= 0f) return false;
            ClearSlots();
            HasEmpoweredAttack = false;
            IsMaskActive = true;
            MaskRemaining = duration;
            Changed?.Invoke();
            return true;
        }

        public void TickMask(float normalGameSeconds)
        {
            if (!IsMaskActive || normalGameSeconds <= 0f) return;
            MaskRemaining = Math.Max(0f, MaskRemaining - normalGameSeconds);
            if (MaskRemaining <= 0f)
            {
                IsMaskActive = false;
                MaskRemaining = 0f;
            }
            Changed?.Invoke();
        }

        public void EndMask()
        {
            if (!IsMaskActive) return;
            IsMaskActive = false;
            MaskRemaining = 0f;
            Changed?.Invoke();
        }

        public void Reset()
        {
            Charge = 0;
            HasEmpoweredAttack = false;
            IsMaskActive = false;
            MaskRemaining = 0f;
            ClearSlots();
            Changed?.Invoke();
        }

        bool AllSlotsAre(ColorKind color)
        {
            for (int i = 0; i < m_ColorSlots.Length; i++)
                if (m_ColorSlots[i] != color) return false;
            return true;
        }

        void ClearSlots()
        {
            for (int i = 0; i < m_ColorSlots.Length; i++) m_ColorSlots[i] = ColorKind.None;
        }
    }
}
