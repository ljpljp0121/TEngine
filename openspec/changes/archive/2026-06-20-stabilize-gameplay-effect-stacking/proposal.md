## 为什么

当前 GameplayEffect 的叠层匹配依赖运行时对象引用。相同配置如果通过工厂或表格反复创建为不同实例，就会绕过 Stack/Refresh/Replace 规则，导致同类持续效果被重复挂载。

同时，动态来源使用 Source 当前值重建 Ongoing modifier 时，跨 CombatUnit 的互相光环会形成 AttributeGraph 无法检测的反馈环。持续效果应优先使用快照来源，替换类叠层也应避免新效果在旧效果仍参与目标当前值时完成捕获或计算。

## 变更内容

- 为 `GameplayEffect` 引入稳定 `EffectId`，用于运行时身份、叠层匹配、日志和后续配置表映射。
- 将 GameplayEffect stacking 匹配从对象引用改为稳定 `EffectId`，并保留 source/target scope 规则。
- 调整示例和测试的持续效果建模方式：同一种状态/机制复用同一个 Effect 定义，例如多个 Ability 施加中毒时共享同一个中毒 `EffectId`；光环使用独立 Aura `EffectId`，不与中毒共享。重复施加同一 `EffectId` 应叠层或刷新，而不是创建无法匹配的等价新实例。
- 将依赖 source 属性的持续伤害/持续状态默认设计为快照来源，避免跨单位实时反馈环。
- 修正 Replace/ReplaceOldest 语义，使被替换的旧效果不参与新效果的目标当前值计算；失败时仍保持旧效果和旧属性状态。
- 增加覆盖 EffectId 匹配、重复创建等价 Effect、快照来源、Replace 顺序和互相动态光环风险的测试。

## 功能 (Capabilities)

### 新增功能
- `gameplay-effect-stability`: 定义 GameplayEffect 稳定身份、叠层匹配、快照来源和替换语义，防止等价效果重复挂载和动态来源反馈爆炸。

### 修改功能

## 影响

- `UnityProject/Assets/PFGAS/Runtime/GAS/Effects/GameplayEffect.cs`
- `UnityProject/Assets/PFGAS/Runtime/GAS/Effects/GameplayEffectSpec.cs`
- `UnityProject/Assets/PFGAS/Runtime/GAS/Effects/GameplayEffectStackingResolver.cs`
- `UnityProject/Assets/PFGAS/Runtime/GAS/Effects/GameplayEffectContainer.cs`
- `UnityProject/Assets/PFGAS/Runtime/GAS/Effects/GameplayEffectMagnitudeSpec.cs`
- `UnityProject/Assets/PFGAS/Tests/Runtime/PFGASSamples.cs`
- `UnityProject/Assets/PFGAS/Tests/Runtime/GameplayEffectRuntimeTests.cs`
- 可能影响后续 Luban/配置表导入 Effect 时的 ID 字段映射。
