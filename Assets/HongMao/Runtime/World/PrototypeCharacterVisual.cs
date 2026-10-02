using UnityEngine;

namespace HongMao
{
    [DisallowMultipleComponent]
    public sealed class PrototypeCharacterVisual : MonoBehaviour
    {
        [SerializeField] Transform visualRoot;
        [SerializeField] Transform head;
        [SerializeField] Transform weapon;
        [SerializeField] SpriteRenderer[] tintedParts;
        [SerializeField] SpriteRenderer animatedRenderer;
        [SerializeField] Sprite[] groundOneFrames;
        [SerializeField] Sprite[] groundTwoFrames;
        [SerializeField] Sprite[] groundThreeFrames;
        [SerializeField] Sprite[] runFrames;
        [SerializeField] Sprite[] groundDodgeFrames;

        Rigidbody2D m_Body;
        PlayerMotor2D m_PlayerMotor;
        PlayerCombatController m_PlayerCombat;
        CombatBotController m_Bot;
        Color[] m_BaseColors;
        Vector3 m_VisualHome;
        Quaternion m_HeadHome;
        Quaternion m_WeaponHome;
        float m_AttackElapsed;
        float m_AttackStartup;
        float m_AttackActive;
        float m_AttackRecovery;
        float m_RunElapsed;
        Sprite[] m_ActiveAttackFrames;
        bool m_IsPlayingGroundAttack;
        bool m_IsShowingGroundDodge;
        bool m_IsShowingRun;
        int m_LastFacing = 1;

        public void Configure(Transform root, Transform headTransform, Transform weaponTransform,
            SpriteRenderer[] renderers)
        {
            visualRoot = root;
            head = headTransform;
            weapon = weaponTransform;
            tintedParts = renderers;
            CacheAuthoringPose();
        }

        public void ConfigureSpriteAnimation(SpriteRenderer renderer, Sprite[] firstAttackFrames,
            Sprite[] secondAttackFrames, Sprite[] thirdAttackFrames, Sprite[] runningFrames)
        {
            animatedRenderer = renderer;
            groundOneFrames = firstAttackFrames;
            groundTwoFrames = secondAttackFrames;
            groundThreeFrames = thirdAttackFrames;
            runFrames = runningFrames;
            bool containsRenderer = false;
            if (tintedParts != null)
            {
                for (int i = 0; i < tintedParts.Length; i++)
                    if (tintedParts[i] == renderer) containsRenderer = true;
            }

            if (renderer != null && !containsRenderer)
            {
                int existingCount = tintedParts == null ? 0 : tintedParts.Length;
                var renderers = new SpriteRenderer[existingCount + 1];
                for (int i = 0; i < existingCount; i++) renderers[i] = tintedParts[i];
                renderers[existingCount] = renderer;
                tintedParts = renderers;
            }

            CacheAuthoringPose();
            ShowIdleSprite();
        }

        public void PlayAttack(AttackKind kind, float startup, float active, float recovery)
        {
            Sprite[] frames = GetAttackFrames(kind);
            if (animatedRenderer == null || frames == null || frames.Length < 6)
            {
                StopAttackAnimation();
                return;
            }

            m_ActiveAttackFrames = frames;
            m_AttackElapsed = 0f;
            m_AttackStartup = Mathf.Max(0.001f, startup);
            m_AttackActive = Mathf.Max(0.001f, active);
            m_AttackRecovery = Mathf.Max(0.001f, recovery);
            m_IsPlayingGroundAttack = true;
            SetAnimatedFrame(0);
        }

        public void StopAttackAnimation()
        {
            m_IsPlayingGroundAttack = false;
            m_ActiveAttackFrames = null;
            ShowIdleSprite();
        }

        Sprite[] GetAttackFrames(AttackKind kind)
        {
            if (kind == AttackKind.GroundOne) return groundOneFrames;
            if (kind == AttackKind.GroundTwo)
                return groundTwoFrames != null && groundTwoFrames.Length >= 6 ? groundTwoFrames : groundOneFrames;
            if (kind == AttackKind.GroundThree)
                return groundThreeFrames != null && groundThreeFrames.Length >= 6 ? groundThreeFrames : groundOneFrames;
            return null;
        }

        void Awake()
        {
            m_Body = GetComponent<Rigidbody2D>();
            m_PlayerMotor = GetComponent<PlayerMotor2D>();
            m_PlayerCombat = GetComponent<PlayerCombatController>();
            m_Bot = GetComponent<CombatBotController>();
            CacheAuthoringPose();
        }

        void CacheAuthoringPose()
        {
            if (visualRoot != null) m_VisualHome = visualRoot.localPosition;
            if (head != null) m_HeadHome = head.localRotation;
            if (weapon != null) m_WeaponHome = weapon.localRotation;
            if (tintedParts == null) return;
            m_BaseColors = new Color[tintedParts.Length];
            for (int i = 0; i < tintedParts.Length; i++)
                m_BaseColors[i] = tintedParts[i] == null ? Color.white : tintedParts[i].color;
        }

        public void SetTint(Color tint)
        {
            if (tintedParts == null || m_BaseColors == null || m_BaseColors.Length != tintedParts.Length)
                CacheAuthoringPose();
            if (tintedParts == null || m_BaseColors == null) return;
            for (int i = 0; i < tintedParts.Length; i++)
                if (tintedParts[i] != null) tintedParts[i].color = Color.Lerp(m_BaseColors[i], tint, 0.72f);
        }

        public void ClearTint()
        {
            if (tintedParts == null || m_BaseColors == null || m_BaseColors.Length != tintedParts.Length)
                CacheAuthoringPose();
            if (tintedParts == null || m_BaseColors == null) return;
            for (int i = 0; i < tintedParts.Length; i++)
                if (tintedParts[i] != null) tintedParts[i].color = m_BaseColors[i];
        }

        void LateUpdate()
        {
            if (visualRoot == null || m_Body == null) return;

            if (m_PlayerMotor != null) m_LastFacing = m_PlayerMotor.Facing;
            else if (Mathf.Abs(m_Body.linearVelocity.x) > 0.05f) m_LastFacing = m_Body.linearVelocity.x > 0f ? 1 : -1;

            Vector3 scale = visualRoot.localScale;
            scale.x = Mathf.Abs(scale.x) * m_LastFacing;
            visualRoot.localScale = scale;

            float movement = Mathf.Clamp01(Mathf.Abs(m_Body.linearVelocity.x) / 3f);
            float bob = animatedRenderer != null && animatedRenderer.enabled
                ? 0f
                : Mathf.Sin(Time.time * 12f) * 0.035f * movement;
            visualRoot.localPosition = m_VisualHome + Vector3.up * bob;
            UpdateSpriteAnimation();

            if (animatedRenderer != null && animatedRenderer.enabled) return;

            if (head != null) head.localRotation = m_HeadHome * Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 3f) * 1.5f);

            bool acting = (m_PlayerCombat != null && m_PlayerCombat.IsActionBusy) || (m_Bot != null && m_Bot.IsActing);
            if (weapon != null)
            {
                float swing = acting ? Mathf.Sin(Time.time * 20f) * 38f - 24f : Mathf.Sin(Time.time * 2.5f) * 2f;
                weapon.localRotation = m_WeaponHome * Quaternion.Euler(0f, 0f, swing);
            }
        }

        void UpdateSpriteAnimation()
        {
            if (!m_IsPlayingGroundAttack)
            {
                UpdateRunAnimation();
                return;
            }

            m_AttackElapsed += Time.deltaTime;
            float activeEnd = m_AttackStartup + m_AttackActive;
            float total = activeEnd + m_AttackRecovery;

            int frame;
            if (m_AttackElapsed < m_AttackStartup * 0.35f) frame = 0;
            else if (m_AttackElapsed < m_AttackStartup * 0.75f) frame = 1;
            else if (m_AttackElapsed < m_AttackStartup) frame = 2;
            else if (m_AttackElapsed < activeEnd) frame = 3;
            else if (m_AttackElapsed < activeEnd + m_AttackRecovery * 0.55f) frame = 4;
            else frame = 5;

            SetAnimatedFrame(frame);
            if (m_AttackElapsed >= total) StopAttackAnimation();
        }

        bool UpdateGroundDodgeAnimation()
        {
            bool canDodge = animatedRenderer != null
                && groundDodgeFrames != null
                && groundDodgeFrames.Length >= 6
                && m_PlayerMotor != null
                && m_PlayerMotor.IsGrounded
                && m_PlayerMotor.IsDodging;

            if (!canDodge)
            {
                if (!m_IsShowingGroundDodge) return false;

                m_IsShowingGroundDodge = false;
                ShowIdleSprite();
                return false;
            }

            m_IsShowingGroundDodge = true;

            int frame = Mathf.Min(
                groundDodgeFrames.Length - 1,
                Mathf.FloorToInt(m_PlayerMotor.DodgeProgress * groundDodgeFrames.Length));

            SetSprite(groundDodgeFrames, frame);
            return true;
        }

        void UpdateRunAnimation()
        {
            bool canRun = animatedRenderer != null && runFrames != null && runFrames.Length >= 6
                && m_PlayerMotor != null && m_PlayerMotor.IsGrounded && !m_PlayerMotor.IsDodging
                && (m_PlayerCombat == null || !m_PlayerCombat.IsActionBusy)
                && Mathf.Abs(m_Body.linearVelocity.x) > 0.1f;

            if (!canRun)
            {
                if (!m_IsShowingRun) return;
                m_IsShowingRun = false;
                m_RunElapsed = 0f;
                ShowIdleSprite();
                return;
            }

            m_IsShowingRun = true;
            m_RunElapsed = Mathf.Repeat(m_RunElapsed + Time.deltaTime, 0.5f);
            int frame = Mathf.FloorToInt(m_RunElapsed * 12f) % runFrames.Length;
            SetSprite(runFrames, frame);
        }

        void ShowIdleSprite()
        {
            SetSprite(groundOneFrames, 0);
        }

        void SetAnimatedFrame(int index)
        {
            SetSprite(m_ActiveAttackFrames, index);
        }

        void SetSprite(Sprite[] frames, int index)
        {
            if (animatedRenderer == null || frames == null || frames.Length == 0) return;
            animatedRenderer.sprite = frames[Mathf.Clamp(index, 0, frames.Length - 1)];
        }

        void OnValidate()
        {
            if (!Application.isPlaying) ShowIdleSprite();
        }
    }
}
