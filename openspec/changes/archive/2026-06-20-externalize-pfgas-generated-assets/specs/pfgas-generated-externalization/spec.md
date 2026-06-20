## 新增需求

### 需求:Runtime 必须独立于项目生成物编译
`PFGAS.Runtime` 必须在没有 `Assets/PFGASGenerated`、Luban 生成代码、JSON/bytes 数据和项目资源加载适配的情况下完成编译。

#### 场景:删除项目生成目录后编译 Runtime
- **当** 项目中不存在 `UnityProject/Assets/PFGASGenerated`
- **那么** `com.peifeng.pfgas.Runtime` 程序集必须仍然可以编译

#### 场景:Runtime 不引用生成程序集
- **当** 检查 `UnityProject/Assets/PFGAS/Runtime/com.peifeng.pfgas.Runtime.asmdef`
- **那么** 该程序集禁止引用 `PFGASGenerated`、Luban、TEngine 或 Excel 读写程序集

### 需求:项目生成物必须输出到包外目录
PFGAS 编辑器生成器必须默认把项目级生成物输出到包外目录：PFGAS 专属适配代码进入 `UnityProject/Assets/PFGASGenerated/PFGAS`，Luban 配置生成代码进入 `UnityProject/Assets/GameScripts/HotFix/GameProto`。生成器禁止默认写入 `UnityProject/Assets/PFGAS`。

#### 场景:生成 Tag 和 Attribute 代码
- **当** 用户在 PFGAS 编辑器中生成 Tag 或 Attribute 代码
- **那么** 生成文件必须写入 `UnityProject/Assets/PFGASGenerated/PFGAS`

#### 场景:生成 Luban 代码和数据
- **当** 用户执行 PFGAS 配置导出流程
- **那么** Luban C# 输出必须写入 `UnityProject/Assets/GameScripts/HotFix/GameProto`，JSON 或 bytes 数据必须写入项目侧数据目录

#### 场景:默认路径检查
- **当** 编辑器配置没有显式覆盖输出路径
- **那么** 默认输出路径禁止位于 `UnityProject/Assets/PFGAS` 下

#### 场景:删除旧包内副本
- **当** LubanLib 或 Luban 配置生成代码已经迁移到项目侧目录
- **那么** `UnityProject/Assets/PFGAS/Generated` 下对应旧代码必须删除，禁止作为兼容副本保留

### 需求:PFGAS 适配生成程序集必须依赖 Runtime 和 GameProto
项目侧 PFGAS 适配生成程序集必须引用 `com.peifeng.pfgas.Runtime` 和 `GameProto`，并负责把 GameProto/Luban 配置装配到 Runtime 可消费的数据结构中。

#### 场景:生成 asmdef
- **当** PFGAS 生成器创建或刷新项目生成程序集
- **那么** `PFGASGenerated.asmdef` 必须引用 `com.peifeng.pfgas.Runtime` 和 `GameProto`

#### 场景:Runtime 装配入口
- **当** 游戏启动代码调用项目生成层初始化入口
- **那么** PFGAS 适配生成层必须从 `GameProto` 读取配置并注册 Tag 层级、Attribute 规则和配置 provider，Runtime 禁止直接查找 Luban 表或项目资源路径

#### 场景:GameProto 不反向依赖 PFGAS
- **当** 检查 `UnityProject/Assets/GameScripts/HotFix/GameProto/GameProto.asmdef`
- **那么** `GameProto` 禁止引用 `com.peifeng.pfgas.Runtime` 或 PFGAS 适配生成程序集

### 需求:Runtime ID 类型必须与项目具体值分离
`PFTagId` 和 `PFAttributeId` 的类型定义必须由 Runtime 提供，具体项目命名常量和注册表必须由项目生成层提供。

#### 场景:Runtime 定义 ID 类型
- **当** `PFGAS.Runtime` 独立编译
- **那么** `PFTagId` 和 `PFAttributeId` 类型必须存在，并且不要求任何项目生成文件参与编译

#### 场景:项目生成命名常量
- **当** 生成器根据配置表或编辑器配置输出项目代码
- **那么** 生成层必须提供项目命名常量，例如 Tag 和 Attribute 的可读名称到 Runtime ID 的映射

#### 场景:未注册数据错误
- **当** 游戏代码在没有注册项目生成数据的情况下访问需要配置注册的 Runtime 能力
- **那么** Runtime 必须给出清晰错误，而不是静默返回空数据或产生隐式默认行为

### 需求:资源加载适配必须位于 GameProto 或项目层
依赖 TEngine、Resources、Addressables 或其他项目资源系统的加载器必须位于 `GameProto` 或项目 adapter 程序集，禁止位于 PFGAS 包本体 Runtime 中。

#### 场景:TEngine 加载器迁移
- **当** 项目使用 TEngine 加载 Luban JSON 或 bytes
- **那么** TEngine loader 必须位于 `UnityProject/Assets/GameScripts/HotFix/GameProto`、`Assets/PFGASGenerated/Adapters` 或项目自定义程序集

#### 场景:非 TEngine 项目接入
- **当** 项目不使用 TEngine
- **那么** 项目必须能够通过自定义 loader 初始化 PFGAS 生成数据，而无需修改 `PFGAS.Runtime`

### 需求:编辑器工具可以依赖生产工具链
`PFGAS.Editor` 可以依赖 UnityEditor、Excel/Luban 工作流和编辑器 UI，但必须把这些依赖限制在编辑器程序集或项目生成工具链中。

#### 场景:Editor 读写配置
- **当** 用户通过 PFGAS 编辑器读写 Excel、ScriptableObject 配置或运行 Luban
- **那么** 相关依赖必须只影响 Editor 或项目生成阶段，不得进入 `PFGAS.Runtime`

#### 场景:配置资产位置
- **当** 编辑器创建默认配置资产实例
- **那么** 配置资产实例必须位于项目侧配置目录，而不是位于 `UnityProject/Assets/PFGAS/Editor` 包目录

## 修改需求

## 移除需求
