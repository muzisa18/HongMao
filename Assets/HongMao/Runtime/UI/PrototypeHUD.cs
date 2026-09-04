using UnityEngine;

namespace HongMao
{
    public sealed class PrototypeHUD : MonoBehaviour
    {
        CombatantBody m_Player;
        CombatantBody m_Enemy;
        PlayerCombatController m_Combat;
        CombatResourceModel m_Resources;
        GameFlowCoordinator m_Flow;
        GUIStyle m_Text;
        GUIStyle m_Title;
        GUIStyle m_Center;
        GUIStyle m_SmallCenter;
        GUIStyle m_AbsorbCenter;
        Texture2D m_White;
        float m_TutorialRemaining = 12f;

        public void Configure(CombatantBody player, CombatantBody enemy, PlayerCombatController combat,
            CombatResourceModel resources, GameFlowCoordinator flow)
        {
            m_Player = player;
            m_Enemy = enemy;
            m_Combat = combat;
            m_Resources = resources;
            m_Flow = flow;
        }

        void Update() => m_TutorialRemaining = Mathf.Max(0f, m_TutorialRemaining - Time.unscaledDeltaTime);

        void EnsureStyles()
        {
            if (m_Text != null) return;
            Font chinese = Resources.Load<Font>("Fonts/NotoSansCJKsc-Regular");
            m_Text = new GUIStyle(GUI.skin.label) { font = chinese, fontSize = 18, normal = { textColor = Color.white } };
            m_Title = new GUIStyle(m_Text) { fontSize = 28, fontStyle = FontStyle.Bold };
            m_Center = new GUIStyle(m_Title) { alignment = TextAnchor.MiddleCenter, fontSize = 42 };
            m_SmallCenter = new GUIStyle(m_Text) { alignment = TextAnchor.MiddleCenter, fontSize = 25 };
            m_AbsorbCenter = new GUIStyle(m_Text) { alignment = TextAnchor.MiddleCenter, fontSize = 22 };
            m_White = new Texture2D(1, 1) { name = "HUDWhite" };
            m_White.SetPixel(0, 0, Color.white);
            m_White.Apply();
        }

        void OnGUI()
        {
            if (m_Player == null || m_Enemy == null || m_Combat == null || m_Resources == null || m_Flow == null) return;
            EnsureStyles();
            DrawPanel(new Rect(18, 16, 410, 142), new Color(0.04f, 0.05f, 0.07f, 0.82f));
            GUI.Label(new Rect(32, 24, 180, 30), "玩家", m_Title);
            DrawBar(new Rect(32, 60, 240, 18), m_Player.Health / (float)m_Player.MaxHealth, new Color(0.12f, 0.72f, 0.28f));
            GUI.Label(new Rect(282, 53, 110, 30), $"{m_Player.Health}/{m_Player.MaxHealth}", m_Text);
            GUI.Label(new Rect(32, 88, 100, 28), "充能", m_Text);
            for (int i = 0; i < CombatResourceModel.MaxCharge; i++)
                DrawPanel(new Rect(92 + i * 34, 92, 24, 20), i < m_Resources.Charge ? new Color(0.15f, 0.75f, 1f) : new Color(0.18f, 0.2f, 0.24f));
            GUI.Label(new Rect(208, 88, 100, 28), "红槽", m_Text);
            for (int i = 0; i < CombatResourceModel.MaxColorSlots; i++)
                DrawPanel(new Rect(264 + i * 34, 92, 24, 20), m_Resources.GetSlot(i) == ColorKind.Red ? new Color(0.9f, 0.04f, 0.04f) : new Color(0.18f, 0.2f, 0.24f));
            string special = m_Resources.IsMaskActive ? $"红脸谱 {m_Resources.MaskRemaining:0.0}s" : (m_Resources.HasEmpoweredAttack ? "强化攻击：就绪" : "强化攻击：无");
            GUI.Label(new Rect(32, 118, 340, 28), special, m_Text);

            float right = Screen.width - 428;
            DrawPanel(new Rect(right, 16, 410, 114), new Color(0.04f, 0.05f, 0.07f, 0.82f));
            GUI.Label(new Rect(right + 14, 24, 220, 30), "CombatBot", m_Title);
            DrawBar(new Rect(right + 14, 60, 280, 16), m_Enemy.Health / (float)m_Enemy.MaxHealth, new Color(0.84f, 0.12f, 0.12f));
            GUI.Label(new Rect(right + 306, 52, 90, 28), $"{m_Enemy.Health}", m_Text);
            DrawBar(new Rect(right + 14, 91, 280, 12), m_Enemy.MaxPoise <= 0 ? 0f : m_Enemy.Poise / (float)m_Enemy.MaxPoise, new Color(0.95f, 0.65f, 0.1f));
            GUI.Label(new Rect(right + 306, 80, 90, 28), m_Enemy.IsPoiseBroken ? "已破霸体" : $"霸体 {m_Enemy.Poise}", m_Text);

            if (m_TutorialRemaining > 0f && m_Flow.State == GameFlowState.Running)
            {
                DrawPanel(new Rect(Screen.width / 2f - 360, Screen.height - 112, 720, 88), new Color(0.02f, 0.025f, 0.04f, 0.8f));
                GUI.Label(new Rect(Screen.width / 2f - 342, Screen.height - 102, 684, 32), "A/D 移动　Space 跳跃　Shift 闪避　左键攻击　右键招架", m_Text);
                GUI.Label(new Rect(Screen.width / 2f - 342, Screen.height - 68, 684, 32), "满充能后 Q 吸色　W/空中S＋左键强化攻击　三红槽后 R 脸谱", m_Text);
            }

            if (m_Combat.IsAbsorbing)
            {
                GUI.Label(new Rect(0, Screen.height / 2f - 70, Screen.width, 48), "吸色：将鼠标移到红色柱体，左键确认", m_Center);
                GUI.Label(new Rect(0, Screen.height / 2f - 20, Screen.width, 36), "右键 / Q / Esc 取消（不消耗资源）", m_AbsorbCenter);
                Vector2 mouse = Event.current.mousePosition;
                GUI.Label(new Rect(mouse.x + 22, mouse.y - 18, 120, 34), "红色", m_Title);
            }

            if (m_Flow.State != GameFlowState.Running)
            {
                string message = m_Flow.State == GameFlowState.Victory ? "胜利" : "失败";
                DrawPanel(new Rect(0, 0, Screen.width, Screen.height), new Color(0f, 0f, 0f, 0.62f));
                GUI.Label(new Rect(0, Screen.height / 2f - 80, Screen.width, 70), message, m_Center);
                GUI.Label(new Rect(0, Screen.height / 2f, Screen.width, 42), "按 Enter 重新开始", m_SmallCenter);
            }
        }

        void DrawBar(Rect rect, float value, Color color)
        {
            DrawPanel(rect, new Color(0.12f, 0.13f, 0.16f, 1f));
            DrawPanel(new Rect(rect.x + 2, rect.y + 2, (rect.width - 4) * Mathf.Clamp01(value), rect.height - 4), color);
        }

        void DrawPanel(Rect rect, Color color)
        {
            Color old = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, m_White);
            GUI.color = old;
        }

        void OnDestroy()
        {
            if (m_White != null) Destroy(m_White);
        }
    }
}
