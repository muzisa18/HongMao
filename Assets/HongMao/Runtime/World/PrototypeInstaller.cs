using System.Collections.Generic;
using UnityEngine;

namespace HongMao
{
    [DefaultExecutionOrder(-1000)]
    public sealed class PrototypeInstaller : MonoBehaviour
    {
        readonly List<AttackDefinition> m_RuntimeDefinitions = new();
        bool m_Installed;

        public CombatResourceModel Resources { get; private set; }
        public PlayerCombatController Player { get; private set; }
        public CombatBotController Enemy { get; private set; }
        public GameFlowCoordinator Flow { get; private set; }

        void Awake()
        {
            if (!m_Installed) Install();
        }

        public void Install()
        {
            if (m_Installed) return;
            m_Installed = true;
            Physics2D.gravity = new Vector2(0f, -9.81f);

            Camera camera = Camera.main;
            if (camera == null)
            {
                GameObject cameraObject = new("Main Camera");
                cameraObject.tag = "MainCamera";
                camera = cameraObject.AddComponent<Camera>();
            }
            camera.orthographic = true;
            camera.orthographicSize = 5.625f;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.backgroundColor = new Color(0.055f, 0.06f, 0.075f);
            camera.clearFlags = CameraClearFlags.SolidColor;

            CombatTimeController time = gameObject.AddComponent<CombatTimeController>();
            gameObject.AddComponent<AudioSource>();
            PrototypeAudio audio = gameObject.AddComponent<PrototypeAudio>();
            audio.Configure();
            PrototypeFeedback feedback = gameObject.AddComponent<PrototypeFeedback>();
            feedback.Configure(camera);

            Transform world = new GameObject("World").transform;
            world.SetParent(transform);
            CreateStaticBox("Floor", new Vector2(0f, -3.15f), new Vector2(20f, 0.7f), new Color(0.16f, 0.17f, 0.2f), world);
            CreateStaticBox("LeftWall", new Vector2(-9.65f, 0f), new Vector2(0.7f, 7f), new Color(0.11f, 0.12f, 0.15f), world);
            CreateStaticBox("RightWall", new Vector2(9.65f, 0f), new Vector2(0.7f, 7f), new Color(0.11f, 0.12f, 0.15f), world);
            CreateBackdrop(world);

            RedColorSource source = CreateColorSource(world);
            CombatantBody playerBody;
            PlayerInputReader input;
            Player = CreatePlayer(world, out playerBody, out input);
            Enemy = CreateEnemy(world, Player.transform, playerBody, audio, feedback);

            Resources = new CombatResourceModel();
            List<AttackDefinition> definitions = BuildAttackDefinitions();
            Player.Configure(Resources, time, audio, feedback, definitions, new[] { source });
            Enemy.Configure(Player.transform, playerBody, audio, feedback);

            Flow = gameObject.AddComponent<GameFlowCoordinator>();
            Flow.Configure(Player, input, playerBody, Enemy, time, audio);
            PrototypeHUD hud = gameObject.AddComponent<PrototypeHUD>();
            hud.Configure(playerBody, Enemy.Body, Player, Resources, Flow);
        }

        PlayerCombatController CreatePlayer(Transform parent, out CombatantBody body, out PlayerInputReader input)
        {
            GameObject player = CreateDynamicBox("Player", new Vector2(-4.4f, -1.75f), new Vector2(0.9f, 2f), new Color(0.1f, 0.52f, 0.82f), parent);
            body = player.AddComponent<CombatantBody>();
            body.Configure(100, 0);
            input = player.AddComponent<PlayerInputReader>();
            PlayerMotor2D motor = player.AddComponent<PlayerMotor2D>();
            motor.Configure(input);
            return player.AddComponent<PlayerCombatController>();
        }

        CombatBotController CreateEnemy(Transform parent, Transform player, CombatantBody playerBody, PrototypeAudio audio, PrototypeFeedback feedback)
        {
            GameObject enemy = CreateDynamicBox("CombatBot", new Vector2(3.3f, -1.65f), new Vector2(1.15f, 2.2f), new Color(0.24f, 0.28f, 0.34f), parent);
            CombatantBody body = enemy.AddComponent<CombatantBody>();
            body.Configure(400, 100);
            return enemy.AddComponent<CombatBotController>();
        }

        RedColorSource CreateColorSource(Transform parent)
        {
            GameObject source = new("RedColorSource");
            source.transform.SetParent(parent);
            source.transform.position = new Vector3(7.2f, -1.85f, 0f);
            source.transform.localScale = new Vector3(0.8f, 1.9f, 1f);
            PrototypeVisuals.AddBoxRenderer(source, new Color(0.82f, 0.08f, 0.08f), 1);
            BoxCollider2D collider = source.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            RedColorSource component = source.AddComponent<RedColorSource>();
            component.Configure();
            return component;
        }

        static GameObject CreateDynamicBox(string objectName, Vector2 position, Vector2 size, Color color, Transform parent)
        {
            GameObject box = new(objectName);
            box.transform.SetParent(parent);
            box.transform.position = position;
            box.transform.localScale = new Vector3(size.x, size.y, 1f);
            PrototypeVisuals.AddBoxRenderer(box, color, 2);
            BoxCollider2D collider = box.AddComponent<BoxCollider2D>();
            collider.size = Vector2.one;
            Rigidbody2D body = box.AddComponent<Rigidbody2D>();
            body.freezeRotation = true;
            return box;
        }

        static void CreateStaticBox(string objectName, Vector2 position, Vector2 size, Color color, Transform parent)
        {
            GameObject box = new(objectName);
            box.transform.SetParent(parent);
            box.transform.position = position;
            box.transform.localScale = new Vector3(size.x, size.y, 1f);
            PrototypeVisuals.AddBoxRenderer(box, color, 0);
            box.AddComponent<BoxCollider2D>();
        }

        static void CreateBackdrop(Transform parent)
        {
            GameObject moon = new("BackdropMoon");
            moon.transform.SetParent(parent);
            moon.transform.position = new Vector3(-5.8f, 2f, 2f);
            moon.transform.localScale = new Vector3(2.4f, 2.4f, 1f);
            PrototypeVisuals.AddBoxRenderer(moon, new Color(0.85f, 0.78f, 0.58f, 0.16f), -5);

            for (int i = 0; i < 5; i++)
            {
                GameObject stroke = new($"InkStroke_{i + 1}");
                stroke.transform.SetParent(parent);
                stroke.transform.position = new Vector3(-2.5f + i * 1.3f, -2.5f + (i % 2) * 0.22f, 1f);
                stroke.transform.localScale = new Vector3(1.8f, 0.08f + i * 0.018f, 1f);
                stroke.transform.rotation = Quaternion.Euler(0f, 0f, (i - 2) * 3f);
                PrototypeVisuals.AddBoxRenderer(stroke, new Color(0.75f, 0.76f, 0.78f, 0.08f), -4);
            }
        }

        List<AttackDefinition> BuildAttackDefinitions()
        {
            AttackDefinition[] persisted = Resources.LoadAll<AttackDefinition>("Attacks");
            if (persisted.Length >= 11) return new List<AttackDefinition>(persisted);

            return new List<AttackDefinition>
            {
                Attack(AttackKind.GroundOne, .11f, .07f, .18f, 10, 8, new Vector2(3.2f, 1f)),
                Attack(AttackKind.GroundTwo, .10f, .08f, .19f, 12, 10, new Vector2(3.5f, 1.2f)),
                Attack(AttackKind.GroundThree, .16f, .10f, .28f, 18, 18, new Vector2(5f, 2.2f)),
                Attack(AttackKind.Air, .12f, .09f, .24f, 12, 10, new Vector2(3f, 1.6f)),
                Attack(AttackKind.DashSlash, .12f, .10f, .25f, 24, 38, new Vector2(6f, 1.8f), 7f),
                Attack(AttackKind.Uppercut, .16f, .10f, .30f, 25, 42, new Vector2(2.2f, 8f), 0f, true, new Vector2(1.6f, 2.5f), new Vector2(.8f, .8f)),
                Attack(AttackKind.DiveSlam, .10f, .10f, .34f, 28, 45, new Vector2(4f, 2f), 0f, false, new Vector2(3.5f, 1.7f), Vector2.zero),
                Attack(AttackKind.MaskGroundOne, .07f, .06f, .12f, 18, 26, new Vector2(4f, 1.5f)),
                Attack(AttackKind.MaskGroundTwo, .07f, .06f, .13f, 20, 30, new Vector2(4.5f, 1.8f)),
                Attack(AttackKind.MaskGroundThree, .11f, .08f, .20f, 28, 42, new Vector2(6f, 2.4f)),
                Attack(AttackKind.MaskAir, .08f, .07f, .15f, 22, 34, new Vector2(4f, 2f))
            };
        }

        AttackDefinition Attack(AttackKind kind, float startup, float active, float recovery,
            int health, int poise, Vector2 knockback, float impulse = 0f, bool launches = false,
            Vector2? hitboxSize = null, Vector2? hitboxOffset = null)
        {
            AttackDefinition attack = ScriptableObject.CreateInstance<AttackDefinition>();
            attack.name = kind.ToString();
            attack.hideFlags = HideFlags.DontSave;
            attack.kind = kind;
            attack.startup = startup;
            attack.active = active;
            attack.recovery = recovery;
            attack.healthDamage = health;
            attack.poiseDamage = poise;
            attack.knockback = knockback;
            attack.movementImpulse = impulse;
            attack.launches = launches;
            attack.hitboxSize = hitboxSize ?? new Vector2(1.9f, 1.45f);
            attack.hitboxOffset = hitboxOffset ?? new Vector2(1.05f, 0.05f);
            m_RuntimeDefinitions.Add(attack);
            return attack;
        }

        void OnDestroy()
        {
            for (int i = 0; i < m_RuntimeDefinitions.Count; i++)
                if (m_RuntimeDefinitions[i] != null) Destroy(m_RuntimeDefinitions[i]);
            m_RuntimeDefinitions.Clear();
        }
    }
}
