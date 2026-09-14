using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HongMao.Editor
{
    public static class PrototypeSceneAuthoring
    {
        const string ScenePath = "Assets/HongMao/Scenes/PrototypeCombat.unity";
        const string WhitePath = "Assets/HongMao/Art/Prototype/PrototypeWhite.asset";
        const string PlayerPrefabPath = "Assets/HongMao/Prefabs/PlayerPrototype.prefab";
        const string EnemyPrefabPath = "Assets/HongMao/Prefabs/CombatBotPrototype.prefab";
        const string SourcePrefabPath = "Assets/HongMao/Prefabs/RedColorSource.prefab";
        const string PlayerConfigPath = "Assets/HongMao/Configs/PlayerConfig.asset";
        const string EnemyConfigPath = "Assets/HongMao/Configs/CombatBotConfig.asset";

        [MenuItem("HongMao/重建可编辑战斗测试场景")]
        static void RebuildFromMenu()
        {
            bool accepted = EditorUtility.DisplayDialog("重建战斗测试场景",
                "这会用模板重新创建 PrototypeCombat 场景。当前场景会先保存。是否继续？", "重建", "取消");
            if (accepted) BuildScene();
        }

        public static void BuildScene()
        {
            if (EditorApplication.isPlaying)
                throw new System.InvalidOperationException("Stop Play Mode before rebuilding the editable scene.");

            Scene active = SceneManager.GetActiveScene();
            if (active.IsValid() && active.isDirty && !string.IsNullOrEmpty(active.path))
                EditorSceneManager.SaveScene(active);

            EnsureFolder("Assets/HongMao", "Art");
            EnsureFolder("Assets/HongMao/Art", "Prototype");
            EnsureFolder("Assets/HongMao", "Prefabs");
            EnsureFolder("Assets/HongMao", "Configs");

            Sprite square = LoadOrCreateSquare();
            PlayerDefinition playerConfig = LoadOrCreate<PlayerDefinition>(PlayerConfigPath);
            CombatBotDefinition enemyConfig = LoadOrCreate<CombatBotDefinition>(EnemyConfigPath);
            GameObject playerPrefab = BuildPlayerPrefab(square);
            GameObject enemyPrefab = BuildEnemyPrefab(square);
            GameObject sourcePrefab = BuildSourcePrefab(square);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Camera camera = BuildCamera();
            GameObject world = new("World");
            GameObject geometry = Child(world, "Geometry");
            GameObject decoration = Child(world, "Decoration");
            GameObject colorSourcesRoot = Child(world, "Color Sources");
            GameObject actors = Child(world, "Actors");
            BuildRoom(square, geometry.transform, decoration.transform);

            GameObject playerObject = InstantiatePrefab(playerPrefab, scene, actors.transform, "Player",
                new Vector3(-7.1f, -1.8f, 0f), Vector3.one);
            GameObject enemyObject = InstantiatePrefab(enemyPrefab, scene, actors.transform, "CombatBot",
                new Vector3(3.5f, -1.65f, 0f), Vector3.one);

            var sources = new List<RedColorSource>
            {
                PlaceSource(sourcePrefab, scene, colorSourcesRoot.transform, "Red Source - Ground",
                    new Vector3(7.8f, -1.88f, 0f), new Vector3(0.72f, 1.72f, 1f)),
                PlaceSource(sourcePrefab, scene, colorSourcesRoot.transform, "Red Source - Low Platform",
                    new Vector3(-2.7f, 0.33f, 0f), new Vector3(0.62f, 1.65f, 1f)),
                PlaceSource(sourcePrefab, scene, colorSourcesRoot.transform, "Red Source - High Platform",
                    new Vector3(4.8f, 1.73f, 0f), new Vector3(0.62f, 1.65f, 1f))
            };

            GameObject systems = new("Combat Systems");
            AudioSource audioSource = systems.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            CombatTimeController time = systems.AddComponent<CombatTimeController>();
            PrototypeAudio audio = systems.AddComponent<PrototypeAudio>();
            PrototypeFeedback feedback = systems.AddComponent<PrototypeFeedback>();
            GameFlowCoordinator flow = systems.AddComponent<GameFlowCoordinator>();
            PrototypeHUD hud = systems.AddComponent<PrototypeHUD>();
            CombatDebugOverlay debug = systems.AddComponent<CombatDebugOverlay>();
            PrototypeInstaller installer = systems.AddComponent<PrototypeInstaller>();

            AttackDefinition[] attacks = Resources.LoadAll<AttackDefinition>("Attacks");
            AttackDefinition preview = FindAttack(attacks, AttackKind.GroundOne);
            PlayerCombatController player = playerObject.GetComponent<PlayerCombatController>();
            CombatBotController enemy = enemyObject.GetComponent<CombatBotController>();
            installer.ConfigureEditableScene(camera, player, enemy, sources.ToArray(), attacks,
                playerConfig, enemyConfig, time, audio, feedback, flow, hud);
            debug.Configure(player, enemy, preview);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new System.InvalidOperationException($"Failed to save scene at {ScenePath}");

            EnsureSceneInBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"HongMao editable combat room rebuilt: {attacks.Length} attacks, {sources.Count} color sources.");
        }

        static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        static Sprite LoadOrCreateSquare()
        {
            Object[] existing = AssetDatabase.LoadAllAssetsAtPath(WhitePath);
            for (int i = 0; i < existing.Length; i++)
                if (existing[i] is Sprite sprite) return sprite;

            if (AssetDatabase.LoadAssetAtPath<Object>(WhitePath) != null) AssetDatabase.DeleteAsset(WhitePath);
            var texture = new Texture2D(4, 4, TextureFormat.RGBA32, false)
            {
                name = "PrototypeWhiteTexture",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            var pixels = new Color[16];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
            texture.SetPixels(pixels);
            texture.Apply();
            AssetDatabase.CreateAsset(texture, WhitePath);
            Sprite created = Sprite.Create(texture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), 4f);
            created.name = "PrototypeWhiteSprite";
            AssetDatabase.AddObjectToAsset(created, texture);
            AssetDatabase.SaveAssets();
            return created;
        }

        static GameObject BuildPlayerPrefab(Sprite sprite)
        {
            GameObject root = BuildActorRoot("Player", sprite, new Vector2(0.82f, 1.9f), new Vector2(0f, 0.02f));
            root.AddComponent<PlayerInputReader>();
            root.AddComponent<PlayerMotor2D>();
            root.AddComponent<PlayerCombatController>();
            Transform visualRoot = Child(root, "Visual").transform;
            var tinted = new List<SpriteRenderer>();
            Part(visualRoot, "Back Scarf", sprite, new Vector3(-0.44f, 0.34f), new Vector3(0.72f, 0.16f), new Color(0.75f, 0.04f, 0.05f), 2, 12f, tinted);
            Part(visualRoot, "Back Leg", sprite, new Vector3(-0.2f, -0.58f), new Vector3(0.22f, 0.78f), new Color(0.06f, 0.14f, 0.2f), 2, 2f, tinted);
            Part(visualRoot, "Front Leg", sprite, new Vector3(0.2f, -0.58f), new Vector3(0.22f, 0.78f), new Color(0.08f, 0.24f, 0.35f), 3, -2f, tinted);
            Part(visualRoot, "Torso", sprite, new Vector3(0f, 0.02f), new Vector3(0.72f, 0.96f), new Color(0.08f, 0.42f, 0.65f), 3, 0f, tinted);
            Part(visualRoot, "Sash", sprite, new Vector3(0f, -0.22f), new Vector3(0.8f, 0.13f), new Color(0.82f, 0.08f, 0.08f), 4, 0f, tinted);
            Transform head = Part(visualRoot, "Head", sprite, new Vector3(0f, 0.76f), new Vector3(0.62f, 0.58f), new Color(0.9f, 0.82f, 0.66f), 4, 0f, tinted).transform;
            Part(head, "Hair", sprite, new Vector3(-0.03f, 0.34f), new Vector3(1.1f, 0.3f), new Color(0.045f, 0.05f, 0.065f), 5, -4f, tinted);
            Part(head, "Face Mark", sprite, new Vector3(0.24f, 0.02f), new Vector3(0.11f, 0.12f), new Color(0.75f, 0.04f, 0.05f), 6, 0f, tinted);
            Transform weapon = Part(visualRoot, "Sword", sprite, new Vector3(0.57f, 0.08f), new Vector3(0.12f, 1.38f), new Color(0.82f, 0.86f, 0.9f), 5, -26f, tinted).transform;
            Part(weapon, "Hilt", sprite, new Vector3(0f, -0.55f), new Vector3(2.8f, 0.13f), new Color(0.32f, 0.12f, 0.06f), 6, 0f, tinted);
            Part(visualRoot, "Shadow", sprite, new Vector3(0f, -0.96f), new Vector3(1.15f, 0.12f), new Color(0f, 0f, 0f, 0.3f), 1, 0f, null);
            PrototypeCharacterVisual visual = root.AddComponent<PrototypeCharacterVisual>();
            visual.Configure(visualRoot, head, weapon, tinted.ToArray());
            return SavePrefab(root, PlayerPrefabPath);
        }

        static GameObject BuildEnemyPrefab(Sprite sprite)
        {
            GameObject root = BuildActorRoot("CombatBot", sprite, new Vector2(1.05f, 2.15f), new Vector2(0f, 0.03f));
            root.AddComponent<CombatBotController>();
            Transform visualRoot = Child(root, "Visual").transform;
            var tinted = new List<SpriteRenderer>();
            Part(visualRoot, "Back Robe", sprite, new Vector3(-0.08f, -0.38f), new Vector3(0.92f, 1.35f), new Color(0.16f, 0.18f, 0.22f), 2, 2f, tinted);
            Part(visualRoot, "Armor", sprite, new Vector3(0f, 0.1f), new Vector3(1f, 0.88f), new Color(0.34f, 0.16f, 0.12f), 3, 0f, tinted);
            Part(visualRoot, "Belt", sprite, new Vector3(0f, -0.22f), new Vector3(1.08f, 0.16f), new Color(0.73f, 0.5f, 0.1f), 4, 0f, tinted);
            Transform head = Part(visualRoot, "Mask", sprite, new Vector3(0f, 0.9f), new Vector3(0.78f, 0.64f), new Color(0.72f, 0.68f, 0.58f), 4, 0f, tinted).transform;
            Part(head, "Mask Brow", sprite, new Vector3(0.12f, 0.02f), new Vector3(0.72f, 0.1f), new Color(0.55f, 0.04f, 0.03f), 6, -12f, tinted);
            Part(head, "Helmet", sprite, new Vector3(-0.02f, 0.37f), new Vector3(1.15f, 0.3f), new Color(0.09f, 0.1f, 0.13f), 5, 0f, tinted);
            Part(visualRoot, "Back Leg", sprite, new Vector3(-0.27f, -0.86f), new Vector3(0.3f, 0.58f), new Color(0.08f, 0.09f, 0.11f), 1, 0f, tinted);
            Part(visualRoot, "Front Leg", sprite, new Vector3(0.27f, -0.86f), new Vector3(0.3f, 0.58f), new Color(0.12f, 0.13f, 0.16f), 3, 0f, tinted);
            Transform weapon = Part(visualRoot, "Polearm", sprite, new Vector3(0.76f, 0.05f), new Vector3(0.12f, 2.35f), new Color(0.28f, 0.15f, 0.07f), 5, -18f, tinted).transform;
            Part(weapon, "Blade", sprite, new Vector3(0f, 0.56f), new Vector3(2.6f, 0.3f), new Color(0.72f, 0.78f, 0.82f), 6, 18f, tinted);
            Part(visualRoot, "Shadow", sprite, new Vector3(0f, -1.08f), new Vector3(1.45f, 0.15f), new Color(0f, 0f, 0f, 0.34f), 1, 0f, null);
            PrototypeCharacterVisual visual = root.AddComponent<PrototypeCharacterVisual>();
            visual.Configure(visualRoot, head, weapon, tinted.ToArray());
            return SavePrefab(root, EnemyPrefabPath);
        }

        static GameObject BuildSourcePrefab(Sprite sprite)
        {
            GameObject root = new("RedColorSource");
            SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = new Color(0.75f, 0.035f, 0.035f);
            renderer.sortingOrder = 2;
            root.AddComponent<BoxCollider2D>().isTrigger = true;
            root.AddComponent<RedColorSource>();
            Part(root.transform, "Gold Cap", sprite, new Vector3(0f, 0.53f), new Vector3(1.3f, 0.13f), new Color(0.82f, 0.58f, 0.12f), 3, 0f, null);
            Part(root.transform, "Ink Core", sprite, Vector3.zero, new Vector3(0.24f, 0.65f), new Color(0.14f, 0.015f, 0.02f), 3, 0f, null);
            return SavePrefab(root, SourcePrefabPath);
        }

        static GameObject BuildActorRoot(string name, Sprite sprite, Vector2 colliderSize, Vector2 colliderOffset)
        {
            GameObject root = new(name);
            SpriteRenderer compatibilityRenderer = root.AddComponent<SpriteRenderer>();
            compatibilityRenderer.sprite = sprite;
            compatibilityRenderer.enabled = false;
            Rigidbody2D body = root.AddComponent<Rigidbody2D>();
            body.freezeRotation = true;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            BoxCollider2D collider = root.AddComponent<BoxCollider2D>();
            collider.size = colliderSize;
            collider.offset = colliderOffset;
            root.AddComponent<CombatantBody>();
            return root;
        }

        static GameObject SavePrefab(GameObject source, string path)
        {
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(source, path);
            Object.DestroyImmediate(source);
            return prefab;
        }

        static Camera BuildCamera()
        {
            GameObject cameraObject = new("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5.625f;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.backgroundColor = new Color(0.055f, 0.06f, 0.075f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            cameraObject.AddComponent<AudioListener>();
            return camera;
        }

        static void BuildRoom(Sprite sprite, Transform geometry, Transform decoration)
        {
            StaticBox(geometry, "Floor", sprite, new Vector2(0f, -3.15f), new Vector2(21.5f, 0.8f), new Color(0.12f, 0.13f, 0.16f));
            StaticBox(geometry, "Left Boundary", sprite, new Vector2(-10.45f, 0f), new Vector2(0.55f, 7f), new Color(0.09f, 0.1f, 0.13f));
            StaticBox(geometry, "Right Boundary", sprite, new Vector2(10.45f, 0f), new Vector2(0.55f, 7f), new Color(0.09f, 0.1f, 0.13f));
            StaticBox(geometry, "Low Platform", sprite, new Vector2(-2.7f, -0.68f), new Vector2(3.25f, 0.35f), new Color(0.24f, 0.25f, 0.28f));
            StaticBox(geometry, "High Platform", sprite, new Vector2(4.8f, 0.88f), new Vector2(3.15f, 0.35f), new Color(0.24f, 0.25f, 0.28f));
            StaticBox(geometry, "Low Ceiling", sprite, new Vector2(-6.05f, 1.5f), new Vector2(2.5f, 0.35f), new Color(0.18f, 0.19f, 0.22f));
            VisualBox(decoration, "Moon", sprite, new Vector2(-6.6f, 2.9f), new Vector2(2.2f, 2.2f), new Color(0.86f, 0.77f, 0.53f, 0.22f), -8, 0f);
            VisualBox(decoration, "Mountain Far", sprite, new Vector2(2.8f, 0.2f), new Vector2(8f, 2.1f), new Color(0.12f, 0.14f, 0.18f), -9, 8f);
            VisualBox(decoration, "Mountain Near", sprite, new Vector2(-2.4f, -0.35f), new Vector2(7f, 1.45f), new Color(0.16f, 0.17f, 0.2f), -8, -6f);
            for (int i = 0; i < 7; i++)
                VisualBox(decoration, $"Ink Stroke {i + 1}", sprite,
                    new Vector2(-7.2f + i * 2.25f, -2.5f + (i % 2) * 0.13f),
                    new Vector2(2.7f, 0.07f + i * 0.008f), new Color(0.7f, 0.72f, 0.76f, 0.11f), -2, (i - 3) * 1.7f);
        }

        static GameObject InstantiatePrefab(GameObject prefab, Scene scene, Transform parent, string name,
            Vector3 position, Vector3 scale)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            instance.name = name;
            instance.transform.SetParent(parent);
            instance.transform.position = position;
            instance.transform.localScale = scale;
            return instance;
        }

        static RedColorSource PlaceSource(GameObject prefab, Scene scene, Transform parent, string name,
            Vector3 position, Vector3 scale)
        {
            return InstantiatePrefab(prefab, scene, parent, name, position, scale).GetComponent<RedColorSource>();
        }

        static GameObject Child(GameObject parent, string name)
        {
            GameObject child = new(name);
            child.transform.SetParent(parent.transform);
            child.transform.localPosition = Vector3.zero;
            return child;
        }

        static SpriteRenderer Part(Transform parent, string name, Sprite sprite, Vector3 position,
            Vector3 scale, Color color, int order, float rotation, List<SpriteRenderer> tinted)
        {
            GameObject part = new(name);
            part.transform.SetParent(parent);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            part.transform.localRotation = Quaternion.Euler(0f, 0f, rotation);
            SpriteRenderer renderer = part.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = order;
            tinted?.Add(renderer);
            return renderer;
        }

        static void StaticBox(Transform parent, string name, Sprite sprite, Vector2 position,
            Vector2 size, Color color)
        {
            GameObject box = VisualBox(parent, name, sprite, position, size, color, 0, 0f);
            box.AddComponent<BoxCollider2D>();
        }

        static GameObject VisualBox(Transform parent, string name, Sprite sprite, Vector2 position,
            Vector2 size, Color color, int order, float rotation)
        {
            GameObject box = new(name);
            box.transform.SetParent(parent);
            box.transform.position = position;
            box.transform.localScale = new Vector3(size.x, size.y, 1f);
            box.transform.rotation = Quaternion.Euler(0f, 0f, rotation);
            SpriteRenderer renderer = box.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = order;
            return box;
        }

        static AttackDefinition FindAttack(AttackDefinition[] attacks, AttackKind kind)
        {
            for (int i = 0; i < attacks.Length; i++) if (attacks[i].kind == kind) return attacks[i];
            return null;
        }

        static void EnsureSceneInBuildSettings()
        {
            EditorBuildSettingsScene[] current = EditorBuildSettings.scenes;
            for (int i = 0; i < current.Length; i++) if (current[i].path == ScenePath) return;
            var updated = new List<EditorBuildSettingsScene>(current) { new(ScenePath, true) };
            EditorBuildSettings.scenes = updated.ToArray();
        }

        static void EnsureFolder(string parent, string child)
        {
            string path = $"{parent}/{child}";
            if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, child);
        }
    }
}
