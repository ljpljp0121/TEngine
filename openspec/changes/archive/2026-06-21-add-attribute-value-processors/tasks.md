## 1. 接口和命名

- [x] 1.1 新增 `IAttributeBaseValueProcessor`、默认 no-op BaseValue processor，以及 BaseValue clamp 相关内置 processor
- [x] 1.2 将 `IAttributeEvaluator` 重命名为 `IAttributeCurrentValueProcessor`，并同步更新方法名为 `Process`
- [x] 1.3 重命名内置 CurrentValue processor 类型，保留其原有 CurrentValue 计算行为
- [x] 1.4 更新 `AttributeRule` 构造参数和属性，使其同时持有 BaseValue processor 与 CurrentValue processor

## 2. AttributeGraph 集成

- [x] 2.1 扩展 `AttributeNode`，分别保存 BaseValue processor、CurrentValue processor 及其依赖集合
- [x] 2.2 将 BaseValue processor 依赖纳入属性拓扑边、循环检测和依赖校验
- [x] 2.3 在 `SetBaseValue`、`AddBaseValue` 和 `ApplyBaseModifiers` 的 mutation flow 中执行 BaseValue processor
- [x] 2.4 调整重算顺序，确保依赖属性 CurrentValue 更新后，依赖它的 BaseValue processor 会重新规范化 BaseValue
- [x] 2.5 确保 `AttributeChange` 记录的是同一 transaction 中 BaseValue 和 CurrentValue 的最终变化

## 3. 生成器和示例规则

- [x] 3.1 更新 Attribute 编辑器/代码生成器输出 CurrentValue processor 新命名
- [x] 3.2 为 HP 生成 BaseValue clamp 到 `[0, MaxHP.CurrentValue]` 的 processor 配置
- [x] 3.3 更新当前生成文件或测试生成入口，使示例 HP 使用 BaseValue 和 CurrentValue 双处理器
- [x] 3.4 更新示例 UI 文案或日志中关于 HP Base/Current 的说明，移除隐藏过量血池行为

## 4. 测试覆盖

- [x] 4.1 新增 Runtime 测试：满血治疗后 HP.BaseValue 和 HP.CurrentValue 不超过 MaxHP
- [x] 4.2 新增 Runtime 测试：伤害按可见 HP 生效，且 HP 不低于 0
- [x] 4.3 新增 Runtime 测试：MaxHP 降低会重新 clamp HP.BaseValue
- [x] 4.4 新增 Runtime 测试：MaxHP 提高不会自动治疗
- [x] 4.5 新增 Runtime 测试：BaseValue processor 循环依赖会被拒绝
- [x] 4.6 更新样例 summary 或场景验证，确认周期再生不会堆出过量 HP Base

## 5. 清理和验证

- [x] 5.1 搜索并移除 Runtime、Editor、Tests 中旧的 `IAttributeEvaluator` 和 `*AttributeEvaluator` 引用
- [x] 5.2 运行 PFGAS Runtime/EditMode 相关测试，确认 AttributeGraph、GameplayEffect 和示例行为通过
- [x] 5.3 检查 `openspec` 状态，确认变更产出物完整并准备进入 apply
