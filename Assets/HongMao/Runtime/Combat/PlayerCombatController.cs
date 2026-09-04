using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HongMao
{
    [RequireComponent(typeof(PlayerMotor2D), typeof(PlayerInputReader), typeof(CombatantBody))]
    public sealed class PlayerCombatController : MonoBehaviour
    {
        const float DodgeDuration = 0.3f;
        const float DodgeInvulnerability = 0.18f;
        const float DodgeCooldown = 0.45f;
        const float ParryWindow = 0.18f;
        const float ParryRecovery = 0.3f;

        readonly Dictionary<AttackKind, AttackDefinition> m_Attacks = new();
        readonly HashSet<int> m_RewardedEnemyAttacks = new();
        readonly List<RedColorSource> m_ColorSources = new();

        PlayerInputReader m_Input;
        PlayerMotor2D m_Motor;
        CombatantBody m_Body;
        CombatResourceModel m_Resources;
        CombatTimeController m_Time;
        PrototypeAudio m_Audio;
        PrototypeFeedback m_Feedback;
        SpriteRenderer m_Renderer;
        Coroutine m_Action;
        float m_DodgeCooldownRemaining;
        bool m_ActionBusy;
        bool m_CanCancelAttack;
        bool m_QueuedAttack;
        bool m_IsAbsorbing;
        bool m_IsMaskActivating;
        bool m_CombatEnabled = true;
        int m_ComboIndex;
        int m_PlayerAttackId;
        Vector2 m_SavedMaskVelocity;

        public CombatResourceModel Resources => m_Resources;
        public bool IsAbsorbing => m_IsAbsorbing;
        public bool IsActionBusy => m_ActionBusy;

        public void Configure(CombatResourceModel resources, CombatTimeController timeController,
            PrototypeAudio audio, PrototypeFeedback feedback, IEnumerable<AttackDefinition> attacks,
            IEnumerable<RedColorSource> sources)
        {
            m_Input = GetComponent<PlayerInputReader>();
            m_Motor = GetComponent<PlayerMotor2D>();
            m_Body = GetComponent<CombatantBody>();
            m_Renderer = GetComponent<SpriteRenderer>();
            m_Resources = resources;
            m_Time = timeController;
            m_Audio = audio;
            m_Feedback = feedback;
            m_Attacks.Clear();
            foreach (AttackDefinition definition in attacks) m_Attacks[definition.kind] = definition;
            m_ColorSources.Clear();
            m_ColorSources.AddRange(sources);
            m_Body.HitResolved += OnIncomingHitResolved;
            m_Body.Died += OnDied;
        }

        public void SetCombatEnabled(bool enabled)
        {
            m_CombatEnabled = enabled;
            m_Motor.SetInputEnabled(enabled && !m_IsAbsorbing);
        }

        void Update()
        {
            if (m_Resources == null || m_Input == null) return;
            m_DodgeCooldownRemaining = Mathf.Max(0f, m_DodgeCooldownRemaining - Time.deltaTime);
            if (m_Resources.IsMaskActive && !m_IsMaskActivating) m_Resources.TickMask(Time.deltaTime);
            UpdateMaskVisual();

            if (!m_CombatEnabled || m_Body.IsDead) return;

            if (m_IsAbsorbing)
            {
                UpdateAbsorb();
                return;
            }

            if (m_Input.MaskPressed && m_Resources.CanActivateMask && !m_ActionBusy)
            {
                BeginMask();
                return;
            }

            if (m_Input.AbsorbPressed && m_Resources.CanAbsorb && !m_ActionBusy)
            {
                BeginAbsorb();
                return;
            }

            if (TryStartDefense()) return;

            if (m_Input.AttackPressed)
            {
                if (m_ActionBusy)
                {
                    m_QueuedAttack = true;
                    return;
                }
                StartSelectedAttack();
            }
        }

        bool TryStartDefense()
        {
            if (m_Input.DodgePressed && m_DodgeCooldownRemaining <= 0f && (!m_ActionBusy || m_CanCancelAttack))
            {
                CancelCurrentAction();
                if (m_Motor.TryBeginDodge(DodgeDuration))
                {
                    m_DodgeCooldownRemaining = DodgeCooldown;
                    m_Action = StartCoroutine(DodgeRoutine());
                    return true;
                }
            }

            if (m_Input.ParryPressed && (!m_ActionBusy || m_CanCancelAttack))
            {
                CancelCurrentAction();
                m_Action = StartCoroutine(ParryRoutine());
                return true;
            }
            return false;
        }

        IEnumerator DodgeRoutine()
        {
            m_ActionBusy = true;
            m_Body.IsInvulnerable = true;
            yield return new WaitForSeconds(DodgeInvulnerability);
            m_Body.IsInvulnerable = false;
            float remainder = Mathf.Max(0f, DodgeDuration - DodgeInvulnerability);
            if (remainder > 0f) yield return new WaitForSeconds(remainder);
            FinishAction();
        }

        IEnumerator ParryRoutine()
        {
            m_ActionBusy = true;
            m_Body.IsParrying = true;
            m_Renderer.color = new Color(0.4f, 0.85f, 1f);
            yield return new WaitForSeconds(ParryWindow);
            m_Body.IsParrying = false;
            yield return new WaitForSeconds(ParryRecovery);
            FinishAction();
        }

        void StartSelectedAttack()
        {
            AttackChoice choice = AttackSelectionPolicy.Select(m_Resources.IsMaskActive,
                m_Resources.HasEmpoweredAttack, m_Motor.IsGrounded, m_Input.Vertical, m_ComboIndex);
            AttackKind kind = choice.Kind;

            if (!m_Attacks.TryGetValue(kind, out AttackDefinition attack)) return;
            if (choice.ConsumesEmpowered) m_Resources.TryConsumeEmpoweredAttack();
            m_ComboIndex = (kind == AttackKind.Air || kind == AttackKind.MaskAir) ? 0 : (m_ComboIndex + 1) % 3;
            m_Action = StartCoroutine(kind == AttackKind.DiveSlam ? DiveSlamRoutine(attack) : AttackRoutine(attack));
        }

        IEnumerator AttackRoutine(AttackDefinition attack)
        {
            m_ActionBusy = true;
            m_CanCancelAttack = false;
            m_Motor.SetInputEnabled(false);
            if (attack.movementImpulse != 0f)
                m_Motor.BeginForcedHorizontal(m_Motor.Facing * attack.movementImpulse, attack.startup + attack.active);

            yield return new WaitForSeconds(attack.startup);
            ExecuteHit(attack);
            yield return new WaitForSeconds(attack.active);
            m_CanCancelAttack = true;
            yield return new WaitForSeconds(attack.recovery);
            FinishAction();
            TryRunQueuedAttack();
        }

        IEnumerator DiveSlamRoutine(AttackDefinition attack)
        {
            m_ActionBusy = true;
            m_CanCancelAttack = false;
            m_Motor.SetInputEnabled(false);
            yield return new WaitForSeconds(attack.startup);
            m_Motor.SetVelocity(new Vector2(m_Motor.Velocity.x * 0.35f, -12f));
            float timeout = 0.85f;
            while (!m_Motor.IsGrounded && timeout > 0f)
            {
                timeout -= Time.deltaTime;
                yield return null;
            }
            ExecuteHit(attack, true);
            m_Feedback.Shake(0.16f, 0.12f);
            yield return new WaitForSeconds(attack.recovery);
            FinishAction();
        }

        void ExecuteHit(AttackDefinition attack, bool centered = false)
        {
            int facing = m_Motor.Facing;
            Vector2 offset = attack.hitboxOffset;
            offset.x *= facing;
            Vector2 center = (Vector2)transform.position + (centered ? Vector2.zero : offset);
            Collider2D[] hits = Physics2D.OverlapBoxAll(center, attack.hitboxSize, 0f);
            var handled = new HashSet<CombatantBody>();
            for (int i = 0; i < hits.Length; i++)
            {
                if (!hits[i].TryGetComponent(out CombatantBody target) || target == m_Body || !handled.Add(target)) continue;
                Vector2 knockback = attack.knockback;
                knockback.x *= facing;
                var request = new HitRequest(++m_PlayerAttackId, attack.healthDamage, attack.poiseDamage,
                    knockback, false, attack.launches, gameObject);
                HitResult hit = target.ReceiveHit(request);
                if (hit.Outcome == HitOutcome.Damaged || hit.Outcome == HitOutcome.Killed)
                {
                    m_Audio.Play(PrototypeSound.Hit);
                    m_Time.HitStop(m_Resources.IsMaskActive ? 0.055f : 0.04f);
                    m_Feedback.Shake(0.07f, 0.055f);
                    if (hit.BrokePoise) m_Feedback.Pulse(new Color(1f, 0.18f, 0.08f), 0.16f);
                }
            }
            PrototypeVisuals.FlashBox(center, attack.hitboxSize, m_Resources.IsMaskActive ? new Color(1f, 0.05f, 0.05f, 0.42f) : new Color(1f, 0.82f, 0.22f, 0.34f), 0.09f);
        }

        void TryRunQueuedAttack()
        {
            if (!m_QueuedAttack || !m_CombatEnabled || m_Body.IsDead) return;
            m_QueuedAttack = false;
            StartSelectedAttack();
        }

        void BeginAbsorb()
        {
            m_IsAbsorbing = true;
            m_Motor.SetInputEnabled(false);
            m_Time.EnterAbsorb();
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            for (int i = 0; i < m_ColorSources.Count; i++) m_ColorSources[i].SetHighlighted(m_ColorSources[i].IsAvailable);
        }

        void UpdateAbsorb()
        {
            if (m_Input.AbsorbPressed || m_Input.ParryPressed || m_Input.CancelPressed)
            {
                EndAbsorb();
                return;
            }

            if (!m_Input.AttackPressed) return;
            Vector2 screen = Mouse.current == null ? Vector2.zero : Mouse.current.position.ReadValue();
            Vector2 world = Camera.main.ScreenToWorldPoint(screen);
            Collider2D hit = Physics2D.OverlapPoint(world);
            if (hit != null && hit.TryGetComponent(out RedColorSource source) && source.TryExtractColor(out ColorKind color)
                && m_Resources.TryCommitAbsorb(color))
            {
                m_Audio.Play(PrototypeSound.Absorb);
                m_Feedback.Pulse(new Color(1f, 0.04f, 0.04f), 0.22f);
            }
            EndAbsorb();
        }

        void EndAbsorb()
        {
            for (int i = 0; i < m_ColorSources.Count; i++) m_ColorSources[i].SetHighlighted(false);
            m_IsAbsorbing = false;
            m_Time.ExitAbsorb();
            m_Motor.SetInputEnabled(m_CombatEnabled);
        }

        void BeginMask()
        {
            if (!m_Resources.TryActivateMask()) return;
            CancelCurrentAction();
            m_Action = StartCoroutine(MaskActivationRoutine());
        }

        IEnumerator MaskActivationRoutine()
        {
            m_ActionBusy = true;
            m_IsMaskActivating = true;
            m_Motor.SetInputEnabled(false);
            m_Body.IsInvulnerable = true;
            m_SavedMaskVelocity = m_Motor.Velocity;
            m_Motor.SetVelocity(Vector2.zero);
            m_Time.HitStop(0.12f);
            m_Audio.Play(PrototypeSound.Mask, 0.5f);
            m_Feedback.Pulse(new Color(1f, 0f, 0f), 0.4f);
            m_Feedback.Shake(0.4f, 0.1f);
            yield return new WaitForSecondsRealtime(0.4f);
            m_Motor.SetVelocity(m_SavedMaskVelocity);
            m_Body.IsInvulnerable = false;
            m_IsMaskActivating = false;
            FinishAction();
        }

        void OnIncomingHitResolved(HitRequest request, HitResult hit)
        {
            if (m_Resources.IsMaskActive || !m_RewardedEnemyAttacks.Add(request.AttackId)) return;
            if (hit.Outcome == HitOutcome.Parried) m_Resources.TryAddCharge(2);
            else if (hit.Outcome == HitOutcome.Dodged) m_Resources.TryAddCharge(1);
        }

        void UpdateMaskVisual()
        {
            if (m_Renderer == null) return;
            if (m_Resources != null && m_Resources.IsMaskActive)
                m_Renderer.color = Color.Lerp(new Color(0.72f, 0.02f, 0.02f), new Color(1f, 0.35f, 0.08f), Mathf.PingPong(Time.unscaledTime * 2f, 1f));
            else if (!m_ActionBusy || (!m_Body.IsParrying && !m_Body.IsDead))
                m_Renderer.color = new Color(0.1f, 0.52f, 0.82f);
        }

        void CancelCurrentAction()
        {
            if (m_Action != null) StopCoroutine(m_Action);
            m_Action = null;
            m_ActionBusy = false;
            m_IsMaskActivating = false;
            m_CanCancelAttack = false;
            m_Body.IsParrying = false;
            m_Body.IsInvulnerable = false;
            m_Motor.SetInputEnabled(m_CombatEnabled && !m_IsAbsorbing);
        }

        void FinishAction()
        {
            m_Action = null;
            m_ActionBusy = false;
            m_CanCancelAttack = false;
            m_Body.IsParrying = false;
            m_Body.IsInvulnerable = false;
            m_Motor.SetInputEnabled(m_CombatEnabled && !m_IsAbsorbing);
        }

        void OnDied()
        {
            m_CombatEnabled = false;
            if (m_IsAbsorbing) EndAbsorb();
            CancelCurrentAction();
            m_Resources.EndMask();
            m_Time.ForceRestore();
            if (m_Renderer != null) m_Renderer.color = new Color(0.16f, 0.16f, 0.2f);
        }

        void OnDisable()
        {
            if (m_IsAbsorbing) EndAbsorb();
            if (m_Time != null) m_Time.ForceRestore();
        }

        void OnDestroy()
        {
            if (m_Body == null) return;
            m_Body.HitResolved -= OnIncomingHitResolved;
            m_Body.Died -= OnDied;
        }
    }
}
