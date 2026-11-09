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
        [SerializeField] Sprite[] idleFrames;
        [SerializeField] Sprite[] leftRunFrames;
        [SerializeField] Sprite[] rightRunFrames;
        [SerializeField, Min(0.01f)] float locomotionFramesPerSecond = 10f;
        [SerializeField] Sprite[] leftJumpFrames;
        [SerializeField] Sprite[] rightJumpFrames;
        [SerializeField] Sprite[] leftFallFrames;
        [SerializeField] Sprite[] rightFallFrames;
        [SerializeField] Sprite[] redMaskIdleFrames;
        [SerializeField, Min(0.01f)] float airborneFramesPerSecond = 10f;

        enum AirbornePose { None, Rising, Falling }

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
        float m_IdleElapsed;
        float m_AirborneElapsed;
        AirbornePose m_AirbornePose;
        Sprite[] m_ActiveIdleFrames;
        Sprite[] m_ActiveAttackFrames;
        bool m_IsPlayingGroundAttack;
        bool m_IsShowingGroundDodge;
        bool m_IsShowingRun;
        bool m_IsShowingIdle;
        int m_LastFacing = 1;

        bool IsRedMaskActive => m_PlayerCombat != null && m_PlayerCombat.Resources != null
            && m_PlayerCombat.Resources.IsMaskActive;

        public bool UsesAuthoredMaskPalette => IsRedMaskActive && redMaskIdleFrames != null
            && redMaskIdleFrames.Length > 0 && CanShowLocomotion() && m_Body != null
            && Mathf.Abs(m_Body.linearVelocity.x) <= 0.1f;

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
            ConfigureAnimatedRenderer(renderer);
            ShowIdleSprite();
        }

        public void ConfigureAnimatedRenderer(SpriteRenderer renderer)
        {
            animatedRenderer = renderer;
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
        }

        public void ConfigureLocomotionSprites(Sprite[] standingFrames, Sprite[] leftMovingFrames,
            Sprite[] rightMovingFrames, float framesPerSecond)
        {
            idleFrames = standingFrames;
            leftRunFrames = leftMovingFrames;
            rightRunFrames = rightMovingFrames;
            locomotionFramesPerSecond = Mathf.Max(0.01f, framesPerSecond);
            m_IdleElapsed = 0f;
            m_RunElapsed = 0f;
            m_IsShowingIdle = false;
            m_IsShowingRun = false;
            ShowIdleSprite();
        }

        public void ConfigureAirborneSprites(Sprite[] leftJumpingFrames, Sprite[] rightJumpingFrames,
            Sprite[] leftFallingFrames, Sprite[] rightFallingFrames, float framesPerSecond)
        {
            leftJumpFrames = leftJumpingFrames;
            rightJumpFrames = rightJumpingFrames;
            leftFallFrames = leftFallingFrames;
            rightFallFrames = rightFallingFrames;
            airborneFramesPerSecond = Mathf.Max(0.01f, framesPerSecond);
            m_AirbornePose = AirbornePose.None;
            m_AirborneElapsed = 0f;
        }

        public void ConfigureRedMaskIdleSprites(Sprite[] standingFrames)
        {
            redMaskIdleFrames = standingFrames;
            m_ActiveIdleFrames = null;
            ShowIdleSprite();
        }

        public void PlayAttack(AttackKind kind, float startup, float active, float recovery)
        {
            m_AirbornePose = AirbornePose.None;
            m_AirborneElapsed = 0f;
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
                if (UpdateAirborneAnimation()) return;
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

        bool UpdateAirborneAnimation()
        {
            bool canShow = animatedRenderer != null && m_PlayerMotor != null && !m_PlayerMotor.IsGrounded
                && !m_PlayerMotor.IsDodging && (m_PlayerCombat == null || !m_PlayerCombat.IsActionBusy);
            if (!canShow)
            {
                m_AirbornePose = AirbornePose.None;
                m_AirborneElapsed = 0f;
                return false;
            }

            bool rising = m_Body.linearVelocity.y > 0f;
            AirbornePose pose = rising ? AirbornePose.Rising : AirbornePose.Falling;
            Sprite[] frames = SelectDirectionalFrames(rising ? leftJumpFrames : leftFallFrames,
                rising ? rightJumpFrames : rightFallFrames, out bool authoredFacingLeft);
            if (frames == null || frames.Length == 0) return false;

            if (pose != m_AirbornePose)
            {
                m_AirbornePose = pose;
                m_AirborneElapsed = 0f;
            }

            m_IsShowingRun = false;
            m_IsShowingIdle = false;
            m_RunElapsed = 0f;
            m_IdleElapsed = 0f;
            float fps = Mathf.Max(0.01f, airborneFramesPerSecond);
            int frame = Mathf.Min(frames.Length - 1, Mathf.FloorToInt(m_AirborneElapsed * fps));
            SetSprite(frames, frame);
            animatedRenderer.flipX = authoredFacingLeft;
            // Keep the source's leg extension: all airborne frames share one body reference pivot.
            m_AirborneElapsed = rising
                ? Mathf.Min(m_AirborneElapsed + Time.deltaTime, frames.Length / fps)
                : Mathf.Repeat(m_AirborneElapsed + Time.deltaTime, frames.Length / fps);
            return true;
        }

        Sprite[] SelectDirectionalFrames(Sprite[] leftFrames, Sprite[] rightFrames, out bool authoredFacingLeft)
        {
            authoredFacingLeft = m_LastFacing < 0;
            Sprite[] frames = authoredFacingLeft ? leftFrames : rightFrames;
            if (frames != null && frames.Length > 0) return frames;
            // A single available original direction can still be mirrored without reverting to a placeholder.
            authoredFacingLeft = !authoredFacingLeft;
            return authoredFacingLeft ? leftFrames : rightFrames;
        }

        Sprite[] GetIdleFrames()
        {
            return IsRedMaskActive && redMaskIdleFrames != null && redMaskIdleFrames.Length > 0
                ? redMaskIdleFrames : idleFrames;
        }

        void UpdateRunAnimation()
        {
            Sprite[] directionalFrames = SelectDirectionalFrames(leftRunFrames, rightRunFrames,
                out bool authoredFacingLeft);
            bool useDirectionalFrames = directionalFrames != null && directionalFrames.Length > 0;
            Sprite[] movingFrames = useDirectionalFrames ? directionalFrames : runFrames;
            bool canRun = animatedRenderer != null && movingFrames != null && movingFrames.Length > 0
                && CanShowLocomotion()
                && Mathf.Abs(m_Body.linearVelocity.x) > 0.1f;

            if (!canRun)
            {
                bool wasMoving = m_IsShowingRun;
                m_IsShowingRun = false;
                m_RunElapsed = 0f;

                Sprite[] standingFrames = GetIdleFrames();
                if (CanShowLocomotion() && standingFrames != null && standingFrames.Length > 0)
                {
                    if (m_ActiveIdleFrames != standingFrames)
                    {
                        m_ActiveIdleFrames = standingFrames;
                        m_IdleElapsed = 0f;
                    }
                    m_IsShowingIdle = true;
                    float duration = standingFrames.Length / Mathf.Max(0.01f, locomotionFramesPerSecond);
                    int idleFrame = Mathf.FloorToInt(m_IdleElapsed * locomotionFramesPerSecond);
                    SetSprite(standingFrames, idleFrame);
                    m_IdleElapsed = Mathf.Repeat(m_IdleElapsed + Time.deltaTime, duration);
                    return;
                }

                if (wasMoving || m_IsShowingIdle || (idleFrames != null && idleFrames.Length > 0))
                {
                    m_IsShowingIdle = false;
                    m_IdleElapsed = 0f;
                    // Actions without authored replacements keep the existing placeholder.
                    SetSprite(groundOneFrames, 0);
                }
                return;
            }

            m_IsShowingIdle = false;
            m_IdleElapsed = 0f;
            m_IsShowingRun = true;
            float fps = useDirectionalFrames ? Mathf.Max(0.01f, locomotionFramesPerSecond) : 12f;
            int frame = Mathf.FloorToInt(m_RunElapsed * fps);
            SetSprite(movingFrames, frame);
            // Cancel the parent's mirror for a native-left original, including a mirrored fallback.
            if (animatedRenderer != null) animatedRenderer.flipX = useDirectionalFrames && authoredFacingLeft;
            m_RunElapsed = Mathf.Repeat(m_RunElapsed + Time.deltaTime, movingFrames.Length / fps);
        }

        bool CanShowLocomotion()
        {
            return m_PlayerMotor != null && m_PlayerMotor.IsGrounded && !m_PlayerMotor.IsDodging
                && (m_PlayerCombat == null || !m_PlayerCombat.IsActionBusy);
        }

        void ShowIdleSprite()
        {
            m_IdleElapsed = 0f;
            Sprite[] standingFrames = GetIdleFrames();
            bool useOriginalIdle = standingFrames != null && standingFrames.Length > 0
                && (m_PlayerMotor == null || CanShowLocomotion());
            m_ActiveIdleFrames = useOriginalIdle ? standingFrames : null;
            SetSprite(useOriginalIdle ? standingFrames : groundOneFrames, 0);
        }

        void SetAnimatedFrame(int index)
        {
            SetSprite(m_ActiveAttackFrames, index);
        }

        void SetSprite(Sprite[] frames, int index)
        {
            if (animatedRenderer == null || frames == null || frames.Length == 0) return;
            animatedRenderer.flipX = false;
            animatedRenderer.sprite = frames[Mathf.Clamp(index, 0, frames.Length - 1)];
            if (frames == redMaskIdleFrames && UsesAuthoredMaskPalette)
                animatedRenderer.color = Color.white;
        }

        void OnValidate()
        {
            if (!Application.isPlaying) ShowIdleSprite();
        }
    }
}
