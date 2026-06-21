## 为什么

当前 `AttributeModifier` 已经是一条修改只持有一个 `IAttributeMagnitude`，这个外层模型和参考项目一致；问题在于 `IAttributeMagnitude` 现在暴露了过强的表达式树能力，已经不适合作为后续编辑器和 Luban Excel 的正式配置模型。

这次要做破坏性重构：保留“一条 Modifier 一个 Magnitude”，但把 Magnitude 收敛为有限、可命名、可配置的计算类型；复杂公式通过自定义 Magnitude 类表达，而不是用 `Add/Multiply/Clamp` 等任意组合堆出来。

## 变更内容

- **BREAKING**: 收敛 `IAttributeMagnitude` 正式模型，移除或隐藏通用表达式树式 API。
- **BREAKING**: 清理 `AttributeMagnitude.Add/Subtract/Multiply/Divide/Min/Max/Clamp` 等组合工厂的正式使用。
- **BREAKING**: 删除或迁移 `BinaryAttributeMagnitude`、`ClampAttributeMagnitude` 这类通用表达式节点。
- 保留 `AttributeModifier` 的 `AttributeId + Operation + Magnitude` 单 Magnitude 结构。
- 将内置 Magnitude 改为有限类型，例如固定值、线性缩放、属性读取缩放、自定义 Magnitude。
- 复杂公式必须通过明确的自定义 Magnitude 类型表达，并显式声明依赖属性。
- 继续让 AttributeGraph 从单个 Magnitude 的依赖声明建立拓扑边、循环检测和 dirty 传播。
- 对 Runtime、Editor、Tests 做清洁，移除旧命名和旧表达式模型残留。
- 所有新增或调整的代码注释必须使用中文。

## 功能 (Capabilities)

### 新增功能

### 修改功能

- `attribute-value-processing`: 收敛 Attribute Modifier 的 Magnitude 模型，使复杂公式通过有限 Magnitude 类型和自定义 Magnitude 表达，而不是通过通用表达式树组合。

## 影响

- `UnityProject/Assets/PFGAS/Runtime/GAS/Attributes/Modifier/AttributeModifier.cs`
- `UnityProject/Assets/PFGAS/Runtime/GAS/Attributes/Magnitude/**`
- `UnityProject/Assets/PFGAS/Runtime/GAS/Attributes/Graph/AttributeGraph.Modifiers.cs`
- `UnityProject/Assets/PFGAS/Runtime/GAS/Attributes/Graph/AttributeGraph.Recalculation.cs`
- `UnityProject/Assets/PFGAS/Editor/Scripts/Attribute/**`
- `UnityProject/Assets/PFGAS/Tests/Runtime/**`
- 后续 Luban Excel / 编辑器配置映射中涉及 Magnitude 的部分
