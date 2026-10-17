using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HongMao.Editor
{
    public static class CombatRoomAuthoring
    {
        const string Art = "Assets/HongMao/Art/Environment/TutorialPlatformer/";
        public const string ScenePath = "Assets/HongMao/Scenes/BattleTestRoom.unity";
        static readonly Color Stone = new(.7f, .8f, .86f);

        [MenuItem("HongMao/创建新版战斗测试房（保留旧场景）")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
            if (File.Exists(ScenePath)) throw new InvalidOperationException("BattleTestRoom already exists; edit the scene directly to preserve your layout.");
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            foreach (string path in Directory.GetFiles(Art, "*.png"))
            {
                AssetDatabase.ImportAsset(path);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 100;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
            var scene = EditorSceneManager.OpenScene("Assets/HongMao/Scenes/PrototypeCombat.unity");
            RemoveLoosePrototypeSprite();
            Transform geometry = GameObject.Find("World/Geometry").transform;
            Transform decoration = GameObject.Find("World/Decoration").transform;
            Clear(geometry);
            Clear(decoration);
            Solid(geometry, "Floor — 64m", new Vector2(0, -4.25f), new Vector2(64, 3));
            Solid(geometry, "Left Boundary", new Vector2(-32.5f, 5), new Vector2(1, 20));
            Solid(geometry, "Right Boundary", new Vector2(32.5f, 5), new Vector2(1, 20));
            for (int x = -32; x < 32; x++)
            {
                Picture(geometry, "Ground Surface", "TileGroundTop", new(x + .5f, -3.25f), new(1, 1), Stone, 0);
                for (int row = 0; row < 2; row++)
                    Picture(geometry, "Ground Foundation", "TileGround", new(x + .5f, -4.25f - row), new(1, 1), Stone, 0);
            }
            for (int side = -1; side <= 1; side += 2)
                for (int step = 0; step < 7; step++)
                    Platform(geometry, $"{(side < 0 ? "West" : "East")} Step {step + 1}", side * (10 + step * 3), -1.55f + step * 1.2f, 3);
            foreach (float x in new[] { -22f, -16f, 16f, 22f })
                Platform(geometry, "Upper Sparring Deck", x, 6.85f, 4);
            Picture(decoration, "Sky", "TileGround", new(0, 5), new(90, 30), new(.32f,.43f,.49f), -30);
            for (int i = -1; i <= 1; i++)
                Picture(decoration, "Distant Mountains", "mountains", new(i * 30, 4), new(30, 18), new(.58f,.7f,.76f), -20);
            for (int i = 0; i < 9; i++)
            {
                float x = -30 + i * 7.5f;
                Picture(decoration, "Cloud", "cloud", new(x, 10 + i % 3), new(6, 1.5f), new(.75f,.83f,.85f,.65f), -15);
                Picture(decoration, "Tree", "tree", new(x, -.25f), new(3.4f, 5), new(.45f,.6f,.62f), -8);
                Picture(decoration, "Grass", "plant", new(x + 2, -2.35f), new(1.2f,.8f), new(.6f,.75f,.7f), -3);
            }
            Picture(decoration, "Training House", "house", new(0, -.15f), new(7,5.2f), new(.55f,.64f,.68f), -9);
            for (int i = -3; i <= 3; i++)
                Picture(decoration, "Courtyard Fence", "fence", new(i * 2.6f, -2.05f), new(2.6f,1.4f), new(.6f,.7f,.73f), -4);

            var player = UnityEngine.Object.FindFirstObjectByType<PlayerCombatController>();
            var sources = UnityEngine.Object.FindObjectsByType<RedColorSource>(FindObjectsSortMode.None);
            Array.Sort(sources, (a,b) => string.CompareOrdinal(a.name,b.name));
            Vector3[] spots = { new(7.5f,-1.88f), new(-19,2.87f), new(19,2.87f) };
            for (int i = 0; i < sources.Length; i++) sources[i].transform.position = spots[i % spots.Length];
            Camera camera = UnityEngine.Object.FindFirstObjectByType<Camera>();
            GameObject rig = new("Camera Follow Rig");
            rig.transform.position = new(player.transform.position.x, 1.5f, 0);
            camera.transform.SetParent(rig.transform);
            camera.transform.localPosition = new(0,0,-10);
            camera.orthographicSize = 6.5f;
            rig.AddComponent<CombatRoomCamera>().Configure(player.transform, camera);
            EditorSceneManager.SaveScene(scene, ScenePath);
            RegisterScene();
            AssetDatabase.SaveAssets();
            Debug.Log("BattleTestRoom created: 64m floor, 18 one-way platforms, existing combat actors and sources preserved.");
        }

        public static void FinalizeRoom()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath);
            RemoveLoosePrototypeSprite();
            EditorSceneManager.SaveScene(scene);
            RegisterScene();
        }

        static void RemoveLoosePrototypeSprite()
        {
            // The source prototype contains an unused white sprite below the floor.
            foreach (var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
                if (root.name == "PrototypeWhiteSprite") UnityEngine.Object.DestroyImmediate(root);
        }

        static void RegisterScene()
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (!scenes.Exists(scene => scene.path == ScenePath))
                scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        static void Clear(Transform root)
        {
            while (root.childCount > 0) UnityEngine.Object.DestroyImmediate(root.GetChild(0).gameObject);
        }

        static GameObject Solid(Transform parent, string name, Vector2 position, Vector2 size)
        {
            GameObject go = new(name);
            go.transform.SetParent(parent);
            go.transform.position = position;
            go.AddComponent<BoxCollider2D>().size = size;
            return go;
        }

        static void Platform(Transform parent, string name, float x, float top, int width)
        {
            var go = Solid(parent, name, new(x, top - .15f), new(width, .3f));
            var collider = go.GetComponent<BoxCollider2D>();
            collider.usedByEffector = true;
            var effector = go.AddComponent<PlatformEffector2D>();
            effector.useOneWay = true;
            effector.useOneWayGrouping = true;
            effector.surfaceArc = 160;
            for (int tile = 0; tile < width; tile++)
                Picture(go.transform, "Platform Tile", tile == 0 ? "TileFloatingLeftEdge" : tile == width - 1 ? "TileFloatingRightEdge" : "TileFloatingMiddle",
                    new(x - width * .5f + tile + .5f, top - .5f), Vector2.one, Stone, 0);
        }

        static void Picture(Transform parent, string name, string asset, Vector2 position, Vector2 size, Color tint, int order)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Art + asset + ".png");
            if (sprite == null) throw new InvalidOperationException("Missing environment sprite: " + asset);
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.position = position;
            go.transform.localScale = new(size.x / sprite.bounds.size.x, size.y / sprite.bounds.size.y, 1);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = tint;
            renderer.sortingOrder = order;
        }
    }
}
