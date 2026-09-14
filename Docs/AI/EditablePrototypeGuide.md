# 鸿毛可编辑战斗测试房

停止 Play Mode 后，`PrototypeCombat` 场景中的玩家、敌人、地图和红色源都会保留，可以直接在 Scene 与 Inspector 中编辑。

## 常用编辑入口

- 拖动场景物体：展开 `World`。`Geometry` 是有碰撞的地形，`Decoration` 只负责背景，`Color Sources` 是吸色物体，`Actors` 是玩家和敌人。
- 调整玩家：打开 `Assets/HongMao/Configs/PlayerConfig.asset`，修改生命、移动、跳跃、重力、闪避与招架参数。
- 调整敌人：打开 `Assets/HongMao/Configs/CombatBotConfig.asset`，修改生命、霸体、追击距离、攻击预警、伤害与硬直。
- 调整攻击：打开 `Assets/HongMao/Resources/Attacks`，每个资产对应一种玩家攻击。
- 修改所有玩家或敌人的占位外观：打开 `Assets/HongMao/Prefabs` 中对应的 Prefab，编辑其 `Visual` 子对象。
- 观察判定框：选中 `Combat Systems`，在 `Combat Debug Overlay` 中选择要预览的攻击资产。青色为玩家身体与地面检测，粉色为敌人身体，橙色为敌人攻击范围，黄色为玩家攻击范围。

## 保存规则

- 停止 Play Mode 后再移动物体或调整参数，然后保存场景。
- Play Mode 中的临时位置、生命和状态不会保存，这是 Unity 的正常行为。
- 场景里的 Prefab 实例可以保留单独的位置和缩放；希望所有实例一起变化时，应打开 Prefab 本体编辑。
- `HongMao > 重建可编辑战斗测试场景` 会重新生成模板布局。已经手工修改地图后不要随意执行这个菜单。
