using UnityEngine;

namespace HongMao
{
    [CreateAssetMenu(menuName = "HongMao/Attack Definition", fileName = "AttackDefinition")]
    public sealed class AttackDefinition : ScriptableObject
    {
        public AttackKind kind;
        [Min(0f)] public float startup = 0.12f;
        [Min(0.01f)] public float active = 0.08f;
        [Min(0f)] public float recovery = 0.2f;
        [Min(0)] public int healthDamage = 10;
        [Min(0)] public int poiseDamage = 8;
        public Vector2 knockback = new(3f, 1f);
        public Vector2 hitboxSize = new(1.8f, 1.4f);
        public Vector2 hitboxOffset = new(1.1f, 0f);
        public float movementImpulse;
        public bool launches;

        public float TotalDuration => startup + active + recovery;
    }
}
