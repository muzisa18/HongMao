using UnityEngine;

namespace HongMao
{
    [DisallowMultipleComponent]
    public sealed class FirstLevelPickup : MonoBehaviour
    {
        FirstLevelController m_Level;
        PlayerCombatController m_Player;
        int m_RewardIndex;
        SpriteRenderer[] m_Renderers;

        public bool IsCollected { get; private set; }

        public void Configure(FirstLevelController level, PlayerCombatController player, int rewardIndex)
        {
            m_Level = level;
            m_Player = player;
            m_RewardIndex = rewardIndex;
            m_Renderers = GetComponentsInChildren<SpriteRenderer>();
        }

        void Update()
        {
            if (IsCollected || m_Level == null || m_Player == null || m_Level.IsComplete || m_Level.IsFailed) return;
            if (((Vector2)(m_Player.transform.position - transform.position)).sqrMagnitude > .7f * .7f) return;
            IsCollected = true;
            m_Level.CollectReward(m_RewardIndex);
            for (int i = 0; i < m_Renderers.Length; i++) m_Renderers[i].enabled = false;
        }
    }
}
