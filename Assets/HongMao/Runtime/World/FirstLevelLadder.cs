using UnityEngine;

namespace HongMao
{
    // Read the sampled W/S input before the normal motor or combat update.
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    public sealed class FirstLevelLadder : MonoBehaviour
    {
        [SerializeField] FirstLevelController level;
        [SerializeField] PlayerCombatController player;
        [SerializeField] Transform bottom;
        [SerializeField] Transform top;
        [SerializeField, Min(.1f)] float speed = 5f;
        PlayerInputReader m_Input;
        PlayerMotor2D m_Motor;
        Rigidbody2D m_Body;
        float m_Gravity;
        bool m_MotorWasEnabled;
        bool m_CombatWasEnabled;

        public bool IsClimbing { get; private set; }
        public bool HasBeenUsed { get; private set; }

        public void Configure(FirstLevelController controller, PlayerCombatController actor,
            Transform bottomMount, Transform topMount, float climbSpeed = 5f)
        {
            level = controller;
            player = actor;
            bottom = bottomMount;
            top = topMount;
            speed = Mathf.Max(.1f, climbSpeed);
            CachePlayer();
        }

        void Awake() => CachePlayer();

        void CachePlayer()
        {
            if (player == null) return;
            m_Input = player.GetComponent<PlayerInputReader>();
            m_Motor = player.GetComponent<PlayerMotor2D>();
            m_Body = player.GetComponent<Rigidbody2D>();
        }

        void Update()
        {
            if (level == null || player == null || m_Input == null || bottom == null || top == null) return;
            if (level.IsComplete || level.IsFailed)
            {
                StopClimbing();
                return;
            }
            if (IsClimbing || m_Input.Vertical <= .1f || m_Input.GameplayBlocked || player.IsActionBusy || player.IsAbsorbing) return;
            if (((Vector2)(player.transform.position - bottom.position)).sqrMagnitude > 1.3f * 1.3f) return;
            IsClimbing = true;
            HasBeenUsed = true;
            m_Gravity = m_Body.gravityScale;
            m_MotorWasEnabled = m_Motor.enabled;
            m_CombatWasEnabled = player.enabled;
            player.SetCombatEnabled(false);
            player.enabled = false;
            m_Motor.enabled = false;
            m_Body.gravityScale = 0f;
            m_Body.linearVelocity = Vector2.zero;
            m_Body.position = bottom.position;
        }

        void FixedUpdate()
        {
            if (!IsClimbing || level.IsComplete || level.IsFailed) return;
            m_Body.linearVelocity = Vector2.zero;
            float vertical = m_Input.Vertical;
            float nextY = m_Body.position.y + vertical * speed * Time.fixedDeltaTime;
            if (vertical > .1f && nextY >= top.position.y + .15f)
            {
                // Ascend beside the ledge first, then dismount with feet above its top.
                m_Body.position = top.position;
                StopClimbing();
                return;
            }
            if (vertical < -.1f && nextY <= bottom.position.y)
            {
                m_Body.position = bottom.position;
                StopClimbing();
                return;
            }
            m_Body.MovePosition(new Vector2(bottom.position.x, Mathf.Max(bottom.position.y, nextY)));
        }

        void StopClimbing()
        {
            if (!IsClimbing) return;
            IsClimbing = false;
            if (m_Body != null)
            {
                m_Body.gravityScale = m_Gravity;
                m_Body.linearVelocity = Vector2.zero;
            }
            if (m_Motor != null) m_Motor.enabled = m_MotorWasEnabled;
            if (player != null)
            {
                player.enabled = m_CombatWasEnabled;
                player.SetCombatEnabled(level != null && !level.IsComplete && !level.IsFailed);
            }
        }

        void OnDisable() => StopClimbing();
    }
}
