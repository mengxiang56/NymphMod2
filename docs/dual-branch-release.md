# 正式版与测试版双分支发布

项目会分别编译正式版和测试版 DLL，不能把同一个 DLL 同时用于两个游戏分支。

## 一次性配置

1. 保留现有 `local.props` 作为测试版配置，或复制 `local.public-beta.props.template` 为 `local.public-beta.props`。
2. 准备一份正式版 `v0.107.1` 游戏程序集，复制 `local.public.props.template` 为 `local.public.props`，并填写对应路径。
3. 把工坊条目的 `mod_id.txt` 放到 `workshop/mod_id.txt`。正式版和测试版必须使用同一个文件，才能更新同一个工坊条目。
4. 如需上传预览图，把它放到 `workshop/image.png`。

## 本地构建

```powershell
.\build-public-beta.cmd
.\build-public.cmd
```

测试版使用 `STS2.RitsuLib 0.5.11`，正式版使用 `STS2.RitsuLib.Compat.0.107.1 0.5.11`。两种产物的运行时依赖仍然都是 `STS2-RitsuLib 0.5.11`。

## 准备工坊上传目录

```powershell
.\prepare-workshop.cmd -Branch public-beta
.\prepare-workshop.cmd -Branch public
```

完成后会生成：

- `artifacts/workshop/public`
- `artifacts/workshop/public-beta`

每个目录都符合官方上传器工作区结构，包含 `content/` 和对应的 `workshop.json`。正式版配置为 `minBranch=maxBranch=public`，测试版配置为 `minBranch=maxBranch=public-beta`。

分别在这两个目录运行官方上传器。Steam 会把它们保存为同一工坊条目的两个兼容版本，并根据玩家所处的游戏分支分发对应版本。

## 发布前验证

1. 在正式版进入一场战斗，检查角色选择、能力、遗物、灵感牌堆和主要卡牌流程。
2. 在测试版重复同一套检查。
3. Harmony 补丁和读取私有字段的代码风险最高；任一分支更新后都应重新测试这些界面和流程。
4. 先以工坊私密可见性上传验证，确认两个分支都下载到正确 DLL 后再公开。
