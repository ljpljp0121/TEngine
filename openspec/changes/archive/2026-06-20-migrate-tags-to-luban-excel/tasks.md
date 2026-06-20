## 1. 调查和路径收束

- [x] 1.1 核对 `PFTag.xlsx`、`Defines/tag.xml`、`luban.conf` 和现有 Luban 输出，确认 Tag schema 来源和字段定义一致
- [x] 1.2 梳理当前 `gen_code_*_to_project.*` 脚本、Unity 菜单入口和实际生成目录，确定本变更使用的唯一导出入口
- [x] 1.3 选择 Editor-only Excel 读写方案，并确认许可证、Unity 兼容性和 asmdef 引用边界
- [x] 1.4 确定项目侧 PFGAS 适配生成目录和 asmdef 名称

## 2. Tag Excel 数据层

- [x] 2.1 实现 Tag Excel 行模型，覆盖 `Id`、`ParentId`、`Name`、`Desc` 和派生完整路径
- [x] 2.2 实现 Tag Excel 读取服务，保留 Luban 表头、类型、分组和注释行元数据
- [x] 2.3 实现 Tag Excel 写入服务，支持保存前备份和文件锁定错误提示
- [x] 2.4 实现 Excel 数据到树模型、树模型到 Excel 行数据的双向转换

## 3. Tag 校验

- [x] 3.1 校验 Tag ID 唯一且非空
- [x] 3.2 校验 `ParentId` 指向有效节点或根节点标记
- [x] 3.3 校验同父级短名唯一且可生成合法代码标识符
- [x] 3.4 校验父子关系不存在循环
- [x] 3.5 校验生成常量名或枚举名不会冲突
- [x] 3.6 将校验结果接入保存和适配生成流程

## 4. Excel-backed Tag 树编辑器

- [x] 4.1 将 `PFTagTreeWindow` 从 `PFTreeConfig` ScriptableObject 数据源切换为 Excel 数据源
- [x] 4.2 保留现有树形新增、删除、重命名、移动、搜索、展开和折叠体验
- [x] 4.3 增加刷新按钮，从外部修改后的 Excel 重新加载树
- [x] 4.4 增加保存按钮，将树编辑结果写回 `PFTag.xlsx`
- [x] 4.5 增加一键保存并导出按钮，串联保存、Luban 导出、PFGAS 适配生成和 AssetDatabase 刷新

## 5. Luban 导出链路

- [x] 5.1 修正或替换 Unity 菜单中指向不存在 lazyload 脚本的导表入口
- [x] 5.2 统一 Windows 和 shell 脚本的 Luban C# 输出目录、JSON 输出目录和命名约定
- [x] 5.3 验证 Luban 导出能生成 `pfgas_tbpftag.json` 和 `GameConfig.PFGAS.TbPFTag`
- [x] 5.4 将导出失败、脚本缺失和 Luban 进程错误反馈到 Unity Console

## 6. PFGAS Tag 适配生成层

- [x] 6.1 实现 Tag 适配生成器，从有效 Tag 数据生成 PFGAS Runtime 可消费的注册数据
- [x] 6.2 生成项目侧 Tag 常量或兼容命名，并统一测试和示例使用方式
- [x] 6.3 生成 Tag 层级、父子关系和显示名注册代码
- [x] 6.4 生成或维护 PFGAS 适配程序集 asmdef，引用 `PFGAS.Runtime` 和项目 Luban 生成程序集
- [x] 6.5 确保重复生成在无输入变化时不产生无意义 diff

## 7. Runtime 注册切换

- [x] 7.1 从 `PFTagContainer` 和 `GameplayTagAggregator` 中移除对包内 `PFTagGenerated` 静态构造的硬依赖
- [x] 7.2 为 `TagHelper` 增加显式注册、清理和未注册诊断能力
- [x] 7.3 在项目启动流程或测试 bootstrap 中调用 PFGAS Tag 适配生成层注册入口
- [x] 7.4 保持 `HasTag`、`HasExactTag`、`IsOrUnder` 和显示名查询语义

## 8. 删除旧 ScriptableObject 链路

- [x] 8.1 删除或隐藏旧 `PFTagConfig.asset` 可编辑入口
- [x] 8.2 删除或替换旧 `PFTagConfig.cs` ScriptableObject 配置类型
- [x] 8.3 删除或替换旧 `PFTagCodeGenerator.cs` 中从 `PFTagConfig.asset` 生成运行时代码的逻辑
- [x] 8.4 删除包内旧 `Runtime/Gen/PFTagGenerated.cs` 生成物
- [x] 8.5 搜索并移除旧 Tag ScriptableObject 链路的残留引用

## 9. 测试和验证

- [x] 9.1 添加或更新 Editor 测试，覆盖 Excel 读取、写入、刷新、文件锁和校验失败
- [x] 9.2 更新 Runtime 测试和示例，使用新的 Tag 初始化和常量访问方式
- [x] 9.3 验证手改 Excel 后编辑器刷新可见，编辑器保存后 Excel 内容真实变化
- [x] 9.4 验证一键导出后 Luban 输出和 PFGAS 适配生成代码一致
- [x] 9.5 验证删除旧 ScriptableObject 配置后项目不再需要旧 Tag 链路
- [x] 9.6 运行相关 Unity EditMode/Runtime 测试，确认 Tag 查询和 GameplayEffect Tag 行为通过
