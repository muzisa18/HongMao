using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HongMao.Editor
{
    // Creates saved, individually editable objects. Never regenerates shared prefabs.
    public static class FirstLevelAuthoring
    {
        public const string ScenePath = "Assets/HongMao/Scenes/FirstLevelGraybox.unity";
        const string Root = "Assets/HongMao/";
        const string Art = Root + "Art/Environment/TutorialPlatformer/";
        static readonly Color Gold = new(1f, .74f, .25f);
        static Sprite s_Square;

        [MenuItem("HongMao/第一关/创建草稿白盒（仅创建一次）")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
            if (File.Exists(ScenePath))
                throw new InvalidOperationException("FirstLevelGraybox already exists. Edit the saved scene to preserve your layout.");
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            s_Square = AssetDatabase.LoadAllAssetsAtPath(Root + "Art/Prototype/PrototypeWhite.asset").OfType<Sprite>().First();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Transform world = new GameObject("World").transform;
            Transform geometry = Group(world, "Geometry");
            Transform decoration = Group(world, "Decoration");
            Transform actors = Group(world, "Actors");
            Transform rewards = Group(world, "Rewards");
            Transform markers = Group(world, "Route Markers");

            ComposeTerrain(geometry, decoration);

            var player = Actor<PlayerCombatController>(actors, "Player", "PlayerPrototype", new(0, 4.43f));
            var dummy = Actor<CombatBotController>(actors, "Practice Target", "CombatBotPrototype", new(15, 1.045f));
            var enemy = Actor<CombatBotController>(actors, "Lower Route Guard", "CombatBotPrototype", new(43, -4.755f));
            BoxCollider2D practiceArea = Area(markers, "Practice encounter bounds", new(14.5f, 1.2f), new(11, 3.8f));
            BoxCollider2D fightArea = Area(markers, "Lower encounter bounds", new(42.5f, -4.7f), new(10, 3.8f));
            var pickups = new[] { Star(rewards, "Upper Reward", new(41.5f, 6.15f)), Star(rewards, "Exploration Reward", new(13.5f, -3.2f)) };

            Transform exit = Group(markers, "Exit");
            exit.position = new(70.5f, 7.03f);

            Camera camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
            camera.tag = "MainCamera";
            camera.orthographic = true;
            camera.orthographicSize = 6.7f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new(.065f, .085f, .11f);
            Transform rig = new GameObject("Camera Follow Rig").transform;
            rig.position = new(3, 4, 0);
            camera.transform.SetParent(rig, false);
            camera.transform.localPosition = new(0, 0, -10);
            var follow = rig.gameObject.AddComponent<CombatRoomCamera>();
            follow.Configure(player.transform, camera);
            var serialized = new SerializedObject(follow);
            serialized.FindProperty("horizontalBounds").vector2Value = new(-4, 74);
            serialized.FindProperty("verticalBounds").vector2Value = new(-12, 14);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            GameObject systems = new("First Level Systems");
            var time = systems.AddComponent<CombatTimeController>();
            var audio = systems.AddComponent<PrototypeAudio>();
            var feedback = systems.AddComponent<PrototypeFeedback>();
            var level = systems.AddComponent<FirstLevelController>();
            var passiveConfig = EnemyConfig("FirstLevelPracticeConfig", true);
            var guardConfig = EnemyConfig("FirstLevelGuardConfig", false);
            level.Configure(camera, player, AssetDatabase.LoadAssetAtPath<PlayerDefinition>(Root + "Configs/PlayerConfig.asset"),
                new[] { dummy, enemy }, new[] { passiveConfig, guardConfig }, new[] { practiceArea, fightArea },
                time, audio, feedback, exit, pickups);

            Transform ladder = Group(world, "Recovery Ladder");
            Transform bottom = Group(ladder, "Bottom Mount");
            Transform top = Group(ladder, "Top Mount");
            bottom.position = new(54.6f, -8.47f);
            top.position = new(56.9f, 5.93f);
            DressLadder(ladder);
            ladder.gameObject.AddComponent<FirstLevelLadder>().Configure(level, player, bottom, top);
            GameObject.Find("World/Geometry/15 One-way slide").AddComponent<FirstLevelSlide>()
                .Configure(level, player, new(48.5f, -5.8f), new(53.3f, -9.4f));

            ApplySurfaceMaterial();
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("FIRST_LEVEL_CREATED: " + ScenePath + "; upper teaching route + lower exploration/combat recovery route; shared prefabs unchanged.");
        }

        // Applies the user's corrected map intent to this scene, retaining actor/prefab references.
        public static void ReviseFromDraft()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Directory.CreateDirectory("Builds/FirstLevelValidation");
            const string backup = "Builds/FirstLevelValidation/BeforeTutorialRevision.unity";
            if (!File.Exists(backup)) File.Copy(ScenePath, backup);
            var scene = EditorSceneManager.OpenScene(ScenePath);
            s_Square = AssetDatabase.LoadAllAssetsAtPath(Root + "Art/Prototype/PrototypeWhite.asset").OfType<Sprite>().First();
            var geometry = GameObject.Find("World/Geometry").transform;
            var decoration = GameObject.Find("World/Decoration").transform;
            Clear(geometry);
            Clear(decoration);
            ComposeTerrain(geometry, decoration);
            foreach (var label in UnityEngine.Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None))
                UnityEngine.Object.DestroyImmediate(label.gameObject);
            var references = GameObject.Find("World/Route Markers");
            if (references != null)
            {
                foreach (Transform child in references.transform.Cast<Transform>().ToArray())
                    if (child.name == "Route arrow" || child.name == "Arrow head") UnityEngine.Object.DestroyImmediate(child.gameObject);
                references.name = "Level References";
            }
            var level = UnityEngine.Object.FindFirstObjectByType<FirstLevelController>();
            var player = UnityEngine.Object.FindFirstObjectByType<PlayerCombatController>();
            var ladder = UnityEngine.Object.FindFirstObjectByType<FirstLevelLadder>();
            foreach (Transform child in ladder.transform.Cast<Transform>().ToArray())
                if (child.name != "Bottom Mount" && child.name != "Top Mount") UnityEngine.Object.DestroyImmediate(child.gameObject);
            Transform bottom = ladder.transform.Find("Bottom Mount");
            Transform top = ladder.transform.Find("Top Mount");
            bottom.position = new(54.6f, -8.47f);
            top.position = new(56.9f, 5.93f);
            DressLadder(ladder.transform);
            ladder.Configure(level, player, bottom, top);
            GameObject.Find("World/Geometry/15 One-way slide").AddComponent<FirstLevelSlide>()
                .Configure(level, player, new(48.5f, -5.8f), new(53.3f, -9.4f));
            ApplySurfaceMaterial();
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("FIRST_LEVEL_REVISED: tutorial art, raised platforms, hollow chamber, continuous one-way slope, no sketch labels.");
        }

        static void Clear(Transform root)
        {
            while (root.childCount > 0) UnityEngine.Object.DestroyImmediate(root.GetChild(0).gameObject);
        }

        static void ComposeTerrain(Transform geometry, Transform decoration)
        {
            Color stone = new(.84f, .91f, .93f);
            Mass(geometry, "01 Start platform", -3, 8, 3.5f, -12.8f, stone);
            Mass(geometry, "02 Practice platform", 8, 21, 0, -2.1f, stone);
            Floating(geometry, "03 Jump platform 1", 23.2f, 1, 2.4f, stone);
            Floating(geometry, "04 Jump platform 2", 26.3f, 2, 2.4f, stone);
            Floating(geometry, "05 Jump platform 3", 29.4f, 3, 2.4f, stone);
            Transform hollow = Group(geometry, "06 Hollow right platform");
            Mass(hollow, "Roof", 31.4f, 53, 4, 2.8f, stone);
            Mass(hollow, "West support", 31.4f, 33.4f, 2.8f, -4.9f, stone);
            Mass(hollow, "East support", 51.8f, 53, 2.8f, -4.6f, stone);
            Floating(geometry, "07 Reward platform", 41.5f, 5.15f, 3, stone);
            Mass(geometry, "08 Exploration platform", 9.1f, 19.9f, -5, -7, stone);
            Mass(geometry, "09 Reward plinth", 12.6f, 14.4f, -4.2f, -5, stone);
            Floating(geometry, "10 Exploration return platform", 21.2f, -6.9f, 2.4f, stone);
            Floating(geometry, "11 Exploration upper platform", 20.4f, -5.8f, 1.4f, stone);
            Mass(geometry, "12 Lower path", 19.9f, 35, -8, -12.8f, stone);
            Mass(geometry, "13 Chamber entrance step", 35, 37, -6.9f, -12.8f, stone);
            Mass(hollow, "14 Chamber floor", 37, 48.5f, -5.8f, -12.8f, stone);
            Slope(geometry, stone);
            Mass(geometry, "16 Ladder foot", 53.3f, 55.2f, -9.4f, -12.8f, stone);
            Mass(geometry, "17 Exit approach platform", 55.2f, 63, 5, -12.8f, stone);
            Mass(geometry, "18 Exit platform", 63, 73, 6.1f, -12.8f, stone);
            Mass(geometry, "West boundary", -4, -3, 12, -14, stone);
            Mass(geometry, "Lower west boundary", 8.5f, 9.1f, -4.9f, -12.8f, stone);
            Mass(geometry, "East boundary", 73, 74, 13, -14, stone);

            Picture(decoration, "Sky", new(35, 1), new(160, 80), new(.58f, .74f, .82f)).GetComponent<SpriteRenderer>().sortingOrder = -50;
            for (int i = -1; i < 3; i++)
                ArtPicture(decoration, "mountains", new(i * 37 + 12, 4.2f), new(42, 11.3f), new(.78f, .87f, .91f), -35);
            for (int i = 0; i < 8; i++)
                ArtPicture(decoration, "cloud", new(-3 + i * 11, 10 + i % 2 * 1.3f), new(5, 1.37f), Color.white, -30);
            // A recessed, darker wall makes the room read as the interior of the raised platform.
            Transform cave = Group(decoration, "Recessed chamber wall");
            TileFill(cave, 33.4f, 51.8f, 2.8f, -5.8f, new(.24f, .35f, .4f), -10, false);
            foreach (var spot in new[] { new Vector2(1.5f, 3.5f), new(18.3f, 0), new(35.5f, 4), new(48.2f, 4), new(60.2f, 5) })
            {
                ArtPicture(decoration, "tree", spot + new Vector2(0, 2.1f), new(2.25f, 4.2f), new(.84f, .93f, .91f), -3);
                ArtPicture(decoration, "plant", spot + new Vector2(1.2f, .37f), new(.8f, .73f), Color.white, 1);
            }
            ArtPicture(decoration, "fence", new(4.3f, 4), new(2.2f, 1), Color.white, -2);
            ArtPicture(decoration, "fence", new(38.2f, 4.5f), new(2.2f, 1), Color.white, -2);
            ArtPicture(decoration, "house", new(70.5f, 8.1625f), new(5.5f, 4.125f), Color.white, -1);
        }

        static void Mass(Transform parent, string name, float left, float right, float top, float bottom, Color tint)
        {
            Transform root = Group(parent, name);
            root.position = new((left + right) / 2, (top + bottom) / 2);
            root.gameObject.AddComponent<BoxCollider2D>().size = new(right - left, top - bottom);
            TileFill(root, left, right, top, bottom, tint, 0, true);
        }

        static void TileFill(Transform parent, float left, float right, float top, float bottom, Color tint, int order, bool surface)
        {
            for (float x = left; x < right - .001f; x += 1)
                for (float y = top; y > bottom + .001f; y -= 1)
                {
                    float width = Mathf.Min(1, right - x);
                    float height = Mathf.Min(1, y - bottom);
                    ArtPicture(parent, surface && y == top ? "TileGroundTop" : "TileGround",
                        new(x + width / 2, y - height / 2), new(width + .008f, height + .008f), tint, order);
                }
        }

        static void Floating(Transform parent, string name, float x, float top, float width, Color tint)
        {
            Transform root = Group(parent, name);
            root.position = new(x, top - .45f);
            var collider = root.gameObject.AddComponent<BoxCollider2D>();
            collider.size = new(width, .9f);
            collider.usedByEffector = true;
            var effector = root.gameObject.AddComponent<PlatformEffector2D>();
            effector.useOneWay = true;
            effector.useOneWayGrouping = true;
            effector.surfaceArc = 160;
            int count = Mathf.CeilToInt(width);
            float tileWidth = width / count;
            for (int i = 0; i < count; i++)
                ArtPicture(root, i == 0 ? "TileFloatingLeftEdge" : i == count - 1 ? "TileFloatingRightEdge" : "TileFloatingMiddle",
                    new(x - width / 2 + (i + .5f) * tileWidth, top - .45f), new(tileWidth + .008f, .9f), tint, 0);
        }

        static void Slope(Transform parent, Color tint)
        {
            Vector2 start = new(48.5f, -5.8f);
            Vector2 end = new(53.3f, -9.4f);
            Transform root = Group(parent, "15 One-way slide");
            var collider = root.gameObject.AddComponent<PolygonCollider2D>();
            collider.points = new[] { start, end, new Vector2(end.x, -12.8f), new Vector2(start.x, -12.8f) };
            for (float x = start.x; x < end.x - .001f; x += 1)
            {
                float width = Mathf.Min(1, end.x - x);
                float maxTop = Mathf.Lerp(start.y, end.y, (x + width - start.x) / (end.x - start.x));
                for (float y = -12.8f; y + 1 <= maxTop; y += 1)
                    ArtPicture(root, "TileGround", new(x + width / 2, y + .5f), new(width + .008f, 1.008f), tint, 0);
            }
            Vector2 direction = (end - start).normalized;
            Vector2 normal = new(-direction.y, direction.x);
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            int segments = Mathf.CeilToInt(Vector2.Distance(start, end));
            float length = Vector2.Distance(start, end) / segments;
            for (int i = 0; i < segments; i++)
            {
                var tile = ArtPicture(root, "TileGroundTop", start + direction * ((i + .5f) * length) - normal * .9f,
                    new(length + .008f, 1.8f), tint, 1);
                tile.transform.rotation = Quaternion.Euler(0, 0, angle);
            }
        }

        static void DressLadder(Transform parent)
        {
            for (int i = 0; i < 9; i++)
            {
                var section = ArtPicture(parent, "fence", new(54.6f, -8.5f + i * 1.6f), new(1.8f, .85f), Color.white, 1);
                section.name = "Ladder section " + (i + 1);
                section.transform.rotation = Quaternion.Euler(0, 0, 90);
            }
        }

        static GameObject ArtPicture(Transform parent, string asset, Vector2 position, Vector2 size, Color tint, int order)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Art + asset + ".png");
            if (sprite == null) throw new InvalidOperationException("Missing tutorial sprite: " + asset);
            Transform go = Group(parent, asset);
            go.position = position;
            Vector3 parentScale = parent.lossyScale;
            go.localScale = new(size.x / sprite.bounds.size.x / parentScale.x, size.y / sprite.bounds.size.y / parentScale.y, 1);
            var renderer = go.gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = tint;
            renderer.sortingOrder = order;
            return go.gameObject;
        }

        [MenuItem("HongMao/第一关/打开草稿白盒")]
        public static void Open()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(ScenePath);
        }

        // Also works in batchmode with graphics; leaves an overview outside Assets.
        public static void CaptureOverview()
        {
            EditorSceneManager.OpenScene(ScenePath);
            var camera = Camera.main;
            camera.transform.SetParent(null);
            camera.transform.position = new(35, -.5f, -10);
            camera.orthographicSize = 21.5f;
            camera.aspect = 2;
            var render = new RenderTexture(2400, 1200, 24);
            camera.targetTexture = render;
            camera.Render();
            RenderTexture.active = render;
            var image = new Texture2D(2400, 1200, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, 2400, 1200), 0, 0);
            image.Apply();
            string path = Environment.GetEnvironmentVariable("HONGMAO_LEVEL_OVERVIEW") ?? "/private/tmp/hongmao-first-level-overview.png";
            File.WriteAllBytes(path, image.EncodeToPNG());
            RenderTexture.active = null;
            camera.targetTexture = null;
            UnityEngine.Object.DestroyImmediate(image);
            UnityEngine.Object.DestroyImmediate(render);
            Debug.Log("FIRST_LEVEL_OVERVIEW: " + path);
        }

        static void ApplySurfaceMaterial()
        {
            const string path = Root + "Configs/FirstLevelSurface.physicsMaterial2D";
            var material = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(path);
            if (material == null)
            {
                material = new PhysicsMaterial2D("First Level Surface") { friction = 0, bounciness = 0 };
                AssetDatabase.CreateAsset(material, path);
            }
            // Constant horizontal motor input must not turn wall friction into a jump brake.
            foreach (var collider in GameObject.Find("World/Geometry").GetComponentsInChildren<Collider2D>())
                collider.sharedMaterial = material;
        }

        static CombatBotDefinition EnemyConfig(string name, bool practice)
        {
            string path = Root + "Configs/" + name + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<CombatBotDefinition>(path);
            if (existing != null) return existing;
            var config = ScriptableObject.CreateInstance<CombatBotDefinition>();
            config.maxHealth = practice ? 30 : 45;
            config.maxPoise = practice ? 0 : 15;
            config.moveSpeed = practice ? 0 : .8f;
            config.attackRange = practice ? 0 : 1.5f;
            config.openingGrace = practice ? 99999 : 1.5f;
            config.parryableTelegraph = 1.2f;
            config.unblockableTelegraph = 1.5f;
            config.attackRecovery = 1.5f;
            config.parryableDamage = practice ? 0 : 5;
            config.unblockableDamage = practice ? 0 : 5;
            config.knockback = practice ? Vector2.zero : new(1, .5f);
            AssetDatabase.CreateAsset(config, path);
            return config;
        }

        static Transform Group(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        static T Actor<T>(Transform parent, string name, string prefabName, Vector2 position) where T : Component
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Prefabs/" + prefabName + ".prefab");
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.name = name;
            instance.transform.position = position;
            return instance.GetComponent<T>();
        }

        static GameObject Picture(Transform parent, string name, Vector2 position, Vector2 size, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            Vector3 parentScale = parent.lossyScale;
            go.transform.localScale = new(size.x / parentScale.x, size.y / parentScale.y, 1);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = s_Square;
            renderer.color = new(color.r, color.g, color.b, 1);
            renderer.sortingOrder = 0;
            return go;
        }

        static BoxCollider2D Area(Transform parent, string name, Vector2 position, Vector2 size)
        {
            Transform area = Group(parent, name);
            area.position = position;
            var collider = area.gameObject.AddComponent<BoxCollider2D>();
            collider.size = size;
            collider.isTrigger = true;
            return collider;
        }

        static FirstLevelPickup Star(Transform parent, string name, Vector2 position)
        {
            var go = Picture(parent, name, position, new(.65f, .65f), Gold);
            go.transform.rotation = Quaternion.Euler(0, 0, 45);
            go.GetComponent<SpriteRenderer>().sortingOrder = 4;
            return go.AddComponent<FirstLevelPickup>();
        }

    }
}
