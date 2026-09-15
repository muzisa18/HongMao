# 《鸿毛》协作开发说明

## 开始前

1. 安装 Unity `6000.3.23f1`。
2. 克隆仓库并从 `main` 拉取最新内容。
3. 在自己的功能分支上开发，例如 `feature/player-animation`、`fix/parry-window` 或 `docs/update-guide`。
4. 首次打开后等待 Unity 完成导入，确认 Console 没有红色错误。

## 建议工作流

```bash
git switch main
git pull --ff-only
git switch -c feature/简短功能名
```

开发完成并验证后提交，再在 GitHub 创建 Pull Request。不要直接在 `main` 上长期开发，也不要用强制推送覆盖他人的提交。

## Unity 项目协作规则

- 永远提交 `.meta` 文件；不要只复制 Unity 资产本体。
- 不要提交 `Library`、`Temp`、`Logs`、`Obj`、`Build`、`UserSettings` 等本机生成目录。
- 两个人不要同时编辑同一个 `.unity` 场景或 `.prefab`。开始前先在群里说明自己要改的资产，减少 YAML 合并冲突。
- 一个提交尽量只解决一个问题，提交说明写清“改了什么”和“为什么”。
- 调整数值时记录原值、新值和试玩结论，避免只写“优化手感”。
- 正式图片、音频或大文件导入前先沟通；仓库当前未启用 Git LFS。
- 不要随意升级 Unity 或 Package 版本。需要升级时单独开分支，并让另一位成员验证工程能否打开。

## 提交前检查

- 场景与 Prefab 已保存，没有无意产生的 Overrides。
- Console 没有新的 Error 或 Warning。
- `PrototypeCombat` 可以正常进入和退出 Play Mode。
- 基本战斗循环仍可完成。
- `git status` 中没有个人设置、缓存、测试录屏或无关大文件。
- Pull Request 描述包含测试方法；视觉变化附截图或短视频。

## 场景冲突处理

`.unity` 和 `.prefab` 虽然是文本文件，但手工合并容易破坏文件 ID 与引用。发生冲突时不要盲目选择“全部接受当前／传入”。优先让负责该场景的人先提交，另一方更新分支后重新应用少量修改；确实需要合并时，应在 Unity 中逐项验证引用和 Prefab 状态。

## 反馈问题

请在 GitHub Issues 中写明：

- Unity 版本和系统版本。
- 可稳定复现的操作步骤。
- 预期结果与实际结果。
- Console 完整错误和截图。
- 如果与数值有关，注明改动过的 Config 或 AttackDefinition。
