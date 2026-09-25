---
kind: error_handling
name: Unity Demo 中的轻量级错误处理与容错策略
category: error_handling
scope:
    - '**'
source_files:
    - Assets/Editor/MilkTeaDemoProjectSetup.cs
    - Assets/Scripts/MilkTeaDemoBootstrap.cs
---

## 1. 使用的系统/方法

该仓库是一个 Unity 奶茶店模拟经营 Demo，代码量很小（仅两个 C# 脚本），没有引入任何第三方错误处理框架、异常类型定义或统一的错误码体系。整体采用 **Unity 原生机制 + 防御式编程** 的轻量级方式：
- 编辑器初始化通过 `InitializeOnLoad` 静态构造器在编辑器启动时执行。
- 运行时引导通过 `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]` 自动注入单例根节点。
- UI 交互使用 Unity UI (`UnityEngine.UI`) 的 Button onClick 事件回调，无自定义事件总线。
- 资源加载失败等异常情况通过 try/catch 捕获并降级到内置资源。

## 2. 关键文件

- `Assets/Editor/MilkTeaDemoProjectSetup.cs`：编辑器侧项目初始化，负责创建演示场景、设置 PlayerSettings、注册菜单项。
- `Assets/Scripts/MilkTeaDemoBootstrap.cs`：运行时主入口，动态构建整个 UI、管理对话流程、原料选择、糖度/冰度调节、提交校验与结果反馈。

## 3. 架构与约定

### 3.1 编辑器侧容错（`MilkTeaDemoProjectSetup`）
- 通过 `SessionState` 标记 `MilkTeaDemo.ProjectConfigured` 确保配置只执行一次，避免重复写入。
- `EnsureSceneExists()` 先检查 `File.Exists`，若不存在则创建目录 `Assets/Scenes`、新建空场景并保存为 `MilkTeaDemo.unity`，再调用 `AssetDatabase.Refresh()`。
- 打开场景前判断当前是否处于 play mode 以及场景是否已脏，避免覆盖未保存的工作场景。
- 所有操作均包裹在 `#if UNITY_EDITOR` 中，保证打包产物不包含编辑器代码。

### 3.2 运行时容错（`MilkTeaDemoBootstrap`）
- **字体回退**：`CreateChineseFont()` 尝试按优先级加载 `Microsoft YaHei UI` → `Microsoft YaHei` → `SimHei` → `Arial`，任一失败则 catch 并回退到 Unity 内置 `Arial.ttf`。
- **EventSystem 自检**：`EnsureEventSystem()` 检测场景中是否已有 `EventSystem`，若无则动态创建一个包含 `StandaloneInputModule` 的新 GameObject。
- **单例保护**：`StartDemo()` 通过 `FindObjectOfType<MilkTeaDemoBootstrap>() != null` 防止重复实例化；`DontDestroyOnLoad` 使根节点跨场景保留。
- **颜色解析容错**：`Hex()` 使用 `ColorUtility.TryParseHtmlString`，解析失败返回 `Color.white`，避免崩溃。
- **提交校验而非异常**：`SubmitRoutine()` 将用户输入与期望配方（红茶+鲜奶+珍珠+少糖+少冰）做硬编码比对，正确则解锁配方图标并进入服务对话，不正确则清空选择、提示“请重新调配”，不抛出异常。
- **防抖/状态锁**：`isSubmitting` 标志位配合协程 `SubmitRoutine()` 防止重复点击拉杆按钮；提交期间禁用按钮并在结束后恢复。

### 3.3 无统一错误类型/中间件
- 代码中没有自定义 Exception 子类、错误码枚举、错误日志记录器或全局错误处理器。
- 所有“错误”以 UI 文本反馈（如 `machineStatus.text = "请重新调配"; machineStatus.color = coral;`）和状态重置的方式呈现给玩家。
- 编辑器侧的错误同样以静默降级为主（如场景不存在就创建），不弹出错误对话框。

## 4. 约定与约束

- **编辑器代码隔离**：编辑器逻辑严格限定在 `Assets/Editor/` 下，并用 `#if UNITY_EDITOR` 条件编译包裹，确保不会进入构建产物。
- **运行时自启动**：通过 Unity 生命周期钩子（`InitializeOnLoad`、`RuntimeInitializeOnLoadMethod`）实现零配置启动，无需手动拖拽 GameObject。
- **资源缺失即降级**：字体找不到时回退到 Arial；颜色字符串无效时回退到白色；EventSystem 缺失时动态创建——这些是项目中实际执行的容错约定。
- **业务错误用 UI 反馈代替异常**：配方校验失败属于业务分支，不是异常路径，因此不使用 throw/catch，而是通过切换 UI 状态和文案来告知用户。
- **幂等初始化**：项目配置、场景创建、EventSystem 注入等操作都做了存在性检查，可安全重复调用。

总体而言，这是一个面向演示用途的小型 Unity 项目，错误处理以“尽可能不中断运行 + 视觉反馈”为核心原则，没有建立通用的错误抽象层。