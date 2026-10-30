using UnityEngine;

namespace HongMao
{
    // Keep the motor's action timers running, then own movement after its physics update.
    [DefaultExecutionOrder(100)]
    [DisallowMultipleComponent]
    public sealed class FirstLevelSlide : MonoBehaviour
    {
        [SerializeField] FirstLevelController level;
        [SerializeField] PlayerCombatController player;
        [SerializeField] Vector2 start;
        [SerializeField] Vector2 end;
        [SerializeField, Min(.1f)] float speed = 7f;

        PlayerInputReader m_Input;
        PlayerMotor2D m_Motor;
        Rigidbody2D m_Body;
        Collider2D m_Collider;
        Vector2 m_Direction;
        Vector2 m_Normal;
        Vector2 m_CenterOffset;
        float m_Length;
        float m_Distance;
        float m_SupportDistance;
        float m_Gravity;
        bool m_InputWasBlocked;

        public bool IsSliding { get; private set; }
        public bool HasBeenUsed { get; private set; }

        public void Configure(FirstLevelController controller, PlayerCombatController actor,
            Vector2 startSurface, Vector2 endSurface, float slideSpeed = 7f)
        {
            StopSliding();
            level = controller;
            player = actor;
            start = startSurface;
            end = endSurface;
            speed = Mathf.Max(.1f, slideSpeed);
            CachePlayer();
        }

        void Awake() => CachePlayer();

        void CachePlayer()
        {
            Vector2 segment = end - start;
            m_Length = segment.magnitude;
            m_Direction = m_Length > .01f ? segment / m_Length : Vector2.zero;
            m_Normal = new Vector2(-m_Direction.y, m_Direction.x);
            if (player == null) return;
            m_Input = player.GetComponent<PlayerInputReader>();
            m_Motor = player.GetComponent<PlayerMotor2D>();
            m_Body = player.GetComponent<Rigidbody2D>();
            m_Collider = player.GetComponent<Collider2D>();
        }

        void Update()
        {
            if (level == null || player == null || m_Input == null || m_Body == null || m_Collider == null) return;
            if (level.IsComplete || level.IsFailed || !m_Body.simulated)
            {
                StopSliding();
                return;
            }
            if (IsSliding || m_Input.GameplayBlocked || !m_Motor.enabled || m_Length < .01f || end.y >= start.y) return;

            Bounds bounds = m_Collider.bounds;
            Vector2 center = bounds.center;
            m_CenterOffset = center - m_Body.position;
            // An upright box rests on its uphill bottom corner, not the centre of its feet.
            m_SupportDistance = Mathf.Abs(m_Normal.x) * bounds.extents.x + Mathf.Abs(m_Normal.y) * bounds.extents.y;
            Vector2 relative = center - start;
            float distance = Vector2.Dot(relative, m_Direction);
            float clearance = Vector2.Dot(relative, m_Normal) - m_SupportDistance;
            if (distance < 0f || distance > m_Length || clearance < -.18f || clearance > .3f) return;
            // The flat landing remains free to walk on; trying to climb back onto the ramp slides down again.
            if (distance > m_Length - .3f && m_Input.Horizontal >= -.05f && m_Body.linearVelocity.x >= -.1f) return;

            IsSliding = true;
            HasBeenUsed = true;
            m_Distance = distance;
            m_Gravity = m_Body.gravityScale;
            m_InputWasBlocked = m_Input.GameplayBlocked;
            m_Input.SetGameplayBlocked(true);
            player.SetCombatEnabled(false);
            m_Body.gravityScale = 0f;
            m_Body.linearVelocity = Vector2.zero;
        }

        void FixedUpdate()
        {
            if (!IsSliding) return;
            if (level == null || level.IsComplete || level.IsFailed || !m_Body.simulated || m_Distance >= m_Length)
            {
                StopSliding();
                return;
            }
            m_Distance = Mathf.Min(m_Length, m_Distance + speed * Time.fixedDeltaTime);
            Vector2 target = start + m_Direction * m_Distance + m_Normal * (m_SupportDistance + .025f) - m_CenterOffset;
            // Follow the real collidable ramp continuously, including when landing partway down it.
            m_Body.linearVelocity = Vector2.zero;
            m_Body.MovePosition(Vector2.MoveTowards(m_Body.position, target, speed * Time.fixedDeltaTime));
        }

        void StopSliding()
        {
            if (!IsSliding) return;
            IsSliding = false;
            if (m_Body != null)
            {
                m_Body.gravityScale = m_Gravity;
                m_Body.linearVelocity = Vector2.zero;
            }
            bool canPlay = level != null && !level.IsComplete && !level.IsFailed;
            if (m_Input != null) m_Input.SetGameplayBlocked(m_InputWasBlocked || !canPlay);
            if (player != null) player.SetCombatEnabled(canPlay);
        }

        void OnDisable() => StopSliding();
    }
}
