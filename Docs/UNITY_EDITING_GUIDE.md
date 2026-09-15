# 《鸿毛》Unity 编辑与调参说明

本文面向参与灰盒开发的成员。修改前请确认已经停止 Play Mode；Play Mode 中改出的对象位置和运行状态通常不会保存。

## 场景结构

打开 `Assets/HongMao/Scenes/PrototypeCombat.unity` 后，Hierarchy 的主要结构为：

```text
PrototypeCombat
├── Main Camera
├── World
│   ├── Geometry       # 有 Collider2D 的地板、墙和平台
│   ├── Decoration     # 无碰撞的背景与墨迹占位图
│   ├── Color Sources  # 可吸取的红色源
│   └── Actors         # Player 与 CombatBot Prefab 实例
└── Combat Systems     # 安装器、时间、HUD、反馈和调试显示
```

移动地图时优先编辑 `World/Geometry` 下的对象。检查平台是否包含 `BoxCollider2D`，背景装饰则不要添加碰撞，避免出现不可见墙体。

## 调整玩家基础属性

在 Project 窗口中选择 `Assets/HongMao/Configs/PlayerConfig.asset`，从 Inspector 修改：

| 字段 | 作用 | 调整建议 |
| --- | --- | --- |
| Max Health | 玩家最大生命 | 用于调整容错率；先不要用极端数值掩盖受击问题 |
| Max Poise | 玩家霸体上限 | 当前玩家默认不使用霸体，可保持 0 |
| Move Speed | 水平移动速度 | 过高会让近身距离和闪避判断失真 |
| Jump Speed | 起跳速度 | 与 Gravity Scale 一起决定跳跃高度和滞空时间 |
| Gravity Scale | 重力倍率 | 越高下落越快；修改后需要重新检查平台和下砸手感 |
| Coyote Time | 离开平台后仍可跳跃的宽限时间 | 适当增加可以降低边缘跳跃挫败感 |
| Jump Buffer | 落地前提前按跳的输入缓存 | 适当增加会让连续移动更顺手 |
| Dodge Speed | 闪避水平速度 | 决定闪避位移长度 |
| Dodge Duration | 闪避总时长 | 与 Dodge Speed 共同决定位移距离 |
| Dodge Invulnerability | 闪避无敌时间 | 不应超过 Dodge Duration |
| Dodge Cooldown | 两次闪避之间的冷却 | 越短越容易连续规避 |
| Parry Window | 招架有效窗口 | 越长越宽松；需结合敌人预警时间一起测试 |
| Parry Recovery | 招架结束后的恢复时间 | 决定失败招架的风险 |

修改 ScriptableObject 资产后会直接影响下一次 Play。无需把该资产拖回 Player，因为场景中的 `PrototypeInstaller` 已经引用它。

## 调整敌人基础属性

选择 `Assets/HongMao/Configs/CombatBotConfig.asset`：

| 字段 | 作用 | 调整建议 |
| --- | --- | --- |
| Max Health | 敌人最大生命 | 与玩家一整套循环的总伤害一起衡量 |
| Max Poise | 敌人霸体上限 | 决定需要多少次攻击才能进入可挑空状态 |
| Gravity Scale | 敌人重力倍率 | 影响击飞后的落地速度 |
| Move Speed | 敌人靠近玩家的速度 | 过快会压缩观察攻击提示的时间 |
| Attack Range | 敌人开始攻击的水平距离 | 修改后配合橙色 Gizmo 检查范围 |
| Opening Grace | 开局首次行动前的等待时间 | 给第一次试玩者阅读提示和站位的时间 |
| Parryable Telegraph | 可招架攻击的预警时间 | 越长越容易反应 |
| Unblockable Telegraph | 不可招架攻击的预警时间 | 应足够让玩家识别橙色提示并闪避 |
| Attack Recovery | 每次敌人攻击后的恢复时间 | 决定玩家反击窗口 |
| Stagger Duration | 破霸体后的硬直时间 | 应足够完成上挑和一次空中追击 |
| Parryable Damage | 可招架攻击伤害 | 用于控制招架失败代价 |
| Unblockable Damage | 不可招架攻击伤害 | 通常应高于或不低于普通攻击 |
| Knockback | 命中玩家后的击退 | X 控制水平、Y 控制垂直方向 |
| Hitbox Size | 敌人攻击判定框尺寸 | 必须结合角色视觉和武器长度检查 |
| Hitbox Offset | 判定框相对敌人中心的偏移 | X 会根据敌人朝向自动翻转 |

## 调整玩家攻击

`Assets/HongMao/Resources/Attacks` 中每个 `.asset` 对应一种攻击：

- `GroundOne`、`GroundTwo`、`GroundThree`：地面三连击。
- `Air`：普通空中攻击。
- `DashSlash`：强化突进斩。
- `Uppercut`：强化上挑。
- `DiveSlam`：强化空中下砸。
- `MaskGroundOne`、`MaskGroundTwo`、`MaskGroundThree`：红脸谱地面三连。
- `MaskAir`：红脸谱空中攻击。

通用字段含义：

| 字段 | 作用 |
| --- | --- |
| Kind | 攻击类型；通常不要随意改成与文件名不一致的类型 |
| Startup | 前摇时间，判定出现前玩家需要等待多久 |
| Active | 攻击判定保持有效的时间 |
| Recovery | 判定结束后的后摇时间 |
| Health Damage | 对生命造成的伤害 |
| Poise Damage | 对霸体造成的伤害 |
| Knockback | 命中后的水平／垂直击退 |
| Hitbox Size | 攻击判定框宽高 |
| Hitbox Offset | 判定框相对玩家的位置，X 会随朝向翻转 |
| Movement Impulse | 招式发动时附加的水平位移速度 |
| Launches | 是否尝试将破霸体敌人挑空 |

一次只改一类变量并试玩。例如先调时间，再调伤害，最后调判定框；否则很难判断手感变化来自哪里。

## 查看碰撞与攻击范围

1. 在 Hierarchy 中选择 `Combat Systems`。
2. 找到 `Combat Debug Overlay` 组件。
3. 根据需要开关 Body Colliders、Ground Probe、Enemy Range 和 Preview Attack。
4. 将某个 AttackDefinition 拖到 Preview Attack，即可查看该攻击的黄色范围框。

颜色含义：青色为玩家身体和地面检测，粉色为敌人身体，橙色为敌人攻击范围，黄色为当前预览的玩家攻击范围。需要在 Scene 视图右上方开启 Gizmos。

## 修改地图和角色占位模型

- 地图：停止 Play 后，移动或缩放 `World/Geometry` 中的平台并保存场景。缩放后检查 Collider2D 是否仍与视觉一致。
- 单个场景实例：直接修改 `World/Actors` 下的 Player 或 CombatBot；这会形成 Prefab Override。
- 所有同类实例：打开 `Assets/HongMao/Prefabs` 中对应 Prefab，编辑 `Visual` 子对象。
- 更换正式 Sprite：保留根对象上的 Rigidbody2D、Collider2D 和战斗脚本，优先只替换 `Visual` 下的渲染对象。
- 红色源：修改 `RedColorSource.prefab` 会影响所有实例；场景中可以单独调整位置和缩放。

不要删除 Actor 根对象上已存在的组件，也不要随意断开 `Combat Systems/PrototypeInstaller` 的引用，否则运行时可能回退到旧的临时生成逻辑。

## 保存与 Prefab Override

- 场景布局修改后按 `Command+S` 保存。
- 修改 Prefab Mode 中的内容后保存 Prefab。
- 对场景 Prefab 实例的改动会显示在 Inspector 的 Overrides 中：需要全局应用时选择 Apply，只想保留该实例差异时不要 Apply。
- 不要在 Play Mode 中做正式调参；退出 Play 后运行态修改会被 Unity 恢复。

## 重建场景模板

菜单 `HongMao > 重建可编辑战斗测试场景` 会用代码模板覆盖当前 `PrototypeCombat` 布局。它只适合场景损坏后重建基线，不是日常刷新按钮。已经手工调整地图后，执行前必须先提交或备份当前场景。

## 修改后的最低检查

1. Console 没有编译错误或 Missing Reference。
2. 开始 Play 后只有一个 Player 和一个 CombatBot。
3. 玩家能移动、跳跃、攻击、闪避和招架。
4. 充能、吸色、强化攻击与红脸谱可以形成闭环。
5. 胜利、失败与 `Enter` 重开正常。
6. 退出 Play 后 `Time.timeScale` 恢复，场景对象仍可编辑。
