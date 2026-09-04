using System.Collections;
using UnityEngine;

namespace HongMao
{
    [RequireComponent(typeof(Rigidbody2D), typeof(CombatantBody), typeof(SpriteRenderer))]
    public sealed class CombatBotController : MonoBehaviour
    {
        const float MoveSpeed = 2.2f;
        const float AttackRange = 2.05f;
        const float StaggerDuration = 1.5f;

        Rigidbody2D m_Rigidbody;
        CombatantBody m_Body;
        CombatantBody m_PlayerBody;
        Transform m_Player;
        SpriteRenderer m_Renderer;
        PrototypeAudio m_Audio;
        PrototypeFeedback m_Feedback;
        Coroutine m_Action;
        int m_AttackIndex;
        int m_AttackId = 1000;
        bool m_Staggered;
        bool m_CombatEnabled = true;

        public CombatantBody Body => m_Body;
        public bool IsStaggered => m_Staggered;

        public void Configure(Transform player, CombatantBody playerBody, PrototypeAudio audio, PrototypeFeedback feedback)
        {
            m_Rigidbody = GetComponent<Rigidbody2D>();
            m_Body = GetComponent<CombatantBody>();
            m_Player = player;
            m_PlayerBody = playerBody;
            m_Renderer = GetComponent<SpriteRenderer>();
            m_Audio = audio;
            m_Feedback = feedback;
            m_Rigidbody.freezeRotation = true;
            m_Rigidbody.gravityScale = 2.2f;
            m_Rigidbody.interpolation = RigidbodyInterpolation2D.Interpolate;
            m_Body.PoiseBroken += OnPoiseBroken;
            m_Body.Died += OnDied;
        }

        public void SetCombatEnabled(bool enabled)
        {
            m_CombatEnabled = enabled;
            if (!enabled) m_Rigidbody.linearVelocity = new Vector2(0f, m_Rigidbody.linearVelocity.y);
        }

        public void OnAttackParried()
        {
            if (m_Body.IsDead) return;
            StartStagger(0.65f, false);
        }

        void FixedUpdate()
        {
            if (!m_CombatEnabled || m_Body == null || m_Body.IsDead || m_Player == null || m_PlayerBody.IsDead || m_Staggered || m_Action != null)
                return;

            float delta = m_Player.position.x - transform.position.x;
            if (Mathf.Abs(delta) > AttackRange)
            {
                m_Rigidbody.linearVelocity = new Vector2(Mathf.Sign(delta) * MoveSpeed, m_Rigidbody.linearVelocity.y);
            }
            else
            {
                m_Rigidbody.linearVelocity = new Vector2(0f, m_Rigidbody.linearVelocity.y);
                m_Action = StartCoroutine(AttackRoutine(m_AttackIndex++ % 2 == 0));
            }
        }

        IEnumerator AttackRoutine(bool parryable)
        {
            float telegraph = parryable ? 0.7f : 0.9f;
            m_Renderer.color = parryable ? new Color(1f, 0.9f, 0.2f) : new Color(1f, 0.32f, 0.03f);
            yield return new WaitForSeconds(telegraph);

            if (!m_CombatEnabled || m_Body.IsDead || m_Staggered)
            {
                m_Action = null;
                yield break;
            }

            int direction = m_Player.position.x >= transform.position.x ? 1 : -1;
            Vector2 center = (Vector2)transform.position + new Vector2(direction * 1.1f, 0.15f);
            Collider2D[] hits = Physics2D.OverlapBoxAll(center, new Vector2(2.2f, 1.7f), 0f);
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].TryGetComponent(out CombatantBody target) && target == m_PlayerBody)
                {
                    var request = new HitRequest(++m_AttackId, parryable ? 15 : 20, 0,
                        new Vector2(direction * 4.5f, 2.5f), parryable, false, gameObject);
                    HitResult hit = target.ReceiveHit(request);
                    if (hit.Outcome == HitOutcome.Parried)
                    {
                        m_Audio.Play(PrototypeSound.Parry);
                        m_Feedback.Pulse(Color.white, 0.12f);
                        OnAttackParried();
                    }
                    else if (hit.Outcome == HitOutcome.Dodged)
                    {
                        m_Audio.Play(PrototypeSound.Dodge);
                        m_Feedback.Pulse(new Color(0.35f, 0.8f, 1f), 0.1f);
                    }
                    else if (hit.Outcome == HitOutcome.Damaged || hit.Outcome == HitOutcome.Killed)
                    {
                        m_Audio.Play(PrototypeSound.Hit);
                        m_Feedback.Shake(0.08f, 0.08f);
                    }
                    break;
                }
            }

            m_Renderer.color = new Color(0.24f, 0.28f, 0.34f);
            yield return new WaitForSeconds(0.55f);
            m_Action = null;
        }

        void OnPoiseBroken()
        {
            m_Audio.Play(PrototypeSound.PoiseBreak);
            m_Feedback.Pulse(new Color(1f, 0.22f, 0.08f), 0.18f);
            StartStagger(StaggerDuration, true);
        }

        void StartStagger(float duration, bool recoverPoise)
        {
            if (m_Action != null) StopCoroutine(m_Action);
            m_Action = StartCoroutine(StaggerRoutine(duration, recoverPoise));
        }

        IEnumerator StaggerRoutine(float duration, bool recoverPoise)
        {
            m_Staggered = true;
            m_Rigidbody.linearVelocity = new Vector2(0f, m_Rigidbody.linearVelocity.y);
            m_Renderer.color = new Color(0.5f, 0.5f, 0.55f);
            yield return new WaitForSeconds(duration);
            if (recoverPoise) m_Body.RecoverPoise();
            m_Staggered = false;
            if (!m_Body.IsDead) m_Renderer.color = new Color(0.24f, 0.28f, 0.34f);
            m_Action = null;
        }

        void OnDied()
        {
            m_CombatEnabled = false;
            if (m_Action != null) StopCoroutine(m_Action);
            m_Action = null;
            m_Rigidbody.linearVelocity = Vector2.zero;
            m_Renderer.color = new Color(0.12f, 0.12f, 0.12f);
        }

        void OnDestroy()
        {
            if (m_Body == null) return;
            m_Body.PoiseBroken -= OnPoiseBroken;
            m_Body.Died -= OnDied;
        }
    }
}
