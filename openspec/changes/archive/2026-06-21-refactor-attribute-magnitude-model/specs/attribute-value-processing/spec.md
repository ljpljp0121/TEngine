## 新增需求

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

## 修改需求

## 移除需求
