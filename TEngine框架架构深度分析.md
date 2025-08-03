# TEngine框架架构深度分析

## 概述

TEngine是一个基于Unity的完整游戏开发框架，采用模块化设计，支持HybridCLR热更新，集成了YooAsset资源管理系统、Luban配置表系统等商业级解决方案。框架设计遵循**面向接口编程**的原则，实现了高内聚低耦合的架构设计。

## 1. 项目整体结构

### 1.1 根目录结构

```
TEngine/
├── Books/                     # 框架文档
├── BuildCLI/                  # 构建脚本
├── Configs/                   # 配置文件（Luban配置表）
├── Tools/                     # 开发工具集
├── UnityProject/              # Unity主项目
├── LICENSE                    # 开源协议
└── README.md                  # 项目说明
```

### 1.2 Unity项目结构

```
UnityProject/Assets/
├── AssetArt/                  # 美术资源（自动生成图集等）
├── AssetRaw/                  # 热更资源目录
│   ├── UI/                    # UI预制体
│   ├── UIRaw/                 # UI原始素材
│   ├── Configs/               # 配置文件
│   ├── DLL/                   # 热更DLL
│   └── ...                    # 其他资源分类
├── GameScripts/               # 主程序集
│   ├── Procedure/             # 流程管理
│   └── HotFix/                # 热更新程序集
│       ├── GameLogic/         # 游戏逻辑
│       └── GameProto/         # 协议配置
├── TEngine/                   # 框架核心
│   ├── Runtime/               # 运行时核心
│   ├── Editor/                # 编辑器扩展
│   └── Settings/              # 框架配置
└── Scenes/                    # 场景文件
```

## 2. 核心架构设计

### 2.1 模块系统架构

TEngine采用**模块化架构**，核心由`ModuleSystem`统一管理所有模块：

#### 模块基类设计
```csharp
// 模块抽象基类
public abstract class Module
{
    public virtual int Priority => 0;  // 模块优先级
    public abstract void OnInit();     // 初始化
    public abstract void Shutdown();   // 关闭清理
}

// 需要Update的模块接口
public interface IUpdateModule
{
    void Update(float elapseSeconds, float realElapseSeconds);
}
```

#### 模块系统管理器
- **自动发现与注册**：通过反射自动创建模块实例
- **优先级管理**：根据Priority排序，高优先级模块优先更新
- **生命周期管理**：统一管理模块的初始化、更新、销毁
- **依赖注入**：通过接口获取模块实例

### 2.2 面向接口编程

框架严格遵循接口驱动设计：

```csharp
// 1. 定义接口规范
public interface IResourceModule { /* ... */ }

// 2. 实现具体模块
internal sealed class ResourceModule : Module, IResourceModule { /* ... */ }

// 3. 通过接口获取模块
IResourceModule resource = ModuleSystem.GetModule<IResourceModule>();
```

### 2.3 事件驱动架构

实现了高效的事件系统，支持MVE（Model-View-Event）模式：

```csharp
// 事件定义（推荐使用int避免哈希碰撞）
public static readonly int PlayerHpChange = StringId.StringToHash("PlayerHpChange");

// 事件监听
GameEvent.AddEventListener<HpChangeData>(PlayerHpChange, OnHpChanged);

// 事件分发
GameEvent.Send(PlayerHpChange, hpChangeData);
```

**特点：**
- 零GC分配的事件系统
- 支持泛型事件参数
- UI模块集成自动事件清理
- 局部和全局事件管理器

## 3. 核心模块详解

### 3.1 资源管理模块 (ResourceModule)

基于**YooAsset**实现的资源管理系统：

#### 主要特性
- **多种运行模式**：EditorSimulateMode、OfflinePlayMode、HostPlayMode
- **Addressable可寻址定位**：无需关心资源路径
- **自动内存管理**：支持LRU、ARC缓存策略
- **资源引用追踪**：AssetReference自动管理资源生命周期

#### 核心API
```csharp
// 同步加载
T LoadAsset<T>(string assetName) where T : Object;

// 异步加载 (UniTask)
UniTask<T> LoadAssetAsync<T>(string assetName, CancellationToken token);

// 场景加载
SceneOperationHandle LoadSceneAsync(string location, LoadSceneMode mode);
```

#### 资源组管理
- **AssetReference**：资源引用标识，自动管理资源句柄
- **AssetGroup**：资源分组管理，统一生命周期控制
- **自动释放机制**：基于引用计数的智能资源释放

### 3.2 UI管理模块 (UIModule)

**商业级UI开发框架**，实现MVE架构：

#### 设计特点
- **纯C#实现**：脱离MonoBehaviour生命周期
- **层级管理**：支持多层UI堆栈管理
- **代码生成**：右键自动生成UI绑定代码
- **事件绑定**：UI生命周期自动管理事件监听

#### UI基类体系
```csharp
// UI行为接口
public interface IUIBehaviour { /* ... */ }

// UI基类
public abstract class UIBase : IUIBehaviour { /* ... */ }

// UI窗口基类
public abstract class UIWindow : UIBase { /* ... */ }

// UI组件基类  
public abstract class UIWidget : UIBase { /* ... */ }
```

#### 开发工作流
1. **UI编排**：按照命名规范在Unity中制作UI
2. **代码生成**：右键生成UI绑定代码到剪贴板
3. **脚本创建**：创建UI脚本并粘贴生成的代码
4. **事件绑定**：通过`AddUIEvent`自动管理事件生命周期

#### 示例代码
```csharp
[Window(UILayer.Bottom, fullScreen: true)]
class BattleMainUI : UIWindow
{
    // 自动生成的UI绑定代码
    private Button m_btnPause;
    
    public override void ScriptGenerator()
    {
        m_btnPause = FindChildComponent<Button>("m_btnPause");
        m_btnPause.onClick.AddListener(OnClickPauseBtn);
    }
    
    // 自动管理事件生命周期
    public override void RegisterEvent()
    {
        AddUIEvent(PlayerHpChange, OnHpChanged);
    }
}
```

### 3.3 流程管理模块 (ProcedureModule)

基于**有限状态机**的流程管理系统：

#### 完整启动流程
```
ProcedureLaunch          # 启动器初始化
    ↓
ProcedureSplash          # 闪屏展示
    ↓
ProcedureInitPackage     # 初始化资源包
    ↓
ProcedurePreload         # 预加载资源
    ↓
ProcedureInitResources   # 初始化资源系统
    ↓
ProcedureUpdateVersion   # 检查版本更新
    ↓
ProcedureCreateDownloader # 创建下载器
    ↓
ProcedureDownloadFile    # 下载资源文件
    ↓
ProcedureDownloadOver    # 下载完成
    ↓
ProcedureClearCache      # 清理缓存
    ↓
ProcedureLoadAssembly    # 加载热更程序集
    ↓
ProcedureStartGame       # 启动游戏
```

#### 特点
- **状态驱动**：每个流程为一个状态，清晰的状态转换
- **可扩展**：易于添加自定义流程节点
- **错误处理**：每个流程都有完善的错误处理机制

### 3.4 配置表模块 (ConfigSystem)

集成**Luban**配置表解决方案：

#### 主要优势
- **多格式支持**：Excel、JSON、XML等
- **强类型检查**：编译期数据验证
- **本地化支持**：多语言配置管理
- **多种加载方式**：同步、异步、懒加载

#### 使用示例
```csharp
public class ItemConfigMgr : Singleton<ItemConfigMgr>
{
    private TbItem TbItem => ConfigLoader.Instance.Tables.TbItem;
    
    public ItemConfig GetItemConfig(int itemId)
    {
        TbItem.DataMap.TryGetValue(itemId, out var config);
        return config;
    }
}
```

### 3.5 内存池与对象池模块

#### 内存池 (MemoryPool)
- **零GC分配**：复用内存块，避免频繁分配
- **多种规格**：预定义多种内存块大小
- **自动管理**：低内存时自动清理

#### 对象池 (ObjectPoolModule)
- **泛型对象池**：支持任意类型对象池化
- **生命周期管理**：自动创建、获取、释放、清理
- **性能优化**：避免频繁的new/destroy操作

### 3.6 音频模块 (AudioModule)

完整的音频解决方案：

#### 功能特点
- **分类管理**：音乐、音效、UI音效分别控制
- **3D音效支持**：空间音效处理
- **音频池化**：AudioSource对象池管理
- **淡入淡出**：音频过渡效果

### 3.7 调试模块 (DebuggerModule)

运行时调试系统：

#### 调试功能
- **性能监控**：FPS、内存、GPU使用率
- **模块状态**：各模块运行状态监控
- **资源监控**：资源加载、引用计数监控
- **日志系统**：分级日志记录和查看

## 4. 热更新架构

### 4.1 HybridCLR热更新方案

TEngine采用**HybridCLR**实现全平台热更新：

#### 程序集设计
```
Assets/GameScripts/
├── Main程序集           # AOT主程序（启动器、流程）
└── HotFix/             # 热更程序集目录
    ├── GameLogic.dll   # 游戏业务逻辑程序集
    └── GameProto.dll   # 协议配置程序集
```

#### 热更新流程
1. **AOT程序集启动**：主程序负责框架初始化
2. **元数据加载**：LoadMetadataForAOTAssembly补充AOT泛型元数据
3. **热更程序集加载**：动态加载热更新DLL
4. **入口调用**：反射调用GameApp.Entrance进入热更域

#### 关键代码
```csharp
// ProcedureLoadAssembly.cs - 热更程序集加载
private void AllAssemblyLoadComplete()
{
    var appType = _mainLogicAssembly.GetType("GameApp");
    var entryMethod = appType.GetMethod("Entrance");
    object[] objects = new object[] { new object[] { _hotfixAssemblyList } };
    entryMethod.Invoke(appType, objects);
}

// GameApp.cs - 热更域入口
public static void Entrance(object[] objects)
{
    _hotfixAssembly = (List<Assembly>)objects[0];
    Log.Warning("======= 看到此条日志代表你成功运行了热更新代码 =======");
    StartGameLogic();
}
```

### 4.2 热更新优势

- **全平台支持**：iOS、Android、WebGL等全平台热更
- **性能优异**：接近原生IL2CPP性能
- **完全兼容**：支持.NET所有特性
- **零学习成本**：纯C#开发，无需lua等脚本语言

## 5. 核心设计模式

### 5.1 单例模式
```csharp
// 线程安全单例基类
public abstract class Singleton<T> where T : class, new()
{
    private static T _instance;
    public static T Instance => _instance ??= new T();
    
    protected virtual void OnInit() { }
    protected virtual void OnDestroy() { }
}
```

### 5.2 工厂模式
- **模块工厂**：ModuleSystem自动创建模块实例
- **对象池工厂**：ObjectPool根据类型创建对象池

### 5.3 观察者模式
- **事件系统**：完整的观察者模式实现
- **UI事件绑定**：自动管理观察者生命周期

### 5.4 状态模式
- **FSM有限状态机**：流程管理、AI状态管理
- **UI状态管理**：UI显示、隐藏状态控制

### 5.5 策略模式
- **资源加载策略**：同步、异步、流式加载
- **缓存策略**：LRU、ARC缓存算法

### 5.6 命令模式
- **事件系统**：事件作为命令执行
- **操作记录**：支持操作撤销重做

## 6. 性能优化特性

### 6.1 内存优化
- **对象池化**：减少GC分配
- **内存池**：固定大小内存块复用
- **智能缓存**：LRU/ARC算法优化内存使用
- **资源引用计数**：精确控制资源生命周期

### 6.2 渲染优化
- **图集自动生成**：减少DrawCall
- **UI分层管理**：优化UI渲染顺序
- **资源预加载**：减少运行时加载卡顿

### 6.3 代码优化
- **零GC事件系统**：避免委托分配
- **StringBuilder缓存**：字符串操作优化
- **协程池化**：UniTask替代Coroutine

## 7. 扩展性设计

### 7.1 模块扩展
```csharp
// 1. 定义接口
public interface ICustomModule
{
    void DoSomething();
}

// 2. 实现模块
public class CustomModule : Module, ICustomModule
{
    public override void OnInit() { /* 初始化逻辑 */ }
    public void DoSomething() { /* 具体实现 */ }
    public override void Shutdown() { /* 清理逻辑 */ }
}

// 3. 使用模块
var custom = ModuleSystem.GetModule<ICustomModule>();
custom.DoSomething();
```

### 7.2 UI扩展
- **自定义UI基类**：继承UIWindow/UIWidget实现特定功能
- **UI组件扩展**：实现IUIBehaviour接口
- **UI层级自定义**：枚举定义自定义UI层级

### 7.3 事件扩展
```csharp
// 定义事件接口
public interface ICustomEvent
{
    void OnCustomEvent(CustomData data);
}

// 通过代码生成器自动生成事件调用代码
GameEvent.Get<ICustomEvent>().OnCustomEvent(data);
```

## 8. 工具链支持

### 8.1 编辑器工具
- **TEngine设置面板**：框架参数配置
- **UI脚本生成器**：自动生成UI绑定代码
- **资源包配置器**：资源打包规则配置
- **构建工具**：一键构建多平台

### 8.2 调试工具
- **运行时调试器**：实时监控各模块状态
- **性能分析器**：FPS、内存、资源监控
- **日志查看器**：分级日志实时查看
- **控制台扩展**：运行时参数调整

### 8.3 代码生成
- **UI代码生成**：根据UI结构自动生成绑定代码
- **事件接口生成**：自动生成事件调用代码
- **配置表代码生成**：Luban自动生成配置访问代码

## 9. 最佳实践

### 9.1 开发规范
- **命名规范**：模块、接口、事件命名约定
- **代码组织**：按功能模块组织代码结构
- **接口优先**：优先定义接口，后实现功能
- **事件驱动**：通过事件解耦模块间依赖

### 9.2 性能建议
- **对象池化**：频繁创建的对象使用对象池
- **事件优化**：使用int类型事件ID避免字符串哈希
- **资源管理**：及时释放不用的资源引用
- **异步加载**：大资源使用异步加载避免卡顿

### 9.3 架构建议
- **模块划分**：按功能职责清晰划分模块
- **依赖管理**：通过接口注入减少模块间耦合
- **错误处理**：完善的异常处理和错误恢复机制
- **版本管理**：清晰的热更新版本管理策略

## 10. 总结

TEngine是一个**设计优良、功能完整、性能优异**的Unity游戏框架：

### 10.1 核心优势
1. **模块化架构**：高内聚低耦合，易于维护和扩展
2. **面向接口编程**：清晰的抽象层，便于测试和替换
3. **完整热更新方案**：HybridCLR全平台热更新支持
4. **商业级UI系统**：完整的UI开发工作流和MVE架构
5. **性能优化**：多层次的性能优化机制
6. **丰富工具链**：完善的开发和调试工具支持

### 10.2 适用场景
- **商业游戏项目**：完整的商业级解决方案
- **快速原型开发**：开箱即用，5分钟上手
- **大型项目架构**：模块化设计支持团队协作
- **全平台发布**：支持所有主流平台

### 10.3 学习建议
1. **理解核心概念**：模块系统、事件驱动、热更新机制
2. **熟悉开发流程**：UI开发、资源管理、配置表使用
3. **掌握扩展方法**：自定义模块、UI组件、事件系统
4. **实践项目开发**：通过实际项目加深理解

TEngine为Unity开发者提供了一个**现代化、工程化、商业化**的游戏开发框架，是构建高质量游戏项目的优秀选择。

---

*本文档基于TEngine源码深度分析编写，涵盖了框架的方方面面。建议结合实际代码学习，在实践中加深对框架架构的理解。*