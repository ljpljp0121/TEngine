## 上下文

AttributeGraph 当前把单个属性拆成 `BaseValue` 和 `CurrentValue`。`BaseValue` 是 Instant/Periodic modifier 和外部 `SetBaseValue` 的落点；`CurrentValue` 是 `BaseValue + Ongoing modifiers` 经过 `IAttributeEvaluator` 后的最终值。

这个模型对攻击力、MaxHP、移动速度等普通属性足够清晰，但 HP、MP、体力这类资源属性还需要约束 BaseValue。当前 HP 只通过 `ClampMaxAttributeEvaluator(MaxHP)` 约束 CurrentValue，导致治疗能把 `HP.BaseValue` 推高到 MaxHP 之上，形成不可见的过量血池。

项目里已经有 `IAttributeEvaluator`、`AttributeRule`、AttributeGraph 拓扑依赖和 `AttributeChange` 事件。这个变更应复用这些机制，不把资源属性规则塞进 GameplayEffect，也不为 HP 增加到处调用的专用 API。

## 目标 / 非目标

**目标：**
- 增加 BaseValue 写入后的后处理能力，使 HP 这类资源属性能够把 BaseValue 约束到 `[0, MaxHP.Current]`。
- 将现有 `IAttributeEvaluator` 重命名为 CurrentValue processor，准确表达其职责。
- 让 BaseValue processor 的依赖参与 AttributeGraph 拓扑与脏传播，保证 MaxHP 变化能触发 HP.BaseValue 重新规范化。
- 保持 GameplayEffect modifier 的使用方式简单：伤害、治疗、DoT、再生仍可以表现为 HP 的 Add modifier，最终由属性规则规整。

**非目标：**
- 不引入 UE GAS Meta Attribute、Execution capture 或复杂伤害结算管线。
- 不把 GameplayEffect 扩展成资源结算系统。
- 不改变 GameplayEffect stacking、duration、capture policy 的既有语义。
- 不在本变更中迁移 Attribute 数据来源或 Luban 表结构。

## 决策

### 决策 1: 新增 BaseValue processor，与 CurrentValue processor 平级

新增接口：

```csharp
public interface IAttributeBaseValueProcessor
{
    IReadOnlyList<PFAttributeId> Dependencies { get; }

    float Process(
        AttributeGraphContext context,
        PFAttributeId attributeId,
        float proposedBaseValue);
}
```

现有 `IAttributeEvaluator` 重命名为：

```csharp
public interface IAttributeCurrentValueProcessor
{
    IReadOnlyList<PFAttributeId> Dependencies { get; }

    float Process(
        AttributeGraphContext context,
        PFAttributeId attributeId,
        float rawCurrentValue);
}
```

理由：BaseValue 和 CurrentValue 是两条不同生命周期。BaseValue processor 处理写入约束，CurrentValue processor 处理最终计算，两者都可以依赖其他属性，都应参与拓扑验证。

替代方案是继续只使用 CurrentValue processor。这个方案无法修复隐藏 Base 血池，因为它不改 BaseValue。

### 决策 2: AttributeRule 同时持有 BaseValue 和 CurrentValue processor

`AttributeRule` 增加 BaseValue processor 参数，默认使用 no-op processor。现有 current processor 参数从 `evaluator` 重命名为 `currentValueProcessor`。

内置类型命名调整：
- `DefaultAttributeEvaluator` -> `DefaultAttributeCurrentValueProcessor`
- `ClampMaxAttributeEvaluator` -> `ClampMaxCurrentValueProcessor`
- `ClampMinAttributeEvaluator` -> `ClampMinCurrentValueProcessor`
- `ClampRangeAttributeEvaluator` -> `ClampRangeCurrentValueProcessor`
- `FormulaAttributeEvaluator` -> `FormulaCurrentValueProcessor`

新增 BaseValue processor：
- `DefaultAttributeBaseValueProcessor`
- `ClampBaseValueProcessor` 或更具体的 `ClampBaseValueToAttributeRangeProcessor`

理由：命名变更虽然是 BREAKING，但能避免长期混淆。当前系统还处于 PFGAS 内部迭代阶段，趁接口面较小修正命名更划算。

### 决策 3: BaseValue processor 参与拓扑依赖和脏传播

AttributeGraph 节点需要同时保存：
- BaseValue processor
- CurrentValue processor
- Base processor dependencies
- Current processor dependencies

拓扑边统一来源于 processor dependencies 和 Modifier dependencies。若 `HP.BaseValueProcessor` 依赖 `MaxHP`，则 MaxHP 变化必须使 HP 进入 dirty set。

重算顺序采用现有拓扑顺序扩展：
1. 对 dirty 节点按拓扑顺序处理。
2. 先对节点当前 BaseValue 执行 BaseValue processor。
3. 若 BaseValue 被规范化，记录 AttributeChange，并继续使用规范化后的 BaseValue 计算 raw current。
4. 再计算 raw current 并执行 CurrentValue processor。
5. 最终统一发布 AttributeChange / AttributesChanged。

理由：MaxHP 先算出 CurrentValue，HP 后处理才能读取正确上限。把 BaseValue processor 纳入同一拓扑比额外事件监听更可预测。

### 决策 4: HP 使用 Base 和 Current 两层 clamp

生成的 HP 规则应表达：
- BaseValue processor: `HP.BaseValue = clamp(HP.BaseValue, 0, MaxHP.CurrentValue)`
- CurrentValue processor: `HP.CurrentValue = min(rawCurrentValue, MaxHP.CurrentValue)`，并保留静态 MinValue 0。

理由：Base clamp 防止隐藏血池；Current clamp 仍用于 Ongoing modifier、护盾类临时加成和最终读取安全。

### 决策 5: 示例继续使用简单 GameplayEffect modifier

瞬时伤害、治疗、周期伤害和再生示例仍使用 HP Add modifier，不改成 Execution。变更后的 AttributeGraph 会在 modifier 落地后规范化 HP BaseValue。

理由：这保留了 GameplayEffect 的简单性，也验证属性系统能承接资源约束。

## 风险 / 权衡

- [风险] 重命名 `IAttributeEvaluator` 会破坏现有引用。 -> 通过集中重命名 Runtime、Editor 生成器、测试和示例，并在任务中明确搜索旧名。
- [风险] BaseValue processor 依赖加入拓扑后可能引入新循环。 -> 复用现有拓扑循环检测，Base 和 Current processor dependencies 都必须验证。
- [风险] BaseValue processor 修改 BaseValue 可能导致事件顺序变化。 -> 在同一次 mutation transaction 内记录旧值和最终值，只发布最终 AttributeChange。
- [风险] HP 的 Base clamp 会改变当前示例输出。 -> 更新样例断言和 UI 文案，明确 HP Base/Current 不再允许出现过量隐藏血池。
