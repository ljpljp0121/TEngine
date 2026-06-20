## 1. EffectId 身份模型

- [x] 1.1 为 `GameplayEffect` 增加稳定 `EffectId` 属性，并确保构造时非空归一化
- [x] 1.2 移除 `GameplayEffect.Name` 和 `name` 回退兼容，构造时必须显式提供 `EffectId`
- [x] 1.3 增加测试覆盖相同 `EffectId`、不同对象实例被识别为同一配置效果
- [x] 1.4 增加测试覆盖空白 `EffectId` 的拒绝或归一化行为

## 2. 叠层匹配修正

- [x] 2.1 将 `GameplayEffectStackingResolver` 的同效果匹配从 `ReferenceEquals` 改为 `EffectId`
- [x] 2.2 保留 `GameplayEffectStackingScope.BySourceAndTarget` 的 source 匹配行为
- [x] 2.3 增加 Stack 测试：相同 `EffectId`、不同对象实例应共享一个 active effect 并增加 StackCount
- [x] 2.4 增加 Refresh 测试：相同 `EffectId`、不同对象实例应返回已有 handle 并刷新时长
- [x] 2.5 增加 BySourceAndTarget 测试：不同 source 的相同 `EffectId` 仍保持独立 stack

## 3. 快照来源和示例建模

- [x] 3.1 调整中毒/持续伤害示例，使不同能力复用同一个中毒 `EffectId`
- [x] 3.2 将依赖 source 属性的持续伤害示例改为 `SnapshotOnApply`
- [x] 3.3 增加测试覆盖 source 属性变化后，快照来源的已激活效果不改变
- [x] 3.4 保留单向 `DynamicWhileActive + SourceAttribute` 测试，确认同一 Tick 前多次 source 变化仍只重建一次
- [x] 3.5 增加或更新互相动态光环风险测试/示例说明，避免把动态来源作为推荐持续伤害模型

## 4. Replace / ReplaceOldest 替换语义

- [x] 4.1 梳理 `GameplayEffectContainer` 的 Prepare/Commit 流程，定位 target-current magnitude 的解析时机
- [x] 4.2 在 Replace/ReplaceOldest 路径中隔离旧 active effect 后再计算新效果的 target-current magnitude
- [x] 4.3 为替换路径增加回滚能力，失败时恢复旧 modifier、tag、active record、subscriptions 和 timing
- [x] 4.4 增加测试覆盖 Replace 新效果不捕获旧效果对 target 当前值的贡献
- [x] 4.5 增加测试覆盖 Replace/ReplaceOldest 新效果失败后旧效果完整恢复

## 5. 回归验证

- [x] 5.1 运行 PFGAS Runtime 相关测试，确认现有 GameplayEffect、AttributeGraph 和示例测试通过
- [x] 5.2 检查 `PFGASSamples` 和 `PFGASSampleScenarioRunner` 中重复施加效果的行为符合新的 EffectId 语义
- [x] 5.3 检查 Runtime API 变更是否需要更新文档或后续 Luban Effect 表字段映射说明
