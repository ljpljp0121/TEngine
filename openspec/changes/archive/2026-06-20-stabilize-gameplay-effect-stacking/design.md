## 上下文

PFGAS 当前的 `GameplayEffectStackingResolver` 通过 `ReferenceEquals(activeEffect.Effect, spec.Effect)` 判断两个 active effect 是否属于同一效果。这个实现只在调用方复用同一个 `GameplayEffect` 对象时生效；当能力系统、工厂方法或后续配置表每次构造等价的新实例时，Stack/Refresh/Replace 会被绕过。

`DynamicWhileActive + SourceAttribute` 的实现通过监听 source AttributeChanged，在目标 Effect Tick 时重建目标上的 ModifierSource。这个机制适合单向动态增益，但它不在单个 AttributeGraph 的拓扑检查范围内，因此 A 影响 B、B 又影响 A 的跨单位反馈环不会被提前发现。

Replace/ReplaceOldest 当前在新效果创建流程后段才移除旧效果。若新效果的 magnitude 读取 target 当前值，旧效果仍可能参与新效果的捕获或 modifier 计算，导致替换语义和实际数值不一致。

## 目标 / 非目标

**目标：**

- 使用稳定 `EffectId` 作为 GameplayEffect 的运行时身份。
- 让 Stack/Refresh/Replace/ReplaceOldest 使用 `EffectId` 匹配同一效果，而不是对象引用。
- 让同一个 Effect 配置被多次 new 出来时仍能正确叠层、刷新或替换。
- 将示例中的持续伤害/中毒模型收敛为同一个 EffectId 的叠层效果，来源属性默认快照。
- 修正 Replace/ReplaceOldest，使旧效果不参与新效果的 target-current 计算，同时保留失败回滚。
- 用测试覆盖等价新实例、不同 source scope、快照来源、替换顺序和旧效果回滚。

**非目标：**

- 不在本变更中实现完整跨 CombatUnit 动态依赖图或固定点求解。
- 不引入 StackGroupId、多来源逐层独立伤害、最强层覆盖等高级叠层策略。
- 不重写 AttributeGraph 聚合模型。
- 不迁移 Luban Effect 配置表；仅为后续映射预留稳定 ID 语义。

## 决策

### 决策 1：新增稳定 EffectId

`GameplayEffect` 将只持有稳定 `EffectId`，不再保留 `Name`，也不做 `name` 或配置行 ID 回退。构造器第一参数就是必填 `EffectId`；空白 `EffectId` 会被拒绝。显示文本、策划备注或配置描述若需要，应留在配置层的 DisplayName/Desc 等字段，不进入 Runtime 身份模型。

替代方案是继续要求调用方复用同一个 `GameplayEffect` 实例。这个方案对测试方便，但对工厂、热更、配置表和网络同步都脆弱，因此不采用。

### 决策 2：Stacking 匹配按 EffectId

`GameplayEffectStackingResolver.FindMatchingActiveEffect` 将比较 `EffectId`。`GameplayEffectStackingScope.BySourceAndTarget` 仍然要求 source 相同；`ByTarget` 则允许同一目标上来自不同 source 的同一 EffectId 合并。

这能支持“毒箭、毒雾、毒刃都施加同一个中毒 EffectId”的简单模型：目标身上只有一条中毒 active effect，重复施加增加 StackCount 并刷新持续时间。

### 决策 3：来源属性默认快照，动态来源保留但作为显式高级行为

对持续伤害这类玩法，source 属性应在应用时快照，例如 `source.MagicAttack * coefficient`。这样后续 source 属性变化不会反向驱动已挂载的持续效果，也不会形成跨单位反馈环。

`DynamicWhileActive + SourceAttribute` 不会被移除，但示例和测试会强调它是显式动态行为，不应作为持续伤害默认建模方式。

### 决策 4：Replace/ReplaceOldest 先隔离旧效果再计算新效果

替换语义不能只是简单交换两行代码。实现应在进入替换路径时，把旧 active effect 从目标属性/标签/订阅中临时移除或隔离，然后计算并提交新 effect。若新 effect 准备、触发器或提交失败，必须恢复旧 effect 的 modifier、tags、active record、subscriptions 和 timing。

可选实现路径：

- 在替换路径中引入专用 transaction，支持 temporary cleanup 和 restore。
- 或把 Prepare/Commit 分层，使 target-current magnitude 的解析发生在旧效果移除之后。

### 决策 5：不引入 StackGroupId

当前需求可以通过复用同一个 `EffectId` 完成。不同 Ability 应用同一个中毒 Effect；Ability 差异通过 level、payload 或后续 set-by-caller 参数表达。StackGroupId 会增加设计面，等真正出现“不同 EffectId 共享层数但保留不同身份”的需求时再引入。

## 风险 / 权衡

- [风险] 移除 `Name` 和默认回退会要求所有构造调用显式传入稳定身份。
  [缓解] 本变更同步迁移 Runtime、测试和样例调用；后续配置生成层必须提供非空且唯一的 `EffectId`。

- [风险] Replace 路径回滚复杂，可能在失败时丢失旧效果订阅或标签。  
  [缓解] 增加失败路径测试，覆盖 modifier、tag、active count、dynamic source subscription 和 trigger rollback。

- [风险] 将示例改为快照来源后，动态光环示例覆盖减少。  
  [缓解] 保留单向动态来源测试，但新增互相动态光环风险测试或文档说明，避免把动态来源当作默认持续伤害方案。

- [风险] EffectId 匹配改变现有依赖对象引用的调用行为。  
  [缓解] 这是预期修复；对确实需要独立多实例的效果，应配置不同 EffectId 或使用 Independent stacking。
