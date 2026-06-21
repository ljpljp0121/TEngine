## 1. 现状盘点和旧模型定位

- [x] 1.1 搜索 `IAttributeMagnitude`、`AttributeMagnitude` 工厂和所有内置 Magnitude 实现，列出现有调用点
- [x] 1.2 确认 `AttributeModifier` 继续保持 `AttributeId + Operation + Magnitude` 单 Magnitude 结构
- [x] 1.3 识别 Runtime、Editor、Tests 中依赖 `Add/Subtract/Multiply/Divide/Min/Max/Clamp` 的代码
- [x] 1.4 确认后续删除旧表达式节点不会影响 BaseValue/CurrentValue processor 的职责边界

## 2. 收敛 Magnitude 运行时模型

- [x] 2.1 删除或隐藏 `AttributeMagnitude.Add/Subtract/Multiply/Divide/Min/Max/Clamp` 等通用表达式工厂
- [x] 2.2 删除或迁移 `BinaryAttributeMagnitude` 和 `ClampAttributeMagnitude`
- [x] 2.3 保留并清理固定值 Magnitude，使其成为正式内置类型
- [x] 2.4 新增线性缩放 Magnitude，表达 `baseValue * K + B`
- [x] 2.5 新增属性读取 Magnitude，表达 `attributeValue * K + B` 并显式声明依赖属性
- [x] 2.6 确认自定义 Magnitude 可以通过实现 `IAttributeMagnitude` 表达复杂公式
- [x] 2.7 确保新增和调整的代码注释全部使用中文

## 3. AttributeGraph 集成和依赖传播

- [x] 3.1 确认 Modifier 依赖边仍从 `modifier.Magnitude.Dependencies` 建立
- [x] 3.2 确认属性读取 Magnitude 的依赖参与拓扑排序和循环检测
- [x] 3.3 确认依赖属性变化会触发使用该 Magnitude 的目标属性重算
- [x] 3.4 保留 `EvaluateMagnitude` 对非有限数值的校验和清晰错误
- [x] 3.5 清理因旧表达式树移除后不再需要的依赖合并辅助代码

## 4. 测试和示例迁移

- [x] 4.1 迁移所有使用旧 `AttributeMagnitude` 表达式工厂的 Runtime 测试
- [x] 4.2 增加固定值 Magnitude 的 Modifier 行为测试
- [x] 4.3 增加线性缩放 Magnitude 的 Modifier 行为测试
- [x] 4.4 增加属性读取 Magnitude 的依赖重算测试
- [x] 4.5 增加自定义 Magnitude 复杂公式测试，覆盖多个依赖属性
- [x] 4.6 更新示例或测试辅助代码，避免继续展示旧表达式树写法

## 5. Editor 和未来配置边界清理

- [x] 5.1 检查 Editor 配置代码中是否引用旧 Magnitude 工厂或旧表达式节点
- [x] 5.2 确认 Editor 侧命名和文案不再暗示 Magnitude 可以任意组合表达式树
- [x] 5.3 为后续 Luban Excel 映射保留“Magnitude 类型 + 参数”的清晰边界

## 6. 验证和收尾

- [x] 6.1 搜索并移除旧表达式 API 的残留引用
- [x] 6.2 运行 PFGAS Runtime 相关测试
- [x] 6.3 运行 PFGAS Editor/EditMode 相关测试
- [x] 6.4 检查 Unity Console 是否存在新增错误
- [x] 6.5 检查 OpenSpec 状态，确认变更产出物完整并准备进入 apply
