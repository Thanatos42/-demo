---
kind: dependency_management
name: Unity Package Manager (UPM) 依赖管理
category: dependency_management
scope:
    - '**'
source_files:
    - Packages/manifest.json
    - ProjectSettings/ProjectVersion.txt
---

## 1. 使用的系统/方法

本项目采用 **Unity Package Manager (UPM)** 作为唯一的第三方依赖管理机制，通过 `Packages/manifest.json` 声明项目对 Unity 官方包（Package）的依赖。没有使用任何外部语言包管理器（如 npm、pip、go mod），也没有 vendoring 策略或私有注册表配置。

## 2. 关键文件

- `Packages/manifest.json`：UPM 的核心清单，声明项目依赖。
- `ProjectSettings/ProjectVersion.txt`：记录 Unity Editor 版本（`2022.3.62f1`），间接约束了可安装的 UPM 包版本范围。

## 3. 架构与约定

- **单一清单**：所有依赖集中在 `Packages/manifest.json` 的 `dependencies` 字段中，当前仅声明了一个官方包：`com.unity.ugui` 版本 `1.0.0`。
- **无本地包**：项目中不存在 `Packages/com.*` 下的本地自定义包目录，也未见 `packages-lock.json`（由 UPM 在首次解析后生成），说明该仓库可能处于初始状态或由工具链自动维护锁文件。
- **无私有源**：未发现 `Packages/packages.config`、`Packages/RegistryConfig.json` 或 `.npmrc`/`go.mod` 等用于配置私有注册表的文件，表明项目完全依赖 Unity 官方公共包源。
- **编辑器脚本独立于运行时**：`Assets/Editor/MilkTeaDemoProjectSetup.cs` 和 `Assets/Scripts/MilkTeaDemoBootstrap.cs` 是项目自有代码，不引入额外运行时依赖。

## 4. 约定与约束

- **依赖声明方式**：通过 UPM 的 `manifest.json` 以 `"包名": "版本号"` 键值对形式声明，版本号采用语义化版本字符串（如 `1.0.0`）。
- **版本锁定**：未显式提供 `packages-lock.json`；若存在，应由 UPM 在导入/更新包时自动生成并纳入版本控制。
- **Unity 版本约束**：`ProjectVersion.txt` 固定为 `2022.3.62f1`，这意味着所有 UPM 包必须与该 Unity 版本兼容。
- **最小依赖集**：当前仅依赖 Unity 内置的 UI 框架（UGUI），未引入任何第三方扩展包（如 TextMeshPro、Input System、Addressables 等），保持 Demo 项目的极简性。
- **无外部包管理器集成**：项目中不存在 `package.json`、`go.mod`、`requirements.txt` 等文件，也不包含 CI 脚本中的包更新步骤，因此依赖更新需手动通过 Unity Editor 的 Package Manager 窗口完成。