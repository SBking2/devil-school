# FPS 代码框架设计文档

> 本文档面向单人 FPS 游戏在 Godot 4.6 + C#（`EGame` 命名空间）下的底层框架设计，设计思路综合参考了四款经典 FPS 引擎的公开源码：
> - **id Software `DOOM 3 BFG`**（id Tech 4，面向对象风格）
> - **Valve `Source SDK 2013`**（`gamemovement.cpp`/`basecombatweapon_shared.cpp`/`takedamageinfo.h` 等）
> - **id Software `Quake III Arena`**（及其现代维护分支 `ioquake3`/`Quake3e`）
> - **id Software `Quake II`**（通过 `Q2RTX` 的玩法代码部分）
>
> 这四款引擎代际跨度从 1997 到 2011，商业模式、网络架构、渲染技术完全不同，但在"角色移动""武器开火""命中判定""伤害应用""AI 战斗决策"这几件事上，收敛出了高度一致的设计形状。这份文档的核心任务，就是把这套收敛出来的形状，翻译成适合 devil-school 项目现有约定（`Core/`与`Core/Nodes/`镜像目录、`AbstractModel`数据目录、`N`前缀节点类、意图对象解耦输入源）的 C# 框架。
>
> 本文档只讨论**单人**场景下的框架设计——不引入网络权威校验、客户端预测、快照同步等联机相关的复杂度；但会在关键节点标注"如果未来要上网，这里的设计边界在哪"，避免现在的设计彻底堵死以后联机的路。

---

## 目录

1. 设计目标与非目标
2. 整体分层架构
3. 五条核心设计原则
4. 角色与移动层（已有基础的梳理与形式化）
5. 相机与视角层
6. 战斗核心：伤害管线
7. 命中判定层：Hitscan 与 Projectile
8. 武器与装备系统
9. 视觉反馈与"枪感"（武器手感 / Juice）
10. AI 与战斗决策层
11. 遭遇战与生成系统
12. UI / HUD 层
13. 需要废弃的旧系统与理由
14. 目录结构与文件清单
15. 关键类型签名草案（附录）
16. 四引擎设计对照表
17. 分阶段实施路线图

---

## 1. 设计目标与非目标

### 1.1 目标

- 提供一套**能同时服务玩家和怪物**的战斗底层——开枪、命中判定、伤害应用、死亡处理，这四件事必须是同一套代码，不能玩家一套、怪物一套。
- **瞄准方向与视觉表现严格分离**——不管镜头怎么晃、武器怎么后坐，真正决定"打没打中"的那条射线永远只由玩家的真实输入朝向决定。
- **武器是数据，不是代码**——新增一把武器应该是"填一张配置表"，而不是"写一个新类"。
- **AI 的"要不要打"和"打了会怎样"彻底解耦**——决策逻辑（`WorldAI` 行为树）只管什么时候扣扳机，扣下去之后发生什么事,跟决策代码完全无关。
- 复用 devil-school 已经验证过的好的设计（`MovementIntent` 的意图解耦模式、`Log.Logger` 按子系统分类、`AbstractModel`+`AbstractModelSubtypes` 的数据目录模式）——不是另起一套风格,是把现有的好模式延伸到新系统里。

### 1.2 非目标

- 不设计联机同步/权威校验协议（单人游戏不需要，但会标注设计上的"预留缝隙"）。
- 不涉及具体数值平衡（伤害数字、TTK、经济系统），这是策划要填的表,不是框架要决定的事。
- 不涉及渲染/粒子特效的具体实现细节（枪口火光用什么 Shader、弹孔贴花怎么做），只讨论"这些效果应该挂在哪一层、由谁触发"。
- 不重新设计整个 GDD 里提到的"任务驱动伪开放世界"区域状态系统——遭遇战/生成这一章只解决"怎么把一波怪放进当前场景"，不解决"这个区域根据任务进度应该呈现什么状态"，那是独立的、规模更大的系统，值得单独立项讨论。

---

## 2. 整体分层架构

在正式拆分系统之前，先明确一件事：devil-school 现有的 `Core/` 与 `Core/Nodes/` 镜像结构本身就是对的分层原则，本文档里所有新系统都会延续这个原则，不会引入第三种分层方式。简单复述一下这个原则，因为后面每一节都会用到：

- **`Core/<Feature>/`**：纯 C# 类，不继承任何 Godot 类型。这一层放"数据"和"规则"——什么是伤害、伤害怎么应用、武器有哪些属性。这一层的代码理论上脱离 Godot 引擎本身也该能做单元测试（虽然这个项目目前没有测试框架，但这个特性依然是层与层之间该不该混在一起的一个很好的试金石）。
- **`Core/Nodes/<Feature>/`**：Godot 节点包装类，`N` 前缀。这一层是"表现"和"输入"——武器模型怎么摆在屏幕上、开火键按下去这个事件怎么被捕获。这一层的类允许直接 `GetNode`、直接碰 `Position`/`Velocity` 这些 Godot 特有的东西。
- **`Model/`**：编译期数据目录，`AbstractModel` 体系。这一层是"配置"——一把武器伤害多少、射速多快，这些数字应该长在这里，而不是散落在某个节点脚本的字段里。

新增的战斗系统整体上挂在这三层之下，大致的调用链是：

```
玩家输入 (NWeaponInput)
    ↓ 写意图
MovementIntent 式的 WeaponIntent（想开火/想换弹/想切枪）
    ↓ 读意图，做真正的开火判断
NWeapon（Node，挂在相机的武器挂点上）
    ↓ 调用共享的命中判定
HitDetection（静态方法，Core 层，不认识"这是谁开的枪"）
    ↓ 命中后构建
DamageInfo（纯数据）
    ↓ 应用到
Creature.ApplyDamage()（已有的 HP 系统的扩展）
    ↓ 触发事件
Creature.OnDamaged / OnKilled
    ↓ 双向消费
NCreatureHUD（UI 反应） + WorldAI.NotifyEvent（AI 反应）
```

怪物开火是同一条链的另一个入口：`WorldNodeAction`（行为树的攻击节点）直接调用 `HitDetection`，跳过 `NWeapon`（那是给玩家输入用的），但落到 `DamageInfo` → `ApplyDamage` 这一段完全相同。这正是四个引擎调研里最一致的那条规律的直接体现。

---

## 3. 五条核心设计原则

这五条是贯穿整份文档的准则，后面每一节的具体设计都是在贯彻这五条，先摆在这里，方便后面每次做设计取舍的时候回来对照。

### 原则一：单一伤害管线，来源解耦

Quake II 的怪物调用和玩家完全相同的 `fire_bullet()`；Quake III 的 bot AI 是直接模拟按键，走的是和真人玩家一模一样的 `FireWeapon()`；Source 的 NPC 用的是和玩家相同的 `CBaseCombatWeapon` 类实例；就连做法最特殊的 DOOM 3（AI 有自己独立的开火/瞄准代码）最终也收敛到同一个 `idProjectile` 类和同一个 `Entity::Damage()` 调用上。**没有一家引擎让 AI 走一条独立的伤害判定代码。**

翻译过来的设计约束是：`HitDetection.FireHitscan(...)` 和 `Creature.ApplyDamage(...)` 这两个入口，必须同时被"玩家武器开火代码"和"怪物攻击行为树节点"调用，而且调用方式没有任何特殊分支去区分"这次调用是不是玩家发起的"。如果发现自己在这两个函数里写了 `if (attacker.IsPlayer) { ... } else { ... }` 这种分支，基本可以确定是设计错了。

### 原则二：瞄准方向与视觉表现严格分离

DOOM 3 的做法最直白：`GetProjectileLaunchOriginAndAxis()` 在任何后坐力/视觉抖动被应用**之前**，先用玩家相机的真实朝向（`playerViewAxis`）算出开火方向；后坐力（`MuzzleRise`）只改武器**渲染模型**的位置和角度，从头到尾不碰这个已经算好的开火方向。devil-school 的 `NFirstPersonCamera` 其实已经在贯彻同一个原则了——`CameraEffectPos`（Camera Bob 用的挂点）是相机层级里专门隔出来的一层，`HorizontalQuaternion`/`VerticalQuaternion`（真正的瞄准朝向,给 `INCamera` 接口用的）读的是更上层、没被 Bob 污染的节点。这条原则不是新东西，是把已经验证过的分层继续延伸到武器后坐力上。

具体约束：`NWeapon` 计算开火射线的时候，必须读 `NFirstPersonCamera` 暴露的"纯瞄准朝向"（`HorizontalQuaternion`/`VerticalQuaternion` 或者相机的 `GlobalTransform.Basis` 在 Bob/Sway 应用之前的版本），**不能**读武器模型自己节点的 `GlobalTransform`——因为武器模型节点本身就是会被后坐力动画污染的那个节点。

### 原则三：决策与执行分离

`WorldAI` 的行为树只回答"现在要不要攻击"这一个问题，一旦决定"要"，剩下的（命中判定、伤害应用、死亡处理）全部交给共享管线，行为树节点本身不应该包含任何伤害计算逻辑。这跟 devil-school 现有的 `WorldNodeAction`（比如 `ZombieChaseNode`）只负责设置 `Intent.MoveDir`、不负责移动物理计算，是完全一致的分层思路。

### 原则四：数据驱动，不是子类驱动

DOOM 3 只有**一个** `idWeapon` C++ 类，所有武器的差异（伤害、射速、弹药类型、视角摇摆参数）都是数据（`idDeclEntityDef`），不是子类。Source 相反，走的是"每把武器一个 C++ 子类 + 一个 `.txt` 静态配置文件"的路子，代码量明显更大，新增一把武器的成本也更高。devil-school 已经有一套现成的、被验证过的数据驱动机制——`AbstractModel` + `AbstractModelSubtypes` 反射注册表——武器系统应该直接躺进这套机制里，而不是给每把武器写一个 C# 子类。

### 原则五：权威点唯一（即使单人也要遵守）

即使不联机，`Creature.HP` 也应该只有一个地方能被写——`ApplyDamage()` 内部。任何代码想给某个生物扣血/加血，都必须走这一个入口，不允许在别的地方直接 `creature.HP -= x`。这不是为了性能或者代码整洁，是为了给"以后要不要联机"这件事留活路：如果现在到处都在直接改 HP，以后要把这个改动集中到一个权威节点上，就是一次痛苦的全项目重构；如果现在就只有一个入口，以后加一层"只有 host 才能真正调用这个入口"的检查，改动量极小。

---

## 4. 角色与移动层（已有基础的梳理）

这一层这次对话里已经花了大量篇幅打磨过，这里做一次结构化的复述，作为后面章节的基础，同时明确"哪些是已经定型、不该再动的地基"。

### 4.1 意图与执行分离

`NEnvCreature.Intent`（`MovementIntent` 类型）是玩家输入和怪物 AI 共用的"意图槽"——`NEnviromentInput`（玩家输入）和 `ZombieChaseNode`（AI 行为树节点）都只管写 `Intent.MoveDir`/`Intent.WantsCrouch`/`Intent.WantsRun`/`Intent.WantsJump`，移动状态机自己决定"这个意图现在能不能被满足"。这个模式在本文档里会被直接复用到武器系统上——见 8.2 节的 `WeaponIntent`。

### 4.2 状态机与正交修饰

`CharacterMovementStateMachine` 管的是互斥状态（Idle/Walk/Run/Jump/Fall），`IsCrouching` 是叠加在任何状态之上的正交修饰,不是状态机里的一个状态节点——这个设计决策（用"正交修饰位"而不是"状态数量爆炸"）在后面设计"瞄准/开镜（ADS）"的时候会被直接复用：ADS 不该是移动状态机里的一个新状态,应该是跟 `IsCrouching` 同级的另一个正交修饰位。

### 4.3 重力与碰撞体的通用能力

`NEnvCreature` 上的 `UpdateGravity`/`ColliderHeight` 这些是"任何生物无条件需要的底盘能力",跟具体在哪个移动状态无关——这个"底盘能力 vs 状态相关行为"的区分标准，后面设计 `Creature.HP`/`ApplyDamage` 要不要挂在 `NEnvCreature` 还是 `Creature` 上时会再次用到（结论见 6.4 节：挂在 `Creature`，因为伤害是纯数据层面的事，不需要 Godot 节点）。

### 4.4 AI 的移动意图从哪来——不能是"直线冲过去"

`MovementIntent`（`Intent.MoveDir` 这个字段）本身是不是一个好设计，之前查过 Quake III 的 `usercmd_t`，确认玩家输入和 bot 输入共用同一个执行层结构体、共用同一个 `Pmove()`，跟 devil-school 现在的做法是同一个思路。但那只回答了"要不要共享这个字段"，没回答**AI 怎么算出该往哪个方向走**——这是更难、也更容易被忽略的问题。补一轮专门查 DOOM 3 `idAI` 内部移动机制之后，答案很清楚：**没有一个引擎真的让怪物 AI 直接把"目标位置减去自己位置"塞进执行层**——那样做在真实关卡（有墙、有障碍物、多个怪物同时围上来）里会立刻穿帮：撞墙、卡在场景里、一堆怪物挤在一起走不动。现在 `ZombieChaseNode` 里 `Intent.MoveDir = (target.GlobalPosition - context.Owner.GlobalPosition).Normalized()` 这行代码，正是这个"会穿帮"的写法。

DOOM 3 的 `idAI` 实际上是一条五层管线（`neo/d3xp/ai/AI.cpp`）：

1. **目标设定**（`MoveToEntity`/`MoveToPosition`，事件触发，不是每帧调）——只存一个目标位置，并且用 AAS（Area Awareness System，关卡离线烘焙出的区域连通图）校验过这个目标真的可达，存的是区域号,不是方向。
2. **AAS 寻路**（每帧都重新算，但很便宜）——`PathToGoal()` 从一张预先烘焙、按目标区域缓存的路由表里查表（不是每帧现场跑 A*），再做一次"能不能抄近路直接看到下一段路"的直线可见性裁剪（string-pulling），产出的是沿途"下一步该往哪走"的一个点，不是终点本身。
3. **局部动态避障**（`FindPathAroundObstacles`，每帧）——跟静态寻路完全独立的第二套系统，专门躲开寻路网格不知道的东西：别的怪物、可推动的物体。这是"直线冲过去"完全没有的能力。
4. **平滑转向**（`Turn`/`TurnToward`）——朝向不是瞬间对齐，是按角速度上限慢慢转过去,不会瞬间掉头，读起来才像个活物而不是机器人。
5. **位移来源**——走路的怪物用**动画自带的位移**（root motion，速度和步频由动画本身决定）；飞行怪物才是"速度向量 + Seek 转向"这种传统 steering 写法。

**关键的一点**：DOOM 3 自己也确实有一个"直接冲过去"的模式——`DirectMoveToPosition()`，公式就是 `(目标位置-自己位置).Normalize() * speed`，跟 `ZombieChaseNode` 现在写的一模一样——但 id 把它明确当成**特例**（脚本/过场强制移动用），不是通用的战斗 AI 移动方式。翻译过来：`ZombieChaseNode` 现在这行代码不是"简化版寻路"，是 id 自己也只拿来处理特殊情况的那条路，不该是怪物打架时唯一的移动方式。

**这不代表要在 devil-school 里重新发明一套 AAS。** Godot 引擎自己已经内置了对应的东西——`NavigationRegion3D`（离线烘焙导航网格，对应 AAS 的静态几何路由表）+ `NavigationAgent3D`（挂在怪物身上的组件，`TargetPosition` 设一次目标、每帧读 `GetNextPathPosition()` 拿到"下一步该往哪走"，内置基于 RVO 的避障，对应 DOOM 3 的局部避障层）。这两个组件几乎是 DOOM 3 那套管线里第 2、3 层的现成替代品，不需要手写寻路和避障算法，Godot 已经把这块做进引擎了。

**"意图机制"这个抽象本身不用推翻，需要改的是意图从哪来。** `MovementIntent.MoveDir` 依然是对的边界——移动状态机不关心这个方向是玩家按键算出来的还是 AI 算出来的，这条分层原则站得住,前面查证的"跟 Quake III 的 `usercmd_t` 是同一个思路"这个结论也依然成立。真正要改的是 `ZombieChaseNode` 内部怎么产生这个 `MoveDir`：

```csharp
// AI/WorldAI/MonsterAI/Zombie/ZombieChaseNode.cs —— 修改后
namespace EGame
{
    public class ZombieChaseNode : WorldNodeAction
    {
        public ZombieChaseNode() : base("chase") { }

        public override void OnEnter(WorldBehaviorContext context)
        {
            // 目标设定：只在进入这个节点/目标变化时设一次，不是每帧重算
            if (context.Blackboard.TryGetValue(ZombieAI.TargetKey, out var v) && v is NEnvCreature target)
                context.Owner.NavigationAgent.TargetPosition = target.GlobalPosition;
        }

        protected override WorldBehaviorStatus OnTick(WorldBehaviorContext context)
        {
            var nav_agent = context.Owner.NavigationAgent;   // NavigationAgent3D，挂在 NEnvCreature 上
            if (nav_agent.IsNavigationFinished())
                return WorldBehaviorStatus.Success;

            // 每帧只做一件事：读"下一步该往哪走"，塞进意图——不再是直接对目标位置做减法
            var next_point = nav_agent.GetNextPathPosition();
            context.Owner.Intent.MoveDir = (next_point - context.Owner.GlobalPosition).Normalized();

            return WorldBehaviorStatus.Running;
        }

        public override void OnExit(WorldBehaviorContext context)
        {
            context.Owner.Intent.MoveDir = Vector3.Zero;
        }
    }
}
```

`Intent.MoveDir` 这个字段完全没变，移动状态机、玩家输入那边一行代码都不用碰——变的只是"谁在往这个字段里写什么值"。这正是"意图机制"这个设计原本就该有的弹性：**执行层的抽象足够稳定，决策层可以从"直线冲过去"升级成"绕着走"，不需要动执行层一行代码**。这也是为什么第 3 节的五条原则要把"决策与执行分离"单独列一条——它带来的好处不只是代码整洁，是真的允许决策逻辑独立升级而不牵连执行层，这次的调研正好是这条原则的一个实例。

落地上要补的东西：`NEnvCreature` 加一个 `NavigationAgent3D` 子节点引用（跟现在的 `_MoveCollider` 是同一种"挂点缓存"模式），场景里怪物能走动的区域需要铺一个 `NavigationRegion3D` 并烘焙导航网格（这是策划/关卡搭建阶段的工作，不是代码）。这块不是本文档战斗系统的核心内容，但既然直接影响 `WorldNodeAction` 怎么写，放在这里一起记录。

---

## 5. 相机与视角层

### 5.1 现状与要补的东西

`NFirstPersonCamera` 目前实现了 Camera Bob / Weapon Bob / Weapon Sway，`FPS_Camera_Tutorial.md` 里描述的 ADS、视觉后坐力拆分、探头倾身、武器防穿墙这些还没做。武器系统真正依赖的，只有其中两块：**瞄准射线的获取方式**，和**开火后坐力挂在哪个节点上**。

### 5.2 瞄准射线

新增一个只读属性，供 `NWeapon` 使用：

```csharp
// NFirstPersonCamera.cs
public Vector3 AimDirection => -_YawPivot.GlobalTransform.Basis.Z.Normalized();
public Vector3 AimOrigin => _YawPivot.GlobalPosition;
```

注意这里读的是 `_YawPivot`，不是 `_CameraEffectPos`（Bob 效果挂点）也不是最终的 `_RealCamera`——`_YawPivot` 是 Bob/Sway 计算发生**之前**的那一层，是"真实瞄准朝向"和"视觉表现"的分界线。这跟 `GetLookAtPos()` 内部已经在用 `_YawPivot.GlobalTransform.Basis.Z` 是同一个思路，只是现在要把它暴露成公开接口给武器系统用。

### 5.3 武器挂点与后坐力隔离

`_WeaponViewRoot` 这个挂点已经存在（`NFirstPersonCamera.cs`），目前只用来挂视觉模型（`SetVisualParent`）。新增一层专门给开火后坐力用的挂点，插在 `_WeaponSwayPos` 和 `_WeaponViewRoot` 之间：

```
_CameraEffectPos
  └─ _WeaponBobPos       (移动引起的位置起伏)
      └─ _WeaponSwayPos  (视角/移动引起的旋转摇摆)
          └─ _WeaponKickPos   ← 新增，开火后坐力专用
              └─ _WeaponViewRoot  (真正挂武器模型)
```

`_WeaponKickPos` 由 `NWeapon` 自己驱动（不是 `NFirstPersonCamera` 驱动），每次开火往上叠加一个衰减的旋转/位移脉冲，用法跟现有 `WeaponSway` 的指数衰减平滑（`1-exp(-speed*delta)`）完全一致，风格上不引入新写法。

---

## 6. 战斗核心：伤害管线

这是整份文档承上启下的核心章节，前面的移动/相机是基础设施，后面的武器/AI/UI 都是这一节定义的管线的消费者。

### 6.1 为什么不是简单的 `HP -= damage`

四个引擎里，最简单的 Quake II 的 `T_Damage` 都不是一行减法——它至少要处理：难度系数缩放、友军伤害过滤、护甲吸收、击退力（跟血量damage分开算）、死亡判定与回调分发。Source 的 `CTakeDamageInfo` → `TakeDamage()` → `OnTakeDamage()` 这条链更进一步，把"命中判定/多重伤害合并"、"伤害门禁过滤（游戏规则/伤害过滤器）"、"按生命状态分发（活着/濒死/已死）"拆成了三个独立阶段。

devil-school 目前的 `Creature.HP` 只有属性 setter 触发事件，没有任何中间处理——这是要补的第一个大缺口。

### 6.2 DamageInfo：伤害的数据载体

参考 Source 的 `CTakeDamageInfo`，但做简化（不需要 `m_iDamageStats`/`m_iPlayerPenetrationCount` 这类联机统计相关的字段）：

```csharp
// Core/Combat/DamageInfo.cs
namespace EGame
{
    public class DamageInfo
    {
        // 谁该为这次伤害负责——用于击杀归因、AI 转向反击的目标
        public Creature Attacker { get; }

        // 伤害的直接来源——可能和 Attacker 是同一个人（近战），
        // 也可能不是（火箭弹本身是 Inflictor，发射火箭的人是 Attacker）
        public object Inflictor { get; }

        // 原始伤害量，护甲/抗性吸收之前的数值
        public float Amount { get; }

        public DamageType DamageType { get; }

        // 命中点与命中法线，世界坐标——给击退力、命中特效、受击方向指示器用
        public Vector3 HitPoint { get; }
        public Vector3 HitNormal { get; }

        // 击退冲量方向（通常等于开火方向，但爆炸伤害是从爆心指向外）
        public Vector3 KnockbackDirection { get; }
        public float KnockbackForce { get; }

        public DamageInfo(Creature attacker, object inflictor, float amount, DamageType type,
            Vector3 hit_point, Vector3 hit_normal, Vector3 knockback_dir, float knockback_force)
        {
            Attacker = attacker;
            Inflictor = inflictor;
            Amount = amount;
            DamageType = type;
            HitPoint = hit_point;
            HitNormal = hit_normal;
            KnockbackDirection = knockback_dir;
            KnockbackForce = knockback_force;
        }
    }
}
```

`Inflictor` 故意声明成 `object` 而不是具体类型——命中判定阶段（hitscan）可能没有一个实体化的"子弹对象"，`Inflictor` 这时候就是 `WeaponModel` 本身；如果是火箭弹这种真的会飞行的实体，`Inflictor` 就是那个飞行中的 `Creature`/节点。用 `object` 避免为了这一个字段单独抽一个 `IInflictor` 接口——如果以后发现真的需要区分处理，再回来加接口也不迟（这是"不要为了假设中的未来需求引入抽象"这条一般性原则的具体应用）。

### 6.3 DamageType：伤害类型标志位

参考 Source 的 `DMG_*`（30 个标志位，但注释里也承认"大多数 mod 只会用到其中几个"）和 DOOM 3 的"伤害类型是数据不是代码"思路，做一个精简版：

```csharp
// Core/Combat/DamageType.cs
namespace EGame
{
    [Flags]
    public enum DamageType
    {
        None = 0,
        Bullet = 1 << 0,
        Blast = 1 << 1,      // 爆炸/范围伤害
        Melee = 1 << 2,
        Fall = 1 << 3,
        Fire = 1 << 4,
        NoArmor = 1 << 5,      // 无视护甲吸收（修饰位，不是"伤害种类"）
        NoKnockback = 1 << 6,  // 无视击退（修饰位）
    }
}
```

不做成 30 个标志位——devil-school 目前没有护甲系统、没有多种死法特效、没有肢解/溶解这类需要精细分类的表现，先给够用的最小集合，真正要加新类型的时候，加一个标志位是一行代码的事，不需要一开始就预判所有可能性。

### 6.4 Creature.ApplyDamage：管线的落地点

在现有的 `Creature.cs` 上做**扩展**，不是重写——`HP`/`MaxHP`/`OnHPChanged` 这些已经工作正常的东西原样保留，新增：

```csharp
// Creature.cs 新增部分
public event Action<DamageInfo> OnDamaged;
public event Action<DamageInfo> OnKilled;

public bool IsDead => HP <= 0;

public void ApplyDamage(DamageInfo info)
{
    // 门禁：已经死了的目标不再处理伤害（防止爆炸范围伤害对同一个尸体连续触发多次死亡回调）
    if (IsDead)
        return;

    float actual = ResolveDamageAmount(info);

    HP = Math.Max(0, HP - (int)actual);

    OnDamaged?.Invoke(info);

    if (IsDead)
        OnKilled?.Invoke(info);
}

// 护甲/抗性缩放的唯一入口——现在没有护甲系统，先原样返回，
// 以后加护甲/抗性百分比，只需要改这一个方法，不用动 ApplyDamage 本身
private float ResolveDamageAmount(DamageInfo info)
{
    return info.Amount;
}
```

为什么不让 `ApplyDamage` 直接判断护甲逻辑，而是拆出一个 `ResolveDamageAmount`？因为参考 Source 的设计——`GetAttackDamageScale()`（攻击者侧的伤害倍率，比如暴击/难度加成）和 `GetReceivedDamageScale()`（承受者侧的伤害倍率，比如护甲/抗性）是两个独立的缩放点，即使现在没有护甲系统，把这个缝隙先留出来，比日后在 `HP -= (int)actual` 这一行代码周围反复插入 if 分支要干净得多。

`HP` 复用现有的 `int` 类型和 setter——`Creature.cs` 里 `HP`/`MaxHP` 已经是 `int` 并且 setter 会触发 `OnHPChanged`，`ApplyDamage` 只是在这个已有机制外面包了一层伤害计算和事件派发，`OnHPChanged` 依然会像现在一样被 UI 消费（比如未来的 HUD 血条），`OnDamaged`/`OnKilled` 是新增的、专门给"这次伤害是怎么来的"这类信息服务的事件，两者不冲突，各管各的。

---

## 7. 命中判定层：Hitscan 与 Projectile

### 7.1 一个方法覆盖两种命中方式

DOOM 3 给出了这份文档里最值得抄的一个设计决策：**不为"瞬间命中"和"会飞行的子弹"设计两套代码**，而是统一走同一个 `idProjectile` 类，用速度是否为"瞬间"（`net_instanthit` 标志、或者速度大到一帧内飞完全程）来区分表现上的差异。命中判定和伤害应用这两段代码完全共享。

devil-school 的实现可以更直接——因为 Godot 的物理系统里，"瞬间完成的移动"本来就是一次 `IntersectRay`，"会飞行的移动"是每帧位移的 `RigidBody3D`/手写位移，没必要强行套 DOOM 3 那种"极快的刚体"的壳子。所以设计成两个方法，但共享同一个"命中后做什么"的收尾：

```csharp
// Core/Combat/HitDetection.cs
namespace EGame
{
    public static class HitDetection
    {
        // 瞬间命中（子弹类武器）：一条射线，立即算出结果
        public static bool FireHitscan(
            World3D world,
            Vector3 origin,
            Vector3 direction,
            float range,
            Creature attacker,
            WeaponModel weapon,
            Rid excluded_rid)
        {
            var space_state = world.DirectSpaceState;
            var end = origin + direction.Normalized() * range;

            var query = PhysicsRayQueryParameters3D.Create(origin, end);
            query.Exclude = new Godot.Collections.Array<Rid> { excluded_rid };
            query.CollisionMask = (uint)(1u << (int)LayerManager.Layer.Creature | 1u << (int)LayerManager.Layer.Ground);

            var result = space_state.IntersectRay(query);
            if (result.Count == 0)
                return false;

            var hit_point = (Vector3)result["position"];
            var hit_normal = (Vector3)result["normal"];
            var collider = result["collider"].AsGodotObject();

            if (collider is NEnvCreature hit_creature)
            {
                var info = new DamageInfo(
                    attacker, weapon, weapon.Damage, weapon.DamageType,
                    hit_point, hit_normal, direction, weapon.KnockbackForce);

                hit_creature.Data.ApplyDamage(info);
            }

            // 不管打没打到生物，都要给命中特效系统一个落点——弹孔贴花/火花这些不关心打没打到人
            // （具体特效触发留给调用方或者一个单独的 HitFx 系统，这里不做，保持这个方法职责单一）

            return true;
        }

        // 带扩散的多发命中（霰弹枪）：内部循环调用 FireHitscan，
        // 参考 Source 的 CShotManipulator 做法——每一发独立算一个在扩散锥内随机偏转的方向
        public static void FireHitscanSpread(
            World3D world, Vector3 origin, Vector3 base_direction,
            int pellet_count, float spread_degrees, float range,
            Creature attacker, WeaponModel weapon, Rid excluded_rid, Rng rng)
        {
            for (int i = 0; i < pellet_count; i++)
            {
                var dir = ApplySpread(base_direction, spread_degrees, rng);
                FireHitscan(world, origin, dir, range, attacker, weapon, excluded_rid);
            }
        }

        private static Vector3 ApplySpread(Vector3 direction, float spread_degrees, Rng rng)
        {
            // 在朝向为 direction 的圆锥内取一个随机方向，锥角为 spread_degrees
            float yaw = rng.RealRandom.RangeFloat(-spread_degrees, spread_degrees);
            float pitch = rng.RealRandom.RangeFloat(-spread_degrees, spread_degrees);
            var basis = Basis.Identity;
            basis = basis.Rotated(Vector3.Up, Mathf.DegToRad(yaw));
            basis = basis.Rotated(Vector3.Right, Mathf.DegToRad(pitch));
            return (basis * direction).Normalized();
        }
    }
}
```

`FireHitscan` 用到的 `PhysicsRayQueryParameters3D`/`DirectSpaceState.IntersectRay` 写法，跟 `NEnvCreature.CanStandUp()` 里已经在用的完全一致——这不是巧合，是刻意保持项目里"怎么用 Godot 做射线检测"这件事只有一种写法，减少认知负担。

### 7.2 Projectile（会飞行的弹药）

不是这次要立刻实现的东西，但设计上要留好扩展点：`idProjectile` 的模式是"一个 `NEnvCreature` 之外的、独立的 `CharacterBody3D`/`Area3D` 节点，每帧自己检测碰撞，碰到东西就调用跟 `HitDetection.FireHitscan` 收尾部分完全相同的伤害应用代码"。等真的要做火箭弹/手雷这类武器时，应该新增 `Nodes/Combat/NProjectile.cs`，内部碰撞后直接构造同样的 `DamageInfo` 调 `Creature.ApplyDamage`，不要另起一套伤害应用逻辑。

### 7.3 多重命中合并（先记下，不急着做）

Source 的霰弹枪用一个全局的 `g_MultiDamage` 累加器，把同一帧打在同一个目标身上的多发伤害合并成一次 `TakeDamage()` 调用，而不是让 `OnDamaged`/`OnHPChanged` 事件为一枪霰弹触发八次。这在乎"击杀播报只报一次""受击特效只放一次"这类表现细节的时候才有意义——单人游戏第一版可以先不做，`FireHitscanSpread` 允许每个 pellet 各自独立调用 `ApplyDamage`，等实际测试觉得受击反馈"抖得太碎"了，再回来加合并逻辑。

### 7.4 近战判定：短距离扫描，不是长射线

补了一轮专门针对近战的调研之后（DOOM 3、Source、Quake III、Quake II 四家都查了），近战判定在四个引擎里呈现出跟远程武器明显不同、但彼此高度一致的形状——**没有一家用远程武器那种长射线做近战判定**，而是用更短、更"宽容"的检测方式：

- **DOOM 3** 的 `idAI::TestMelee()` 是"包围盒扩张重叠检测 + 一条视线确认射线"的组合：先把攻击者自己的碰撞盒沿 `melee_range` 往外扩张，跟目标的碰撞盒做重叠测试（粗判），重叠了之后再补一条从攻击者视点到目标视点的点射线，确认中间没有墙挡着（细判）。玩家的近战（拳头 `weapon_fists`）本身是一个完整的 `idWeapon` 实例，走的是跟远程武器完全相同的武器管线，没有另起一套"近战武器系统"——这一点直接验证了本文档 8.1 节"武器是数据不是代码"的设计是站得住脚的。
- **Source** 有专门的近战武器基类 `CBaseHLBludgeonWeapon`（撬棍 `CWeaponCrowbar` 继承自它），检测方式是"先来一条短距离细射线，没打中就退一步用一个盒子（Hull）扫一次"——细射线负责精确，盒子扫描负责容错，两者都比子弹用的那种"零体积长射线"更短更"粗"。NPC 近战（比如僵尸的抓挠攻击）走的是共享的 `CheckTraceHullAttack()`，同样是盒子扫描，不是点射线。
- **Quake III** 的拳套（Gauntlet）反而是这几个里最接近"远程武器判定"的——就是一条 32 单位的短射线，复用跟子弹完全相同的 `trap_Trace`，只是距离短。但判定时机很特殊：不走 `FireWeapon()` 的正常分发，而是每帧单独在玩家输入处理里检查，跟武器状态机本身脱钩（这个游戏本身没有怪物/NPC，纯 PvP，所以查不到任何 AI 近战的对照）。
- **Quake II** 的怪物近战 `fire_hit()` 是"先算直线距离，够近才继续；再补一条射线算出精确的命中点（用来做击退方向）"——距离检测在前、射线在后，跟 DOOM 3 的思路（重叠检测在前、射线在后）本质上是同一个"粗判 + 细判"两段式，只是粗判手段不同（包围盒重叠 vs 纯距离比较）。Quake II 的玩家本身没有任何近战武器，`fire_hit()` 只给怪物用。

**共同点：近战判定要么是短射线，要么是短射线+盒子的组合，从来不是远程武器那种穿过整个地图的长射线；但伤害应用这一步，四家全部复用跟远程武器完全相同的函数**（`G_Damage`/`T_Damage`/`Entity::Damage`/`TakeDamage`）——近战没有让任何一家引擎破例开一条独立的伤害通道，这跟第 3 节"原则一：单一伤害管线"完全吻合。

翻译成 `HitDetection` 的新方法，作为 `FireHitscan`/`FireHitscanSpread` 的第三个兄弟方法，命中判定用"细射线优先、miss 了退一步用盒子扫一次"（照抄 Source 的 `Swing()` 思路，因为这是四家里对"容错"处理得最细致的一版）：

```csharp
// Core/Combat/HitDetection.cs 新增
// 近战判定：短距离，比 Hitscan 更"宽容"——先用一条细射线，miss 了再退一步用盒子扫一次，
// 兼顾精确（打得准的时候不需要盒子那种误差）和容错（近战不该因为准星差了几像素就打空）
public static bool FireMelee(
    World3D world,
    Vector3 origin,
    Vector3 direction,
    float reach,
    Vector3 hull_extents,
    Creature attacker,
    WeaponModel weapon,
    Rid excluded_rid)
{
    var space_state = world.DirectSpaceState;
    var end = origin + direction.Normalized() * reach;

    var ray_query = PhysicsRayQueryParameters3D.Create(origin, end);
    ray_query.Exclude = new Godot.Collections.Array<Rid> { excluded_rid };
    ray_query.CollisionMask = (uint)(1u << (int)LayerManager.Layer.Creature);

    var ray_result = space_state.IntersectRay(ray_query);
    if (ray_result.Count > 0)
    {
        var hit_point = (Vector3)ray_result["position"];
        return ApplyMeleeHit(ray_result["collider"].AsGodotObject(), attacker, weapon, hit_point, direction);
    }

    // 细射线没打中，退一步在终点用盒子做一次重叠检测——参考 Source Swing() 的"细线优先、盒子兜底"
    var shape_query = new PhysicsShapeQueryParameters3D
    {
        Shape = new BoxShape3D { Size = hull_extents * 2f },
        Transform = new Transform3D(Basis.Identity, end),
        CollisionMask = (uint)(1u << (int)LayerManager.Layer.Creature),
        Exclude = new Godot.Collections.Array<Rid> { excluded_rid }
    };
    var overlaps = space_state.IntersectShape(shape_query);
    if (overlaps.Count == 0)
        return false;

    return ApplyMeleeHit(overlaps[0]["collider"].AsGodotObject(), attacker, weapon, end, direction);
}

private static bool ApplyMeleeHit(GodotObject collider, Creature attacker, WeaponModel weapon, Vector3 hit_point, Vector3 direction)
{
    if (collider is not NEnvCreature hit_creature)
        return false;

    var info = new DamageInfo(attacker, weapon, weapon.Damage, weapon.DamageType,
        hit_point, -direction, direction, weapon.KnockbackForce);
    hit_creature.Data.ApplyDamage(info);
    return true;
}
```

给 `WeaponModel` 补一个命中方式的标志，不需要为近战武器单独派生一个类——延续"原则四：数据驱动"，见 8.1 节的更新。

---

## 8. 武器与装备系统

### 8.1 WeaponModel：数据驱动的武器定义

延续 `CharacterModel`/`MonsterModel`/`PlayerModel` 那一套 `AbstractModel` 体系，武器作为平级的新分类：

```csharp
// Model/WeaponModel.cs
namespace EGame
{
    public enum WeaponHitMode { Hitscan, Melee }

    [ModelCategory]
    public abstract class WeaponModel : AbstractModel
    {
        public virtual float Damage => 10f;
        public virtual float FireRate => 0.15f;       // 两次开火之间的最小间隔（秒）
        public virtual float Range => 100f;
        public virtual DamageType DamageType => DamageType.Bullet;
        public virtual float KnockbackForce => 2f;

        // 0 = 单发命中（手枪/步枪）；> 1 = 霰弹式扩散
        public virtual int PelletCount => 1;
        public virtual float SpreadDegrees => 0f;

        public virtual int MaxAmmo => 30;
        public virtual float ReloadTime => 1.5f;

        // Hitscan = 走 7.1 节的长射线；Melee = 走 7.4 节的短距离扫描，两者共用同一个 NWeapon 类，
        // 不需要为近战另外派生一个节点类型——这是"武器是数据不是代码"（原则四）的直接体现
        public virtual WeaponHitMode HitMode => WeaponHitMode.Hitscan;
        public virtual Vector3 MeleeHullExtents => new Vector3(0.3f, 0.3f, 0.3f);

        protected virtual string _ViewModelPath => "weapons/" + ID.Entry.ToLowerInvariant();

        public NWeaponVisual CreateViewModel()
        {
            return SceneHelper.LoadScene<NWeaponVisual>(_ViewModelPath);
        }
    }
}
```

具体武器直接继承，只覆盖需要不同的字段：

```csharp
// Model/WeaponModels/PistolModel.cs
namespace EGame
{
    public class PistolModel : WeaponModel
    {
        public override float Damage => 15f;
        public override float FireRate => 0.25f;
        public override int MaxAmmo => 12;
    }
}

// Model/WeaponModels/ShotgunModel.cs
namespace EGame
{
    public class ShotgunModel : WeaponModel
    {
        public override float Damage => 8f;      // 单发 pellet 伤害
        public override float FireRate => 0.8f;
        public override int PelletCount => 8;
        public override float SpreadDegrees => 4f;
        public override float Range => 25f;      // 有效射程比手枪短
        public override int MaxAmmo => 6;
    }
}

// Model/WeaponModels/KnifeModel.cs —— 近战武器，走同一个 WeaponModel 体系，
// 只是把 HitMode 切成 Melee，Range 当成"近战触及距离"用，MaxAmmo/ReloadTime 保持默认（用不上，不用管）
namespace EGame
{
    public class KnifeModel : WeaponModel
    {
        public override float Damage => 35f;
        public override float FireRate => 0.5f;
        public override float Range => 1.5f;             // 近战触及距离
        public override DamageType DamageType => DamageType.Melee;
        public override WeaponHitMode HitMode => WeaponHitMode.Melee;
    }
}
```

跟所有 Model 一样，新武器写完之后要手动加进 `AbstractModelSubtypes.cs` 的数组——这是整个 Model 体系一贯的手动注册要求，不因为这是"武器"就有例外。`KnifeModel` 这个例子直接验证了 8.1 节开头那句话——近战武器不需要一个新的类层级，只是 `WeaponModel` 的又一个具体子类，`NWeapon.Fire()` 内部按 `HitMode` 分流到 `HitDetection.FireHitscan` 还是 `HitDetection.FireMelee`（7.4 节），冷却/弹药这些逻辑完全不用重写。

### 8.2 装备与武器的关系：现在做多深

上一轮讨论里提到 GDD 强调"装备驱动的长期成长"，这里给一个务实的分层建议，而不是现在就把整个装备系统焊死：

- **这一版只做"武器本身是一种可切换的装备"**——`WeaponModel` 是数据，玩家有一个"当前装备的武器"槽位。
- **不在这一版设计"武器改装件/词条/护甲"**——但 `WeaponModel` 的字段设计要避免把数值写死成常量表达式以外的形式（比如不要把伤害算法直接内联在开火代码里），这样以后要加"这把武器装了一个+20%伤害的配件"，只需要在 `WeaponModel` 和实际使用伤害数值的地方之间插入一层"最终伤害 = 基础伤害 × 装备加成"的计算，不需要动 `HitDetection`/`ApplyDamage` 这些已经写好的管线。
- 装备槽位/背包/词条系统本身，属于"物品系统"，应该是独立于武器开火逻辑的另一层——`WeaponModel` 可以是"物品"体系里的一个子类别，但物品系统本身的设计（背包容量、拾取、丢弃、堆叠）不在本文档范围内，等真的要做的时候单独立项讨论会更合适，现在强行把它塞进战斗框架里只会让这份文档失焦。

### 8.3 WeaponIntent：复用 MovementIntent 的模式

完全照抄 `MovementIntent` 的形状——一个纯数据类，玩家输入和（未来可能有的）AI 都只管写这个对象，武器节点自己决定要不要真的执行：

```csharp
// Core/Combat/WeaponIntent.cs
namespace EGame
{
    public class WeaponIntent
    {
        public bool WantsFire;
        public bool WantsReload;
        public bool WantsADS;   // 开镜/精确瞄准，正交修饰位，参考 4.2 节的思路
    }
}
```

挂在 `NEnvCreature` 上，跟 `Intent`（`MovementIntent`）平级：

```csharp
// NEnvCreature.cs 新增
public WeaponIntent WeaponIntent { get; } = new WeaponIntent();
```

玩家输入侧（新增一个 `NWeaponInput`，跟现有的 `NEnviromentInput` 平级，或者直接扩展 `NEnviromentInput`，取决于以后输入相关代码量会不会膨胀到需要拆分）：

```csharp
public override void _Process(double delta)
{
    ...
    _Creature.WeaponIntent.WantsFire = Input.IsActionPressed(EGInput.FIRE);
    _Creature.WeaponIntent.WantsReload = Input.IsActionJustPressed(EGInput.RELOAD);
    _Creature.WeaponIntent.WantsADS = Input.IsActionPressed(EGInput.ADS);
}
```

### 8.4 NWeapon：武器节点与开火状态机

```csharp
// Nodes/Combat/NWeapon.cs
namespace EGame
{
    public partial class NWeapon : Node3D
    {
        private NEnvCreature _Owner;
        private NFirstPersonCamera _Camera;   // 只有玩家持有的武器才会有相机引用
        private WeaponModel _Model;

        private float _NextFireTime;   // 绝对时间戳，参考 Source 的 m_flNextPrimaryAttack 设计
        private int _CurrentAmmo;
        private bool _IsReloading;
        private float _ReloadEndTime;

        public static NWeapon Create(NEnvCreature owner, WeaponModel model)
        {
            var instance = new NWeapon();
            instance._Owner = owner;
            instance._Model = model;
            instance._CurrentAmmo = model.MaxAmmo;
            return instance;
        }

        public override void _PhysicsProcess(double delta)
        {
            base._PhysicsProcess(delta);

            float now = Time.GetTicksMsec() / 1000f;

            if (_IsReloading)
            {
                if (now >= _ReloadEndTime)
                    FinishReload();
                return;
            }

            if (_Owner.WeaponIntent.WantsReload && _CurrentAmmo < _Model.MaxAmmo)
            {
                StartReload(now);
                return;
            }

            if (_Owner.WeaponIntent.WantsFire && now >= _NextFireTime && _CurrentAmmo > 0)
            {
                Fire(now);
            }
        }

        private void Fire(float now)
        {
            _NextFireTime = now + _Model.FireRate;
            _CurrentAmmo--;

            // 严格按照原则二：瞄准方向从相机的"纯瞄准朝向"取，不受后坐力/Bob影响
            var origin = _Camera.AimOrigin;
            var direction = _Camera.AimDirection;

            if (_Model.HitMode == WeaponHitMode.Melee)
            {
                // 近战：走 7.4 节的短距离扫描，Range 在这里语义上就是"触及距离"
                HitDetection.FireMelee(
                    GetWorld3D(), origin, direction, _Model.Range, _Model.MeleeHullExtents,
                    _Owner.Data, _Model, _Owner.GetRid());
            }
            else if (_Model.PelletCount > 1)
            {
                HitDetection.FireHitscanSpread(
                    GetWorld3D(), origin, direction,
                    _Model.PelletCount, _Model.SpreadDegrees, _Model.Range,
                    _Owner.Data, _Model, _Owner.GetRid(), Rng.RealRandom);
            }
            else
            {
                HitDetection.FireHitscan(
                    GetWorld3D(), origin, direction, _Model.Range,
                    _Owner.Data, _Model, _Owner.GetRid());
            }

            ApplyMuzzleKick();
            // 枪口火光/开火音效/弹壳抛出这些纯表现的东西，见第9节，不在这里直接写
        }

        private void StartReload(float now)
        {
            _IsReloading = true;
            _ReloadEndTime = now + _Model.ReloadTime;
        }

        private void FinishReload()
        {
            _IsReloading = false;
            _CurrentAmmo = _Model.MaxAmmo;
        }

        private void ApplyMuzzleKick()
        {
            // 往 5.3 节新增的 _WeaponKickPos 叠加一个衰减脉冲，具体实现见第9节
        }
    }
}
```

`_NextFireTime` 用绝对时间戳而不是倒计时——这是直接照抄 Source 的做法。倒计时（`_cooldownRemaining -= delta`）在帧率波动或者一帧卡顿之后容易出现"这一帧漏判"的问题；绝对时间戳配合"如果当前时间已经超过好几个 `FireRate` 周期，允许追赶式连续开火"（Source `PrimaryAttack` 里的 `while` 循环）能保证在低帧率下射速依然准确，不会因为帧率低就变相削弱了武器的实际射速。这一版先不做"追赶式连续开火"（用简单的 `if` 而不是 `while`），因为单人游戏对帧率的容忍度设计不需要一开始就做到这么严格，但把时间戳设计成绝对值而不是倒计时,是为了不堵死以后想加这个特性的路。

### 8.5 武器切换

参考 Source 的 `SelectWeapon()`/`Weapon_Combat()` 分层思路（`idealWeapon` vs `currentWeapon` 分开、切枪要走收枪/举枪的过渡动画），但大幅简化——单人游戏、武器数量不多的情况下，不需要 Source 那种完整的状态机，一个持有当前 `NWeapon` 引用、切枪时销毁旧的重建新的，配合一个简单的"收枪/举枪"动画触发即可：

```csharp
// 挂在 NEnvCreature 或者专门的 NWeaponHolder 上
public void EquipWeapon(WeaponModel model)
{
    _CurrentWeapon?.QueueFree();
    _CurrentWeapon = NWeapon.Create(this, model);
    // 挂到 NFirstPersonCamera 的 _WeaponViewRoot 下（第5.3节的挂点）
}
```

---

## 9. 视觉反馈与"枪感"（武器手感）

这一节呼应之前讨论过的 Game Feel 理论资料（Steve Swink《Game Feel》、"Juice it or lose it"、"The Art of Screenshake"），把抽象的"手感原则"落地成具体要实现的效果清单，并且明确每个效果**归属哪一层**。

### 9.1 视觉后坐力（Muzzle Kick）

上文 8.4 节 `ApplyMuzzleKick()` 的具体实现——往新增的 `_WeaponKickPos` 节点叠加一个"快速上升、缓慢回落"的旋转偏移，用现有的指数衰减写法：

```csharp
private Vector3 _KickOffset;
private Vector3 _KickTargetOffset;

private void ApplyMuzzleKick()
{
    _KickTargetOffset += new Vector3(
        Mathf.DegToRad(RandomKickPitch()), 0f, 0f);
}

private void ProcessKickDecay(float delta)
{
    _KickTargetOffset = _KickTargetOffset.Lerp(Vector3.Zero, GetLerpWeight(KickReturnSpeed, delta));
    _KickOffset = _KickOffset.Lerp(_KickTargetOffset, GetLerpWeight(KickRiseSpeed, delta));
    _WeaponKickPos.Rotation = _KickOffset;
}
```

`KickRiseSpeed`（上升速度）应该明显快于回落——这是之前讨论"跳跃手感"时提到的"上升快、下落带一点滞留感"同一个原则在开火反馈上的应用：后坐力如果上升和回落速度一样，会显得绵软；上升快、回落稍微拖一点尾巴，才有"每一枪都有分量"的感觉。

### 9.2 屏幕震动（Screenshake）

参考"The Art of Screenshake"演讲的核心建议——震动不应该是纯随机噪声，应该是有**方向性、有衰减包络**的短脉冲。实现上挂在 `NFirstPersonCamera` 的 `_CameraEffectPos` 上（复用 Camera Bob 已经在用的那个挂点，因为职责一致：都是"叠加在真实瞄准朝向之上的短暂视觉扰动"），触发方式是订阅 `Creature.OnDamaged`（自己受伤时震动）和武器开火事件（开火后坐力带一点点屏幕震动，跟纯武器后坐力区分开——前者影响相机，后者只影响武器模型）。

### 9.3 命中反馈（Hit Marker / 受击方向指示）

`HitDetection.FireHitscan` 返回值和命中的 `NEnvCreature` 都是现成的信息——命中确认（准星变化/音效）应该由 `NWeapon` 在 `Fire()` 拿到命中结果之后直接触发，不需要额外的事件系统；受击方向指示器（"我从背后中弹了"这种 HUD 提示）应该由挨打的一方订阅自己的 `Creature.OnDamaged`，用 `DamageInfo.HitPoint`/`KnockbackDirection` 反推来源方向，画在 HUD 上——这块留给第12节展开。

### 9.4 音效与粒子

枪口火光、弹壳抛出、命中火花/血液粒子——这些应该由 `NWeapon`/`HitDetection` 触发一个轻量的信号（Godot 的 `Signal` 或者简单的 `event`），具体挂什么粒子/音效交给美术在场景里配置，代码层不应该硬编码"打中人就放这个粒子"，应该是"打中人这个事件发生了，谁想订阅就订阅"。

---

## 10. AI 与战斗决策层

### 10.1 扩展 WorldAI 的事件词汇表

现有的 `WorldAIEvent` 大概率只有 `FindTarget`/`MissingTarget` 这类感知事件（`ZombieAI` 用到的）。参考 DOOM 3 的 `idAI::Pain()` 设置 `AI_PAIN`/`AI_DAMAGE` 标志位、并且在满足"反应表"（`ReactionTo()`）条件时直接调用 `SetEnemy()` 的做法，新增一个 `TookDamage` 事件：

```csharp
// 挂在 MonsterModel 或者 NEnvCreature 的初始化流程里
creature.OnDamaged += (info) =>
{
    _WorldBehaviorTree.NotifyEvent(WorldAIEvent.TookDamage, info.Attacker);
};
```

`NotifyEvent` 本身已经支持"中断并重新 tick 整棵树"（`WorldBehaviorTree.NotifyEvent` 现有实现），所以这里不需要新的机制,只是新增一种事件类型和对应的触发点。

### 10.2 攻击节点：决策与执行的边界，两段式判定

近战调研补完之后发现一个之前没注意到的细节，四个引擎的怪物近战全部是**两段式**：一段是**粗判**——AI 决策层用来决定"要不要切换到近战攻击这个分支"，通常只是一次便宜的距离/朝向检查（Quake II 的 `range()` 距离分桶、Source 的 `MeleeAttack1Conditions()`、DOOM 3 独立调用一次 `TestMelee()`）；另一段是**细判**——真正扣下攻击的那一刻，再确认一次命中（Quake II 在攻击帧回调里再调一次 `fire_hit`、Source 在动画事件里调 `CheckTraceHullAttack`、DOOM 3 的 `AttackMelee` 内部再调一次 `TestMelee`）。这么分层不只是代码整洁的问题——距离检测比射线/形状检测便宜得多，先用便宜的过滤一遍要不要考虑近战这个分支，真正进入近战分支之后才对少数情况做一次射线/盒子检测，是很直接的性能考量。

这个两段式设计跟 devil-school 已有的 `WorldAI` 词汇表严丝合缝——`WorldNodeCondition`（粗判，决定要不要走这个分支）和 `WorldNodeAction`（细判，真正执行攻击）本来就是分开的节点类型，不需要发明新概念：

```csharp
// AI/WorldAI/MonsterAI/Zombie/ZombieInMeleeRangeCondition.cs —— 粗判，只做距离比较，不碰物理查询
namespace EGame
{
    public class ZombieInMeleeRangeCondition : WorldNodeCondition
    {
        public ZombieInMeleeRangeCondition() : base("in_melee_range") { }

        protected override bool Evaluate(WorldBehaviorContext context)
        {
            if (!context.Blackboard.TryGetValue(ZombieAI.TargetKey, out var v) || v is not NEnvCreature target)
                return false;

            float dist = (context.Owner.GlobalPosition - target.GlobalPosition).Length();
            return dist <= ZombieAI.MeleeRange;
        }
    }
}
```

```csharp
// AI/WorldAI/MonsterAI/Zombie/ZombieAttackNode.cs —— 细判，真正调用命中判定的地方
namespace EGame
{
    public class ZombieAttackNode : WorldNodeAction
    {
        private float _NextAttackTime;

        public ZombieAttackNode() : base("attack") { }

        protected override WorldBehaviorStatus OnTick(WorldBehaviorContext context)
        {
            if (!context.Blackboard.TryGetValue(ZombieAI.TargetKey, out var v) || v is not NEnvCreature target)
                return WorldBehaviorStatus.Failure;

            float now = Time.GetTicksMsec() / 1000f;
            if (now < _NextAttackTime)
                return WorldBehaviorStatus.Running;   // 冷却中，占住这个节点但不重复攻击

            var weapon = context.Owner.Data.MonsterModel.MeleeWeapon;   // 见 10.3 节
            _NextAttackTime = now + weapon.FireRate;

            var direction = (target.GlobalPosition - context.Owner.GlobalPosition).Normalized();

            // 细判：走 7.4 节的短距离扫描（细射线 + 盒子兜底），不是粗判用的那种纯距离比较
            HitDetection.FireMelee(
                context.Owner.GetWorld3D(), context.Owner.GlobalPosition, direction,
                weapon.Range, weapon.MeleeHullExtents,
                context.Owner.Data, weapon, context.Owner.GetRid());

            context.Owner.SetAnimTrigger("attack");
            return WorldBehaviorStatus.Running;
        }
    }
}
```

`ZombieInMeleeRangeCondition` 挂在 Selector 分支的前置条件上（进入"近战攻击"这个分支之前先过一遍这个便宜的检查），`ZombieAttackNode` 是真正被执行的叶子节点——这跟 `WorldAI` 已有的 Selector/Condition/Action 组合方式完全一致，不需要为了"两段式"这个概念额外设计新的节点类型。`ZombieAttackNode` 本身**没有自己的伤害计算**——伤害数值来自 `weapon`（一个 `WeaponModel` 实例），命中判定调用的是跟玩家近战武器完全相同的 `HitDetection.FireMelee`。这正是第 3 节"原则一"要求的具体落地：不因为攻击者是怪物就走一条不同的代码路径。

### 10.3 怪物的"武器"：复用 WeaponModel

僵尸近战攻击本质上也是"一次伤害判定"，没有理由不复用 `WeaponModel` 这套数据结构——只是这个"武器"不会被拾取、不会被切换，是直接内嵌在 `MonsterModel` 里的一个固定字段：

```csharp
// MonsterModel.cs 新增
public virtual WeaponModel MeleeWeapon => null;   // 具体怪物覆盖这个属性

// ZombieModel.cs
public override WeaponModel MeleeWeapon => ModelDB.Weapon<ZombieClawModel>();
```

```csharp
// Model/WeaponModels/ZombieClawModel.cs
namespace EGame
{
    public class ZombieClawModel : WeaponModel
    {
        public override float Damage => 12f;
        public override float FireRate => 1.2f;          // 攻击间隔
        public override float Range => 1.2f;              // 近战触及距离
        public override DamageType DamageType => DamageType.Melee;
        public override WeaponHitMode HitMode => WeaponHitMode.Melee;
    }
}
```

这样"怪物近战"和"玩家开枪/玩家近战"在数据层面是完全同一种东西（都是 `WeaponModel`，都走 `HitDetection`，都落到 `Creature.ApplyDamage`），差别只在于：谁持有这把"武器"、以及触发它的是玩家输入（`WeaponIntent`）还是行为树节点（`ZombieAttackNode`）。`MaxAmmo`/`ReloadTime` 这些近战用不上的字段保持基类默认值，不需要为了"这是近战"就去改动 `WeaponModel` 的字段结构。

---

## 11. 遭遇战与生成系统

### 11.1 现状：NMonsterSpwanPoint 已经是正确的路径

`NMonsterSpwanPoint` 直接在 `_Ready()` 里 `new Creature(...)` + `NEnvCreature.Create(...)`，完全绕开了旧的 `EncounterModel`/`CombatRoom` 路径，把怪物真正生成进 3D 世界——这条路径本身没有问题，问题是它目前只能在编辑器里手工摆一个点、绑一个固定的 `MonsterID`，没有"一次触发生成一波"的能力。

### 11.2 EncounterModel 的新定位：一波生成的配置，不是"进入战斗"的入口

把 `EncounterModel`（如果决定不删、复用这个名字）的职责重新定义成："定义一组要生成的怪物和它们各自的相对生成点"，消费方不再是一个"进入 2D 战斗房间"的流程，而是一个新的触发节点：

```csharp
// Nodes/Enviroment/NEncounterTrigger.cs
namespace EGame
{
    public partial class NEncounterTrigger : Area3D
    {
        [Export] public string EncounterID;
        private bool _Triggered;

        public override void _Ready()
        {
            base._Ready();
            BodyEntered += OnBodyEntered;
        }

        private void OnBodyEntered(Node3D body)
        {
            if (_Triggered || body is not NEnvCreature creature || !creature.Data.IsPlayer)
                return;

            _Triggered = true;
            var encounter = ModelDB.Encounter(EncounterID).MutableClone() as EncounterModel;
            SpawnEncounter(encounter);
        }

        private void SpawnEncounter(EncounterModel encounter)
        {
            foreach (var (monster_model, spawn_offset) in encounter.MonsterWithSpawnOffsets)
            {
                var creature = new Creature(monster_model.MutableClone() as MonsterModel, CombatSide.Enemy, "");
                var n_creature = NEnvCreature.Create(creature);
                NEnviroment.Instance.AddMonsterCreature(n_creature);
                n_creature.GlobalPosition = GlobalPosition + spawn_offset;
            }
        }
    }
}
```

这跟 `NMonsterSpwanPoint` 的生成代码几乎一模一样（`new Creature` → `NEnvCreature.Create` → `AddMonsterCreature`），只是触发条件从"场景加载时"变成了"玩家进入区域时"，生成数量从"一个"变成了"一批"。`EncounterModel.MonsterWithSlot`（原来给 2D 战斗房间用的"生成点名字"）改造成 `MonsterWithSpawnOffsets`（相对触发器的生成偏移量），语义上更贴近"在这个区域里散开生成一波"而不是"分配到 UI 上的具名插槽"。

这一节刻意不展开太多——按照第 1.2 节"非目标"里说的，"任务驱动的区域状态"是一个远比这个更大的系统，这里只解决"怎么让一个触发器生成一波在正确设计原则下（复用 `Creature`/`NEnvCreature`/`HitDetection`）能打的怪"，不解决"这个区域该不该刷这波怪、刷完之后状态怎么持久化"这类问题。

---

## 12. UI / HUD 层

### 12.1 从"战斗界面"到"HUD 叠加层"

旧的 `NCreatureStateDisplay` 是"战斗界面里挂在角色头顶的血条"，这个概念在 FPS 里要转换成"屏幕上常驻的 HUD 元素"——不再是某个 `NCreature`（2D 战斗节点）的子节点，而是一个独立的、挂在 `NRun` 或者 `NEnviroment` 层级下的常驻 UI 层，订阅本地玩家的 `Creature`：

```csharp
// Nodes/UI/NPlayerHUD.cs
namespace EGame
{
    public partial class NPlayerHUD : Control
    {
        private Creature _PlayerCreature;

        public void BindPlayer(Creature creature)
        {
            _PlayerCreature = creature;
            creature.OnHPChanged += OnHPChanged;
            creature.OnDamaged += OnDamaged;
        }

        private void OnHPChanged(int old_hp, int new_hp)
        {
            // 刷新血条数值
        }

        private void OnDamaged(DamageInfo info)
        {
            // 播放受击闪红、根据 info.HitPoint 计算受击方向指示器角度
        }
    }
}
```

这跟已有的 `HP`/`OnHPChanged` 事件是直接兼容的——`NCreatureStateDisplay` 当年读 `OwnerCreature.Data.HP`/`MaxHP` 的写法完全可以照搬，只是"谁在挂这个订阅"从"战斗房间里的某个角色节点"变成了"常驻的 HUD"。

### 12.2 弹药显示

订阅 `NWeapon` 暴露的当前弹药数（简单起见可以是一个公开只读属性 + 一个变化事件，不需要多复杂的设计，跟血量的模式保持一致即可）。

---

## 13. 需要废弃的旧系统与理由

逐一列出上一轮讨论里定下来要删的东西，这里补全每一条"为什么删、删了会不会牵连什么"的完整理由，作为实际执行删除时的检查清单。

| 文件/系统 | 现状 | 删除理由 |
|---|---|---|
| `Combat/CombatManager.cs` | `StartTurn()` 方法体为空，`ExecutePlayerTurn`/`ExecuteEnemyTurn` 是从未被调用的空 stub | 回合推进这件事在 FPS 里不存在，没有任何等价物需要保留 |
| `Combat/CombatRoom.cs` | 构造函数会真的从 `EncounterModel` 建 `Creature` 列表，`EnterRoom()` 会真的切场景——是这几个文件里"最像在工作"的一个，但整体存在的意义（组织一场关进 2D 房间的战斗）跟 FPS 不兼容 | 生成怪物的职责被 `NMonsterSpwanPoint`/`NEncounterTrigger`（11.2 节）取代；"进入战斗场景"这个动作本身被废除 |
| `Combat/CombatState.cs` | `CurTurn`/`CombatSide` 从未被赋值，是死字段；`Allies`/`Enemies` 列表管理被证明也不是必要的全局状态 | 每个生物是否"在战斗"是它自己 AI 的状态，不需要全局名单 |
| `Nodes/Combat/NCombatRoom.cs`、`NCreature.cs`、`NCreatureVisual.cs`、`NCreatureStateDisplay.cs` | 2D `Control` 战斗界面表现层 | FPS 战斗直接发生在 3D 世界里，不需要独立的 2D 战斗场景；`NCreatureStateDisplay` 的血条逻辑被 12.1 节的 `NPlayerHUD` 以更合适的形式取代 |
| `AI/TurnBaseAI/` 全部 | 全项目零个具体 `TurnStateMove` 实例，`CreateTurnMoveStateMachine()` 零覆盖，属性从未被真正赋值——从写完到现在没有任何实际运行过的证据 | "选招"这个概念是回合制专属的，实时战斗的决策逻辑完全由 `WorldAI` 承担 |
| `GameAction/`、`ActionQueueSet.cs`、`ActionExecutor.cs` | 全仓库搜索除自身定义外零使用 | 这套命令队列当初大概率是为回合制"技能结算队列"设计的，实时战斗不需要"排队执行动作"这个概念；如果以后对话系统/过场需要类似模式，等真的需要时按那时候的具体需求重新设计,会比现在猜一个通用方案更准 |

`CombatSide.cs` 明确保留，不在删除清单里——`Player`/`Enemy`/`None` 这个概念在友军伤害判断、AI"这个目标该不该打"的场合依然需要，只是不再挂在一个已删除的 `CombatState` 上，直接是 `Creature.Side`（现状已经是这样）。

`Model/EncounterModel.cs`、`Model/EncounterModels/DebugEncounterModel.cs` 保留但要改——按 11.2 节的方案，把 `GenerateMonsterWithSlost()`/`MonsterWithSlot`（生成"具名插槽"给 2D UI 摆放用）改造成面向 3D 空间生成偏移量的版本，方法名顺带可以修正拼写（虽然项目的既定拼法是"错的也不改"，但如果这次改造顺带把语义都换了，是一个合理的时机去把 `GenerateMonsterWithSlost` 重命名成一个新的、拼写正确的名字，因为这已经不是"同一个方法的实现变了"，而是"这个方法的语义整个被重新设计了"——不属于"现有代码的既定拼法不要修正"这条约定要保护的范畴）。

---

## 14. 目录结构与文件清单

```
scripts/Core/
├── Combat/                          ← 目录名保留，内容整个换成新战斗系统
│   ├── DamageInfo.cs                 (新增)
│   ├── DamageType.cs                 (新增)
│   ├── HitDetection.cs               (新增)
│   ├── WeaponIntent.cs               (新增)
│   └── CombatSide.cs                 (保留原样)
│
├── Model/
│   ├── WeaponModel.cs                (新增，[ModelCategory]，含 WeaponHitMode 枚举)
│   └── WeaponModels/
│       ├── PistolModel.cs            (新增，HitMode = Hitscan)
│       ├── ShotgunModel.cs           (新增，HitMode = Hitscan + PelletCount)
│       ├── KnifeModel.cs             (新增，HitMode = Melee，玩家近战)
│       ├── ZombieClawModel.cs        (新增，HitMode = Melee，怪物近战)
│       └── ...
│   ├── MonsterModel.cs               (改动：新增 MeleeWeapon 属性)
│   ├── EncounterModel.cs             (改动：MonsterWithSlot → MonsterWithSpawnOffsets)
│   └── EncounterModels/
│       └── DebugEncounterModel.cs    (改动：配合上面的语义调整)
│
├── AI/WorldAI/
│   ├── WorldAIEvent.cs               (改动：新增 TookDamage 事件类型，若该枚举存在)
│   └── MonsterAI/Zombie/
│       ├── ZombieInMeleeRangeCondition.cs  (新增，粗判)
│       └── ZombieAttackNode.cs       (新增，细判)
│
├── Nodes/Combat/                     ← 目录名保留，内容整个换掉
│   ├── NWeapon.cs                    (新增)
│   └── NWeaponVisual.cs              (新增，武器视觉模型的节点包装)
│
├── Nodes/Enviroment/
│   └── NEncounterTrigger.cs          (新增)
│
├── Nodes/UI/
│   └── NPlayerHUD.cs                 (新增)
│
├── Nodes/Camera/
│   └── NFirstPersonCamera.cs         (改动：新增 AimDirection/AimOrigin，新增 _WeaponKickPos 挂点)
│
├── Nodes/Input/
│   └── NEnviromentInput.cs / NWeaponInput.cs   (改动或新增：写 WeaponIntent)
│
├── Nodes/Enviroment/
│   └── NEnvCreature.cs               (改动：新增 WeaponIntent 属性)
│
└── Entities/Creatures/
    └── Creature.cs                   (改动：新增 ApplyDamage/OnDamaged/OnKilled)

── 删除 ──
scripts/Core/Combat/CombatManager.cs
scripts/Core/Combat/CombatRoom.cs
scripts/Core/Combat/CombatState.cs
scripts/Core/Nodes/Combat/NCombatRoom.cs
scripts/Core/Nodes/Combat/NCreature.cs
scripts/Core/Nodes/Combat/NCreatureVisual.cs
scripts/Core/Nodes/Combat/NCreatureStateDisplay.cs
scripts/Core/AI/TurnBaseAI/               (整个目录)
scripts/Core/GameAction/                  (整个目录)
scripts/Core/ActionQueueSet.cs
scripts/Core/ActionExecutor.cs
scenes/combat/                            (对应的场景文件，含 combat_room.tscn / creature.tscn / creature_state_display.tscn)
```

---

## 15. 关键类型签名草案（附录汇总）

为了方便实际写代码时直接对照，这里把前文分散出现的关键签名汇总一遍（详细实现见对应章节，这里只列签名）：

```csharp
// Core/Combat/DamageInfo.cs
public class DamageInfo
{
    public Creature Attacker { get; }
    public object Inflictor { get; }
    public float Amount { get; }
    public DamageType DamageType { get; }
    public Vector3 HitPoint { get; }
    public Vector3 HitNormal { get; }
    public Vector3 KnockbackDirection { get; }
    public float KnockbackForce { get; }
}

// Core/Combat/DamageType.cs
[Flags] public enum DamageType { None, Bullet, Blast, Melee, Fall, Fire, NoArmor, NoKnockback }

// Core/Combat/HitDetection.cs (static)
public static bool FireHitscan(World3D world, Vector3 origin, Vector3 direction, float range,
    Creature attacker, WeaponModel weapon, Rid excluded_rid);
public static void FireHitscanSpread(World3D world, Vector3 origin, Vector3 base_direction,
    int pellet_count, float spread_degrees, float range,
    Creature attacker, WeaponModel weapon, Rid excluded_rid, Rng rng);
public static bool FireMelee(World3D world, Vector3 origin, Vector3 direction, float reach,
    Vector3 hull_extents, Creature attacker, WeaponModel weapon, Rid excluded_rid);

// Core/Combat/WeaponIntent.cs
public class WeaponIntent { public bool WantsFire; public bool WantsReload; public bool WantsADS; }

// Entities/Creatures/Creature.cs 新增部分
public event Action<DamageInfo> OnDamaged;
public event Action<DamageInfo> OnKilled;
public bool IsDead { get; }
public void ApplyDamage(DamageInfo info);

// Model/WeaponModel.cs
public enum WeaponHitMode { Hitscan, Melee }

public abstract class WeaponModel : AbstractModel
{
    public virtual float Damage { get; }
    public virtual float FireRate { get; }
    public virtual float Range { get; }
    public virtual DamageType DamageType { get; }
    public virtual float KnockbackForce { get; }
    public virtual int PelletCount { get; }
    public virtual float SpreadDegrees { get; }
    public virtual int MaxAmmo { get; }
    public virtual float ReloadTime { get; }
    public virtual WeaponHitMode HitMode { get; }
    public virtual Vector3 MeleeHullExtents { get; }
    public NWeaponVisual CreateViewModel();
}

// Nodes/Combat/NWeapon.cs
public partial class NWeapon : Node3D
{
    public static NWeapon Create(NEnvCreature owner, WeaponModel model);
}

// Nodes/Camera/NFirstPersonCamera.cs 新增部分
public Vector3 AimDirection { get; }
public Vector3 AimOrigin { get; }
```

---

## 16. 四引擎设计对照表

| 关注点 | Quake II | Quake III | Source (HL2) | DOOM 3 BFG | devil-school 采用 |
|---|---|---|---|---|---|
| 武器代码组织 | 函数指针表 + 帧驱动状态机（无 OOP） | `switch` 分发到独立 C 函数 | 一个武器一个 C++ 子类 + `.txt` 静态配置 | **一个通用类 + 数据驱动的 def**，脚本控制状态流程 | 参考 DOOM3：一个 `NWeapon` 通用类 + `WeaponModel` 数据（更贴近现有 Model 体系） |
| 命中判定归属 | `g_weapon.c` 里的共享 `fire_lead`/`fire_bullet` | `g_weapon.c` 里的 `Bullet_Fire`，服务器权威 | `CBaseEntity::FireBullets`（挂在射手实体上，非武器上） | `idProjectile`，一个类同时覆盖瞬间/飞行两种命中 | 独立静态类 `HitDetection`，不挂在任何实体/武器上，纯函数式 |
| 伤害数据结构 | `T_Damage()` 扁平参数列表 | `G_Damage()` 扁平参数列表（业界最常被抄的经典签名） | `CTakeDamageInfo` 结构化对象，字段最全 | 无固定结构体，`damageDefName` 字符串查表 | 参考 Source：`DamageInfo` 结构化类，但字段做精简 |
| 伤害类型分类 | `DAMAGE_BULLET` 等寥寥几个位 | `meansOfDeath_t` 枚举（死亡播报用，非行为分支） | `DMG_*` 约 30 个位标志 | 无枚举，全是数据字段 | 精简版 `[Flags] DamageType`，先给最小可用集合 |
| 玩家/AI 是否共用开火代码 | **完全共用**（怪物调用同一批 `fire_*` 函数） | **完全共用**（bot 模拟按键，走真人同一路径） | **共用同一武器类实例** | **不共用**（AI 独立的 `Event_LaunchProjectile`），但共用 `idProjectile`/`Damage()` | 共用 `HitDetection`/`ApplyDamage`，决策代码（`NWeapon` vs `WorldNodeAction`）分离 |
| 瞄准与视觉后坐力分离 | 未特别设计（时代较早） | 有一定分离（预测系统天然要求瞄准可复现） | 有分离（视图模型 vs 世界模型） | **最彻底**：显式区分 gameplay 轴与渲染轴 | 参考 DOOM3：`AimDirection` 独立于 `_WeaponKickPos` |
| 客户端是否重新判定命中 | 未着重设计 | **明确不重判**，客户端只播服务器给的结果 | 有预测但服务器最终仲裁 | 视 `net_instanthit` 而定 | 单人游戏无此问题，但保留"唯一权威点"设计（原则五） |
| 命中反馈/多重伤害合并 | 无 | 无特别设计 | `g_MultiDamage` 累加器（霰弹合并） | 无 | 先不做，7.3 节留了扩展点 |
| 近战命中判定 | 距离检测 + 确认射线（`fire_hit`） | 短射线（32单位），复用 `trap_Trace` | 短射线 miss 后退一步用盒子兜底（`Swing()`），NPC 用 `CheckTraceHullAttack` | 包围盒重叠 + 视线射线（`TestMelee`） | 参考 Source：细射线优先、盒子兜底（`HitDetection.FireMelee`，7.4节） |
| 近战与远程是否共用武器类 | 否，`fire_hit` 是独立函数，玩家甚至没有近战 | 是，拳套（Gauntlet）是正常的武器槽位之一，但判定时机脱离武器状态机 | 是，`CBaseHLBludgeonWeapon` 继承自 `CBaseCombatWeapon`，是同一武器类家族 | **是，拳头就是一把完整的 `idWeapon`，和步枪走同一套武器管线** | 参考 DOOM3：近战武器就是 `HitMode=Melee` 的 `WeaponModel`，`NWeapon` 不需要子类化（8.1/8.3节） |
| AI 近战决策是否分两段 | **是**：`range()` 粗判距离桶 → 攻击帧回调里 `fire_hit` 细判 | 无 AI 怪物框架，无对照 | **是**：`MeleeAttack1Conditions()` 粗判（含预判追踪）→ 动画事件里 `ClawAttack`/`CheckTraceHullAttack` 细判 | **是**：`TestMelee()` 单独调用做粗判 → `AttackMelee()` 内部再调一次做细判 | 参考三家一致的两段式：`WorldNodeCondition` 粗判 + `WorldNodeAction` 细判（10.2节） |
| 近战伤害是否走独立管线 | 否，`fire_hit` 内部调用和远程完全相同的 `T_Damage` | 否，`Weapon_Gauntlet` 调用和远程完全相同的 `G_Damage` | 否，`Hit()`/`CheckTraceHullAttack` 都构造 `CTakeDamageInfo` 走 `TakeDamage` | 否，`AttackMelee`/`DirectDamage` 都直接调用 `Entity::Damage()` | **四家全部收敛**：近战和远程共用同一个 `Creature.ApplyDamage()`，验证了原则一没有例外 |
| 玩家/AI 是否共用同一个"意图"执行层 | 否，怪物移动（`ai_move`/`ai_walk`）直接改 `origin`/`velocity`，跟玩家的 `usercmd_t` 毫无关系 | **是**，bot 通过 `trap_EA_*` 模拟出一份假 `usercmd_t`，和真人走同一个 `Pmove()` | 否，`ai_basenpc.cpp` 实测零处引用 `CUserCmd`/`CMoveData`，NPC 走独立的 `CAI_Motor` | 否，`idAI` 移动代码零处引用 `usercmd`，走独立的 `idAI::Move` 体系 | 参考 Quake III：`MovementIntent` 是玩家输入和 AI 共用的执行层，四家里只有 Q3 真的这么做（但 Q3 的"AI"是伪装成玩家的 bot，问题比真怪物 AI 简单，见下一行） |
| AI 怎么算出"该往哪走"这个方向 | 独立的怪物移动函数，无寻路/避障系统 | bot 依赖 botlib 自己的路径系统（Q3A 本身没有真正的"怪物"，全是 bot） | NPC 走 `CAI_Motor`/寻路系统（未深入研究，Source 的 NPC 导航系统本身就相当成熟） | **五层管线**：目标设定（一次性）→ AAS 寻路（缓存查表+直线裁剪）→ 局部动态避障→ 平滑转向 → 动画/速度驱动位移；`DirectMoveToPosition()`（直线冲过去）被 id 自己列为特例，不是通用方案 | 参考 DOOM3 的管线形状，但落地用 Godot 自带的 `NavigationAgent3D`/`NavigationRegion3D` 代替手写 AAS——`ZombieChaseNode` 从"每帧算目标方向"改成"设一次目标、每帧读 `GetNextPathPosition()`"，`Intent.MoveDir` 字段本身不变（4.4节） |

---

## 17. 分阶段实施路线图

不建议一次性把前面 16 节的内容全部实现完再测试——按下面的顺序推进，每一步结束都应该有一个可以实际在编辑器里验证的效果，避免长时间没有可运行结果导致方向跑偏都发现不了。

**阶段一：伤害管线打底（不涉及武器）**
- `DamageInfo`、`DamageType`、`Creature.ApplyDamage`/`OnDamaged`/`OnKilled`
- 用 DevConsole 命令（照抄现有 `AbstractConsoleCmd` 模式）手动调用 `ApplyDamage` 测试事件触发、死亡判定是否正确
- 验收标准：能在控制台敲一个命令，把场景里某个怪物打死，控制台能看到 `OnKilled` 触发的日志

**阶段二：命中判定 + 最简单的一把武器**
- `HitDetection.FireHitscan`
- `WeaponModel`/`PistolModel`，`NWeapon` 最简版（先不做换弹、不做多种武器切换）
- `NFirstPersonCamera.AimDirection`/`AimOrigin`
- 验收标准：玩家能在 3D 世界里对着一个 `NMonsterSpwanPoint` 生成的僵尸开枪，血量真的会掉，掉到 0 会触发死亡

**阶段三：怪物反击（近战）**
- `HitDetection.FireMelee`（7.4节），`ZombieClawModel`
- `ZombieInMeleeRangeCondition`（粗判）+ `ZombieAttackNode`（细判），`MonsterModel.MeleeWeapon`
- `WorldAIEvent.TookDamage` 事件接入
- 验收标准：僵尸能在近身之后主动打玩家，玩家 HP 会掉；玩家打僵尸一下，僵尸的行为树能对"挨打"这件事有反应（比如立刻锁定攻击者，即使之前没发现玩家）；顺便验证一下 `FireMelee` 的盒子兜底有没有生效——故意站在一个刁钻角度（准星压线但没完全对准）试试近战武器/僵尸攻击还能不能命中

**阶段四：手感打磨**
- 武器后坐力（`_WeaponKickPos`）、屏幕震动、命中反馈
- 验收标准：主观测试"这把枪打起来有没有分量感"，参考第 9 节的判断标准

**阶段五：HUD**
- `NPlayerHUD`，血条、弹药显示、受击方向指示
- 验收标准：玩家不看编辑器输出也能知道自己还剩多少血、多少弹药

**阶段六：遭遇战生成系统**
- `NEncounterTrigger`，`EncounterModel` 语义改造
- 验收标准：走进一个触发区域，一波怪同时生成并且都能正常战斗

**阶段七：清理旧系统**
- 按第 13 节清单删除废弃文件
- 这一步放在最后，不是因为不重要，而是因为**旧系统目前完全没有被任何新代码依赖**，删除的时机不影响前六个阶段的开发，放在最后可以避免"新系统还没跑通、旧代码已经删了、想对照参考都没得看"的风险——虽然旧代码本身参考价值不大（都是空壳），但保留到最后删除是更保守、更安全的顺序

---

## 结语

这份文档的每一处设计取舍，都能在四个参考引擎里找到至少一处直接对应的先例——这不是巧合，而是因为"命中判定归谁管""瞄准和后坐力怎么分离""AI 打人和玩家打人算不算一回事"这几个问题，本质上是 FPS 这个品类无论哪个年代、哪个引擎都绕不开的同一组约束逼出来的答案。devil-school 现有的代码里，`MovementIntent`、`Core`/`Nodes` 分层、`AbstractModel` 数据目录这几个已经验证过的模式，恰好也是同一组约束在这个项目里已经给出的答案——所以这次的框架设计,与其说是"引入一套新东西",不如说是"把项目自己已经在遵循的原则,补完到目前还空缺的那几块拼图上"。
