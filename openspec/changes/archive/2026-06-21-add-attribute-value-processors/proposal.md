## 为什么

当前 AttributeGraph 只有 `IAttributeEvaluator` 负责把聚合后的 raw value 处理成 `CurrentValue`，但缺少对 `BaseValue` 写入后的领域约束。HP 这类资源属性会被治疗持续推高 `BaseValue`，即使 `CurrentValue` 被 MaxHP 截断，也会形成隐藏的过量血池，导致后续伤害先消耗不可见 Base。

同时，`IAttributeEvaluator` 名称过于宽泛，实际职责是 CurrentValue 后处理。将它重命名并补齐 BaseValue 后处理器，可以让属性系统职责更清晰，并避免把伤害/治疗语义塞进 GameplayEffect。

## 变更内容

- 新增 BaseValue 后处理能力，用于在属性 BaseValue 写入、Instant/Periodic modifier 落地、依赖属性变化后规范化 BaseValue。
- 将现有 `IAttributeEvaluator` 语义重命名为 CurrentValue 后处理器，明确其只负责 raw current 到 final current 的计算。
- 为 AttributeRule 同时配置 BaseValue processor 和 CurrentValue processor。
- 为 HP 示例配置 BaseValue clamp 到 `[0, MaxHP.Current]`，并保留 CurrentValue clamp 到 `MaxHP.Current`。
- 更新示例和测试，覆盖过量治疗不会形成隐藏血池、MaxHP 降低会同步约束 HP Base、普通持续属性修饰仍按现有 GameplayEffect 规则工作。
- **BREAKING**: Runtime API 中 `IAttributeEvaluator` 及内置 evaluator 类型将重命名为 CurrentValue processor 命名。

## 功能 (Capabilities)

### 新增功能
- `attribute-value-processing`: 定义 AttributeGraph 对 BaseValue 和 CurrentValue 的双阶段后处理能力，包括依赖、重算顺序和资源属性约束。

### 修改功能

## 影响

- `UnityProject/Assets/PFGAS/Runtime/GAS/Attributes/AttributeRule.cs`
- `UnityProject/Assets/PFGAS/Runtime/GAS/Attributes/Graph/AttributeGraph.*.cs`
- `UnityProject/Assets/PFGAS/Runtime/GAS/Attributes/Evaluator/**`
- `UnityProject/Assets/PFGAS/Runtime/Gen/PFAttributeGenerated.cs`
- `UnityProject/Assets/PFGAS/Editor/Scripts/Attribute/**`
- `UnityProject/Assets/PFGAS/Tests/Runtime/**`
