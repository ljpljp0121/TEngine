## 新增需求

### 需求:GameplayEffect 必须具有稳定身份
每个 `GameplayEffect` 必须暴露稳定的 `EffectId`，用于识别同一配置效果。`EffectId` 必须非空，并且不得依赖运行时对象引用。

#### 场景:构造等价效果实例
- **当** 两个 `GameplayEffect` 实例使用相同的 `EffectId` 构造
- **那么** 系统必须把它们视为同一个配置效果

#### 场景:拒绝空身份
- **当** 创建 `EffectId` 为空或仅包含空白字符的 `GameplayEffect`
- **那么** 系统必须拒绝该效果或归一化为明确的非空身份

### 需求:叠层匹配必须使用 EffectId
GameplayEffect 的 Stack、Refresh、Replace 和 ReplaceOldest 匹配必须基于 `EffectId`，并继续遵守 stacking scope 对 source/target 的约束。

#### 场景:不同实例共享叠层
- **当** 目标连续应用两个不同对象实例但 `EffectId` 相同、stacking 为 Stack 的效果
- **那么** 目标必须只保留一个 active effect，并增加该 active effect 的 StackCount

#### 场景:不同实例刷新持续时间
- **当** 目标已有一个 `EffectId` 相同的 active effect，并再次应用 stacking 为 Refresh 的等价效果实例
- **那么** 系统必须返回已有 handle，并刷新该 active effect 的持续时间

#### 场景:按 source 分离叠层
- **当** stacking scope 为 BySourceAndTarget，两个不同 source 对同一 target 应用相同 `EffectId` 的效果
- **那么** 系统必须为不同 source 保留独立 active effect 或独立 stack

### 需求:同一持续状态应复用同一个 EffectId
不同 Ability 若要施加同一种持续状态，必须能够复用同一个 `EffectId` 的 GameplayEffect。重复施加同一持续状态时，系统必须按该 Effect 的 stacking 规则更新已有 active effect，而不是创建等价的新持续状态。

#### 场景:多个能力施加中毒
- **当** 毒箭、毒雾和毒刃都对目标应用 `EffectId` 为 `Poison` 的效果
- **那么** 目标必须只维护一条 Poison active effect，并按 Poison 的 stacking 规则叠层和刷新

#### 场景:独立效果需要不同身份
- **当** 两个效果不应共享叠层或刷新语义
- **那么** 它们必须使用不同的 `EffectId`

### 需求:持续伤害来源属性必须支持快照
持续伤害或持续状态若依赖 source 属性，系统必须支持在应用时捕获 source 属性快照，使后续 source 当前值变化不会自动改变已应用效果。

#### 场景:快照魔攻中毒
- **当** source 以魔攻快照创建 Poison，并在 Poison 激活后提高魔攻
- **那么** 已激活 Poison 的周期伤害必须继续使用应用时捕获的魔攻值

#### 场景:周期伤害按层数缩放
- **当** Poison active effect 的 StackCount 增加
- **那么** Poison 的周期伤害必须按 StackCount 缩放，并不得创建额外 Poison active effect

### 需求:动态来源必须保持显式高级行为
`DynamicWhileActive + SourceAttribute` 必须继续可用，但系统和示例不得把它作为持续伤害的默认建模方式。动态来源触发重建时，系统必须继续合并同一 active effect 在同一 Tick 前的多次 source 属性变化。

#### 场景:动态来源延迟重建
- **当** source 属性在同一帧内多次变化
- **那么** 目标效果必须在 Tick 前保持旧 modifier 值，并在 Tick 时使用最新 source 值重建一次

#### 场景:互相动态来源风险被覆盖
- **当** 两个 CombatUnit 互相施加依赖对方当前值的动态 Ongoing 效果
- **那么** 测试或示例必须展示该模式存在反馈风险，并避免把它作为推荐持续伤害模型

### 需求:替换语义必须排除旧效果的目标当前值贡献
Replace 和 ReplaceOldest 应用新效果时，被替换的旧 active effect 禁止参与新效果的 target-current magnitude 计算。若新效果应用失败，旧效果必须恢复为替换前状态。

#### 场景:Replace 不捕获旧效果贡献
- **当** target 已有一个增加 MaxHP 的效果，并应用同 `EffectId` 且 stacking 为 Replace 的新效果，新效果的 magnitude 读取 target 当前 MaxHP
- **那么** 新效果必须基于移除旧效果后的 target 当前值计算

#### 场景:Replace 失败恢复旧效果
- **当** Replace 或 ReplaceOldest 移除旧效果后，新效果在准备、触发器或提交阶段失败
- **那么** 系统必须恢复旧 active effect、旧 modifier、旧 tag、旧订阅和旧时间状态

### 需求:EffectId 行为必须可测试
Runtime 测试必须覆盖稳定 EffectId、重复创建等价效果、source scope、快照来源、Replace 顺序和失败回滚。

#### 场景:测试覆盖等价新实例
- **当** 测试连续应用相同 `EffectId` 但不同对象实例的效果
- **那么** 测试必须断言 Stack/Refresh/Replace 行为按 EffectId 生效

#### 场景:测试覆盖快照来源
- **当** 测试在效果应用后改变 source 属性
- **那么** 测试必须断言快照来源效果不随 source 当前值变化

## 修改需求

## 移除需求
