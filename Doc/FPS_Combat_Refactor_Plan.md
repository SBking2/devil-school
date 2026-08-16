# DevilSchool 战斗系统改造方案：从回合制到实时 FPS

这份文档记录了一次架构评审的结论——评审对象是 `scripts/Core/` 下的现有战斗系统，评审目的是判断它是否适合支撑真正的实时 FPS 战斗。**如果你在新会话里看到这份文档，可以直接把这当成已经确认过的结论继续往下做，不需要重新分析。**

## 背景

这套战斗框架的核心结构（`CombatManager`/`CombatRoom`/`CombatState`/回合制 UI）是从《杀戮尖塔》的源码移植过来的。移植本身没问题（Model/Node 分离、Roguelike 的 Run 结构这些都是通用的好架构），但**回合制战斗的核心判定逻辑跟实时 FPS 战斗在底层假设上是冲突的**，这是这份文档要解决的问题。

已经跟项目负责人确认过：**目标是真正实时的开枪战斗，现在的回合制是阶段性占位，不是最终设计。**

## 现状架构评审（写这份文档时的代码状态）

### 已经是合格 FPS 地基的部分，不用动

- `scripts/Core/Nodes/Enviroment/NEnvCreature.cs`——3D `CharacterBody3D`，玩家/怪物在探索场景里的真实呈现
- `scripts/Core/CharacterStateMachine/Movement/`——移动状态机，`PlayerMovementStateAirMoveBase` 里已经正确实现了 Quake 式空气加速（`ApplyAirAccelerate`/`AIR_ACCELERATE`）
- `scripts/Core/Nodes/Camera/NFirstPersonCamera.cs`——真正能用的第一人称摄像机：鼠标视角、头部晃动、武器摆动/后坐力挂钩全部实现了，`_WeaponViewRoot` 已经预留了武器视觉挂载点
- `scripts/Core/AI/WorldAI/WorldBehaviorTree.cs`——实时行为树，`ZombieChaseNode.cs` 已经在直接写 `context.Owner.Intent.MoveDir`，这是驱动实时怪物追击的正确方式
- `scripts/Core/Model/AbstractModel.cs` + `ModelDB.cs` + `scripts/Core/Entities/`（`Player.cs`/`Creature.cs`）——配置数据 → 领域对象的三层分离，架构合理
- `scripts/Core/Entities/Creatures/Creature.cs` 里的 `HP` 字段，带变化事件——伤害系统的数据基础，直接复用
- `scripts/Core/Nodes/Enviroment/NMonsterSpwanPoint.cs`——怪物生成点，实时遭遇战要用到
- `scripts/Core/Nodes/Collision/LayerManager.cs`——碰撞层管理，命中判定要用到
- `scripts/Core/Run/RunManager.cs`/`RunState.cs`——Roguelike 的"一局一局玩"meta 结构，跟战斗实不实时无关，不用改

### 跟实时 FPS 冲突、需要整体替换的部分

| 文件/系统 | 问题 |
|---|---|
| `scripts/Core/Combat/CombatManager.cs` | `StartTurn()` 是空方法体，`ExecutePlayerTurn`/`ExecuteEnemyTurn` 只 `await Task.CompletedTask`——回合制判定逻辑本身还是占位，但即便补完，回合制这个模型本身就不该要 |
| `scripts/Core/Combat/CombatRoom.cs` | 进入战斗 = 切换到一个独立场景状态，跟探索场景互斥。FPS 里不该有这种切换——怪物应该从头到尾都在同一个持续 3D 世界里 |
| `scripts/Core/Combat/CombatState.cs` | 显式追踪 `_CurTurn`（当前回合数）、`_CurCombatSide`（当前该谁行动）——这是回合制专属的数据结构，实时战斗里没有"轮到谁"这个概念 |
| `scripts/Core/Nodes/Combat/NCreature.cs`（`: Control`） | 战斗中用 2D UI 控件表示角色，按 `SlotName` 摆固定槽位，没有真实 3D 位置——命中判定需要真实空间坐标和碰撞体，UI 控件做不到这件事 |
| `scripts/Core/AI/TurnBaseAI/` 整个目录 | `TurnMoveStateMachine.RollMove()` 是"掷一次骰子决定这回合干嘛、完整执行完、等下一回合"的节奏，跟需要每帧重新评估的实时 AI 节奏不兼容 |

## 具体改造方案

### 保留的骨架

不动：`NEnvCreature`、移动状态机、`NFirstPersonCamera`、`WorldBehaviorTree`、Model/Node 三层分离、`Creature.HP`、`RunManager`。

### 删除/废弃

`CombatManager`、`CombatRoom`、`CombatState`、`NCreature`（2D UI 版本）、`TurnBaseAI/` 整个目录。

### 新建三块

**1. 遭遇战触发器**（替代 `CombatRoom` 的场景切换）
在 `Enviroment` 场景里放触发体积（Godot `Area3D`），玩家进入时：锁门 + 从已有的 `NMonsterSpwanPoint` 把怪物直接生成到同一个 3D 世界里。这跟经典 FPS（DOOM/Half-Life）"进门锁门、清怪开门"的关卡设计模式是一回事，只是不再切场景/切 UI。

**2. 实时命中判定层**（目前完全空缺）
新建 `WeaponController`（挂在 `_WeaponViewRoot` 上），开火输入触发时：从摄像机方向做 raycast，用 `LayerManager` 过滤碰撞层，打中 `NEnvCreature` 就直接扣它绑定的 `Creature.HP`。这条链路很短，`LayerManager` 和 `Creature.HP` 都是现成的，缺的只是"开火 → raycast → 扣血"这几行胶水代码。

**3. 给 `WorldBehaviorTree` 加攻击节点**
参照 `ZombieChaseNode.cs` 的写法，加一个 `ZombieAttackNode`：进入攻击距离、有冷却时间限制、触发时对玩家做一次实时伤害判定，跟 `ZombieChaseNode` 平级挂在同一棵行为树上。

### 建议的改造顺序（每一步都要保持能编译、能跑）

1. 去掉 `CombatRoom` 的场景切换，让 `Enviroment` 成为唯一的游戏场景
2. 用 `NMonsterSpwanPoint` + 一个触发器，做出"进门锁门、生成怪物"的最小闭环
3. 搭开火/命中判定这一层（`WeaponController`）
4. 给行为树加攻击节点，让怪物能反击
5. 验证整条链路能玩之后，再删除 `CombatManager`/`CombatRoom`/`CombatState`/`NCreature`/`TurnBaseAI`，不要让废弃代码留着增加后续维护的认知负担

### 一个暂时不用处理、但现在就该留口子的事

`scripts/Core/Net/` 目前是纯传输层代码（ENet 收发、包序列化），完全没有接入游戏逻辑（`INetGameService.cs` 是空接口，唯一调用点是 `NetTransportTestCmd.cs` 这个调试命令）。`MovementIntent`/`CharacterMovementContext` 目前默认"只有本地一个玩家"，没有"这份输入是本地预测还是网络权威结果"的概念。

**不需要现在就实现联机**，但建议在设计 `WeaponController`/新的战斗触发器时，也保持同样的"输入 → Intent 对象 → 状态机消费"的模式（`MovementIntent` 已经是这个模式了），方便以后如果真做 Host 共享联机，不用把整条链路推倒重来。
