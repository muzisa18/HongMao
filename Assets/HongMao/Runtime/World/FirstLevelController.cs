using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace HongMao
{
    [DefaultExecutionOrder(-1000)]
    [DisallowMultipleComponent]
    public sealed class FirstLevelController : MonoBehaviour
    {
        [SerializeField] Camera sceneCamera;
        [SerializeField] PlayerCombatController player;
        [SerializeField] PlayerDefinition playerDefinition;
        [SerializeField] CombatBotController[] enemies = Array.Empty<CombatBotController>();
        [SerializeField] CombatBotDefinition[] enemyDefinitions = Array.Empty<CombatBotDefinition>();
        [SerializeField] BoxCollider2D[] encounterAreas = Array.Empty<BoxCollider2D>();
        [SerializeField] CombatTimeController timeController;
        [SerializeField] PrototypeAudio prototypeAudio;
        [SerializeField] PrototypeFeedback feedback;
        [SerializeField] Transform exit;
        [SerializeField] FirstLevelPickup[] pickups = Array.Empty<FirstLevelPickup>();

        PlayerInputReader m_Input;
        CombatantBody m_PlayerBody;
        Rigidbody2D m_PlayerRigidbody;
        Rigidbody2D[] m_EnemyRigidbodies;
        Collider2D[] m_EnemyColliders;
        bool[] m_EnemyEnabled;
        bool[] m_Collected;
        bool m_Initialized;
        bool m_Restarting;
        GUIStyle m_Text;
        GUIStyle m_Title;
        GUIStyle m_Status;

        public PlayerCombatController Player => player;
        public CombatantBody PlayerBody => m_PlayerBody;
        public IReadOnlyList<CombatBotController> Enemies => enemies;
        public bool IsComplete { get; private set; }
        public bool IsFailed { get; private set; }
        public int CollectedRewards { get; private set; }
        public int TotalRewards => pickups.Length;

        public void Configure(Camera camera, PlayerCombatController actor, PlayerDefinition actorDefinition,
            CombatBotController[] bots, CombatBotDefinition[] botDefinitions, BoxCollider2D[] areas,
            CombatTimeController time, PrototypeAudio prototypeAudio, PrototypeFeedback prototypeFeedback,
            Transform levelExit, FirstLevelPickup[] rewards)
        {
            sceneCamera = camera;
            player = actor;
            playerDefinition = actorDefinition;
            enemies = bots ?? Array.Empty<CombatBotController>();
            enemyDefinitions = botDefinitions ?? Array.Empty<CombatBotDefinition>();
            encounterAreas = areas ?? Array.Empty<BoxCollider2D>();
            timeController = time;
            this.prototypeAudio = prototypeAudio;
            feedback = prototypeFeedback;
            exit = levelExit;
            pickups = rewards ?? Array.Empty<FirstLevelPickup>();
            if (Application.isPlaying) Initialize();
        }

        void Awake() => Initialize();

        public void Initialize()
        {
            if (m_Initialized || player == null || sceneCamera == null || timeController == null || prototypeAudio == null || feedback == null)
                return;
            if (playerDefinition == null || exit == null || enemies.Length != enemyDefinitions.Length || enemies.Length != encounterAreas.Length)
                throw new InvalidOperationException("First level requires player settings, an exit and one definition/area per enemy.");

            m_Initialized = true;
            Physics2D.gravity = new Vector2(0f, -9.81f);
            timeController.ForceRestore();
            prototypeAudio.Configure();
            feedback.Configure(sceneCamera);
            m_Input = player.GetComponent<PlayerInputReader>();
            m_PlayerBody = player.GetComponent<CombatantBody>();
            m_PlayerRigidbody = player.GetComponent<Rigidbody2D>();
            m_PlayerBody.Configure(playerDefinition.maxHealth, playerDefinition.maxPoise);
            player.GetComponent<PlayerMotor2D>().Configure(m_Input, playerDefinition);
            var resources = new CombatResourceModel();
            player.Configure(resources, timeController, prototypeAudio, feedback,
                Resources.LoadAll<AttackDefinition>("Attacks"), Array.Empty<RedColorSource>(), playerDefinition);
            m_PlayerBody.Died += OnPlayerDied;

            m_EnemyRigidbodies = new Rigidbody2D[enemies.Length];
            m_EnemyColliders = new Collider2D[enemies.Length];
            m_EnemyEnabled = new bool[enemies.Length];
            var playerCollider = player.GetComponent<Collider2D>();
            for (int i = 0; i < enemies.Length; i++)
            {
                CombatBotController enemy = enemies[i];
                CombatBotDefinition definition = enemyDefinitions[i];
                if (enemy == null || definition == null || encounterAreas[i] == null)
                    throw new InvalidOperationException("First level has an unassigned enemy, definition or encounter area.");
                enemy.GetComponent<CombatantBody>().Configure(definition.maxHealth, definition.maxPoise);
                enemy.Configure(player.transform, m_PlayerBody, prototypeAudio, feedback, definition);
                enemy.SetCombatEnabled(false);
                m_EnemyRigidbodies[i] = enemy.GetComponent<Rigidbody2D>();
                m_EnemyColliders[i] = enemy.GetComponent<Collider2D>();
                // Combat uses overlap queries; actors need not physically block this teaching route.
                Physics2D.IgnoreCollision(playerCollider, m_EnemyColliders[i]);
            }
            m_Collected = new bool[pickups.Length];
            for (int i = 0; i < pickups.Length; i++)
                if (pickups[i] != null) pickups[i].Configure(this, player, i);
        }

        void Update()
        {
            if (!m_Initialized) return;
            if (m_Input.RestartPressed && !m_Restarting)
            {
                Restart();
                return;
            }
            if (IsComplete || IsFailed) return;
            if (m_PlayerBody.IsDead || m_PlayerRigidbody.position.y < -14f)
            {
                Finish(false);
                return;
            }
            Vector2 distance = m_PlayerRigidbody.position - (Vector2)exit.position;
            if (Mathf.Abs(distance.x) < 1.2f && Mathf.Abs(distance.y) < 1.8f) Finish(true);
        }

        void FixedUpdate()
        {
            if (!m_Initialized || IsComplete || IsFailed) return;
            Vector2 playerPosition = m_PlayerRigidbody.position;
            for (int i = 0; i < enemies.Length; i++)
            {
                CombatBotController enemy = enemies[i];
                Rigidbody2D body = m_EnemyRigidbodies[i];
                if (enemy.Body.IsDead)
                {
                    m_EnemyColliders[i].enabled = false;
                    body.simulated = false;
                    continue;
                }
                Bounds area = encounterAreas[i].bounds;
                // Keep each bot on its own teaching platform, including after knockback.
                float left = area.min.x + .6f;
                float right = area.max.x - .6f;
                if (right >= left && (body.position.x < left || body.position.x > right))
                {
                    body.position = new Vector2(Mathf.Clamp(body.position.x, left, right), body.position.y);
                    body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
                }
                bool active = playerPosition.x >= area.min.x && playerPosition.x <= area.max.x &&
                    playerPosition.y >= area.min.y && playerPosition.y <= area.max.y &&
                    Mathf.Abs(playerPosition.y - body.position.y) < 2.6f;
                if (active == m_EnemyEnabled[i]) continue;
                m_EnemyEnabled[i] = active;
                enemy.SetCombatEnabled(active);
            }
        }

        public void CollectReward(int index)
        {
            if (!m_Initialized || IsComplete || IsFailed || index < 0 || index >= m_Collected.Length || m_Collected[index]) return;
            m_Collected[index] = true;
            CollectedRewards++;
        }

        void OnPlayerDied() => Finish(false);

        void Finish(bool complete)
        {
            if (IsComplete || IsFailed) return;
            IsComplete = complete;
            IsFailed = !complete;
            m_Input.SetGameplayBlocked(true);
            player.SetCombatEnabled(false);
            player.StopAllCoroutines();
            m_PlayerRigidbody.linearVelocity = Vector2.zero;
            // An in-progress dodge or attack impulse must not carry the actor past the exit.
            m_PlayerRigidbody.simulated = false;
            for (int i = 0; i < enemies.Length; i++) enemies[i].SetCombatEnabled(false);
            timeController.ForceRestore();
            prototypeAudio.Play(complete ? PrototypeSound.Win : PrototypeSound.Lose, .4f);
        }

        public void Restart()
        {
            if (m_Restarting) return;
            m_Restarting = true;
            timeController.ForceRestore();
            string path = gameObject.scene.path;
#if UNITY_EDITOR
            EditorSceneManager.LoadSceneAsyncInPlayMode(path, new LoadSceneParameters(LoadSceneMode.Single));
#else
            SceneManager.LoadScene(path);
#endif
        }

        void EnsureStyles()
        {
            if (m_Text != null) return;
            Font font = Resources.Load<Font>("Fonts/NotoSansCJKsc-Regular");
            m_Text = new GUIStyle(GUI.skin.label) { font = font, fontSize = 18, normal = { textColor = Color.white } };
            m_Title = new GUIStyle(m_Text) { fontSize = 22, fontStyle = FontStyle.Bold };
            m_Status = new GUIStyle(m_Title) { fontSize = 26, alignment = TextAnchor.MiddleCenter };
        }

        void OnGUI()
        {
            if (!m_Initialized) return;
            EnsureStyles();
            float scale = Mathf.Max(.55f, Mathf.Min(Screen.width / 1280f, Screen.height / 720f));
            float width = Screen.width / scale;
            float height = Screen.height / scale;
            Matrix4x4 previous = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(Vector3.one * scale);
            Panel(new Rect(14f, 12f, width - 28f, 48f), new Color(.035f, .045f, .06f, .88f));
            GUI.Label(new Rect(28f, 24f, 410f, 28f), $"体力 {m_PlayerBody.Health}/{m_PlayerBody.MaxHealth}    星星 {CollectedRewards}/{TotalRewards}", m_Text);
            GUI.Label(new Rect(width - 220f, 24f, 200f, 28f), "Enter 重新开始", m_Text);
            if (IsComplete || IsFailed)
            {
                Panel(new Rect(14f, height - 110f, width - 28f, 90f), new Color(.025f, .035f, .05f, .94f));
                string message = IsComplete ? $"已到达出口 · 星星 {CollectedRewards}/{TotalRewards}" : "本次练习结束";
                GUI.Label(new Rect(20f, height - 102f, width - 40f, 40f), message, m_Status);
                GUI.Label(new Rect(20f, height - 62f, width - 40f, 32f), "按 Enter 重新开始", m_Status);
            }
            GUI.matrix = previous;
        }

        static void Panel(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }

        void OnDestroy()
        {
            if (m_PlayerBody != null) m_PlayerBody.Died -= OnPlayerDied;
            if (timeController != null) timeController.ForceRestore();
        }
    }
}
