using UnityEngine;

namespace HongMao
{
    [CreateAssetMenu(menuName = "HongMao/Player Definition", fileName = "PlayerConfig")]
    public sealed class PlayerDefinition : ScriptableObject
    {
        [Header("Body")]
        [Min(1)] public int maxHealth = 100;
        [Min(0)] public int maxPoise;

        [Header("Movement")]
        [Min(0f)] public float moveSpeed = 6f;
        [Min(0f)] public float jumpSpeed = 8.4f;
        [Min(0f)] public float gravityScale = 2.2f;
        [Min(0f)] public float coyoteTime = 0.1f;
        [Min(0f)] public float jumpBuffer = 0.12f;

        [Header("Dodge")]
        [Min(0f)] public float dodgeSpeed = 12f;
        [Min(0.01f)] public float dodgeDuration = 0.3f;
        [Min(0f)] public float dodgeInvulnerability = 0.18f;
        [Min(0f)] public float dodgeCooldown = 0.45f;

        [Header("Parry")]
        [Min(0f)] public float parryWindow = 0.18f;
        [Min(0f)] public float parryRecovery = 0.3f;
    }
}
