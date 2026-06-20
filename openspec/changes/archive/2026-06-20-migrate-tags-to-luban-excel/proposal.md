## 为什么

PFGAS Tag 当前同时存在 `PFTagConfig.asset`、`PFTag.xlsx`、Luban 生成表类和包内 `PFTagGenerated.cs`，配置权威源已经分裂。现在需要先把 Tag 迁移到 Excel/Luban 单一数据源，让树编辑器成为 Excel 的可视化编辑层，并让运行时使用由 Luban 数据派生的生成代码。

## 变更内容

- **BREAKING**: 移除 `PFTagConfig.asset` 作为 Tag 权威源，旧 ScriptableObject Tag 编辑和旧 ScriptableObject 驱动生成链路不再保留兼容副本。
- **BREAKING**: 调整 Tag 生成链路，`PFTagGenerated.cs` 不再从 `PFTagConfig.asset` 生成，也不再作为包内手工维护的生成物。
- 新增 Excel-backed Tag 树编辑器：打开时读取 Luban Excel 表，编辑后写回同一份 Excel。
- 新增 Tag Excel 校验：检查 ID 唯一、父子关系有效、同父级短名唯一、循环引用和生成名冲突。
- 新增基于 Luban Tag 表的 PFGAS 适配生成层：从 Luban 生成表类或数据中生成/注册 Runtime 可消费的 Tag ID、层级、显示名和查询数据。
- 收束导表入口和生成输出路径，保证保存 Excel 后可以明确运行 Luban 导出和 PFGAS Tag 适配生成。
- 删除或隐藏旧 Tag ScriptableObject 编辑入口，避免一边改 ScriptableObject、一边改 Excel。

## 功能 (Capabilities)

### 新增功能

- `pfgas-luban-tag-authoring`: 定义 Tag 以 Luban Excel 为唯一数据源的编辑、校验、导出和运行时适配生成行为。

### 修改功能

## 影响

- `Configs/GameConfig/Datas/PFTag.xlsx`
- `Configs/GameConfig/Defines/tag.xml`
- `Configs/GameConfig/luban.conf`
- `Configs/GameConfig/gen_code_*_to_project.*`
- `UnityProject/Assets/PFGAS/Editor/Scripts/Tags/**`
- `UnityProject/Assets/PFGAS/Runtime/Gen/PFTagGenerated.cs`
- `UnityProject/Assets/PFGAS/Runtime/GAS/Tags/**`
- `UnityProject/Assets/GameScripts/HotFix/GameProto/LubanConfig/PFGAS/**`
- `UnityProject/Assets/AssetRaw/Configs/json/pfgas_tbpftag.json`
- PFGAS runtime tests and samples that reference concrete `PFTagId` members.
