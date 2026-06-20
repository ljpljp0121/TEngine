## 1. 迁移前盘点

- [ ] 1.1 搜索并记录 Runtime、Editor、Tests 中对 `PFTagGenerated`、`PFAttributeGenerated`、`PFTagId.*`、`PFAttributeId.*` 的引用点
- [ ] 1.2 检查场景、Prefab、ScriptableObject 和测试资源中是否序列化了 `PFTagId` 或 `PFAttributeId` 具体枚举成员
- [ ] 1.3 记录当前 `Assets/PFGAS/Generated/**`、`Assets/PFGAS/Runtime/Gen/**` 的职责和可再生成来源
- [ ] 1.4 确认当前 `com.peifeng.pfgas.Runtime.asmdef`、`com.peifeng.pfgas.Editor.asmdef`、`com.peifeng.pfgas.Gen.asmdef` 的引用关系

## 2. Runtime 边界拆分

- [ ] 2.1 在 `PFGAS.Runtime` 中提供非项目生成的 `PFTagId` 类型定义
- [ ] 2.2 在 `PFGAS.Runtime` 中提供非项目生成的 `PFAttributeId` 类型定义
- [ ] 2.3 移除 Runtime 对 `PFTagGenerated` 静态构造或具体生成类的硬依赖
- [ ] 2.4 为 Tag 层级注册提供 Runtime registry API，并在未注册时返回清晰错误
- [ ] 2.5 为 Attribute 规则注册提供 Runtime registry/provider API，并在未注册时返回清晰错误
- [ ] 2.6 确保 `PFGAS.Runtime` 不引用 Luban、TEngine、Excel 读写库或项目生成程序集

## 3. GameProto 与项目生成目录

- [x] 3.1 确认 `Assets/GameScripts/HotFix/GameProto` 的 asmdef、热更设置和现有 `LubanLib` 可以承载 Luban 生成配置代码
- [ ] 3.2 创建 PFGAS 适配默认目录结构 `Assets/PFGASGenerated/PFGAS` 和必要的 `Assets/PFGASGenerated/Adapters`
- [x] 3.3 将 `Assets/PFGAS/Generated/**` 的 Luban 生成代码职责迁移到 `Assets/GameScripts/HotFix/GameProto`
- [x] 3.4 将依赖 TEngine 的 loader 迁移到 `GameProto` 或项目 adapter 程序集
- [ ] 3.5 创建或生成 `PFGASGenerated.asmdef`，并引用 `com.peifeng.pfgas.Runtime` 和 `GameProto`
- [x] 3.6 确认删除 `Assets/PFGAS/Generated` 后 PFGAS 包本体不再需要该目录

## 4. 生成器调整

- [ ] 4.1 修改 Tag 代码生成器默认输出路径为 `Assets/PFGASGenerated/PFGAS`
- [ ] 4.2 修改 Attribute 代码生成器默认输出路径为 `Assets/PFGASGenerated/PFGAS`
- [ ] 4.3 调整 Tag 生成输出，使项目具体值以生成常量和注册表形式存在，而不是定义 Runtime 所需类型
- [ ] 4.4 调整 Attribute 生成输出，使项目具体值和规则注册位于项目生成层
- [ ] 4.5 调整 Luban 生成配置代码输出路径为 `Assets/GameScripts/HotFix/GameProto`
- [ ] 4.6 让生成器创建或刷新项目生成 asmdef，并避免默认输出到 `Assets/PFGAS`
- [ ] 4.7 将编辑器配置资产默认创建位置迁移到项目侧配置目录

## 5. 调用点迁移

- [ ] 5.1 将测试和示例中 `PFTagId.X` 调用迁移到项目生成的 Tag 常量入口
- [ ] 5.2 将测试和示例中 `PFAttributeId.X` 调用迁移到项目生成的 Attribute 常量入口
- [ ] 5.3 在游戏或测试启动路径中显式调用项目生成层初始化入口
- [ ] 5.4 删除旧的包内生成路径引用和兼容说明，确保迁移后没有旧副本参与编译

## 6. 验证

- [ ] 6.1 删除或临时移走 `Assets/PFGASGenerated`，验证 `PFGAS.Runtime` 和 `PFGAS.Editor` 仍能编译
- [ ] 6.2 重新运行生成流程，验证 `Assets/PFGASGenerated/PFGAS` 和 `Assets/GameScripts/HotFix/GameProto` 中的生成代码可完整恢复
- [ ] 6.3 运行 PFGAS Runtime 测试，验证 Tag、Attribute、Effect 相关行为保持一致
- [ ] 6.4 验证非 TEngine loader 可以初始化 PFGAS 生成数据，或至少保留无 TEngine 的扩展入口
- [ ] 6.5 检查 `Assets/PFGAS` 下不再出现项目数据、Luban 输出、JSON/bytes 或 TEngine loader

## 7. 文档

- [ ] 7.1 更新 PFGAS 配置/生成工作流文档，说明包内与项目侧目录边界
- [ ] 7.2 在文档中记录程序集依赖方向：Runtime 被生成层依赖，Runtime 不依赖生成层
- [ ] 7.3 在迁移说明中列出 `PFTagId.X` 和 `PFAttributeId.X` 到新生成常量入口的替换方式
