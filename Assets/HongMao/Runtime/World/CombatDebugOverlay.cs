using UnityEngine;

namespace HongMao
{
    [ExecuteAlways]
    public sealed class CombatDebugOverlay : MonoBehaviour
    {
        [SerializeField] PlayerCombatController player;
        [SerializeField] CombatBotController enemy;
        [SerializeField] AttackDefinition previewAttack;
        [SerializeField] bool showBodyColliders = true;
        [SerializeField] bool showGroundProbe = true;
        [SerializeField] bool showEnemyRange = true;
        [SerializeField] bool showPreviewAttack = true;

        public void Configure(PlayerCombatController playerController, CombatBotController bot,
            AttackDefinition attack)
        {
            player = playerController;
            enemy = bot;
            previewAttack = attack;
        }

        void OnDrawGizmos()
        {
            if (showBodyColliders)
            {
                DrawCollider(player == null ? null : player.GetComponent<Collider2D>(), new Color(0.1f, 0.8f, 1f, 0.9f));
                DrawCollider(enemy == null ? null : enemy.GetComponent<Collider2D>(), new Color(1f, 0.2f, 0.5f, 0.9f));
            }

            if (player != null && showGroundProbe)
            {
                Collider2D collider = player.GetComponent<Collider2D>();
                if (collider != null)
                {
                    Bounds bounds = collider.bounds;
                    Gizmos.color = new Color(0.15f, 1f, 0.9f, 0.85f);
                    Gizmos.DrawWireCube(new Vector3(bounds.center.x, bounds.min.y - 0.04f),
                        new Vector3(bounds.size.x * 0.72f, 0.12f, 0f));
                }
            }

            if (enemy != null && showEnemyRange)
            {
                Gizmos.color = new Color(1f, 0.55f, 0.05f, 0.75f);
                Gizmos.DrawWireCube(enemy.transform.position + new Vector3(0f, 0.15f), new Vector3(4.1f, 1.7f, 0f));
            }

            if (player != null && previewAttack != null && showPreviewAttack)
            {
                int facing = player.GetComponent<PlayerMotor2D>()?.Facing ?? 1;
                Vector2 offset = previewAttack.hitboxOffset;
                offset.x *= facing;
                Gizmos.color = new Color(1f, 0.85f, 0.1f, 0.9f);
                Gizmos.DrawWireCube((Vector2)player.transform.position + offset, previewAttack.hitboxSize);
            }
        }

        static void DrawCollider(Collider2D collider, Color color)
        {
            if (collider == null) return;
            Gizmos.color = color;
            Bounds bounds = collider.bounds;
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }
    }
}
