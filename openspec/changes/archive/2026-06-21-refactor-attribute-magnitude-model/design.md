## 上下文

当前属性系统已经把一条修改建模为 `AttributeModifier`，结构是 `AttributeId + Operation + IAttributeMagnitude`。这说明本项目已经满足“一条 Modifier 只有一个 Magnitude”的外层模型。

参考项目的 Modifier 结构是 `AttrSet + Attribute + Magnitude + Operation + Mmc`。其中 `Magnitude` 是基础值，`Mmc` 是一个计算配置 ID；运行时通过 `Mmc` 找到具体计算类，再把基础值算成最终修改量。

两边的对应关系是：

```text
参考项目 AttrSet + Attribute 约等于本项目 AttributeId
参考项目 Operation 约等于本项目 GEOperation
参考项目 Magnitude + Mmc 约等于本项目 IAttributeMagnitude
参考项目 GameplayEffect.Modifiers[] 约等于本项目 ModifierSource.Modifiers
```

当前问题不在 Modifier 外层，而在 `IAttributeMagnitude` 内部。它现在暴露了 `Add/Subtract/Multiply/Divide/Min/Max/Clamp` 这类通用组合能力，实际已经像一个表达式树。这个模型不利于未来的编辑器配置和 Luban Excel 表达，也不利于做配置校验。

## 目标 / 非目标

**目标：**

- 保持一条 `AttributeModifier` 只持有一个 `IAttributeMagnitude`。
- 收敛 `IAttributeMagnitude` 为有限、可命名、可配置的计算类型。
- 复杂公式通过自定义 Magnitude 类型实现。
- 清理旧表达式树 API 和旧表达式节点。
- 保持 Magnitude 依赖声明继续参与 AttributeGraph 拓扑排序、循环检测和 dirty 传播。
- 破坏性迁移现有 Runtime、Editor、Tests 调用点，不保留旧兼容层。
- 新增和调整的代码注释使用中文。

**非目标：**

- 不实现通用公式字符串。
- 不实现 DOTween 风格链式 Magnitude DSL。
- 不让 `AttributeModifier` 直接持有多个 Magnitude。
- 不在本变更内完整实现 Luban Excel 编辑器。
- 不改变 BaseValue/CurrentValue processor 的职责边界。

## 决策

### 决策 1: 保留单 Modifier 单 Magnitude

`AttributeModifier` 继续只描述一件事：把一个最终修改量按一个操作作用到一个目标属性上。

```csharp
public readonly struct AttributeModifier
{
    public PFAttributeId AttributeId { get; }
    public GEOperation Operation { get; }
    public IAttributeMagnitude Magnitude { get; }
}
```

替代方案是让 Modifier 持有多个 Magnitude 并在内部组合。这个方案会让 Modifier 同时承担目标选择、操作语义和公式组合，职责过重，因此不采用。

### 决策 2: Magnitude 不再作为通用表达式树

移除或隐藏这些正式 API：

```text
AttributeMagnitude.Add
AttributeMagnitude.Subtract
AttributeMagnitude.Multiply
AttributeMagnitude.Divide
AttributeMagnitude.Min
AttributeMagnitude.Max
AttributeMagnitude.Clamp
```

对应的通用表达式节点也要删除或迁移：

```text
BinaryAttributeMagnitude
ClampAttributeMagnitude
```

替代方案是保留表达式树作为高级能力。这个方案会让配置侧逐步变成小型公式语言，不利于 Luban Excel 表达，因此不采用。

### 决策 3: 内置 Magnitude 使用有限类型

正式内置类型按参考项目的 MMC 思路收敛：

```text
FixedAttributeMagnitude
ScalableFloatAttributeMagnitude
AttributeBasedMagnitude
CustomAttributeMagnitude 或自定义 IAttributeMagnitude 实现
```

含义：

- `FixedAttributeMagnitude`: 固定值。
- `ScalableFloatAttributeMagnitude`: 基础值按 `value * K + B` 计算。
- `AttributeBasedMagnitude`: 读取某个属性，按 `attributeValue * K + B` 计算。
- 自定义 Magnitude: 复杂公式写入一个明确的 C# 类型。

替代方案是只保留 `Fixed` 和 `Custom`。这个方案太极端，会让常见线性缩放和属性读取公式都变成自定义类，因此不采用。

### 决策 4: 依赖由具体 Magnitude 显式声明

`IAttributeMagnitude` 保留依赖声明：

```csharp
IReadOnlyList<PFAttributeId> Dependencies { get; }
```

AttributeGraph 继续从 `modifier.Magnitude.Dependencies` 建立依赖边。复杂公式如果读取多个属性，必须由自定义 Magnitude 明确返回这些依赖。

替代方案是由 Graph 反射或递归分析 Magnitude 内部结构。收敛后不再有通用表达式树，显式依赖更清楚，因此不采用。

### 决策 5: 代码清洁优先于兼容

本变更不保留旧 API 的适配层、不保留过时命名、不保留“暂时兼容”的包装类型。所有调用点直接迁移到新模型。

理由：用户已明确要求破坏性重构，当前项目还处于内部迭代阶段，保留兼容会把旧模型继续扩散。

## 风险 / 权衡

- [风险] 删除表达式工厂会破坏现有测试和示例。  
  缓解：集中搜索旧 API，直接迁移到有限 Magnitude 或测试专用自定义 Magnitude。

- [风险] 有限 Magnitude 类型不够表达复杂公式。  
  缓解：复杂公式使用自定义 Magnitude，公式实现集中在 C# 类型中。

- [风险] 自定义 Magnitude 漏声明依赖会导致重算不触发。  
  缓解：为属性读取和自定义 Magnitude 增加依赖传播测试。

- [风险] 后续 Luban 表结构仍需设计。  
  缓解：本次先把运行时模型收敛到“类型 + 参数”的形状，为后续 Luban Bean 映射留出稳定目标。

## 迁移计划

1. 盘点并删除旧表达式树 API。
2. 增加有限 Magnitude 类型。
3. 迁移 Runtime、Editor、Tests 调用点。
4. 增加依赖传播和复杂公式自定义测试。
5. 运行 PFGAS Runtime/EditMode 测试。

## 开放问题

- 自定义 Magnitude 是否需要一个专门的基类来减少依赖声明样板代码，留到实现时根据代码量决定。
