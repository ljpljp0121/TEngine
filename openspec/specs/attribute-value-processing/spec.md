# attribute-value-processing 规范

## 目的
待定 - 由归档变更 add-attribute-value-processors 创建。归档后请更新目的。
## 需求
### 需求:属性必须支持 BaseValue 后处理
AttributeGraph 必须允许 AttributeRule 为属性配置 BaseValue 后处理器。BaseValue 后处理器必须在外部 `SetBaseValue`、`AddBaseValue`、Instant modifier 和 Periodic modifier 改变 BaseValue 后运行，并且最终保存的 BaseValue 必须使用后处理结果。

#### 场景:治疗不会形成隐藏血池
- **当** HP 的 BaseValue 后处理器将 HP 限制到 `[0, MaxHP.CurrentValue]`，MaxHP.CurrentValue 为 100，目标 HP 为 100，治疗效果对 HP 应用 `+20` BaseValue 变化
- **那么** HP.BaseValue 必须保持 100，HP.CurrentValue 必须保持 100

#### 场景:伤害按可见血量生效
- **当** HP.BaseValue 为 100，MaxHP.CurrentValue 为 100，伤害效果对 HP 应用 `-25` BaseValue 变化
- **那么** HP.BaseValue 必须变为 75，HP.CurrentValue 必须变为 75

#### 场景:负向变化不能低于下限
- **当** HP.BaseValue 为 10，MaxHP.CurrentValue 为 100，伤害效果对 HP 应用 `-25` BaseValue 变化
- **那么** HP.BaseValue 必须变为 0，HP.CurrentValue 必须变为 0

### 需求:BaseValue 后处理依赖必须参与属性重算
BaseValue 后处理器声明的依赖必须参与 AttributeGraph 的拓扑排序、循环检测和 dirty 传播。当依赖属性的最终 CurrentValue 改变时，依赖它的 BaseValue 后处理器必须重新运行。

#### 场景:MaxHP 降低会约束 HP BaseValue
- **当** HP.BaseValue 为 100，MaxHP.CurrentValue 从 100 降低到 60，并且 HP 的 BaseValue 后处理器依赖 MaxHP
- **那么** HP.BaseValue 必须被重新处理为 60，HP.CurrentValue 必须为 60

#### 场景:MaxHP 提高不会自动治疗
- **当** HP.BaseValue 为 50，MaxHP.CurrentValue 从 100 提高到 150，并且 HP 的 BaseValue 后处理器依赖 MaxHP
- **那么** HP.BaseValue 必须保持 50，HP.CurrentValue 必须保持 50

#### 场景:循环依赖被拒绝
- **当** 注册的 BaseValue 后处理器依赖形成属性循环
- **那么** AttributeGraph 必须拒绝注册或变更该处理器，并给出清晰错误

### 需求:CurrentValue 后处理器必须表达原 Evaluator 语义
系统必须将现有 `IAttributeEvaluator` 的职责命名为 CurrentValue 后处理器。CurrentValue 后处理器必须只负责将属性聚合后的 raw current value 转换为最终 CurrentValue，不得负责修改 BaseValue。

#### 场景:CurrentValue 后处理器限制最终值
- **当** HP.BaseValue 为 120，MaxHP.CurrentValue 为 100，并且 HP 的 CurrentValue 后处理器限制 HP 不超过 MaxHP
- **那么** HP.CurrentValue 必须为 100

#### 场景:CurrentValue 后处理器不修改 BaseValue
- **当** 属性只配置 CurrentValue 后处理器且 BaseValue 后处理器为默认 no-op，BaseValue 为 120
- **那么** CurrentValue 后处理器运行后 BaseValue 必须仍为 120

### 需求:AttributeRule 必须同时配置 BaseValue 和 CurrentValue 后处理器
AttributeRule 必须能够描述属性默认值、聚合模式、静态范围、BaseValue 后处理器和 CurrentValue 后处理器。未显式配置后处理器时，系统必须使用默认 no-op 处理器保持现有普通属性行为。

#### 场景:普通属性保持原行为
- **当** 攻击力属性未配置自定义 BaseValue 后处理器，也未配置自定义 CurrentValue 后处理器
- **那么** 攻击力 BaseValue 和 CurrentValue 必须按现有聚合规则计算，不得被资源属性规则影响

#### 场景:HP 同时使用两类后处理器
- **当** HP AttributeRule 配置 BaseValue 后处理器依赖 MaxHP，并配置 CurrentValue 后处理器依赖 MaxHP
- **那么** HP 的 BaseValue 和 CurrentValue 必须分别按各自后处理器结果写入和读取

### 需求:属性值处理行为必须可测试
Runtime 测试或示例必须覆盖 BaseValue 后处理、CurrentValue 后处理、依赖属性变化、以及 GameplayEffect modifier 与后处理器协作的行为。

#### 场景:示例不再显示过量 HP Base
- **当** 示例场景中单位 HP/MaxHP 为 100/100，并连续多次点击治疗
- **那么** UI 中 HP Base/Current 必须保持 `100 / 100`

#### 场景:周期再生不会过量堆叠 Base
- **当** 周期再生效果持续对满血单位应用 HP 正向 BaseValue 变化
- **那么** HP.BaseValue 必须保持不超过 MaxHP.CurrentValue

### 需求:Attribute Modifier 必须只持有一个 Magnitude

AttributeGraph 的 Modifier 模型必须保持一条 `AttributeModifier` 只持有一个 `IAttributeMagnitude`。一个 Modifier 禁止直接持有多个 Magnitude，也禁止直接负责组合多个 Magnitude 的计算结果。

#### 场景:固定值 Modifier

- **当** 一个 Modifier 的目标属性为 HP，Operation 为 Add，Magnitude 为固定值 10
- **那么** AttributeGraph 必须把它计算为对 HP 的一条 `+10` 修改

#### 场景:同一来源包含多条 Modifier

- **当** 一个 ModifierSource 同时包含修改 HP 和修改 Atk 的两条 Modifier
- **那么** 每条 Modifier 必须独立持有自己的 Magnitude，并分别作用到自己的目标属性

### 需求:Magnitude 必须使用有限计算类型

正式运行时模型中的 `IAttributeMagnitude` 必须由有限、可命名、可配置的计算类型表达。系统禁止要求配置侧通过通用二元表达式树组合 Magnitude。

#### 场景:固定值 Magnitude

- **当** 一个 Magnitude 类型表示固定值 25
- **那么** 它的 Evaluate 结果必须为 25
- **并且** 它的 Dependencies 必须为空

#### 场景:线性缩放 Magnitude

- **当** 一个 Magnitude 类型表示基础值 10，K 为 3，B 为 0.5
- **那么** 它的 Evaluate 结果必须为 30.5

#### 场景:属性读取 Magnitude

- **当** 一个 Magnitude 类型读取 Atk.CurrentValue，K 为 0.5，B 为 2，且 Atk.CurrentValue 为 20
- **那么** 它的 Evaluate 结果必须为 12
- **并且** 它的 Dependencies 必须包含 Atk

### 需求:复杂公式必须通过自定义 Magnitude 表达

复杂公式必须通过一个明确的自定义 Magnitude 类型表达。该类型必须封装公式实现，并显式声明它读取的属性依赖。

#### 场景:自定义 Magnitude 声明多个依赖

- **当** 一个自定义 Magnitude 读取 Atk 和 Level 两个属性
- **那么** 它的 Dependencies 必须同时包含 Atk 和 Level
- **并且** Atk 或 Level 变化时，依赖该 Magnitude 的目标属性必须进入重算流程

#### 场景:复杂公式不使用通用表达式树

- **当** 需要表达 `Atk * K + Level * B` 这类复杂公式
- **那么** 系统必须通过一个自定义 Magnitude 类型实现
- **并且** 禁止要求配置侧组合 `Add`、`Multiply` 等通用表达式节点

### 需求:旧通用表达式 API 必须退出正式模型

`AttributeMagnitude.Add/Subtract/Multiply/Divide/Min/Max/Clamp` 等通用表达式工厂不得作为正式运行时配置模型保留。现有调用点必须迁移到有限 Magnitude 类型或自定义 Magnitude。

#### 场景:旧表达式 API 不再被正式代码使用

- **当** 搜索 Runtime、Editor 和 Tests 中的 Magnitude 调用点
- **那么** 不应存在依赖旧通用表达式工厂完成正式行为的代码

#### 场景:旧表达式节点不参与配置映射

- **当** 后续编辑器或 Luban 配置需要表达 Magnitude
- **那么** 它们必须映射到有限 Magnitude 类型或自定义 Magnitude 类型
- **并且** 禁止映射到任意嵌套的二元表达式树

