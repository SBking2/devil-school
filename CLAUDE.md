# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 仓库结构

git 仓库根目录（`DevilSchool/`）只是一个外层容器；真正的 Godot 项目在 `devil-school/` 子目录下。下文所有路径除非特别说明，都是相对于 `devil-school/` 的。仓库根目录下的其他文件夹不参与构建：
- `Excel/` — 源数据表（角色/物品/技能/对话等）的 `.xlsx` 文件。目前**没有**接入游戏（见下方 Model 数据系统）。
- `Tool/Excel2Csv/` — 一个独立的 C++ 工具，用于把这些 `.xlsx` 转成 CSV。它的 `config.txt` 目前指向另一个无关项目（`2DGame`），说明这条流水线当前并未真正连到本仓库。
- `ModelProject/`、`GPTDev/` — Blender 资产、以及一个游离的草稿脚本，构建不会用到。
- `Doc/` — 一张架构图（`style.drawio`）,以及几份 FPS 相关的设计文档：`FPS_Camera_Tutorial.md`（第一人称相机/武器手感的设计参考,见下方"摄像机"一节）、`FPS_Architecture_Design.md`（FPS 底层框架改造方案）、`FPS_Combat_Refactor_Plan.md`。这些文档只是设计参考,不代表已经实现——具体哪些已经落地要看下面"架构"里的实际描述。

## 常用命令

- **构建**：`dotnet build devil-school/devil-school.csproj`（或直接打开 `devil-school.sln`）。使用 Godot .NET SDK 4.6.1，目标框架 `net8.0`（Android 导出时为 `net9.0`）。
- **运行**：用 Godot 4.6 编辑器打开项目按 Play，或者用 Godot 可执行文件指向 `devil-school/project.godot` 启动。没有命令行运行脚本。
- **测试**：仓库里没有任何测试项目或测试框架（没有 xunit/nunit/gdunit/gut）。`scripts/Core/Example/GameActions/` 下是 `GameAction` 的示例代码，不是测试。

## 命名规范

- **命名空间**：全项目统一用 `EGame`（`.csproj` 里的 `RootNamespace` 实际没被用到，可以忽略）。
- **`N` 前缀 = Godot 节点包装类**：所有以 `N` 开头的类（`NGame`、`NRun`、`NCombatRoom`、`NFirstPersonCamera` 等）都位于 `scripts/Core/Nodes/` 下，并直接继承某个 Godot `Node` 派生类型（`Node`/`Node3D`/`Control`/`CharacterBody3D` 等）。反过来，`Nodes/` 目录之外没有任何类使用 `N` 前缀，`Nodes/` 目录内也没有不带 `N` 前缀的类——这是一条严格执行的约定，新写节点包装类时要遵守。
- **`I` 前缀 = 接口**：标准接口前缀（`ISerializable`、`INetMessage` 等）。如果一个接口是给 `N` 类实现的，会写成 `IN...`（如 `INCamera`、`INSensor`），表示"这是给某个 N 类用的接口"。
- **`Abstract` 前缀 = 用于多态注册的抽象基类**：`AbstractModel`、`AbstractCharacterMovementState`、`AbstractWorldBehaviorNode`、`AbstractTurnMoveState`、`AbstractConsoleCmd`、`AbstractNetClient`/`AbstractNetHost` 都是这个模式。
- **`Abstract*Subtypes` 反射注册表模式**：与某个 `Abstract` 基类配套，会有一个同名的 `Abstract*Subtypes` 静态类，里面手写一个 `Type[]` 数组列出所有具体子类，运行时通过 `Activator.CreateInstance` 批量实例化注册（例子：`AbstractModelSubtypes`、`AbstractConsoleCmdSubtypes`）。**新增一个 Model 或 DevConsole 命令时，必须手动把它的 `Type` 加进对应数组，否则不会被注册**，这一步不是自动发现的。
- **单例**：静态单例基本用 `public static X Instance { get; } = new X();`（纯 C# 类，立即初始化）或者 `public static X Instance { get; private set; }`，在 `_EnterTree`/`_Ready` 里赋值一次（Godot 节点类）。
- **目录按功能分组，且 `Core/` 与 `Core/Nodes/` 成对镜像**：`scripts/Core/<Feature>/` 放纯 C# 逻辑/数据类，`scripts/Core/Nodes/<Feature>/` 放对应的节点包装类，两边文件夹名一一对应（`Core/Combat/` ↔ `Core/Nodes/Combat/`，`Core/Enviroment/` ↔ `Core/Nodes/Enviroment/`）。`scenes/` 下的文件夹也镜像同一套功能划分（`scenes/combat/`、`scenes/enviroments/` 等）。新增功能时应该保持这个三方（逻辑类/节点类/场景）的目录对应关系。
- **场景加载**：统一走 `Core/Utils/SceneHelper.LoadScene<T>(path)`，`path` 会被解析为相对于 `res://scenes/` 的路径。
- **禁止目标类型 `new()`**：`new` 后面必须写明确类型，写 `new List<T>()`、`new AgentIntent()`，不要写 `new()`。
- **现有的"错别字"就是既定拼法，不要去"修正"它们**（改了反而会和其余代码不一致）：`Enviroment`/`Enviroments`（不是 Environment，遍布文件夹名、类名、场景路径）、`Excute`（不是 Execute，`GameAction.Excute()`）、`Settins`（不是 Settings，`Utils/Settins.cs`）、`Catogory`（`ModelDB` 内部）、`MonsterWithSlost`（`EncounterModel.GenerateMonsterWithSlost()`）。
- **注释以中文为主，标识符（类名/方法名/变量名）用英文**：写新代码时延续这个混合风格。
- `.editorconfig` 只规定了 UTF-8 编码，没有强制缩进规则；实际代码里 tab/空格是按文件各自为政的，改某个文件时跟随该文件已有的缩进风格即可。
- 每个 `.cs` 文件都有一个 Godot 自动生成的 `.cs.uid` 伴随文件（Godot 4.x 的全局 ID 机制），不要手动编辑它们。

## 架构

### 游戏流程

`NGame`（应用级单例，`NGame.Instance`）在 `_EnterTree` 里调用 `ModelDB.OnInit()`，然后持有一个 `NSceneContainer`，在主菜单和 `NRun` 之间切换。`NRun`（代表"进入存档之后的一次游戏会话"，`NRun.Instance`）自己也持有一个 `NSceneContainer`，在 `NEnviroment`（开放世界探索）和 `NCombatRoom`（战斗）之间切换，另外还持有一个 `NCameraController`。`NSceneContainer` 是一个通用的"清空现有子节点、挂载新的当前场景"容器，在两个层级都被复用。

### Model 数据系统

这是一套**编译期、纯代码定义**的数据目录——尽管仓库里已经有 `Excel/` 表格和 `Excel2Csv` 工具，但目前**并没有**接入运行时（这条 CSV 数据管线是规划中但尚未连接的；`scripts/` 下没有任何代码读取 `.csv`）。

- `AbstractModel` 是基类：每个具体类型有且只有一个"标准实例"（不可变），通过无参构造函数创建。运行时需要的可变实例要用 `MutableClone()`（内部是 `MemberwiseClone` + 虚方法 `DeepCopy()`）得到，不能直接 `new`。
- `ModelID` 是 `record class ModelID(string Category, string Entry)`。`Category` 取自最近一个标了 `[ModelCategory]` 特性的祖先类（比如 `MonsterModel`）；`Entry` 取自具体叶子类的类名。
- `ModelDB`（静态类，注释里自称"相当于 GameDataManager"）在 `OnInit()` 时，把 `AbstractModelSubtypes.AllSubTypes` 里列出的每个类型都 `Activator.CreateInstance` 一遍，建好整个数据目录。
- **新增一个 Model**：新建具体类（比如放在 `Model/MonsterModels/` 下），然后把它的 `Type` 加进 `AbstractModelSubtypes.cs` 里那个手写数组——不加就不会被识别到。

### 角色移动状态机

一套和具体生物类型解耦的通用 FSM：`CharacterMovementStateMachine` 内部用 `AbstractCharacterMovementState.StateName` 做 key 存状态，`ChangeState(name)` 遇到未知状态名会抛异常。各生物类型的状态放在各自镜像的子文件夹里：`Movement/Player/`、`Movement/Monster/`、`Movement/Robot/`，一般各自提供 Idle 和 Walk/MoveBase 状态。`MonsterModel.CreateMovementStateMachine()` 是把状态机接到具体 Model 上的入口。

### AI —— 两套互相独立的系统，不要混淆

- **`Core/AI/WorldAI/`**：实时行为树，用于开放世界/探索阶段的 AI，每帧从 `MonsterModel.OnWorldProcess` 驱动。组合/叶子/装饰节点的词汇表在 `AI/WorldAI/Node/`（`WorldNodeSelector`、`WorldNodeSequence`、`WorldNodeAction`、`WorldNodeCondition`、`WorldNodeDecorator`、`WorldNodeRepeat`）；每种怪物的具体行为树放在 `AI/WorldAI/MonsterAI/<怪物名>/`。`WorldBehaviorTree.NotifyEvent()` 会中断并重新 tick 整棵树，实现事件驱动的响应；另外内部维护一个有限长度的节点进入日志，方便调试。
- **`Core/AI/TurnBaseAI/`**：回合制的"选招"状态机，用于战斗中的决策，和 `WorldAI` 完全是两套东西。`TurnMoveStateMachine.RollMove()` 会沿着中间的分支节点（`TurnStateConditionalBranch`、`TurnStateRandomBranch`）走，直到落在一个 `TurnStateMove` 叶子节点上，这个叶子就是本回合实际执行的招式。

### 战斗

`CombatManager`（单例，只管流程，注释里明确说"数据由 CombatRoom 管理"）负责回合推进；`CombatRoom` 持有真正的战斗数据（`CombatState`、双方 `Creature` 列表），由一个 `EncounterModel` 构建而来。`CombatRoom.EnterRoom()` 会创建 Godot 侧的 `NCombatRoom` 并把它切换成 `NRun` 的当前场景。注意 `NCombatRoom`/`NCreature` 是基于 `Control` 的 2D UI 实现，和第一人称 3D 探索那一套（`NEnvCreature : CharacterBody3D`）是两套并存的表现层，不要混着改。

### GameAction / 命令队列

`GameAction.Excute()`（全项目统一是这个拼法，不是 Execute，遇到时保持一致）会调用 `GetActionTask()`，完成后触发 `OnTaskCompleted` 事件。`ActionQueueSet` 按玩家 index 各自维护一条动作队列；`ActionExecutor`（注释自称"只负责执行"）依次从队列里取出就绪的动作执行，并对外暴露一个可以 `await` 的完成状态。

### 摄像机 / 第一人称手感

`INCamera` 是接口（`MakeCurrent()`、水平/垂直 `Quaternion`）；`NCameraController` 负责切换当前生效的 `INCamera` 节点。`NFirstPersonCamera` 在代码里（不是在场景文件里）搭建了 `PitchPivot → YawPivot → CameraEffectPos → Camera3D` 的层级，以及并行的武器视角链，用指数衰减平滑（`1 - exp(-speed*delta)`）实现了 Camera Bob / Weapon Bob / Weapon Sway，对应 [FPS_Camera_Tutorial.md](Doc/FPS_Camera_Tutorial.md) 里描述的方案。这份文档是完整功能列表的设计/工程参考（ADS、视觉后坐力与实际瞄准后坐力的拆分、镜头晃动、探头/倾身、武器防穿墙、网络同步边界等），**要当成一份对照清单，而不是"已实现功能的说明"**——`NFirstPersonCamera` 目前只实现了其中一部分。

### 日志

各子系统各自持有一个按 `Log.LogType`（`World`、`Combat`、`NetWork`、`Generic`、`GameSync`）区分的私有 `Log.Logger` 字段，而不是直接调用静态的 `Log` 方法，例如 `new Log.Logger(Log.LogType.Combat)`。输出前会先按 `Settins.LogType`/`Settins.LogLevel` 过滤。

### 网络

分层是 `Net/Transport/`（抽象的 `AbstractNetClient`/`AbstractNetHost`）→ `Net/Transport/ENet/`（Godot 内置 ENet 的具体实现）→ `Net/Serialization/`（自定义的 `PacketWriter`/`PacketReader`，`QuantizeParam` 用于浮点量化编码）。目前主要通过 DevConsole 的 `NetTransportTestCmd` 命令来手动测试，还处于早期阶段，没有真正接入玩法。
