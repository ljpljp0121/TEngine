## 为什么

PFGAS 当前已经具备 Unity package 外形，但项目级生成物、Luban 输出、资源加载适配和部分配置资产仍位于 `Assets/PFGAS` 包目录内，导致包边界不清晰，也让运行时代码看起来依赖项目基础设施。

这次变更要把 PFGAS 调整为“核心包 + 项目生成物”的结构，接近参考项目的实践：包内保留系统能力和编辑器工具，项目数据、生成代码和资源加载适配留在使用方项目中。

## 变更内容

- **BREAKING**: 将项目级生成物从 PFGAS 包目录迁移到项目侧目录；PFGAS 适配生成物默认进入 `Assets/PFGASGenerated`，Luban 配置生成代码默认进入 `Assets/GameScripts/HotFix/GameProto`。
- **BREAKING**: 调整编辑器生成器默认输出路径，不再默认写入 `Assets/PFGAS/Runtime/Gen` 或 `Assets/PFGAS/Generated`。
- 将 Luban 生成代码、Luban 运行时胶水、JSON/bytes 数据和 TEngine 资源加载适配视为项目层资产，并优先放入现有 HotFix/GameProto 配置程序集边界。
- 明确程序集依赖方向：`PFGAS.Runtime` 不引用 Luban、TEngine、Excel 读写库或项目生成程序集；项目生成程序集引用 `PFGAS.Runtime`。
- 保留 `PFGAS.Editor` 作为生产工具程序集，允许它依赖 UnityEditor、Excel/Luban 工作流和编辑器 UI。
- 按破坏性重构处理旧生成路径：迁移后的旧包内生成代码必须删除，不保留兼容副本。

## 功能 (Capabilities)

### 新增功能

- `pfgas-generated-externalization`: 定义 PFGAS 包本体与项目侧生成物的边界、默认输出位置、程序集依赖方向和迁移约束。

### 修改功能

## 影响

- `UnityProject/Assets/PFGAS/package.json`
- `UnityProject/Assets/PFGAS/Runtime/com.peifeng.pfgas.Runtime.asmdef`
- `UnityProject/Assets/PFGAS/Generated/com.peifeng.pfgas.Gen.asmdef`
- `UnityProject/Assets/PFGAS/Editor/Scripts/com.peifeng.pfgas.Editor.asmdef`
- `UnityProject/Assets/PFGAS/Editor/Scripts/Tags/PFTagCodeGenerator.cs`
- `UnityProject/Assets/PFGAS/Editor/Scripts/Attribute/PFAttributeCodeGenerator.cs`
- `UnityProject/Assets/PFGAS/Generated/**`
- `UnityProject/Assets/PFGAS/Runtime/Gen/**`
- `UnityProject/Assets/GameScripts/HotFix/GameProto/**`
- `UnityProject/Assets/GameScripts/HotFix/GameProto/GameProto.asmdef`
- Project-side output directories such as `UnityProject/Assets/PFGASGenerated/**`
- Existing runtime tests that assume generated files live under the package directory
