using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace HongMao.Editor
{
    public static class PlayerSpritePrototypeAuthoring
    {
        const string GroundOneSheetPath = "Assets/HongMao/Art/Characters/Player/PlayerGroundOneSheet.png";
        const string GroundTwoSheetPath = "Assets/HongMao/Art/Characters/Player/PlayerGroundTwoSheet.png";
        const string GroundThreeSheetPath = "Assets/HongMao/Art/Characters/Player/PlayerGroundThreeSheet.png";
        const string RunSheetPath = "Assets/HongMao/Art/Characters/Player/PlayerRunSheet.png";
        const string PlayerPrefabPath = "Assets/HongMao/Prefabs/PlayerPrototype.prefab";
        const string SpriteObjectName = "Sprite Animation";

        [MenuItem("HongMao/美术/应用玩家角色精灵")]
        public static void Apply()
        {
            Sprite[] groundOneFrames = LoadFrames(GroundOneSheetPath);
            Sprite[] groundTwoFrames = LoadFrames(GroundTwoSheetPath);
            Sprite[] groundThreeFrames = LoadFrames(GroundThreeSheetPath);
            Sprite[] runFrames = LoadFrames(RunSheetPath);

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
                animatedRenderer.sprite = groundOneFrames[0];
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
                visual.ConfigureSpriteAnimation(animatedRenderer, groundOneFrames, groundTwoFrames, groundThreeFrames,
                    runFrames);

                PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
                Debug.Log("Applied the player normal attacks and running sprite animations to PlayerPrototype.prefab.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        static Sprite[] LoadFrames(string path)
        {
            Sprite[] frames = AssetDatabase.LoadAllAssetRepresentationsAtPath(path)
                .OfType<Sprite>()
                .OrderBy(sprite => sprite.name, StringComparer.Ordinal)
                .ToArray();
            if (frames.Length != 6)
                throw new InvalidOperationException($"Expected 6 sprites at {path}, found {frames.Length}.");
            return frames;
        }
    }
}
