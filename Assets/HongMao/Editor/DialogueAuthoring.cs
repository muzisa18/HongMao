using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HongMao.Editor
{
    public static class DialogueAuthoring
    {
        const string DialoguePath = "Assets/HongMao/Configs/TutorialDialogue.asset";
        const string PrefabPath = "Assets/HongMao/Prefabs/TutorialGuide.prefab";
        const string SpritePath = "Assets/HongMao/Art/Characters/TutorialGuide/GuideIdle.png";

        [MenuItem("HongMao/对话/在测试房间添加引路人")]
        public static void Install()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(CombatRoomAuthoring.ScenePath);
            if (UnityEngine.Object.FindFirstObjectByType<DialogueController>() != null)
            {
                Debug.Log("Dialogue already installed. Edit the existing NPC and dialogue asset directly.");
                return;
            }
            var definition = AssetDatabase.LoadAssetAtPath<DialogueDefinition>(DialoguePath);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<DialogueDefinition>();
                definition.speakerName = "引路人";
                definition.lines = new[]
                {
                    "你好，欢迎来到鸿毛的练习场！我是这里的引路人。先在高台上歇一歇，我来陪你熟悉战斗。按 E 继续，随时按 Esc 离开；想复习时再来找我。",
                    "先活动一下：A / D 左右移动，Space 跳跃。左键攻击，地面可连续打出三段，跳起后也能攻击。沿两侧台阶逐级跳跃，就能到达高处。",
                    "敌人出招时，右键招架。成功招架获得 2 格充能。看到橙色攻击预警要用 Shift 闪避；只有在无敌时间内真正躲过命中，才会获得 1 格充能，原地空闪不算。",
                    "充能上限是 3 格。满充能且还有空红槽时，按 Q 进入慢动作吸色：鼠标指向红色柱体，左键确认。右键、Q 或 Esc 可取消，取消不消耗资源。",
                    "成功吸色会清空充能、填入 1 个红槽，并获得一次强化攻击。被吸取的红色源会变灰，3 秒后恢复；可以再次利用它练习。",
                    "有强化攻击时：不按上下方向，左键使出突进斩；W + 左键上挑；空中 S + 左键下砸。地面 S + 左键仍是普通攻击，会保留强化机会。强化攻击不消耗已填入的红槽。",
                    "敌人有生命和霸体两条状态。攻击削减霸体；破霸体后，强化上挑就能把敌人挑空，再接空中攻击。可以从下方的训练对手开始练习。",
                    "攒满 3 个红槽后，按 R 激活红脸谱，持续 8 秒，攻击更快、破霸体能力更强。开启会清空红槽和剩余强化机会；期间不能再积累充能或吸色。",
                    "记住这个循环：招架或闪避 → 满充能吸色 → 使用强化攻击 → 三红槽开启脸谱。胜利或失败后按 Enter 重开。准备好了就回地面试试吧！之后想复习，随时来找我。"
                };
                AssetDatabase.CreateAsset(definition, DialoguePath);
            }
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                Sprite[] frames = AssetDatabase.LoadAllAssetsAtPath(SpritePath).OfType<Sprite>()
                    .OrderBy(sprite => int.Parse(sprite.name.Substring(sprite.name.LastIndexOf('_') + 1))).ToArray();
                if (frames.Length != 38) throw new InvalidOperationException($"Expected 38 tutorial idle frames, found {frames.Length}.");
                var root = new GameObject("Tutorial Guide");
                try
                {
                    var visual = new GameObject("Visual");
                    visual.transform.SetParent(root.transform, false);
                    // Original cell: 128 x 126. Visible feet are ~23px above the bottom.
                    visual.transform.localPosition = new Vector3(0, 1.12f, 0);
                    visual.transform.localScale = Vector3.one * 2.8f;
                    var renderer = visual.AddComponent<SpriteRenderer>();
                    renderer.sprite = frames[0];
                    renderer.sortingOrder = 5;
                    root.AddComponent<DialogueNpc>().Configure(definition, renderer, frames);
                    prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                }
                finally { UnityEngine.Object.DestroyImmediate(root); }
            }
            var npcObject = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            npcObject.transform.SetParent(GameObject.Find("World/Actors").transform, false);
            npcObject.transform.position = new Vector3(28f, 5.65f, 0f);
            var systems = new GameObject("Dialogue Systems");
            var controller = systems.AddComponent<DialogueController>();
            controller.Configure(npcObject.GetComponent<DialogueNpc>(),
                UnityEngine.Object.FindFirstObjectByType<PlayerCombatController>(),
                UnityEngine.Object.FindFirstObjectByType<CombatTimeController>(),
                UnityEngine.Object.FindFirstObjectByType<GameFlowCoordinator>());
            systems.AddComponent<DialogueView>().Configure(controller, Camera.main);
            UnityEngine.Object.FindFirstObjectByType<PrototypeHUD>().ConfigureDialogue(controller);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Installed tutorial guide on East Step 7 with editable dialogue.");
        }
    }
}
