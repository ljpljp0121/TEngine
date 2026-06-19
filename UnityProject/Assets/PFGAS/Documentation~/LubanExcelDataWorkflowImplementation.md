# PFGAS Luban Excel 数据方案实施文档

## 目标

把 PFGAS 的配置数据改造成和参考项目 EX-GAS 类似的工作流：

```text
策划直接编辑 Excel
        或
Unity Editor 可视化编辑 Excel
        |
        v
同一份 Luban 配置表工程
        |
        v
Luban 生成 JSON + C# 表类
        |
        v
Runtime 只读取生成结果
```

核心原则：

- Excel/Luban 配置表是唯一数据源。
- Unity Editor 只是 Excel 的可视化编辑器，不保存第二份业务配置。
- Runtime 不读 Excel，不依赖 Editor，只读取 Luban 生成的 JSON/bytes 和 C# 表类。
- 生成文件不得手改，手改入口只有 Excel 或 Unity Editor。
- 当前 PFGAS Runtime 语义尽量不动，新增的是配置层、生成层和编辑器读写层。

## 不做什么

- 不把 ScriptableObject 继续作为 Tag、Attribute、Effect、Ability 的权威源。
- 不让 Runtime 引用 EPPlus、NPOI、openpyxl、UnityEditor 或任何 Excel 读写库。
- 不在 Runtime 里直接处理 Excel 路径、表头、行列坐标。
- 不让生成代码反向依赖 Editor。
- 不在第一版实现多人同时编辑 Excel 的合并能力。

## 推荐目录

项目级配置和生成物放在包外，避免把使用方项目数据写进 PFGAS 包本体。

```text
PFGAS_Config/
  ProjectConfigTable/
    pfgas_config/
      luban.conf
      gen.bat
      Datas/
        __tables__.xlsx
        __beans__.xlsx
        __enums__.xlsx
        #pfgas.gameplayTags.xlsx
        #pfgas.attribute.xlsx
        #pfgas.gameplayEffect.xlsx
        #pfgas.gameplayEffectModifier.xlsx
        #pfgas.ability.xlsx
        #pfgas.combatUnitPreset.xlsx

Assets/
  PFGASGenerated/
    Luban/
      cfg/...                         # Luban 生成的表类
      LubanTables.asmdef
    Data/
      pfgas_tbgameplaytags.json
      pfgas_tbattribute.json
      pfgas_tbgameplayeffect.json
      pfgas_tbgameplayeffectmodifier.json
      pfgas_tbability.json
      pfgas_tbcombatunitpreset.json
    PFGAS/
      PFGASGeneratedData.gen.cs       # 配置装配入口
      PFTagGenerated.cs               # Tag 常量和注册
      PFAttributeGenerated.cs         # Attribute 常量
      PFAbilityGenerated.cs           # Ability 常量
      PFGASGenerated.asmdef
```

包内只放工具和模板：

```text
Assets/PFPackage/PFGAS/
  Runtime/
    GAS/...
    Data/
      IPFGASConfigProvider.cs
      PFGASConfigRegistry.cs
  Editor/
    Scripts/
      Data/
        PFGASDataSetting.cs
        PFGASDataMenu.cs
        Excel/
        CodeGen/
        Views/
  Samples~/
    LubanConfigTemplate/
```

如果当前阶段只在本仓库内使用，也可以先把生成物放到 `Assets/PFPackage/PFGAS/Runtime/Gen`。但长期看，项目级 `Assets/PFGASGenerated` 更适合包分发。

## 配置设置资产

新增 `PFGASDataSetting`，只保存路径和导出选项，不保存业务数据。

建议位置：

```text
ProjectSettings/PFGASDataSetting.asset
```

字段：

```text
ConfigProjectPath        = PFGAS_Config/ProjectConfigTable/pfgas_config
LubanGenBatPath          = {ConfigProjectPath}/gen.bat
JsonOutputPath           = Assets/PFGASGenerated/Data
LubanCodeOutputPath      = Assets/PFGASGenerated/Luban
PFGASCodeOutputPath      = Assets/PFGASGenerated/PFGAS
UseJsonRuntimeData       = true
UseBytesRuntimeData      = false
AutoExportAfterSave      = false
BackupExcelBeforeSave    = true
```

Editor 菜单：

```text
Game/PFGAS/配置数据/导入 Luban 配置模板
Game/PFGAS/配置数据/打开配置工程目录
Game/PFGAS/配置数据/导出 Luban 数据
Game/PFGAS/配置数据/生成 PFGAS 适配代码
Game/PFGAS/配置数据/一键保存并导出
```

## Excel 规范

所有业务表统一使用 Luban 常见布局：

```text
第 1 行：字段名，可带 # 后缀，例如 Tags#sep=;
第 2 行：Luban 类型
第 3 行：注释
第 4 行起：数据
```

通用约定：

- 第 B 列为 `ID`。
- 第 C 列为 `Name`。
- 第 D 列为 `Desc`。
- Editor 解析表头时只取 `#` 前面的字段名。
- 简单 ID 列表使用 `;` 分隔。
- 复杂结构优先拆子表，不把大型对象强塞进一个单元格。
- 删除数据时不删除前 3 行。
- 保存时必须保留 Luban 类型行和注释行。

## 表结构

### `#pfgas.gameplayTags.xlsx`

用途：定义所有 Tag，并由 Name 的点分层级生成树。

```text
ID      int     全局唯一 Tag ID
Name    string  点分路径，例如 State.Buff.Fire
Desc    string  描述
```

生成：

- `PFTagId` 枚举。
- `PFTagGenerated` 注册层级和显示名。
- Editor 下拉选择项。

校验：

- ID 不重复。
- Name 不为空且不重复。
- 如果 Name 是 `A.B.C`，建议提示缺少父节点 `A` 或 `A.B`，但第一版可以只警告不阻止。

### `#pfgas.attribute.xlsx`

用途：定义 AttributeGraph 可注册的属性。

```text
ID                int     全局唯一 Attribute ID
Name              string  属性名，例如 HP
Desc              string  描述
DefaultValue      float   默认值
AggregationMode   int     对应 AggregationMode
LimitMinValue     bool    是否限制最小值
MinValue          float   最小值
LimitMaxValue     bool    是否限制最大值
MaxValue          float   最大值
EvaluatorType     string  Default / ClampMin / ClampMax / ClampRange / Formula
EvaluatorParams   string  简单参数，第一版可留空
```

生成：

- `PFAttributeId` 枚举。
- Attribute 定义缓存。
- 可选的 Attribute 初始化辅助代码。

校验：

- ID 不重复。
- Name 可生成合法 C# 标识符。
- `LimitMinValue && LimitMaxValue` 时，`MinValue <= MaxValue`。
- `EvaluatorType` 必须存在对应 Runtime 或生成层工厂。

### `#pfgas.gameplayEffect.xlsx`

用途：定义 GameplayEffect 主体。Modifier 拆到子表。

```text
ID                    int     全局唯一 Effect ID
Name                  string  Effect 名
Desc                  string  描述
LifetimePolicy        int     Instant / Duration / Infinite
DurationSeconds       float   Duration 使用，Infinite/Instant 填 0
PeriodSeconds         float   Periodic modifier 使用，非周期填 0
GrantedTags           int[]   激活期间授予 Target 的 Tag，sep=;
SourceRequiredTags    int[]   Source 必须拥有，sep=;
SourceBlockedTags     int[]   Source 禁止拥有，sep=;
TargetRequiredTags    int[]   Target 必须拥有，sep=;
TargetBlockedTags     int[]   Target 禁止拥有，sep=;
StackingMode          int     Independent / Replace / Refresh / Stack
StackLimit            int     Stack 模式上限
OverflowPolicy        int     Fail / Ignore / Refresh / ReplaceOldest
```

生成：

- Effect ID 常量。
- `GetGameplayEffect(id)`，返回静态 `GameplayEffect` 定义。
- `CreateGameplayEffectSpec(id, source, target, level, payload)`。

校验：

- Instant 不能有 Ongoing 或 Periodic modifier。
- Periodic modifier 要求 `PeriodSeconds > 0`。
- Instant 不能配置持久 GrantedTags。
- Tag 引用必须存在。
- Stacking 参数必须和 Runtime 支持的策略一致。

### `#pfgas.gameplayEffectModifier.xlsx`

用途：定义 Effect 的属性修改项。一个 Effect 可以有多行 Modifier。

```text
ID              int     Modifier 行 ID
EffectID        int     所属 GameplayEffect
Order           int     执行顺序
Phase           int     Instant / Ongoing / Periodic
TargetAttribute int     目标 Attribute ID
Operation       int     Add / Multiply / Override / Minus / Divide
MagnitudeType   int     Fixed / SourceAttribute / TargetAttribute
MagnitudeValue  float   Fixed 值，或属性引用倍率
SourceAttribute int     MagnitudeType 为 SourceAttribute 时使用
TargetAttrRef   int     MagnitudeType 为 TargetAttribute 时使用
CapturePolicy   int     SnapshotOnApply / DynamicWhileActive
```

第一版只要覆盖当前 Runtime 已支持的 `GameplayEffectMagnitudeSpec` 形态即可。没有支持的 Magnitude 类型不要提前进表。

### `#pfgas.ability.xlsx`

用途：定义可授予 CombatUnit 的 Ability 配置。Ability 的核心逻辑仍由代码类实现，表只负责 ID、类型键、基础配置和关联 Effect。

```text
ID                    int     全局唯一 Ability ID
Name                  string  Ability 名
Desc                  string  描述
AbilityType           string  Runtime 注册的 Ability 工厂键
Level                 int     默认等级
Enabled               bool    默认是否启用
CostEffectID          int     消耗 Effect，可为 0
CooldownEffectID      int     冷却 Effect，可为 0
ActivationRequiredTags int[]  激活所需 Tag，sep=;
ActivationBlockedTags  int[]  激活禁止 Tag，sep=;
AbilityParams         string  简单参数，复杂逻辑后续拆子表
```

生成：

- `PFAbilityId` 常量。
- Ability 配置缓存。
- Ability 工厂调用代码。

校验：

- `AbilityType` 必须能通过 Runtime 工厂创建。
- Cost/Cooldown Effect ID 必须存在或为 0。
- Tag 引用必须存在。

### `#pfgas.combatUnitPreset.xlsx`

用途：给场景中的 `CombatUnit` 配一个初始化预设，类似参考项目的 ASC 表。

```text
ID              int     预设 ID
Name            string  预设名
Desc            string  描述
Tags            int[]   初始 Tag，sep=;
Attributes      int[]   初始 Attribute ID，sep=;
Abilities       int[]   初始 Ability ID，sep=;
Level           int     默认等级
```

第一版可以只做到：

- 初始化 Tags。
- 初始化 AttributeGraph 中的属性。
- 授予 Abilities。

如果需要覆盖 Attribute 初始值，后续新增 `#pfgas.combatUnitAttributeOverride.xlsx` 子表。

## Luban 生成

`luban.conf` 建议：

```json
{
  "groups": [
    {"names":["c"], "default":true},
    {"names":["s"], "default":true},
    {"names":["e"], "default":true}
  ],
  "schemaFiles": [
    {"fileName":"Defines", "type":""},
    {"fileName":"Datas/__tables__.xlsx", "type":"table"},
    {"fileName":"Datas/__beans__.xlsx", "type":"bean"},
    {"fileName":"Datas/__enums__.xlsx", "type":"enum"}
  ],
  "dataDir": "Datas",
  "targets": [
    {"name":"client", "manager":"Tables", "groups":["c"], "topModule":"cfg"}
  ]
}
```

`gen.bat` 接收两个参数：

```powershell
set OUTPUT_JSON_DIR=%~1
set OUTPUT_CODE_DIR=%~2
set CONF_ROOT=.
set WORKSPACE=..
set LUBAN_DLL=%WORKSPACE%\Tools\Luban\Luban.dll

dotnet %LUBAN_DLL% ^
    -t client ^
    -c cs-simple-json ^
    -d json ^
    --conf %CONF_ROOT%\luban.conf ^
    -x outputCodeDir=%OUTPUT_CODE_DIR% ^
    -x outputDataDir=%OUTPUT_JSON_DIR%
```

如果后续要切 bytes，只新增 bytes 导出目标和 loader，不改变 Editor 写 Excel 的逻辑。

## Runtime 适配层

Runtime 包提供接口，不直接依赖生成代码。

建议新增：

```text
PFGAS.Runtime.Data.IPFGASConfigProvider
PFGAS.Runtime.Data.PFGASConfigRegistry
```

接口职责：

```csharp
public interface IPFGASConfigProvider
{
    bool TryGetTag(int id, out PFTag tag);
    bool TryGetAttribute(int id, out AttributeRule rule);
    bool TryGetGameplayEffect(int id, out GameplayEffect effect);
    bool TryCreateGameplayEffectSpec(
        int id,
        CombatUnit source,
        CombatUnit target,
        int level,
        object payload,
        out GameplayEffectSpec spec);
    bool TryGetAbility(int id, out GameplayAbility ability);
    bool TryInitializeCombatUnit(CombatUnit unit, int presetId);
}
```

生成层实现：

```text
PFGASGeneratedData.LoadTables(Func<string, JSONNode> loader)
PFGASGeneratedData.Provider
PFGASConfigRegistry.SetProvider(PFGASGeneratedData.Provider)
```

Runtime 使用方式：

```text
游戏启动
  -> 初始化资源系统
  -> PFGASGeneratedData.LoadTables(loader)
  -> PFGASConfigRegistry.SetProvider(...)
  -> 场景加载
  -> CombatUnit 使用 presetId 初始化
```

注意：

- `PFGAS.Runtime` 不能引用 `PFGASGenerated` asmdef。
- `PFGASGenerated` asmdef 可以引用 `PFGAS.Runtime` 和 Luban Runtime。
- 这样包本身没有生成物也能编译。

## Editor 读写层

Editor 层按表做可视化窗口，但每个窗口都只读写 Excel。

公共基础设施：

```text
PFGASExcelWorkbook
  Open(path)
  ReadHeaderMap(sheet)
  ReadRows(sheet, startRow = 4)
  SaveWithBackup(path)

PFGASExcelChoiceCache
  Tags()
  Attributes()
  Effects()
  Abilities()

PFGASExcelValidator
  ValidateAll()
  ValidateTags()
  ValidateAttributes()
  ValidateEffects()
  ValidateAbilities()
  ValidateCombatUnitPresets()
```

窗口：

```text
PFGASTagExcelWindow
PFGASAttributeExcelWindow
PFGASGameplayEffectExcelWindow
PFGASAbilityExcelWindow
PFGASCombatUnitPresetExcelWindow
```

每个窗口的按钮：

```text
打开 Excel 所在目录
刷新
保存到 Excel
校验
导出 Luban
保存并导出
```

保存流程：

```text
读取当前 UI DTO
        |
        v
校验本表
        |
        v
如果开启备份，复制 .xlsx 到 .bak
        |
        v
写回 Excel
        |
        v
刷新 UI
```

导出流程：

```text
校验全部 Excel
        |
        v
运行 gen.bat JsonOutputPath LubanCodeOutputPath
        |
        v
生成 PFGAS 适配代码
        |
        v
AssetDatabase.Refresh()
```

Excel 被外部 Excel 程序打开时，写入可能失败。必须捕获异常并提示“请关闭 Excel 后重试”。

## 生成代码职责

Luban 负责生成：

- `cfg.Tables`
- `cfg.pfgas.*` 表数据类
- JSON 数据

PFGAS 自己的生成器负责生成：

- Tag/Attribute/Ability 常量。
- `PFGASGeneratedData.gen.cs`。
- 从 Luban `cfg` 数据到 PFGAS Runtime 类型的装配逻辑。

生成器不应该复制 Runtime 逻辑，只做数据转换。

示例转换：

```text
cfg.pfgas.GameplayEffect
        |
        v
GameplayEffectLifetime
GameplayEffectModifierSpec[]
GameplayEffectTagRequirements
GameplayEffectStackingPolicy
        |
        v
new GameplayEffect(...)
```

## 旧数据迁移

当前 PFGAS 已有两类 ScriptableObject 源：

```text
Editor/Scripts/Tags/PFTagConfig.asset
Editor/Scripts/Attribute/PFAttributeConfig.asset
```

迁移步骤：

1. 写一次性迁移工具，把 `PFTagConfig.asset` 导出到 `#pfgas.gameplayTags.xlsx`。
2. 写一次性迁移工具，把 `PFAttributeConfig.asset` 导出到 `#pfgas.attribute.xlsx`。
3. 用 Luban 导出 JSON 和表类。
4. 用新生成器生成 `PFTagGenerated.cs` 和 `PFAttributeGenerated.cs`。
5. 对比旧生成文件和新生成文件的 ID 是否一致。
6. 标记旧窗口为 Legacy，只允许查看或导出，不再作为权威源。
7. 稳定后删除或隐藏旧 ScriptableObject 编辑入口。

迁移期间的规则：

- 不允许一边改 ScriptableObject，一边改 Excel。
- 迁移完成后，Excel 是唯一允许编辑的数据源。
- 如果保留旧资产，只作为备份或迁移输入。

## 实施阶段

### 阶段 1：配置工程和导表跑通

任务：

- 创建 `PFGAS_Config/ProjectConfigTable/pfgas_config`。
- 加入 Luban 工具、`luban.conf`、`gen.bat`。
- 建立最小 `__tables__.xlsx`、`__beans__.xlsx`、`__enums__.xlsx`。
- 建立 Tag 和 Attribute 两张表。
- Editor 菜单可以运行 Luban 导出。

验收：

- 点击导出后生成 JSON。
- 生成 `cfg.Tables`。
- Unity 编译通过。

### 阶段 2：Tag 和 Attribute 切到 Excel

任务：

- 实现 Tag/Attribute Excel 读写窗口。
- 实现旧 ScriptableObject 到 Excel 的迁移。
- 生成新的 `PFTagGenerated.cs` 和 `PFAttributeGenerated.cs`。
- Runtime 使用新生成文件仍能通过现有测试。

验收：

- 手改 Excel 后，刷新窗口能看到变化。
- 窗口保存后，Excel 内容真实变化。
- 导出后，生成常量 ID 不变。
- 旧 Tag/Attribute 测试通过。

### 阶段 3：GameplayEffect 表和适配器

任务：

- 建立 Effect 主表和 Modifier 子表。
- 实现 Effect Excel 编辑窗口。
- 实现 `GetGameplayEffect(id)` 和 `TryCreateGameplayEffectSpec(...)`。
- 补齐 Effect 表校验。

验收：

- 表中配置 Instant 伤害，Runtime 能应用。
- 表中配置 Duration/Ongoing Buff，Runtime 能添加并移除 ModifierSource。
- 表中配置 Periodic 效果，Runtime 能按 period 生效。
- 非法组合在导出前被拦截。

### 阶段 4：Ability 和 CombatUnitPreset

任务：

- 建立 Ability 表。
- 建立 CombatUnitPreset 表。
- Runtime 提供 Ability 工厂注册机制。
- CombatUnit 支持按 presetId 初始化。

验收：

- 一个 preset 能初始化 Tags、Attributes、Abilities。
- Ability ID 能通过表授予。
- Cost/Cooldown Effect ID 引用正确。

### 阶段 5：完善 Editor 工作流

任务：

- 所有窗口统一按钮、校验、备份、刷新逻辑。
- 下拉项从 Excel 读取，不从 JSON 读取。
- 保存失败时提示 Excel 文件锁定。
- 加入“一键保存并导出”。

验收：

- 外部 Excel 修改后，Unity 点击刷新能同步。
- Unity 修改后，打开 Excel 能看到同步。
- 导出不依赖旧 ScriptableObject。

### 阶段 6：测试和文档

任务：

- Editor 测试：读写 round-trip。
- Editor 测试：重复 ID、丢失引用、非法枚举。
- Runtime 测试：加载生成 JSON。
- Runtime 测试：通过 provider 构造 Effect/Ability/Preset。
- 更新 README 或 Documentation。

验收：

- 没有 Excel 文件时，Runtime 仍可编译。
- 没有生成数据时，Runtime 给出清晰错误。
- 所有生成文件可删除后重新生成。

## 风险和处理

### Excel 文件锁

问题：Excel 程序打开文件时，Editor 保存失败。

处理：

- 保存前尝试以写权限打开。
- 捕获 IOException。
- 提示关闭 Excel。
- 保存前自动生成 `.bak`。

### EPPlus 或 Excel 库许可证

问题：参考项目使用 EPPlus，具体版本许可证要确认。

处理：

- Editor-only 引入。
- 明确记录版本和许可证。
- 如果许可证不合适，替换为 NPOI、ClosedXML 或 OpenXML SDK。

### 复杂字段可维护性

问题：把 Modifier、GrantedAbility 等复杂结构压成一个字符串单元格，后期容易难读难改。

处理：

- 第一版推荐子表。
- 简单列表才使用 `sep=;`。
- Editor 可以把主表和子表合成一个可视化界面。

### 生成程序集依赖

问题：Runtime 引用生成程序集会导致包没有生成物时无法编译。

处理：

- Runtime 只定义接口。
- 生成程序集引用 Runtime。
- 游戏启动时注册生成 provider。

### 旧数据双源

问题：ScriptableObject 和 Excel 同时存在会造成配置分裂。

处理：

- 迁移后旧窗口改名 Legacy。
- 旧窗口只保留“导出到 Excel”。
- 文档明确 Excel 是唯一数据源。

## 最小可行版本

如果想最快落地，先做这个范围：

```text
Tag Excel
Attribute Excel
Luban 导出
生成 PFTagGenerated / PFAttributeGenerated
Editor 读写 Tag / Attribute Excel
禁用旧 ScriptableObject 编辑入口
```

这一步完成后，就已经建立了“Excel/Luban 唯一数据源”的基础。Effect、Ability、Preset 可以在同一套机制上继续扩展。

## 完成定义

当以下条件都满足，可以认为 PFGAS 数据方案完成第一版：

- 所有策划配置都能在 `PFGAS_Config/.../Datas` 中找到源 Excel。
- Unity Editor 中编辑任何配置，最终写入的都是同一份 Excel。
- Luban 导出能从零生成 JSON 和表类。
- PFGAS 生成器能从 Luban 表生成 Runtime 适配代码。
- Runtime 启动只加载生成结果，不访问 Excel。
- 旧 ScriptableObject 配置不再是权威源。
- 删除 `Assets/PFGASGenerated` 后可以通过导出完整恢复。
