using System.Collections;
using UnityEngine;

namespace HongMao
{
    [DisallowMultipleComponent]
    public sealed class CombatTimeController : MonoBehaviour
    {
        const float AbsorbScale = 0.1f;
        Coroutine m_HitStopRoutine;
        bool m_AbsorbActive;
        bool m_DialoguePaused;

        public bool IsAbsorbActive => m_AbsorbActive;
        public bool IsDialoguePaused => m_DialoguePaused;

        public void SetDialoguePaused(bool paused)
        {
            m_DialoguePaused = paused;
            ApplyScale();
        }

        public void EnterAbsorb()
        {
            m_AbsorbActive = true;
            ApplyScale();
        }

        public void ExitAbsorb()
        {
            m_AbsorbActive = false;
            ApplyScale();
        }

        public void HitStop(float realSeconds)
        {
            if (realSeconds <= 0f) return;
            if (m_HitStopRoutine != null) StopCoroutine(m_HitStopRoutine);
            m_HitStopRoutine = StartCoroutine(HitStopRoutine(realSeconds));
        }

        IEnumerator HitStopRoutine(float duration)
        {
            Time.timeScale = 0f;
            yield return new WaitForSecondsRealtime(duration);
            m_HitStopRoutine = null;
            ApplyScale();
        }

        void ApplyScale()
        {
            Time.timeScale = m_DialoguePaused || m_HitStopRoutine != null ? 0f : (m_AbsorbActive ? AbsorbScale : 1f);
            Time.fixedDeltaTime = (1f / 60f) * Mathf.Max(Time.timeScale, 0.01f);
        }

        public void ForceRestore()
        {
            if (m_HitStopRoutine != null) StopCoroutine(m_HitStopRoutine);
            m_HitStopRoutine = null;
            m_AbsorbActive = false;
            m_DialoguePaused = false;
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 1f / 60f;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        void OnDisable() => ForceRestore();
        void OnDestroy() => ForceRestore();
    }
}
