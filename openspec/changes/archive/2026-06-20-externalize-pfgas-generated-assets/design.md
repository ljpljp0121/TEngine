## 上下文

PFGAS 当前目录已经有 package 外形：

```text
UnityProject/Assets/PFGAS/
  package.json
  Runtime/
  Editor/
  Generated/
  Runtime/Gen/
```

其中 `Runtime` 程序集本身没有 asmdef 引用，但 Runtime 源码大量使用 `PFTagId` 和 `PFAttributeId`。这两个类型当前由 `Runtime/Gen/PFTagGenerated.cs` 和 `Runtime/Gen/PFAttributeGenerated.cs` 生成，导致项目数据被编译进 PFGAS 包本体。

`Generated/LubanLib/LubanJsonLoader.cs` 还直接依赖 TEngine 的 `ModuleSystem` 和 `IResourceModule`，这说明 `Generated` 目录已经不是 PFGAS 通用能力，而是当前项目的资源加载适配。

参考项目采用的是“包内核心工具，包外配置工程和生成物”的混合策略。PFGAS 应该采用同类边界，但需要比参考项目更明确地约束程序集依赖方向。

目标结构：

```text
UnityProject/Assets/PFGAS/
  Runtime/                         # 核心运行时
  Editor/                          # 生产工具
  Tests/                           # 可保留包内测试
  package.json

UnityProject/Assets/PFGASGenerated/
  PFGAS/                           # PFGAS 项目适配生成代码
    PFTagGenerated.cs
    PFAttributeGenerated.cs
    PFGASGeneratedData.gen.cs
    PFGASGenerated.asmdef
  Adapters/                        # TEngine/资源加载等项目适配

UnityProject/Assets/GameScripts/HotFix/GameProto/
  GameProto.asmdef                 # 已存在的热更配置程序集
  LubanLib/
  LubanConfig/ 或 cfg/             # Luban C# 输出

Configs/PFGAS/ 或 UnityProject/Assets/PFGASConfig/
  Excel 源表和 Luban 配置工程
```

依赖方向：

```text
PFGAS.Runtime
      ▲
      │
PFGASGenerated ─────▶ GameProto
      ▲
      │
GameScripts

GameProto ─────▶ TEngine / Luban runtime glue

PFGAS.Editor ─────▶ PFGAS.Runtime
      │
      ├── Excel/Luban 工具链
      ├── 写出 Assets/PFGASGenerated/PFGAS
      └── 写出 Assets/GameScripts/HotFix/GameProto
```

## 目标 / 非目标

**目标：**

- 让 `PFGAS.Runtime` 在没有任何项目生成物时仍然可以编译。
- 把 Luban 输出、JSON/bytes、TEngine 加载适配和 PFGAS 项目生成代码迁移到包外目录。
- 调整编辑器生成器默认路径，使 PFGAS 适配生成物进入 `Assets/PFGASGenerated/PFGAS`，Luban 配置生成代码进入 `Assets/GameScripts/HotFix/GameProto`。
- 保留 PFGAS 现有运行时语义，避免把迁移变成 Ability/Attribute/Effect 系统重写。
- 给 `PFTagId` 和 `PFAttributeId` 提供阶段化迁移方式，使 Runtime 保留类型名但不携带项目成员。

**非目标：**

- 不在本变更中重做 Excel 表结构或 Luban schema。
- 不把 PFGAS 做成完全独立于 Unity 的纯 C# 库。
- 不要求 `PFGAS.Editor` 摆脱 Excel/Luban 依赖；编辑器本来就是生产工具。
- 不把 TEngine 声明为 PFGAS 包的硬依赖；TEngine 适配属于项目层或可选 adapter。
- 不保留旧包内生成路径的兼容副本；编译失败的调用点必须随迁移一起修正。

## 决策

### 决策 1: Luban 配置生成代码输出到 `GameProto`

`UnityProject/Assets/GameScripts/HotFix/GameProto` 已经是现有热更配置程序集，包含 `GameProto.asmdef` 和 `LubanLib`。TEngine 的热更设置也已经把 `GameProto.dll` 纳入热更程序集。Luban 生成配置代码应当进入这个程序集，而不是额外放到 `Assets/PFGASGenerated/Luban`。

替代方案：

- 继续输出到 `Assets/PFGAS/Generated`：迁移成本最低，但包边界继续污染。
- 输出到 `Assets/PFGASGenerated/Luban`：边界清晰，但会绕过项目已有的 `GameProto` 热更配置程序集，形成重复的配置归属。
- 输出到 `Assets/Scripts/Gen`：接近参考项目，但会和普通业务生成物混在一起。

选择 `GameProto` 是为了顺着当前项目已经存在的热更/配置编译边界：`GameLogic` 依赖 `GameProto`，`GameProto` 负责配置表类型和 loader 相关代码。

### 决策 2: PFGAS 适配生成物输出到 `Assets/PFGASGenerated/PFGAS`

`Assets/PFGASGenerated/PFGAS` 只承载 PFGAS 专属适配代码，例如 Tag/Attribute 常量、Runtime registry 注册和从 `GameProto`/Luban 表到 PFGAS Runtime 类型的装配入口。它不再承载通用 Luban 表代码。

这样可以避免 `GameProto` 反向依赖 PFGAS。推荐依赖方向是：

```text
GameProto          # 通用配置生成代码，不依赖 PFGAS
PFGASGenerated     # 依赖 GameProto + PFGAS.Runtime，做 PFGAS 专用映射
GameLogic          # 依赖 GameProto + PFGASGenerated + PFGAS.Runtime
```

### 决策 3: Runtime 拥有 ID 类型，Generated 拥有项目具体值和注册

当前 Runtime API 已经广泛使用 `PFTagId` 和 `PFAttributeId`。直接把这两个 enum 文件搬到项目生成程序集会导致 Runtime 无法编译。

迁移策略是：

- `PFGAS.Runtime` 保留 `PFTagId` 和 `PFAttributeId` 类型定义。
- Runtime 类型定义只表达 ID 类型本身，不包含项目具体枚举成员。
- `PFGASGenerated` 生成命名常量、注册表和装配入口。
- Runtime 不再主动调用 `PFTagGenerated` 或 `PFAttributeGenerated`，而是通过 registry/provider 接收项目生成层注册。

可选实现形态：

```text
PFGAS.Runtime:
  enum PFTagId : int {}
  enum PFAttributeId : int {}

PFGASGenerated:
  static class PFGASTagIds
    const PFTagId State = (PFTagId)1
    const PFTagId State_Buff = (PFTagId)2

  static class PFGASGeneratedData
    RegisterTags()
    RegisterAttributeRules()
```

替代方案：

- 把所有 Runtime API 改成 `int`：最简单但丢失类型语义。
- 把 ID 改成 readonly struct：更干净，但会触及更多序列化、比较和调用点。
- 继续让 Runtime 引用 generated assembly：短期省事，但违反包边界。

保留类型名、移出项目成员，是这次迁移降低 API 爆炸范围的方式；旧生成文件本身不保留。

### 决策 4: Editor 可以依赖生产工具链，但只能写项目输出

`PFGAS.Editor` 仍然是编辑器生产工具，可以读写配置资产、运行 Luban、生成 C# 和刷新 AssetDatabase。

约束是：

- 默认输出路径禁止指向 `Assets/PFGAS`。
- 编辑器配置资产实例应当位于项目侧，例如 `Assets/PFGASConfig` 或 `Assets/PFGASGenerated/Settings`。
- Editor 代码可以知道生成目录，但 Runtime 不可以。
- 已迁出的包内生成代码必须删除，避免同一 Luban/runtime 类型在多个程序集里重复编译。

### 决策 5: TEngine 资源加载适配放在 GameProto 或项目 adapter 层

`LubanJsonLoader` 依赖 `TEngine`，因此不应位于 PFGAS 包本体。当前项目已经在 `GameApp` 中调用 `Tables.SetJsonLoader(new LubanJsonLoader())`，所以 loader 更自然地归入 `GameProto` 或与 `GameProto` 同层的项目 adapter。

```text
PFGASGeneratedData.LoadTables(Func<string, JSONNode> loader)
PFGASGeneratedData.RegisterToRuntime()
```

TEngine 项目可以在 `GameProto` 边界提供：

```text
TEnginePFGASLoader -> LoadAsset<TextAsset> -> JSONNode
```

非 TEngine 项目可以提供 Resources/Addressables/自定义加载器。

## 风险 / 权衡

- `PFTagId.X` 和 `PFAttributeId.X` 调用点会破坏 → 通过生成 `PFGASTagIds.X` / `PFGASAttributeIds.X` 并更新测试和示例缓解。
- Unity 序列化可能依赖 enum 成员名 → 迁移前需要检查已有场景、Prefab、ScriptableObject 是否序列化这些 enum。
- 自动注册行为变化 → Runtime 应在未注册数据时给出清晰错误，而不是静默空表。
- 生成目录移出包后 asmdef 引用需要更新 → 生成器应创建或维护 `PFGASGenerated.asmdef`，引用 `com.peifeng.pfgas.Runtime` 和 `GameProto`。
- 参考项目式弱约束容易再次污染包目录 → 生成器和文档都必须明确禁止默认输出到 `Assets/PFGAS`。

## 迁移计划

1. 建立 PFGAS 适配生成根目录 `Assets/PFGASGenerated/PFGAS`。
2. 把 `Assets/PFGAS/Generated/**` 中的 Luban 配置生成代码迁移到 `Assets/GameScripts/HotFix/GameProto`。
3. 把 TEngine loader 迁移到 `GameProto` 或项目 adapter 边界。
4. 在 Runtime 中拆出稳定 ID 类型，让 `PFTagId` 和 `PFAttributeId` 不再由项目生成文件定义。
5. 调整 Tag/Attribute 生成器输出：生成命名常量、注册表和装配入口到 `Assets/PFGASGenerated/PFGAS`。
6. 移除 Runtime 对 `PFTagGenerated` 静态构造的硬调用，改为显式注册或 provider 初始化。
7. 更新测试和示例调用点，从 `PFTagId.X` / `PFAttributeId.X` 迁移到项目生成常量。
8. 验证删除 `Assets/PFGASGenerated` 后，`PFGAS.Runtime` 和 `PFGAS.Editor` 仍能编译；重新导出后项目运行测试通过。

回滚策略：

- 回滚应通过版本控制恢复旧文件；工作树中不保留旧生成路径作为兼容副本。
- 如果 ID 类型拆分导致范围过大，也只能拆小提交推进，不保留已经迁出的包内副本。

## 待定问题

- `PFTagId` / `PFAttributeId` 最终是否保持空 enum，还是在后续变更升级为 readonly struct？
- 项目侧配置资产默认放在 `Assets/PFGASConfig` 还是 `Assets/PFGASGenerated/Settings`？
- TEngine loader 最终放入 `GameProto`，还是放到 `GameLogic`/项目 adapter 程序集？
- 现有 Demo/测试是否应该作为 package samples 留在 PFGAS 内，还是移动到项目侧示例目录？
