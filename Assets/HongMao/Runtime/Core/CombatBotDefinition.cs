using UnityEngine;

namespace HongMao
{
    [CreateAssetMenu(menuName = "HongMao/Combat Bot Definition", fileName = "CombatBotConfig")]
    public sealed class CombatBotDefinition : ScriptableObject
    {
        [Header("Body")]
        [Min(1)] public int maxHealth = 400;
        [Min(0)] public int maxPoise = 100;
        [Min(0f)] public float gravityScale = 2.2f;

        [Header("Movement and spacing")]
        [Min(0f)] public float moveSpeed = 2.2f;
        [Min(0f)] public float attackRange = 2.05f;
        [Min(0f)] public float openingGrace = 2.5f;

        [Header("Attack timing")]
        [Min(0f)] public float parryableTelegraph = 0.7f;
        [Min(0f)] public float unblockableTelegraph = 0.9f;
        [Min(0f)] public float attackRecovery = 0.75f;
        [Min(0f)] public float staggerDuration = 1.5f;

        [Header("Damage")]
        [Min(0)] public int parryableDamage = 15;
        [Min(0)] public int unblockableDamage = 20;
        public Vector2 knockback = new(4.5f, 2.5f);
        public Vector2 hitboxSize = new(2.2f, 1.7f);
        public Vector2 hitboxOffset = new(1.1f, 0.15f);
    }
}
