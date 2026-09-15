# 《鸿毛》Li / Ban 双人协作规则

参与者：

- **Li**：仓库所有者与当前工程维护者。
- **Ban**：协作者；收到 GitHub 用户名后加入仓库。

两人的改动都必须走相同流程，不因为谁是仓库所有者而跳过审核。

## 四条强制规则

### 1. `main` 必须经过 PR

- 禁止直接向 `main` 推送功能、修复或资源改动。
- 每次合并必须创建 Pull Request。
- PR 必须由另一人完成至少 1 次 Approve；作者不能审核自己的 PR。
- 未解决的 Review Conversation 不得合并。
- 禁止对 `main` 使用 force push，也不删除 `main`。

当 Li 提交 PR 时由 Ban 审核；当 Ban 提交 PR 时由 Li 审核。紧急修复也先开短分支和 PR，不设置口头例外。

### 2. 开工前认领目录或资产

Unity 的 `.unity`、`.prefab` 和 `.asset` 都可能包含大量序列化内容。冲突最严重的不是代码目录，而是两个人同时保存同一场景、Prefab 或配置资产。

开始工作前，在 GitHub Issue 或两人的固定沟通渠道中发送：

```text
[认领] Li/Ban
任务：要完成的内容
文件：预计会修改的目录或具体资产
结束：预计解除认领的时间
```

高冲突区域按下表执行：

| 区域 | 协作规则 |
| --- | --- |
| `Assets/HongMao/Scenes` | 同一时间只能一人编辑同一个场景；另一人不要打开后保存该场景 |
| `Assets/HongMao/Prefabs` | 按具体 Prefab 认领；不要两人同时改同一个 Prefab |
| `Assets/HongMao/Configs` | 按具体 Config 资产认领；数值改动在 PR 中写明原值、新值和理由 |
| `Assets/HongMao/Resources/Attacks` | 按具体攻击资产认领，可由两人分别修改不同攻击 |
| `Assets/HongMao/Runtime` | 按功能或脚本认领；接口改动先通知另一人 |
| `Assets/HongMao/Art`、`Audio` | 按资源文件认领；导入资源时同时提交对应 `.meta` |
| `ProjectSettings`、`Packages` | 默认由 Li 维护；Unity 或包版本升级必须单独 PR 并由 Ban 验证 |

任务结束、PR 已创建或工作暂停时，要明确解除认领。认领是避免冲突，不代表永久划分所有权。

### 3. Commit 信息统一

格式：`类型: 简短说明`。一条提交只处理一个清晰目的。

| 前缀 | 用途 | 示例 |
| --- | --- | --- |
| `feat:` | 新功能 | `feat: add player air attack` |
| `fix:` | Bug 修复 | `fix: restore time scale after restart` |
| `docs:` | 文档 | `docs: explain enemy balance fields` |
| `refactor:` | 不改变行为的代码整理 | `refactor: split combat state helpers` |
| `test:` | 测试 | `test: cover authored combat scene` |
| `chore:` | 工程、包或工具维护 | `chore: update project baseline` |
| `art:` | 美术资源 | `art: add player idle placeholder` |
| `balance:` | 纯数值调整 | `balance: widen parry window` |

不要使用“改一下”“最新版”“123”“final final”这类无法说明内容的提交信息。

### 4. 临时文件不得上传

- 必须保留仓库根目录的 `.gitignore`。
- 不提交 `Library`、`Temp`、`Logs`、`Obj`、`Build`、`Builds`、`UserSettings`、`.DS_Store`、IDE 缓存、录屏和临时导出文件。
- 不提交 `.env`、令牌、密码、私钥或个人账号信息。
- Unity 资产必须和对应 `.meta` 一起提交；不要删除不认识的 `.meta` 后让 Unity 重建 GUID。
- 仓库暂未启用 Git LFS。较大的图片、音频、视频或模型导入前，两人先确认资源格式和存放位置。

## 标准工作流

分支名使用小写英文，前面带负责人和类型：

```bash
git switch main
git pull --ff-only
git switch -c li/feat-player-animation
# 或：git switch -c ban/fix-enemy-hitbox
```

完成后：

1. 在 Unity 中保存场景和 Prefab，退出 Play Mode。
2. 检查 Console 和玩法。
3. 查看 `git status` 与 diff，确认没有误带其他人的文件。
4. Commit 并推送自己的分支。
5. 创建 PR，填写仓库提供的模板。
6. 另一人 Review；有修改意见时继续向同一分支提交。
7. 审核通过、检查完成后再合并，合并后删除功能分支。

## PR 审核重点

审核者不只看“能不能编译”，还要检查：

- 改动是否符合本次任务，没有顺手加入额外范围。
- 场景、Prefab 和 ScriptableObject 是否出现意外 Override 或 Missing Reference。
- 玩家、敌人和 `Combat Systems` 是否仍只有一套。
- 数值变化是否注明原因，并能通过实际试玩感知。
- Console 没有新增 Error；Warning 若保留，PR 中必须解释。
- 视觉变化附截图或短视频，行为变化写明复现和测试步骤。

## 提交前最低检查

- 使用 Unity `6000.3.23f1`。
- `PrototypeCombat` 能正常进入和退出 Play Mode。
- 玩家能移动、跳跃、攻击、闪避和招架。
- 充能、吸色、强化攻击和红脸谱仍能形成闭环。
- 胜利、失败与 `Enter` 重开正常。
- `git status` 中没有缓存、个人设置或无关资源。

## 发生场景冲突时

`.unity` 和 `.prefab` 虽然是文本文件，但文件 ID 与对象引用很容易在手工合并时损坏。不要直接选择“全部接受当前”或“全部接受传入”。先让当前认领者完成并提交，另一人更新分支后在 Unity 中重新应用自己的少量修改。合并后必须逐项检查 Hierarchy、Prefab Overrides、Inspector 引用和 Play Mode。

## Issue 内容要求

报告问题时写明：

- 负责人：Li 或 Ban。
- Unity 与系统版本。
- 稳定复现步骤。
- 预期结果和实际结果。
- Console 完整错误及截图。
- 涉及的 Scene、Prefab、Config 或 AttackDefinition。
