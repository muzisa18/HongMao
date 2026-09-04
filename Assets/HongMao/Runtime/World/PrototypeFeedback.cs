using System.Collections;
using UnityEngine;

namespace HongMao
{
    public sealed class PrototypeFeedback : MonoBehaviour
    {
        Camera m_Camera;
        Vector3 m_CameraHome;
        Coroutine m_Shake;
        float m_PulseRemaining;
        float m_PulseDuration;
        Color m_PulseColor;
        Texture2D m_White;

        public void Configure(Camera camera)
        {
            m_Camera = camera;
            m_CameraHome = camera.transform.position;
            m_White = new Texture2D(1, 1) { name = "RuntimeWhitePixel" };
            m_White.SetPixel(0, 0, Color.white);
            m_White.Apply();
        }

        public void Pulse(Color color, float duration)
        {
            m_PulseColor = color;
            m_PulseDuration = Mathf.Max(0.01f, duration);
            m_PulseRemaining = m_PulseDuration;
        }

        public void Shake(float duration, float strength)
        {
            if (m_Shake != null) StopCoroutine(m_Shake);
            m_Shake = StartCoroutine(ShakeRoutine(duration, strength));
        }

        void Update() => m_PulseRemaining = Mathf.Max(0f, m_PulseRemaining - Time.unscaledDeltaTime);

        IEnumerator ShakeRoutine(float duration, float strength)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                m_Camera.transform.position = m_CameraHome + (Vector3)(Random.insideUnitCircle * strength);
                yield return null;
            }
            m_Camera.transform.position = m_CameraHome;
            m_Shake = null;
        }

        void OnGUI()
        {
            if (m_PulseRemaining <= 0f || m_White == null) return;
            float alpha = (m_PulseRemaining / m_PulseDuration) * 0.42f;
            Color old = GUI.color;
            GUI.color = new Color(m_PulseColor.r, m_PulseColor.g, m_PulseColor.b, alpha);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), m_White);
            GUI.color = old;
        }

        void OnDestroy()
        {
            if (m_White != null) Destroy(m_White);
        }
    }
}
