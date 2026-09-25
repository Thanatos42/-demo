---
kind: configuration_system
name: Unity Demo 项目配置与持久化：编辑器初始化 + PlayerPrefs 状态存储
category: configuration_system
scope:
    - '**'
source_files:
    - Assets/Editor/MilkTeaDemoProjectSetup.cs
    - Assets/Scripts/MilkTeaDemoBootstrap.cs
    - ProjectSettings/ProjectVersion.txt
---

## 1. 使用的系统与工具

仓库是一个 Unity 2022.3.62f1 的极简 Demo，没有引入第三方配置库。运行时配置通过两条路径实现：
- **编辑器侧**：`Assets/Editor/MilkTeaDemoProjectSetup.cs` 使用 `InitializeOnLoad` + `SessionState` 在编辑器启动时自动执行一次项目初始化。
- **运行侧**：`Assets/Scripts/MilkTeaDemoBootstrap.cs` 使用 Unity 内置 `PlayerPrefs` 做跨会话的状态持久化（配方解锁标记）。

没有发现 `.env`、`.yaml`、`.toml`、`application.properties` 或任何外部配置文件；所有“配置”以 C# 常量 / 硬编码值形式存在。

## 2. 关键文件

- `Assets/Editor/MilkTeaDemoProjectSetup.cs` — 编辑器初始化脚本
- `Assets/Scripts/MilkTeaDemoBootstrap.cs` — 运行时引导 + 用户状态持久化
- `ProjectSettings/ProjectVersion.txt` — Unity 编辑器版本声明

## 3. 架构与约定

### 3.1 编辑器端配置（MilkTeaDemoProjectSetup）

- 类被 `[InitializeOnLoad]` 标注，静态构造器注册 `EditorApplication.delayCall += ConfigureProjectOnce`，确保编辑器加载后延迟执行一次。
- 使用 `SessionState.GetBool("MilkTeaDemo.ProjectConfigured")` 作为幂等锁，避免重复配置。
- 一次性写入 `PlayerSettings`：
  - `companyName = "MilkTeaDemo"`
  - `productName = "奶茶店模拟经营 Demo"`
  - `defaultScreenWidth = 1920`, `defaultScreenHeight = 1080`
  - `fullScreenMode = Windowed`, `resizableWindow = true`, `runInBackground = true`
- 调用 `EnsureSceneExists()` 检查并创建 `Assets/Scenes/MilkTeaDemo.unity`（不存在则 `Directory.CreateDirectory` + `EditorSceneManager.NewScene` + `SaveScene` + `AssetDatabase.Refresh`）。
- 将 `EditorBuildSettings.scenes` 重写为仅包含该演示场景的单元素数组。
- 若当前活动场景不是目标场景且未处于播放模式，则自动打开演示场景。
- 提供菜单项 `[MenuItem("奶茶店 Demo/打开演示场景")]` 供手动触发。

### 3.2 运行时引导（MilkTeaDemoBootstrap）

- 使用 `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]` 静态方法 `StartDemo()` 作为全局入口，查找已存在的实例以避免重复挂载。
- `Awake()` 中调用 `DontDestroyOnLoad`，保证跨场景存活。
- UI 全部在代码中动态构建（Canvas、Panel、Text、Button），参考分辨率固定为 `1920x1080`，匹配模式 `MatchWidthOrHeight`，宽高比 `16:9`。
- 字体通过 `Font.CreateDynamicFontFromOSFont` 回退到系统 Arial，无外部字体资源依赖。

### 3.3 用户状态持久化（PlayerPrefs）

- 唯一持久化键：`UnlockKey = "MilkTeaDemo.ClassicPearlUnlocked"`。
- 当玩家正确制作经典珍珠奶茶时，`PlayerPrefs.SetInt(UnlockKey, 1); PlayerPrefs.Save();`，并在下次启动时读取以显示解锁后的配方图标。
- 读取使用默认值：`PlayerPrefs.GetInt(UnlockKey, 0)`。

## 4. 约定与约束

- **编辑器初始化幂等性**：通过 `SessionState` 中的 `MilkTeaDemo.ProjectConfigured` 布尔标记保证 `ConfigureProjectOnce` 在整个编辑器会话内只执行一次（来源：`MilkTeaDemoProjectSetup.cs` 第 29–34 行）。
- **演示场景路径固定**：`Assets/Scenes/MilkTeaDemo.unity` 是硬编码的唯一场景路径，编辑器初始化会强制将其设为 `EditorBuildSettings.scenes` 的唯一成员（来源：`MilkTeaDemoProjectSetup.cs` 第 12、44–47 行）。
- **窗口尺寸约定**：编辑器设置与运行时 Canvas 参考分辨率都采用 `1920×1080`，宽高比 `16:9`（来源：`MilkTeaDemoProjectSetup.cs` 第 37–38 行与 `MilkTeaDemoBootstrap.cs` 第 94、105 行）。
- **配方解锁状态持久化**：经典珍珠奶茶的解锁标志通过 `PlayerPrefs` 的 `MilkTeaDemo.ClassicPearlUnlocked` 键保存，键值 `1` 表示已解锁（来源：`MilkTeaDemoBootstrap.cs` 第 17、495、545 行）。
- **无外部配置文件**：项目中未发现任何 `.yaml`/`.json`/`.env`/`.toml` 等外部配置格式；所有可配置项（公司名、产品名、分辨率、场景列表、颜色、文案、配方选项）均以 C# 字符串常量或字面量直接嵌入代码。
- **Unity 版本锁定**：`ProjectSettings/ProjectVersion.txt` 声明编辑器版本为 `2022.3.62f1`，这是仓库对 Unity 版本的显式约束（来源：`ProjectSettings/ProjectVersion.txt`）。