# 鸿毛（HongMao）

《鸿毛》是一款以中国传统色彩与脸谱意象为核心的 2D 动作游戏原型。当前仓库用于验证第一版战斗循环：通过招架和精准闪避积累充能，从场景中吸取红色，获得一次强化攻击；集齐三个红色槽后可发动红脸谱形态。

> 当前版本是可编辑灰盒原型，不代表最终美术、动画、关卡或数值品质。

## 当前已经实现

- 玩家移动、单跳、地面／空中闪避与招架
- 地面三连击、空中攻击和统一受击判定
- 敌人追击、可招架攻击、不可招架攻击、生命与霸体
- 三格充能、慢动作吸色、红色源冷却
- 突进斩、上挑、空中下砸三种强化攻击
- 三个红槽与八秒红脸谱形态
- 胜利、失败和重新开始流程
- 可直接编辑的测试房、角色 Prefab、平衡参数资产和判定框预览
- EditMode 与 PlayMode 自动测试

## 环境要求

- Unity `6000.3.23f1`（Unity 6.3 LTS）
- Universal Render Pipeline 2D
- Unity Input System
- Git（建议同时安装 GitHub Desktop 或 GitHub CLI）

尽量使用完全相同的 Unity 补丁版本，避免场景、Prefab 和包锁文件产生无意义变化。

## 获取与运行

```bash
git clone git@github.com:muzisa18/HongMao.git
```

1. 在 Unity Hub 中选择“添加／从磁盘添加项目”。
2. 选择克隆后的 `HongMao` 文件夹。
3. 使用 Unity `6000.3.23f1` 打开，等待首次导入和脚本编译完成。
4. 打开 `Assets/HongMao/Scenes/PrototypeCombat.unity`。
5. 确认 Console 没有红色错误，然后按顶部 Play 按钮。

## 基本操作

| 操作 | 按键 |
| --- | --- |
| 左右移动 | `A` / `D` |
| 跳跃 | `Space` |
| 闪避 | `Shift` |
| 普通／强化攻击 | 鼠标左键 |
| 招架 | 鼠标右键 |
| 进入／退出吸色 | `Q` |
| 吸色确认 | 吸色状态下鼠标左键 |
| 吸色取消 | 鼠标右键、`Q` 或 `Esc` |
| 强化上挑 | `W` + 鼠标左键 |
| 强化下砸 | 空中 `S` + 鼠标左键 |
| 发动红脸谱 | 三个红槽后按 `R` |
| 胜负后重开 | `Enter` |

更完整的机制和验收路线见 [试玩与操作说明](Docs/PLAYTEST_GUIDE.md)。

## 编辑入口

- 正式灰盒场景：`Assets/HongMao/Scenes/PrototypeCombat.unity`
- 玩家参数：`Assets/HongMao/Configs/PlayerConfig.asset`
- 敌人参数：`Assets/HongMao/Configs/CombatBotConfig.asset`
- 玩家攻击参数：`Assets/HongMao/Resources/Attacks`
- 玩家、敌人与红色源 Prefab：`Assets/HongMao/Prefabs`
- 自有运行时代码：`Assets/HongMao/Runtime`

字段含义、场景层级、Prefab 修改方式和判定框查看方法见 [Unity 编辑与调参说明](Docs/UNITY_EDITING_GUIDE.md)。Li 与 Ban 开工前必须阅读 [双人协作规则](CONTRIBUTING.md)：禁止直推 `main`，改动先认领、再走 PR 和另一人审核。

## 核心规则

1. 成功招架获得 2 格充能；敌人攻击真正穿过闪避无敌帧时获得 1 格充能。
2. 充能达到 3 后按 `Q`，选择红色源完成吸色。
3. 吸色会清空充能、填入一个红槽，并提供一次强化攻击。
4. 集齐三个红槽后按 `R` 进入红脸谱形态。
5. 击败 CombatBot 即胜利；玩家死亡后可按 `Enter` 重开。

## 项目状态与边界

当前版本专注于战斗手感和机制验证。正式角色美术、逐帧动画、水墨 Shader、剧情、探索、存档、装备、Boss、多敌人、手柄支持和独立构建暂不在本阶段范围内。

仓库目前未添加开源许可证。公开可见不代表自动授予复制、再发布或商业使用权限。
