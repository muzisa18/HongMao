using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace HongMao.Editor
{
    public static class PlayerSpritePrototypeAuthoring
    {
        const string OriginalArtPath = "Assets/HongMao/Art/鸿毛精灵图/";
        const string IdleSheetPath = OriginalArtPath + "MA01_待機.png";
        const string LeftRunSheetPath = OriginalArtPath + "MA01_左移動.png";
        const string RightRunSheetPath = OriginalArtPath + "MA01_右移動 .png";
        const string LeftJumpSheetPath = OriginalArtPath + "MA01_左跳躍.png";
        const string RightJumpSheetPath = OriginalArtPath + "MA01_右跳躍.png";
        const string LeftFallSheetPath = OriginalArtPath + "MA01_左下落.png";
        const string RightFallSheetPath = OriginalArtPath + "MA01_右下落.png";
        const string RedMaskIdleSheetPath = OriginalArtPath + "关羽脸谱_待機.png";
        const int OriginalCellSize = 64;
        const float OriginalPixelsPerUnit = 30f;
        const float OriginalFramesPerSecond = 10f;
        const string PlayerPrefabPath = "Assets/HongMao/Prefabs/PlayerPrototype.prefab";
        const string SpriteObjectName = "Sprite Animation";

        [MenuItem("HongMao/美术/应用玩家角色精灵")]
        public static void Apply()
        {
            ImportOriginalSheet(IdleSheetPath, 8, 56);
            ImportOriginalSheet(LeftRunSheetPath, 10, 55);
            ImportOriginalSheet(RightRunSheetPath, 10, 55);
            ImportOriginalSheet(LeftJumpSheetPath, 3, 55);
            ImportOriginalSheet(RightJumpSheetPath, 3, 55);
            ImportOriginalSheet(LeftFallSheetPath, 3, 55);
            ImportOriginalSheet(RightFallSheetPath, 3, 55);
            ImportOriginalSheet(RedMaskIdleSheetPath, 8, 61);
            Sprite[] idleFrames = LoadFrames(IdleSheetPath, 8);
            Sprite[] leftRunFrames = LoadFrames(LeftRunSheetPath, 10);
            Sprite[] rightRunFrames = LoadFrames(RightRunSheetPath, 10);
            Sprite[] leftJumpFrames = LoadFrames(LeftJumpSheetPath, 3);
            Sprite[] rightJumpFrames = LoadFrames(RightJumpSheetPath, 3);
            Sprite[] leftFallFrames = LoadFrames(LeftFallSheetPath, 3);
            Sprite[] rightFallFrames = LoadFrames(RightFallSheetPath, 3);
            Sprite[] redMaskIdleFrames = LoadFrames(RedMaskIdleSheetPath, 8);

            GameObject root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            try
            {
                Transform visualRoot = root.transform.Find("Visual");
                if (visualRoot == null) throw new InvalidOperationException("Player prefab has no Visual child.");

                Transform spriteTransform = visualRoot.Find(SpriteObjectName);
                if (spriteTransform == null)
                {
                    var spriteObject = new GameObject(SpriteObjectName);
                    spriteTransform = spriteObject.transform;
                    spriteTransform.SetParent(visualRoot, false);
                }

                spriteTransform.localPosition = new Vector3(0f, -0.94f, 0f);
                spriteTransform.localRotation = Quaternion.identity;
                spriteTransform.localScale = Vector3.one;

                SpriteRenderer animatedRenderer = spriteTransform.GetComponent<SpriteRenderer>();
                if (animatedRenderer == null) animatedRenderer = spriteTransform.gameObject.AddComponent<SpriteRenderer>();
                animatedRenderer.sprite = idleFrames[0];
                animatedRenderer.color = Color.white;
                animatedRenderer.sortingOrder = 5;
                animatedRenderer.enabled = true;

                SpriteRenderer[] renderers = visualRoot.GetComponentsInChildren<SpriteRenderer>(true);
                for (int i = 0; i < renderers.Length; i++)
                {
                    if (renderers[i] == animatedRenderer || renderers[i].gameObject.name == "Shadow") continue;
                    renderers[i].enabled = false;
                }

                PrototypeCharacterVisual visual = root.GetComponent<PrototypeCharacterVisual>();
                if (visual == null) throw new InvalidOperationException("Player prefab has no PrototypeCharacterVisual component.");
                // Apply only the available originals; never rebind future action art to placeholder sheets.
                visual.ConfigureAnimatedRenderer(animatedRenderer);
                visual.ConfigureLocomotionSprites(idleFrames, leftRunFrames, rightRunFrames, OriginalFramesPerSecond);
                visual.ConfigureAirborneSprites(leftJumpFrames, rightJumpFrames, leftFallFrames, rightFallFrames,
                    OriginalFramesPerSecond);
                visual.ConfigureRedMaskIdleSprites(redMaskIdleFrames);

                PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
                Debug.Log("Applied Li and Ban's original idle, left/right movement, jump/fall, and red-mask idle sprites to PlayerPrototype.prefab; other action bindings retained.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        static void ImportOriginalSheet(string path, int frameCount, int footBottomFromTop)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new InvalidOperationException($"No texture importer at {path}.");
            importer.GetSourceTextureWidthAndHeight(out int width, out int height);
            if (width != OriginalCellSize * frameCount || height != OriginalCellSize)
                throw new InvalidOperationException($"Expected a single row of {frameCount} 64x64 frames at {path}, found {width}x{height}.");

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = OriginalPixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 1024;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteGenerateFallbackPhysicsShape = false;
            importer.SetTextureSettings(settings);

            var factories = new SpriteDataProviderFactories();
            factories.Init();
            ISpriteEditorDataProvider provider = factories.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();
            SpriteRect[] existing = provider.GetSpriteRects();
            var rects = new SpriteRect[frameCount];
            for (int i = 0; i < frameCount; i++)
            {
                string name = $"{Path.GetFileNameWithoutExtension(path).TrimEnd()}_{i:D2}";
                SpriteRect previous = existing.FirstOrDefault(rect => rect.name == name);
                rects[i] = new SpriteRect
                {
                    name = name,
                    rect = new Rect(i * OriginalCellSize, 0, OriginalCellSize, OriginalCellSize),
                    alignment = SpriteAlignment.Custom,
                    pivot = new Vector2(0.5f, (OriginalCellSize - footBottomFromTop) / (float)OriginalCellSize),
                    spriteID = previous == null ? GUID.Generate() : previous.spriteID
                };
            }
            provider.SetSpriteRects(rects);
            provider.GetDataProvider<ISpriteNameFileIdDataProvider>()
                .SetNameFileIdPairs(rects.Select(rect => new SpriteNameFileIdPair(rect.name, rect.spriteID)));
            provider.Apply();
            importer.SaveAndReimport();
        }

        static Sprite[] LoadFrames(string path, int expectedCount = 6)
        {
            Sprite[] frames = AssetDatabase.LoadAllAssetRepresentationsAtPath(path)
                .OfType<Sprite>()
                .OrderBy(sprite => sprite.name, StringComparer.Ordinal)
                .ToArray();
            if (frames.Length != expectedCount)
                throw new InvalidOperationException($"Expected {expectedCount} sprites at {path}, found {frames.Length}.");
            return frames;
        }
    }
}
