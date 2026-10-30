# 测试房间占位对话

打开 `Assets/HongMao/Scenes/BattleTestRoom.unity` 并 Play，沿右侧台阶逐级向上跳。
引路人在 `East Step 7`（最右侧最高一级台阶），脚底坐标 `(28, 5.65)`。
使用教程主角的白色机器人待机动画；没有碰撞体、生命或战斗 AI，不阻挡路线，也不参与地面胜负。

## 交谈

- 站到同层、水平距离 2.2 单位以内，出现「E 与引路人交谈」。
- 按 E 开始，再次按 E 逐页继续；最后一页按 E 结束。
- Esc 随时退出。再次交谈从第一句开始，可重复复习。
- 交谈暂停世界时间，屏蔽移动、跳跃、攻击、招架、闪避、吸色和脸谱输入。
- 必须先落地，且不在攻击、闪避等动作或吸色中才能开始；胜负结算后不开放交谈。
- 关闭、禁用 NPC/对话组件、玩家死亡和场景重开都会释放对话暂停与输入屏蔽。

九页内容依次介绍招呼、移动与攻击、防御充能、吸色操作、吸色收益、强化攻击、破霸体、红脸谱和完整战斗循环。这些是操作引导占位文案，不是正式剧情。

## 修改台词与角色

在 Project 选择 `Assets/HongMao/Configs/TutorialDialogue.asset`：

- `Speaker Name`：对话显示名。
- `Lines`：按顺序显示的段落，可直接添加、删除或修改。每个元素是一页。
- 长页可以滚动阅读；建议每页只讲一个操作点，保持简短。

右键 Project → Create → HongMao → Dialogue 可以新建对话资产，然后赋给 NPC 的 `Dialogue` 字段。
当前场景的 `Dialogue Systems` 绑定一个 NPC；分支选择、任务和存档尚未实现。

NPC prefab 为 `Assets/HongMao/Prefabs/TutorialGuide.prefab`，只替换 `Visual` 和 `Idle Frames` 即可换美术。
根节点是脚底位置；移动场景里的 `World/Actors/TutorialGuide` 即可调整站位。
`Dialogue Systems` 引用玩家、NPC、时间控制器、流程和相机；场景已配置好，无需手动接线。

菜单 `HongMao > 对话 > 在测试房间添加引路人` 只用于首次安装；已有对话组件时不重建、不覆盖台词或布局。

## 验证

PlayMode 的 `DialogueTests` 覆盖实际键盘 E/Esc、范围与同层限制、完整翻页、输入隔离、恢复移动、
顿帧与对话暂停叠加、禁用清理、重开、吸色互斥和胜利结算。

人工试玩路线：从出生处沿右侧七级台阶登顶 → 靠近机器人 → E 阅读全部内容 → Esc 中途退出并重新打开 → 回地面继续战斗 → Enter 重开。

### 本次验证记录（2026-10-25）

- Unity 6000.3.23f1 实际编译、导入并保存场景；新增资产的 GUID 无重复，引用完整。
- 完整 PlayMode：11/11 通过（4 项新对话场景测试、7 项原有回归）。
- 完整 EditMode：14/14 通过。
- 已在有画面的 PlayMode 中检查机器人站位、E 提示、中文招呼页、分页计数和关闭提示；测试使用运行时定位到高台，不改变保存的玩家出生点。
- 未进行从出生点逐级登顶的完整手动试玩，也未制作平台发布包。
- 结果：`/tmp/hongmao-dialogue-visual-playmode.xml`、`/tmp/hongmao-dialogue-editmode.xml`。
- 可选截图：设置环境变量 `HONGMAO_DIALOGUE_PREVIEW_DIR` 后运行 `DialogueTests.Guide_IsNonCombatantAndOnlyReachableOnUpperStep`；在有画面的 Unity 中生成 `npc-prompt.png` 与 `npc-dialogue.png`。
