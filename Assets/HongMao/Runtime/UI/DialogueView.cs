using UnityEngine;

namespace HongMao
{
    public sealed class DialogueView : MonoBehaviour
    {
        [SerializeField] DialogueController dialogue;
        [SerializeField] Camera sceneCamera;
        GUIStyle m_Name;
        GUIStyle m_Body;
        GUIStyle m_Hint;
        GUIStyle m_Prompt;
        Vector2 m_Scroll;
        int m_LastLine = -1;
        static readonly Color Gold = new(0.94f, 0.79f, 0.48f);

        public void Configure(DialogueController controller, Camera camera)
        {
            dialogue = controller;
            sceneCamera = camera;
            if (dialogue != null) dialogue.SetViewAvailable(isActiveAndEnabled);
        }

        void EnsureStyles()
        {
            if (m_Body != null) return;
            Font font = Resources.Load<Font>("Fonts/NotoSansCJKsc-Regular");
            m_Body = new GUIStyle(GUI.skin.label) { font = font, fontSize = 25, wordWrap = true, richText = false, normal = { textColor = new Color(.94f, .94f, .91f) } };
            m_Name = new GUIStyle(m_Body) { fontSize = 28, fontStyle = FontStyle.Bold, normal = { textColor = Gold } };
            m_Hint = new GUIStyle(m_Body) { fontSize = 18, normal = { textColor = new Color(.72f, .77f, .8f) } };
            m_Prompt = new GUIStyle(m_Hint) { alignment = TextAnchor.MiddleCenter, normal = { textColor = Gold } };
        }

        void OnGUI()
        {
            if (dialogue == null) return;
            if (!dialogue.IsOpen && !dialogue.CanStart)
            {
                m_LastLine = -1;
                return;
            }
            EnsureStyles();
            Matrix4x4 previous = GUI.matrix;
            int depth = GUI.depth;
            float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
            float width = Screen.width / scale;
            float height = Screen.height / scale;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            GUI.depth = -20;
            if (dialogue.IsOpen)
            {
                if (m_LastLine != dialogue.LineIndex) m_Scroll = Vector2.zero;
                m_LastLine = dialogue.LineIndex;
                float panelWidth = Mathf.Min(1040, width - 48);
                float left = (width - panelWidth) * .5f;
                float top = height - 254;
                Panel(new Rect(left, top, panelWidth, 230), new Color(.035f, .055f, .075f, .97f));
                Panel(new Rect(left, top, 4, 230), Gold);
                GUI.Label(new Rect(left + 28, top + 16, panelWidth - 210, 40), dialogue.SpeakerName, m_Name);
                GUI.Label(new Rect(left + panelWidth - 112, top + 22, 90, 30), $"{dialogue.LineIndex + 1} / {dialogue.LineCount}", m_Hint);
                float textWidth = panelWidth - 72;
                float textHeight = Mathf.Max(114, m_Body.CalcHeight(new GUIContent(dialogue.CurrentLine), textWidth));
                m_Scroll = GUI.BeginScrollView(new Rect(left + 28, top + 66, panelWidth - 50, 114), m_Scroll,
                    new Rect(0, 0, textWidth, textHeight));
                GUI.Label(new Rect(0, 0, textWidth, textHeight), dialogue.CurrentLine, m_Body);
                GUI.EndScrollView();
                string action = dialogue.LineIndex + 1 == dialogue.LineCount ? "E  结束交谈" : "E  继续";
                GUI.Label(new Rect(left + 28, top + 190, panelWidth - 220, 30), $"{action}      Esc  暂时离开", m_Hint);
                GUI.Label(new Rect(left + panelWidth - 150, top + 190, 140, 30), "战斗已暂停", m_Hint);
            }
            else if (sceneCamera != null)
            {
                m_LastLine = -1;
                Vector3 point = sceneCamera.WorldToScreenPoint(dialogue.Npc.PromptPosition);
                if (point.z > 0)
                {
                    Rect rect = new(point.x / scale - 130, (Screen.height - point.y) / scale - 40, 260, 40);
                    Panel(rect, new Color(.035f, .055f, .075f, .94f));
                    GUI.Label(rect, $"E  与{dialogue.SpeakerName}交谈", m_Prompt);
                }
            }
            GUI.depth = depth;
            GUI.matrix = previous;
        }

        static void Panel(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }

        void OnEnable()
        {
            if (dialogue != null) dialogue.SetViewAvailable(true);
        }

        void OnDisable()
        {
            if (dialogue != null) dialogue.SetViewAvailable(false);
        }
    }
}
