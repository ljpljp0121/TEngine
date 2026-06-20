## 新增需求

### 需求:Tag Excel 必须是唯一权威源
系统必须把 Luban Tag Excel 表作为 Tag 业务数据的唯一权威源，禁止使用 `PFTagConfig.asset` 或其他 ScriptableObject 配置作为并行业务编辑源。

#### 场景:读取 Tag 权威源
- **当** 用户打开 Tag 树编辑器
- **那么** 编辑器必须从配置的 Luban Tag Excel 表读取 Tag 数据

#### 场景:禁止旧配置作为权威源
- **当** 项目中仍存在旧 `PFTagConfig.asset`
- **那么** Tag 树编辑器禁止从该资产加载可编辑 Tag 数据

### 需求:Tag 树编辑器必须读写 Luban Excel
系统必须提供 Unity Editor 内的 Tag 树编辑器，允许用户以树形方式编辑 Tag，并将保存结果写回同一份 Luban Excel 表。

#### 场景:保存树编辑结果
- **当** 用户在 Tag 树编辑器中新增、删除、重命名或移动 Tag 节点并点击保存
- **那么** 系统必须将变更写回 Luban Tag Excel 表，并保留 Luban 表头、类型、分组和注释行

#### 场景:刷新外部 Excel 修改
- **当** 用户在外部 Excel 程序中修改 Tag 表并在 Unity 中点击刷新
- **那么** Tag 树编辑器必须重新读取 Excel 并显示最新树结构

#### 场景:Excel 文件被占用
- **当** Tag 树编辑器保存时 Excel 文件被外部程序锁定
- **那么** 系统必须阻止部分写入并提示用户关闭占用文件后重试

### 需求:Tag Excel 必须在保存和生成前校验
系统必须在保存和生成 PFGAS Tag 适配代码前校验 Tag Excel 数据，禁止将无效树结构导出为运行时注册数据。

#### 场景:检测重复 ID
- **当** Tag 表中存在重复的 `Id`
- **那么** 校验必须失败并指出冲突的 ID

#### 场景:检测无效父级
- **当** 非根 Tag 的 `ParentId` 不存在
- **那么** 校验必须失败并指出缺失的父级 ID

#### 场景:检测同级重名
- **当** 同一个 `ParentId` 下存在重复的 `Name`
- **那么** 校验必须失败并指出冲突的完整路径

#### 场景:检测循环父级
- **当** Tag 表的父子关系形成循环
- **那么** 校验必须失败并指出参与循环的 Tag

#### 场景:检测生成名冲突
- **当** 不同 Tag 生成相同的代码常量名或枚举名
- **那么** 校验必须失败并要求用户修改 Tag 名称或路径

### 需求:Luban 导出必须生成 Tag 表代码和数据
系统必须能从 Tag Excel 和 Luban schema 导出运行时可加载的 Tag 数据以及 Luban C# 表类。

#### 场景:导出 Tag JSON 和表类
- **当** 用户运行 PFGAS 配置导出流程
- **那么** Luban 必须生成包含 Tag 数据的输出文件以及 `GameConfig.PFGAS.PFTag`、`GameConfig.PFGAS.TbPFTag` 等表类

#### 场景:导出入口明确
- **当** 用户在 Unity Editor 中触发一键保存并导出
- **那么** 系统必须按顺序保存 Excel、运行 Luban 导出、生成 PFGAS Tag 适配代码并刷新 AssetDatabase

### 需求:PFGAS 必须生成 Tag 运行时适配层
系统必须基于 Luban Tag 数据生成 PFGAS 专属适配代码，用于向 PFGAS Runtime 注册 Tag ID、层级、显示名和查询数据。

#### 场景:生成适配代码
- **当** Luban Tag 表数据有效并执行 PFGAS Tag 适配生成
- **那么** 系统必须生成项目侧 PFGAS Tag 适配代码，而不是从旧 ScriptableObject 配置生成 `PFTagGenerated.cs`

#### 场景:适配层引用方向
- **当** 检查生成的 PFGAS Tag 适配程序集
- **那么** 该程序集必须能够引用 `PFGAS.Runtime` 和项目 Luban 生成代码

#### 场景:Runtime 不直接依赖 Luban
- **当** 检查 `PFGAS.Runtime` 程序集
- **那么** Runtime 禁止引用 Luban、Excel 读写库、TEngine 或项目生成程序集

### 需求:运行时 Tag 必须由生成适配层注册
系统必须通过生成适配层向 PFGAS Runtime 注册 Tag 层级数据，禁止 Runtime 继续依赖包内旧 `PFTagGenerated` 静态构造链路。

#### 场景:启动注册 Tag
- **当** 游戏启动流程调用 PFGAS 生成适配层初始化入口
- **那么** Runtime 必须获得所有 Luban Tag 表中定义的 Tag 层级和显示名

#### 场景:查询父级 Tag
- **当** Runtime 注册了 `State.DeBuff.Fire` 且用户查询它是否位于 `State.DeBuff` 下
- **那么** `HasTag` 或 `IsOrUnder` 查询必须返回真

#### 场景:未注册时访问 Tag 查询
- **当** Runtime 尚未注册生成 Tag 数据且用户调用需要 Tag 层级数据的查询
- **那么** 系统必须给出清晰诊断，禁止静默返回错误的空结果

### 需求:旧 ScriptableObject Tag 链路必须移除
系统必须删除或隐藏旧 ScriptableObject Tag 编辑和生成入口，避免用户继续维护第二份 Tag 配置。

#### 场景:旧菜单入口不可编辑
- **当** 用户打开 PFGAS Tag 相关菜单
- **那么** 系统禁止提供基于 `PFTagConfig.asset` 的可编辑窗口

#### 场景:旧生成器不可产生运行时代码
- **当** 项目中存在旧 `PFTagCodeGenerator`
- **那么** 系统禁止通过它从 `PFTagConfig.asset` 生成运行时 Tag 代码

### 需求:Tag 生成结果必须可重复
系统必须保证相同的 Tag Excel 输入产生稳定一致的生成结果。

#### 场景:重复生成
- **当** 用户在未修改 Tag Excel 的情况下连续运行两次导出和适配生成
- **那么** 生成的 Tag 代码和数据必须保持等价，禁止产生无意义的 ID 或排序变化

#### 场景:保持 ID 稳定
- **当** 用户重命名或移动已有 Tag 节点但未修改其 `Id`
- **那么** 系统必须保留该 Tag 的运行时 ID

## 修改需求

## 移除需求
