using UnityEngine;
using UnityEngine.SceneManagement;

namespace HongMao
{
    public enum GameFlowState { Running, Victory, Defeat }

    public sealed class GameFlowCoordinator : MonoBehaviour
    {
        PlayerCombatController m_Player;
        PlayerInputReader m_Input;
        CombatantBody m_PlayerBody;
        CombatBotController m_Enemy;
        CombatTimeController m_Time;
        PrototypeAudio m_Audio;

        public GameFlowState State { get; private set; } = GameFlowState.Running;

        public void Configure(PlayerCombatController player, PlayerInputReader input, CombatantBody playerBody,
            CombatBotController enemy, CombatTimeController timeController, PrototypeAudio audio)
        {
            m_Player = player;
            m_Input = input;
            m_PlayerBody = playerBody;
            m_Enemy = enemy;
            m_Time = timeController;
            m_Audio = audio;
            m_PlayerBody.Died += OnPlayerDied;
            m_Enemy.Body.Died += OnEnemyDied;
        }

        void Update()
        {
            if (State != GameFlowState.Running && m_Input.RestartPressed) Restart();
        }

        void OnPlayerDied()
        {
            State = GameFlowState.Defeat;
            m_Player.SetCombatEnabled(false);
            m_Enemy.SetCombatEnabled(false);
            m_Time.ForceRestore();
            m_Audio.Play(PrototypeSound.Lose, 0.55f);
        }

        void OnEnemyDied()
        {
            State = GameFlowState.Victory;
            m_Player.SetCombatEnabled(false);
            m_Enemy.SetCombatEnabled(false);
            m_Time.ForceRestore();
            m_Audio.Play(PrototypeSound.Win, 0.55f);
        }

        public void Restart()
        {
            m_Time.ForceRestore();
            Scene active = SceneManager.GetActiveScene();
            if (!string.IsNullOrEmpty(active.path)) SceneManager.LoadScene(active.path);
            else SceneManager.LoadScene(active.buildIndex);
        }

        void OnDestroy()
        {
            if (m_PlayerBody != null) m_PlayerBody.Died -= OnPlayerDied;
            if (m_Enemy != null && m_Enemy.Body != null) m_Enemy.Body.Died -= OnEnemyDied;
            if (m_Time != null) m_Time.ForceRestore();
        }
    }
}
