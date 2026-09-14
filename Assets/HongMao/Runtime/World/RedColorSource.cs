using UnityEngine;

namespace HongMao
{
    [RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
    public sealed class RedColorSource : MonoBehaviour, IColorSource
    {
        [SerializeField, Min(0f)] float cooldownSeconds = 3f;
        SpriteRenderer m_Renderer;
        Vector3 m_BaseScale = Vector3.one;
        float m_Cooldown;
        bool m_Highlighted;

        public ColorKind AvailableColor => ColorKind.Red;
        public bool IsAvailable => m_Cooldown <= 0f;

        public void Configure()
        {
            m_Renderer = GetComponent<SpriteRenderer>();
            m_BaseScale = transform.localScale;
            ApplyColor();
        }

        public bool TryExtractColor(out ColorKind color)
        {
            color = ColorKind.None;
            if (!IsAvailable) return false;
            color = ColorKind.Red;
            m_Cooldown = cooldownSeconds;
            m_Highlighted = false;
            ApplyColor();
            return true;
        }

        public void SetHighlighted(bool highlighted)
        {
            m_Highlighted = highlighted && IsAvailable;
            ApplyColor();
        }

        void Update()
        {
            if (m_Cooldown > 0f)
            {
                m_Cooldown = Mathf.Max(0f, m_Cooldown - Time.deltaTime);
                ApplyColor();
            }
            else if (m_Highlighted && m_Renderer != null)
            {
                float pulse = 0.7f + Mathf.Sin(Time.unscaledTime * 8f) * 0.25f;
                m_Renderer.color = Color.Lerp(Color.white, new Color(1f, 0.12f, 0.12f), pulse);
                transform.localScale = m_BaseScale * (1f + Mathf.Sin(Time.unscaledTime * 7f) * 0.06f);
            }
        }

        void ApplyColor()
        {
            if (m_Renderer == null) return;
            transform.localScale = m_BaseScale;
            m_Renderer.color = IsAvailable ? (m_Highlighted ? Color.white : new Color(0.82f, 0.08f, 0.08f)) : new Color(0.32f, 0.32f, 0.32f);
        }
    }
}
