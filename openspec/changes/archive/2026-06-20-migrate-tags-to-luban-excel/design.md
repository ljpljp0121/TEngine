## 上下文

当前 Tag 相关数据和生成链路处在分裂状态：

- `UnityProject/Assets/PFGAS/Editor/Scripts/Tags/PFTagConfig.asset` 保存旧 ScriptableObject 树。
- `Configs/GameConfig/Datas/PFTag.xlsx` 已经保存 Luban Tag 表。
- `UnityProject/Assets/GameScripts/HotFix/GameProto/LubanConfig/PFGAS/PFTag.cs` 和 `TbPFTag.cs` 已经是 Luban 生成表类。
- `UnityProject/Assets/PFGAS/Runtime/Gen/PFTagGenerated.cs` 仍由旧 ScriptableObject 生成器产生，并被 Runtime 的 `PFTagContainer` 和 `GameplayTagAggregator` 静态触发。

这导致同一批 Tag 同时有多个权威候选。用户明确接受破坏性更新，因此本设计不保留旧 ScriptableObject 编辑链路作为兼容副本。

已有归档变更 `externalize-pfgas-generated-assets` 确立过一个重要边界：`PFGAS.Runtime` 不应直接引用 Luban、TEngine、Excel 或项目生成程序集；项目生成层可以引用 `PFGAS.Runtime` 和 `GameProto`。这次 Tag 迁移沿用这个方向。

## 目标 / 非目标

**目标：**

- 让 `PFTag.xlsx` 成为 Tag 的唯一业务数据源。
- 让 Tag 树编辑器直接读写 Luban Excel，而不是读写 `PFTagConfig.asset`。
- 用 Luban 导出结果驱动运行时 Tag 注册、名称和层级数据。
- 通过破坏性重构删除旧 ScriptableObject Tag 配置和旧生成器链路。
- 保持 PFGAS Runtime 的 Tag 语义：Tag ID、父子层级、`HasTag`/`IsOrUnder` 等查询行为不变。
- 统一 Tag 导表和适配生成入口，避免保存 Excel 后还需要猜测运行哪个脚本。

**非目标：**

- 不在本变更中迁移 Attribute、Effect、Ability 或 Preset。
- 不让 Runtime 直接读写 Excel。
- 不把 Excel 读写库引入 Runtime 程序集。
- 不支持多人同时编辑同一个 Excel 文件的冲突合并。
- 不保留 `PFTagConfig.asset` 作为第二编辑源。

## 决策

### 决策 1: 保留当前 Tag 表的 `Id + ParentId + Name + Desc` 模型

当前 `PFTag.xlsx` 已经使用 `Id`、`ParentId`、短 `Name` 和 `Desc` 表达树结构。这比单列点分路径更适合树编辑器的增删改和拖拽重挂父级。

替代方案：

- 使用 `Name = State.DeBuff.Fire` 的点分路径作为权威：更接近文档草案，但重命名和移动节点时需要批量重写子树路径，也更容易把显示名和结构混在一起。
- 同时保留 `ParentId` 和 `FullPath` 两个可编辑权威：容易出现不一致，不采用。

选择：

- `Id` 是稳定全局 ID。
- `ParentId` 是树结构权威，根节点为 `-1`。
- `Name` 是同父级短名，用于生成枚举片段或常量片段。
- `Desc` 是说明。
- 完整路径由工具派生，可作为只读显示、导出字段或生成时中间值。

### 决策 2: Tag 树编辑器变成 Excel-backed editor

旧 `PFTagTreeWindow` 继承 `PFTreeEditor<PFTagNodeConfig>`，基类会自动创建和保存 `PFTreeConfig<T>` ScriptableObject。这一层必须打破。

新的 Tag 编辑器可以复用 `PFTreeView` 的树 UI 和交互模型，但数据加载和保存需要改为：

```text
PFTag.xlsx
  -> Excel workbook reader
  -> Tag row model
  -> tree model
  -> editor operations
  -> validator
  -> Excel workbook writer
```

编辑器保存时必须写回同一份 Excel，并保留 Luban 的表头、类型、分组和注释行。保存失败时必须明确提示 Excel 文件被占用或路径配置错误。

替代方案：

- 保留 `PFTagConfig.asset`，增加“导入/导出 Excel”：迁移风险小，但继续双源分裂，不符合本变更目标。
- 直接编辑 Excel 文件，不做 Unity 树编辑器：实现最简单，但失去现有树编辑体验。

### 决策 3: 增加 PFGAS Tag 适配生成层，而不是 Runtime 直接使用 Luban 类型

需要基于 Luban 生成结果再生成一层薄适配代码。这个适配层是必要边界，不是多余包装。

推荐结构：

```text
GameProto / LubanConfig
  GameConfig.PFGAS.PFTag
  GameConfig.PFGAS.TbPFTag
        |
        v
PFGASGenerated / PFGAS
  PFGASTagIds 或 PFTagGenerated
  PFGASGeneratedData.RegisterTags(...)
        |
        v
PFGAS.Runtime
  TagHelper
  PFTagContainer
  GameplayTagAggregator
```

理由：

- Luban 表类表达的是配置表结构，不应泄漏成 PFGAS Runtime 的公共模型。
- Runtime 不应知道 Excel 列、Luban 表名、JSON 加载器或 TEngine 资源系统。
- PFGAS Runtime 只需要稳定的 Tag ID、层级、显示名和查询数据。
- 适配层可以承载项目具体常量、启动注册和生成物 asmdef 引用。

替代方案：

- Runtime 直接引用 `GameConfig.PFGAS.TbPFTag`：短期直接，但会让 PFGAS 包反向依赖项目配置程序集，破坏包边界。
- 只生成静态 Tag 字典，不引用 Luban 表类：运行时最轻，但会削弱“运行时 Tag 依赖 Luban 生成代码”的链路，也容易让 Luban 表类和 PFGAS 适配生成物各自独立漂移。
- 改 Runtime 全部使用 `int`：迁移最简单，但丢失类型语义，不采用。

选择：

- Luban 继续生成通用配置表代码。
- PFGAS 生成器读取 Luban Excel、Luban JSON 或 Luban 表类可消费的数据，生成 PFGAS 专属适配代码。
- `PFGASGenerated` 程序集引用 `PFGAS.Runtime` 和 `GameProto`。
- `PFGAS.Runtime` 不引用 `PFGASGenerated` 或 `GameProto`。

### 决策 4: 运行时注册改为显式或项目启动注册

当前 `PFTagContainer` 和 `GameplayTagAggregator` 通过 `RuntimeHelpers.RunClassConstructor(typeof(PFTagGenerated).TypeHandle)` 隐式触发包内生成类。迁移后 Runtime 不能再知道项目生成类型。

新的方向：

- `TagHelper` 提供清晰的注册入口和未注册诊断。
- 项目启动流程调用 PFGAS 适配生成层的注册入口。
- 测试和示例显式初始化生成数据，或使用测试专用 bootstrap。

替代方案：

- 继续把 `PFTagGenerated` 放在 Runtime 程序集：兼容旧调用，但项目数据继续污染包。
- 用反射在 Runtime 中寻找项目生成类：减少手动调用，但隐藏依赖，错误更难诊断。

### 决策 5: 旧 ScriptableObject 链路删除，不保留 Legacy 编辑

本变更接受破坏性更新，所以旧 `PFTagConfig.asset`、`PFTagConfig.cs` 和旧 `PFTagCodeGenerator` 不应继续作为可编辑入口存在。若需要迁移旧数据，只允许一次性迁移或对比工具读取旧资产，然后删除或隐藏旧入口。

替代方案：

- 保留 Legacy 只读窗口：可以帮助对比，但容易被误用为权威。除非实现阶段发现迁移验证强烈需要，否则不做默认保留。

## 风险 / 权衡

- `PFTagId.State_Buff` 这类调用点可能破坏 -> 生成层需要提供新的项目常量，或者继续生成兼容命名；实施时必须统一测试和示例。
- Runtime 不再自动触发 `PFTagGenerated` -> 启动流程必须显式注册，未注册时要给出清晰错误。
- Excel 被外部程序占用导致保存失败 -> 保存前备份，捕获 IO 异常并提示关闭 Excel。
- Luban 脚本和输出路径当前不一致 -> 迁移时先收束导出入口和输出目录，再接编辑器按钮。
- `__tables__.xlsx` 当前未列出 `TbPFTag`，表由 `Defines/tag.xml` 提供 -> 工具和文档必须明确 schema 来源，避免 helper/导表脚本识别不一致。
- 生成适配层增加一次生成步骤 -> 用“一键保存并导出”菜单串联 Excel 保存、Luban 导出和 PFGAS 适配生成。

## 迁移计划

1. 固定 Tag Excel schema，确认 `PFTag.xlsx` 的 `Id`、`ParentId`、`Name`、`Desc` 列和 Luban 定义一致。
2. 建立 Tag Excel 读写服务，只在 Editor 程序集使用 Excel 读写库。
3. 用 Excel-backed 数据源替换 `PFTagTreeWindow` 的 ScriptableObject 数据源。
4. 增加 Tag 校验和保存前备份。
5. 收束 Luban 导出脚本和 Unity 菜单入口，确保能从 `PFTag.xlsx` 生成 JSON 和 `GameConfig.PFGAS.TbPFTag`。
6. 实现 PFGAS Tag 适配生成器，输出项目侧生成代码并注册 Tag 层级和显示名。
7. 调整 Runtime 的 Tag 注册入口，移除对包内 `PFTagGenerated` 的静态硬依赖。
8. 更新测试、示例和启动流程。
9. 删除旧 `PFTagConfig.asset`、旧 ScriptableObject 配置类型和旧 ScriptableObject 驱动生成器。
10. 验证重新导出后运行时测试通过，并验证手改 Excel 与编辑器保存都能产生一致生成结果。

回滚策略：

- 通过版本控制恢复旧文件，不在工作树中保留旧 ScriptableObject 编辑链路作为并行兼容方案。

## 待定问题

- 项目常量最终命名采用 `PFTagId.State_Buff` 兼容式 enum，还是破坏性迁移为 `PFGASTagIds.State_Buff` 常量类。
- PFGAS 适配生成物最终输出到 `Assets/PFGASGenerated/PFGAS`，还是先沿用当前项目结构中的 `Assets/GameScripts/HotFix/GameProto` 同层目录。
- Excel 写入库选择 OpenXML SDK、NPOI、ClosedXML 或 Unity 可用的轻量封装，需要在实现阶段结合许可证和 Unity 兼容性确认。
