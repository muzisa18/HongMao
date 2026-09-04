using UnityEngine;

namespace HongMao
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public sealed class PlayerMotor2D : MonoBehaviour
    {
        const float MoveSpeed = 6f;
        const float JumpSpeed = 8.4f;
        const float CoyoteTime = 0.1f;
        const float JumpBuffer = 0.12f;
        const float DodgeSpeed = 12f;

        Rigidbody2D m_Body;
        Collider2D m_Collider;
        PlayerInputReader m_Input;
        float m_CoyoteRemaining;
        float m_JumpBufferRemaining;
        float m_DodgeRemaining;
        float m_DodgeDirection = 1f;
        float m_ForcedHorizontalRemaining;
        float m_ForcedHorizontalVelocity;
        bool m_InputEnabled = true;

        public bool IsGrounded { get; private set; }
        public bool IsDodging => m_DodgeRemaining > 0f;
        public bool AirDodgeAvailable { get; private set; } = true;
        public int Facing { get; private set; } = 1;
        public Vector2 Velocity => m_Body == null ? Vector2.zero : m_Body.linearVelocity;

        public void Configure(PlayerInputReader input)
        {
            m_Input = input;
            m_Body = GetComponent<Rigidbody2D>();
            m_Collider = GetComponent<Collider2D>();
            m_Body.freezeRotation = true;
            m_Body.gravityScale = 2.2f;
            m_Body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            m_Body.interpolation = RigidbodyInterpolation2D.Interpolate;
        }

        public void SetInputEnabled(bool enabled) => m_InputEnabled = enabled;

        public bool TryBeginDodge(float duration)
        {
            if (IsDodging || (!IsGrounded && !AirDodgeAvailable)) return false;
            m_DodgeRemaining = duration;
            m_DodgeDirection = Mathf.Abs(m_Input.Horizontal) > 0.1f ? Mathf.Sign(m_Input.Horizontal) : Facing;
            if (!IsGrounded) AirDodgeAvailable = false;
            return true;
        }

        public void AddImpulse(Vector2 impulse)
        {
            m_Body.linearVelocity = new Vector2(m_Body.linearVelocity.x + impulse.x, m_Body.linearVelocity.y + impulse.y);
        }

        public void BeginForcedHorizontal(float velocity, float duration)
        {
            m_ForcedHorizontalVelocity = velocity;
            m_ForcedHorizontalRemaining = Mathf.Max(0f, duration);
        }

        public void SetVelocity(Vector2 velocity) => m_Body.linearVelocity = velocity;

        void Update()
        {
            if (m_Input == null) return;
            if (m_Input.JumpPressed && m_InputEnabled) m_JumpBufferRemaining = JumpBuffer;
            else m_JumpBufferRemaining -= Time.deltaTime;

            if (IsGrounded)
            {
                m_CoyoteRemaining = CoyoteTime;
                AirDodgeAvailable = true;
            }
            else m_CoyoteRemaining -= Time.deltaTime;

            if (m_JumpBufferRemaining > 0f && m_CoyoteRemaining > 0f && !IsDodging)
            {
                m_Body.linearVelocity = new Vector2(m_Body.linearVelocity.x, JumpSpeed);
                m_JumpBufferRemaining = 0f;
                m_CoyoteRemaining = 0f;
                IsGrounded = false;
            }
        }

        void FixedUpdate()
        {
            if (m_Body == null || m_Input == null) return;
            UpdateGrounded();

            if (m_DodgeRemaining > 0f)
            {
                m_DodgeRemaining -= Time.fixedDeltaTime;
                m_Body.linearVelocity = new Vector2(m_DodgeDirection * DodgeSpeed, 0f);
                return;
            }

            if (m_ForcedHorizontalRemaining > 0f)
            {
                m_ForcedHorizontalRemaining -= Time.fixedDeltaTime;
                m_Body.linearVelocity = new Vector2(m_ForcedHorizontalVelocity, m_Body.linearVelocity.y);
                return;
            }

            float horizontal = m_InputEnabled ? m_Input.Horizontal : 0f;
            if (Mathf.Abs(horizontal) > 0.05f) Facing = horizontal > 0f ? 1 : -1;
            m_Body.linearVelocity = new Vector2(horizontal * MoveSpeed, m_Body.linearVelocity.y);
        }

        void UpdateGrounded()
        {
            Bounds bounds = m_Collider.bounds;
            Vector2 origin = new(bounds.center.x, bounds.min.y + 0.02f);
            RaycastHit2D[] hits = Physics2D.BoxCastAll(origin, new Vector2(bounds.size.x * 0.72f, 0.08f), 0f, Vector2.down, 0.12f);
            IsGrounded = false;
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].collider != null && hits[i].collider != m_Collider && !hits[i].collider.isTrigger)
                {
                    IsGrounded = true;
                    break;
                }
            }
        }
    }
}
