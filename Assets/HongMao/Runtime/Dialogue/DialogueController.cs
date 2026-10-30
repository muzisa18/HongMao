using UnityEngine;

namespace HongMao
{
    // Input is sampled at -200; dialogue consumes it before combat/movement at 0.
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    public sealed class DialogueController : MonoBehaviour
    {
        [SerializeField] DialogueNpc npc;
        [SerializeField] PlayerCombatController player;
        [SerializeField] CombatTimeController timeController;
        [SerializeField] GameFlowCoordinator flow;
        PlayerInputReader m_Input;
        PlayerMotor2D m_Motor;
        Collider2D m_PlayerCollider;
        CombatantBody m_PlayerBody;
        DialogueDefinition m_ActiveDialogue;
        bool m_ViewAvailable = true;

        public bool IsOpen => m_ActiveDialogue != null;
        public int LineIndex { get; private set; }
        public int LineCount => IsOpen ? m_ActiveDialogue.lines.Length : 0;
        public string SpeakerName => IsOpen ? m_ActiveDialogue.speakerName : npc != null && npc.Dialogue != null ? npc.Dialogue.speakerName : string.Empty;
        public string CurrentLine => IsOpen ? m_ActiveDialogue.lines[LineIndex] : string.Empty;
        public DialogueNpc Npc => npc;
        public bool CanStart => m_ViewAvailable && !IsOpen && IsAvailable && npc.Dialogue != null && npc.Dialogue.HasContent &&
            m_Motor.IsGrounded && !player.IsActionBusy && !player.IsAbsorbing && !m_Input.GameplayBlocked;

        bool IsAvailable => isActiveAndEnabled && npc != null && npc.isActiveAndEnabled &&
            player != null && player.isActiveAndEnabled && m_Input != null && m_Input.isActiveAndEnabled &&
            m_PlayerBody != null && !m_PlayerBody.IsDead && timeController != null && timeController.isActiveAndEnabled &&
            flow != null && flow.State == GameFlowState.Running &&
            npc.IsInRange(new Vector2(m_PlayerCollider.bounds.center.x, m_PlayerCollider.bounds.min.y));

        public void Configure(DialogueNpc guide, PlayerCombatController actor, CombatTimeController time, GameFlowCoordinator gameFlow)
        {
            Close();
            npc = guide;
            player = actor;
            timeController = time;
            flow = gameFlow;
            CachePlayer();
        }

        void Awake() => CachePlayer();

        public void SetViewAvailable(bool available)
        {
            m_ViewAvailable = available;
            if (!available) Close();
        }

        void CachePlayer()
        {
            if (player == null) return;
            m_Input = player.GetComponent<PlayerInputReader>();
            m_Motor = player.GetComponent<PlayerMotor2D>();
            m_PlayerCollider = player.GetComponent<Collider2D>();
            m_PlayerBody = player.GetComponent<CombatantBody>();
        }

        void Update()
        {
            if (IsOpen)
            {
                if (!IsAvailable || !timeController.IsDialoguePaused || m_Input.CancelPressed) Close();
                else if (m_Input.InteractPressed) Advance();
                return;
            }
            if (m_Input != null && m_Input.InteractPressed) TryStart();
        }

        public bool TryStart()
        {
            if (!CanStart) return false;
            m_ActiveDialogue = npc.Dialogue;
            LineIndex = 0;
            m_Input.SetGameplayBlocked(true);
            timeController.SetDialoguePaused(true);
            return true;
        }

        public void Advance()
        {
            if (!IsOpen) return;
            if (LineIndex + 1 >= LineCount) Close();
            else LineIndex++;
        }

        public void Close()
        {
            if (!IsOpen) return;
            m_ActiveDialogue = null;
            LineIndex = 0;
            if (m_Input != null) m_Input.SetGameplayBlocked(false);
            if (timeController != null) timeController.SetDialoguePaused(false);
        }

        void OnDisable() => Close();
    }
}
