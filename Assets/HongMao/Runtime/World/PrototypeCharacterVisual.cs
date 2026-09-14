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

        Rigidbody2D m_Body;
        PlayerMotor2D m_PlayerMotor;
        PlayerCombatController m_PlayerCombat;
        CombatBotController m_Bot;
        Color[] m_BaseColors;
        Vector3 m_VisualHome;
        Quaternion m_HeadHome;
        Quaternion m_WeaponHome;
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
            float bob = Mathf.Sin(Time.time * 12f) * 0.035f * movement;
            visualRoot.localPosition = m_VisualHome + Vector3.up * bob;
            if (head != null) head.localRotation = m_HeadHome * Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 3f) * 1.5f);

            bool acting = (m_PlayerCombat != null && m_PlayerCombat.IsActionBusy) || (m_Bot != null && m_Bot.IsActing);
            if (weapon != null)
            {
                float swing = acting ? Mathf.Sin(Time.time * 20f) * 38f - 24f : Mathf.Sin(Time.time * 2.5f) * 2f;
                weapon.localRotation = m_WeaponHome * Quaternion.Euler(0f, 0f, swing);
            }
        }
    }
}
