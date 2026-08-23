# 用 Godot（C#）做一个 DOOM 3 风格的单人 FPS

> 版本：v1（替代旧版 `DOOM3-Gameplay-Godot实现指南.md`——那份是"读 DOOM 3 源码笔记 + GDScript 对照"，读者反馈这不是他要的东西：他要的是一份**手把手教你从零做出一个 FPS 的教程**，用 **C#**，从能跑能跳的玩家角色开始做起，一步步加东西，让你先看到成果、再回头理解"这段代码为什么要这么组织"。这份文档就是照这个方向重新写的。）
>
> 引擎版本假设：Godot 4.x，.NET/C# 版本（Godot Mono）。写代码前请确认你下载的是 Godot 官网上标"**.NET**"的那个版本，不是默认的纯 GDScript 版本——两者是分开下载的。
>
> 读法建议：这不是一份"查阅式"的参考手册，是按顺序读的教程。每一章都建立在前一章做出来的东西之上，代码是可以直接抄进项目里跑起来的（个别地方为了篇幅省略了不影响理解的细节，会在注释里说明）。如果你想深入了解 id Software 当年 DOOM 3 具体是怎么实现这些系统的（不是"怎么用 Godot 做类似效果"，而是"C++ 源码长什么样、为什么这么设计"），那是另一份文档 [DOOM3-BFG-Gameplay架构精读.md](DOOM3-BFG-Gameplay架构精读.md) 的内容，本文最后一章会给出具体的章节对照，读完本文再去看会容易得多。

---

## 目录

**第一部分：让角色动起来**
1. 项目搭建
2. 玩家控制器：移动
3. 视角与摄像机：让移动"看起来"对

**第二部分：拿起武器**
4. 你的第一把枪：开火与命中判定
5. 武器系统进阶：弹药、换弹、切枪
6. 武器手感：后坐力、摇摆、近战

**第三部分：让世界活起来**
7. 物理与可交互物体：箱子、门、电梯
8. 做一只会打你的怪物
9. 敌人 AI 进阶：寻路、感知、状态机、难度
10. 死亡的分量：布娃娃与肢解

**第四部分：搭关卡**
11. 关卡工具箱：触发器、开关、拾取物
12. 交互系统：按钮、终端、可点击的屏幕

**第五部分：现在项目大了，该谈谈架构了**
13. 为什么你需要一个事件系统
14. 数据驱动：把"这只怪物"变成"一份配置"
15. 存档系统
16. HUD 与反馈

**第六部分**
17. （可选）联机合作
18. 延伸阅读：如果你想知道 DOOM 3 原版是怎么做的

---

## 1. 项目搭建

打开 Godot（**.NET 版**），新建项目。第一件事：`项目 -> 项目设置 -> 全局 -> Dotnet -> Project`，确认 C# 支持已经启用（.NET 版默认就是启用的，如果你是从 GDScript 项目转过来的，需要手动加）。

新建一个 3D 场景作为你的第一张测试关卡：

- 根节点：`Node3D`，改名 `TestLevel`
- 加一个 `WorldEnvironment`（给个默认环境光，不然场景全黑）
- 加一个 `DirectionalLight3D`
- 加一个大的 `StaticBody3D` + `CollisionShape3D`（用 `BoxShape3D`，压扁拉大做地板）+ `MeshInstance3D`（同样用 `BoxMesh`）
- 保存为 `res://levels/test_level.tscn`

这就是你接下来所有内容的试验场。目录结构现在先不用想太多，跟着教程走，代码写到哪个系统就建到哪个文件夹，第 11 章之后我们会有一个相对成型的目录结构建议。

**输入映射**：`项目 -> 项目设置 -> 输入映射`，现在先加这几个（后面用到什么再加什么，不用一次加全）：

| 动作名 | 建议按键 |
|---|---|
| `move_forward` | W |
| `move_back` | S |
| `move_left` | A |
| `move_right` | D |
| `jump` | 空格 |
| `fire` | 鼠标左键 |

---

## 2. 玩家控制器：移动

### 2.1 搭建玩家场景

新建场景，根节点用 `CharacterBody3D`，改名 `Player`：

```
Player (CharacterBody3D)
├── CollisionShape3D（用 CapsuleShape3D，半径 0.4，高度 1.8）
└── Head (Node3D，放在 y=1.6 左右，眼睛高度)
    └── Camera3D
```

新建 C# 脚本 `PlayerController.cs`，挂在 `Player` 根节点上：

```csharp
using Godot;

public partial class PlayerController : CharacterBody3D
{
    [Export] public float WalkSpeed = 7.0f;
    [Export] public float JumpVelocity = 8.0f;
    [Export] public float Gravity = 20.0f;

    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;
        Vector3 velocity = Velocity;

        // 重力：不在地面时持续往下加速
        if (!IsOnFloor())
        {
            velocity.Y -= Gravity * dt;
        }
        else if (Input.IsActionJustPressed("jump"))
        {
            velocity.Y = JumpVelocity;
        }

        // 读输入，转换成世界空间的移动方向
        Vector2 inputDir = Input.GetVector("move_left", "move_right", "move_forward", "move_back");
        Vector3 direction = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();

        if (direction != Vector3.Zero)
        {
            velocity.X = direction.X * WalkSpeed;
            velocity.Z = direction.Z * WalkSpeed;
        }
        else
        {
            velocity.X = 0;
            velocity.Z = 0;
        }

        Velocity = velocity;
        MoveAndSlide();
    }
}
```

把 `Player.tscn` 拖进 `TestLevel.tscn` 里，摆在地板上方一点。运行游戏（现在还没有摄像机跟视角控制，你会看到角色能走能跳，但看不到自己的移动方向——下一节马上补上视角，先确认这一步能跑）。

`Input.GetVector(...)` 这个 API 直接把四个方向键读成一个已经处理好对角线归一化的二维向量，比自己四个 `if` 拼要省事。`Transform.Basis * new Vector3(...)` 是把这个"相对于角色朝向的输入方向"转换成世界坐标系里的方向——现在角色还不能转向（没有鼠标视角），这一步的意义要等下一节接上摄像机之后才能真正体现出来。

### 2.2 加速度、摩擦力：让移动不是"瞬间到达最大速度"

> **写在前面**：从这一节开始，本教程的移动/视角代码会**逐行照抄 DOOM 3 BFG 源码 `neo/d3xp/physics/Physics_Player.cpp` 里 `idPhysics_Player` 的实际公式和状态机**（对应 [DOOM3-BFG-Gameplay架构精读.md](DOOM3-BFG-Gameplay架构精读.md) 第 5.8/5.10/5.11 节的还原内容），不再是"差不多意思"的简化版。**唯一的改动是数值的量纲**：DOOM 3 的世界单位接近英寸（一个人物模型高度约 74 单位），Godot 项目通常用米做单位（一个角色约 1.8 米），所以下面给出的具体数字是按"米"重新换算过的、比例上等价的值，不是把 DOOM 3 源码里的原始数字直接抄过来——公式的**结构、每一项的作用、彼此的比例关系**才是"完全参考"的对象，抄一个在错误量纲下毫无意义的数字反而是假忠实。

DOOM 3 的移动模型区分"地面摩擦力衰减"和"朝期望方向加速"两个独立步骤，且明确区分走/跑两档速度（按 Shift 键切换），我们从这里开始就把这两点一起做完整，而不是留到后面：

```csharp
using Godot;

public partial class PlayerController : CharacterBody3D
{
    [Export] public float WalkSpeed = 4.0f;
    [Export] public float RunSpeed = 7.0f;
    [Export] public float JumpVelocity = 6.0f;
    [Export] public float Gravity = 20.0f;

    // 以下四个常量的名字和相对比例直接照抄 Physics_Player.cpp 里的硬编码值
    // (PM_ACCELERATE / PM_AIRACCELERATE / PM_FRICTION)——DOOM 3 原版这几个数是无量纲的
    // 比例系数，不随游戏世界单位换算而改变，可以照原样搬
    private const float Accelerate = 10.0f;
    private const float AirAccelerate = 1.0f;
    private const float Friction = 6.0f;
    private const float StopSpeed = 1.0f;   // 对应 PM_STOPSPEED：低于这个速度直接判定摩擦力已经让角色停下

    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;
        Vector3 velocity = Velocity;

        if (!IsOnFloor())
        {
            velocity.Y -= Gravity * dt;
        }
        else if (Input.IsActionJustPressed("jump"))
        {
            velocity.Y = JumpVelocity;
        }

        Vector2 inputDir = Input.GetVector("move_left", "move_right", "move_forward", "move_back");
        Vector3 wishDir = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();
        float wishSpeed = Input.IsActionPressed("sprint") ? RunSpeed : WalkSpeed;

        Vector3 horizontalVelocity = new Vector3(velocity.X, 0, velocity.Z);

        if (IsOnFloor())
        {
            horizontalVelocity = ApplyFriction(horizontalVelocity, dt);
            horizontalVelocity = ApplyAcceleration(horizontalVelocity, wishDir, wishSpeed, Accelerate, dt);
        }
        else
        {
            horizontalVelocity = ApplyAcceleration(horizontalVelocity, wishDir, wishSpeed, AirAccelerate, dt);
        }

        velocity.X = horizontalVelocity.X;
        velocity.Z = horizontalVelocity.Z;

        Velocity = velocity;
        MoveAndSlide();
    }

    // 对应 Physics_Player.cpp::Friction()
    private Vector3 ApplyFriction(Vector3 vel, float dt)
    {
        float speed = vel.Length();
        if (speed < 0.001f)
        {
            return Vector3.Zero;
        }
        float control = speed > StopSpeed ? speed : StopSpeed;
        float drop = control * Friction * dt;
        float newSpeed = Mathf.Max(speed - drop, 0.0f);
        return vel * (newSpeed / speed);
    }

    // 对应 Physics_Player.cpp::Accelerate()——按 "期望方向上的速度差值" 加速，
    // 不是按 "到期望速度矢量的差值" 加速，这个区别正是第 2.3 节要讲的空中变向能不能成立的关键
    private Vector3 ApplyAcceleration(Vector3 vel, Vector3 wishDir, float wishSpeed, float accel, float dt)
    {
        float currentSpeed = vel.Dot(wishDir);
        float addSpeed = wishSpeed - currentSpeed;
        if (addSpeed <= 0)
        {
            return vel;
        }
        float accelSpeed = Mathf.Min(accel * wishSpeed * dt, addSpeed);
        return vel + wishDir * accelSpeed;
    }
}
```

记得在输入映射里加一个 `sprint` 动作（建议绑 Shift）。现在按住 W 会有一个短暂的加速过程，松开会有摩擦力慢慢刹停；按住 Shift 跑得更快。这就是几乎所有 FPS 移动手感的地基公式，后面第 9 章做怪物移动的时候还会用到同一套东西。

> **两处容易漏掉的收尾**，都是完整读完 `Physics_Player.cpp` 全文（而不是只读 `Accelerate`/`Friction` 这两个函数）才会注意到——单独看这两个函数体会跳过它们，因为它们不在这两个函数内部，而是"这两个函数的输出该怎么被使用"这个更外层的问题：
>
> **1. 该拒绝的坡度，要显式配置，不能靠默认值蒙对**——DOOM3 判断"这个坡陡到不能站着走"的标准写死在文件开头：`const float MIN_WALK_NORMAL = 0.7f;`，`CheckGround()` 里用 `groundTrace.c.normal * -gravityNormal < MIN_WALK_NORMAL` 来判定"这个地面太陡，不算贴地"，换算成角度约是 45.57°。`CharacterBody3D` 有一个功能上完全对应的属性 `FloorMaxAngle`——凑巧的是，Godot 4 这个属性的默认值正好是 45°（弧度 0.785），换算成同样的余弦值约 0.707，跟 DOOM3 的 0.7 几乎是同一个数字。但这终究是巧合，不是设计：不显式写出来，你的项目就是在依赖一个自己都不知道"为什么恰好对"的引擎默认值——以后 Godot 版本更新了默认值，或者你想做一关"到处是陡坡"的关卡，都没有一个显式的旋钮可以调。补上：
>
> ```csharp
> // PlayerController.cs 的 _Ready() 里追加
> [Export] public float MaxWalkableSlopeDegrees = 45.57f;   // 对应 MIN_WALK_NORMAL = 0.7f 换算出的坡度上限
>
> public override void _Ready()
> {
>     // ...原有初始化...
>     FloorMaxAngle = Mathf.DegToRad(MaxWalkableSlopeDegrees);
> }
> ```
>
> **2. 移动速度也该留一个能被外部倍率修改的接口**——DOOM3 的 `idPlayer::AdjustSpeed()`（`Player.cpp:6654-6702`）不管算出来的是走路、跑步还是别的速度，最后一步永远是 `speed *= PowerUpModifier(SPEED);`——移动速度在真正喂给物理系统之前，总会被增益系统过一道。6.3 节会引入 `PowerupState` 这个集中查询点（当时只给了近战伤害/推力接口），但如果 2.2 节的 `wishSpeed` 从一开始就不留这个口子，以后想做"减速陷阱""加速药水"这类道具时，就得回头改移动最核心的这段代码。现在先占好位置，哪怕现在恒等于 1：
>
> ```csharp
> // PlayerController.cs 追加——现在恒定是 1.0，6.3 节会说明它什么时候真正被改
> public float SpeedModifier = 1.0f;
> ```
>
> 上面 `_PhysicsProcess` 里 `float wishSpeed = Input.IsActionPressed("sprint") ? RunSpeed : WalkSpeed;` 这一行改成 `float wishSpeed = (Input.IsActionPressed("sprint") ? RunSpeed : WalkSpeed) * SpeedModifier;`（2.8 节给出的"完整版" `_PhysicsProcess` 已经把这处改动合并进去了）。游泳、爬梯两节的移动速度走的是各自独立的分支，原理完全一样，本教程不再逐处重复这行乘法。

### 2.3 空中控制：为什么要"故意"把空中加速度调低而不是直接锁死

你可能会问：为什么空中不干脆完全不能变向（像很多游戏那样，起跳瞬间轨迹就固定了）？上面代码里空中依然调用了 `ApplyAcceleration`，只是把加速度系数换成了单独的 `AirAccelerate = 1.0f`（DOOM 3 原版 `PM_AIRACCELERATE` 就是这个值，是地面 `PM_ACCELERATE=10.0` 的十分之一）——这是刻意的，效果是：**你在空中依然能调整方向，但改变不了太多**。

这里必须完全参考的是 DOOM 3 的另一个关键事实：**空中摩擦力是 0**（`PM_AIRFRICTION = 0.0f`，代码里空中分支根本不调用 `ApplyFriction`）。这正是 Quake/DOOM 系列"空中控制/兔跳"手感成立的两个必要条件之一——另一个条件是上面 `ApplyAcceleration` 用"当前速度在期望方向上的**投影**"而不是"当前速度矢量到期望速度矢量的**差值**"来算 `addSpeed`。这两点任何一点做错（比如空中也加摩擦力，或者用矢量差值算加速度），都会让"斜着按住 A 或 D、同时转动视角"这个动作没办法持续增速——这正是很多自己实现移动手感的项目容易在不知不觉中"手感就是不对但说不出哪里错了"的地方，上面代码已经完全对齐了 DOOM 3（本质也是 Quake）的原始实现，不需要再改。

### 2.4 蹲下：真正缩小碰撞体积，不是只降摄像机

很多简化实现只是把摄像机往下移，碰撞体积不变——这样角色"蹲下"之后依然占着站立时的体积，钻不进本该能钻进去的矮空间。DOOM 3 的做法（`CheckDuck()`）是**真正重塑碰撞体**，并用一个固定速率把眼睛高度平滑过渡到新高度，不是瞬间跳变：

```csharp
// PlayerController.cs 追加
[Export] public float StandHeight = 1.8f;
[Export] public float CrouchHeight = 0.9f;
[Export] public float CrouchSpeed = 2.0f;
[Export] public float CrouchTransitionRate = 0.87f;   // 对应 DOOM3 的 pm_crouchrate

private CollisionShape3D _collisionShape;
private bool _isDucked;
private float _currentEyeOffset;

public override void _Ready()
{
    _collisionShape = GetNode<CollisionShape3D>("CollisionShape3D");
    _currentEyeOffset = StandHeight - 0.15f;
    // ...原有初始化...
}
```

> **一个跟 DOOM3 源码无关、纯 Godot 的坑**：下面 `UpdateCrouch()` 会直接改 `_collisionShape.Shape` 上的 `Height`，而场景里内嵌的 Shape 资源默认是所有实例共享的——如果这个 `Player.tscn` 在场景里摆了不止一个实例，一个角色蹲下会连带改到其他实例的碰撞体高度。修法不用写代码：选中 `CollisionShape3D` 的 `Shape` 资源，在检查器面板右下角把它勾成 **Local to Scene**，Godot 在每次实例化这个场景时就会自动各给一份独立拷贝，不用手动 `Duplicate()`。

> **这一节第一版也写错了**，直接去读 `Physics_Player.cpp::CheckDuck()`（1070-1113 行）和 `Player.cpp` 里眼睛高度插值那段代码（约 6955-6974 行）才发现：**碰撞体的高度切换是瞬间完成的，完全没有平滑**——`CheckDuck()` 只是判断"该不该蹲"（按了蹲下键就蹲，站起来之前要先做一次向上 trace 确认没被挡住才允许站起），一旦判断结果变化，碰撞体高度直接改成新的目标值，中间没有任何插值。**真正被平滑处理的，只有摄像机的眼睛高度**——`pm_crouchrate` 是专门用在这一步的：`SetEyeHeight(EyeHeight()*pm_crouchrate + newEyeOffset*(1-pm_crouchrate))`，这是一个逐帧固定权重混合，不是我之前写的"拿这个数字去插值碰撞体高度"。上一版把这两件事混在一起、用同一个 `Lerp` 处理，是错的。改正版本：

```csharp
private void UpdateCrouch()
{
    bool wantsDuck = Input.IsActionPressed("crouch") && !IsOnLadder;   // 贴着梯子时不允许蹲下，源码里有这个限制

    if (wantsDuck)
    {
        _isDucked = true;
    }
    else if (_isDucked)
    {
        // 起身前先确认头顶没有东西挡着才允许站起来——对应源码 CheckDuck() 里的
        // gameLocal.clip.Translation(trace, current.origin, end, clipModel, ...)：注意 clipModel
        // 这时候还是"蹲着"那个尺寸（源码里改高度是这次检测之后才做的事），源码测的是"把现在这个
        // 蹲着的胶囊体，往上平移站高和蹲高的差值，这一路有没有撞到东西"——是一次完整的胶囊体扫掠
        // 测试，不是从角色中心打一条细线往上戳。用单点射线代替胶囊扫掠是这一节第一版藏着的一个
        // 不易发现的 bug：站在一个只在角色胶囊边缘（不在正中心）有低矮横梁/管道的地方，中心射线
        // 可能穿过横梁之间的空隙、判定"可以站起来"，但实际站起来的胶囊体积会和横梁重叠——玩家
        // 会卡进几何体里。2.7 节的台阶步进已经在用 PhysicsServer3D.BodyTestMotion 对整个角色刚体
        // 做真正的形状扫掠，这里用同一个工具复刻同样的语义："把当前（蹲着）的碰撞体，往上扫过
        // 站起来需要抬升的这段距离"，而不是另起一套单独构造胶囊形状的查询
        var standCheckParams = new PhysicsTestMotionParameters3D
        {
            From = GlobalTransform,
            Motion = Vector3.Up * (StandHeight - CrouchHeight)
        };
        var standCheckResult = new PhysicsTestMotionResult3D();
        bool standBlocked = PhysicsServer3D.BodyTestMotion(GetRid(), standCheckParams, standCheckResult);
        if (!standBlocked)
        {
            _isDucked = false;
        }
    }

    // 碰撞体高度：瞬间切换，不做任何平滑——这是源码的真实行为
    float targetHeight = _isDucked ? CrouchHeight : StandHeight;
    var capsule = (CapsuleShape3D)_collisionShape.Shape;
    capsule.Height = targetHeight;
    _collisionShape.Position = new Vector3(0, targetHeight * 0.5f, 0);

    // 只有眼睛高度（摄像机位置）才平滑：每个物理帧固定往目标值靠近一点，
    // 不随dt缩放——Godot的_PhysicsProcess本身就是固定步长跑的，效果跟DOOM3原版一致。
    // DOOM3源码字面写的是 a*rate + b*(1-rate) 这种加权平均，本质就是 Lerp，
    // 这里直接用Lerp写，比照抄那个写法好懂
    float targetEyeOffset = targetHeight - 0.15f;
    _currentEyeOffset = Mathf.Lerp(_currentEyeOffset, targetEyeOffset, 1f - CrouchTransitionRate);
    _head.Position = new Vector3(0, _currentEyeOffset, 0);
}
```

记得加 `crouch` 输入映射（建议绑 Ctrl），并在 `_PhysicsProcess` 里调用 `UpdateCrouch();`（不再需要传 `dt`，因为不再做 dt 缩放的插值了），同时蹲下时把 `wishSpeed` 换成 `CrouchSpeed`（在 2.2 节 `wishSpeed` 那一行加一个判断：蹲下优先于跑/走——这一点验证下来也是对的，源码里 `CheckDuck()` 对 `playerSpeed = crouchSpeed` 的赋值是无条件的，不会被同时按住的跑步键覆盖）。

### 2.5 游泳

> 这一节此前的描述也不够准确，"水的粘滞感更强"那句拗口的话已经在前面的问答里更正过一次（加速度更低、摩擦力也更低，两个系数都比陆地小，不是"更强"）。这次直接读了 `WaterMove()`（`Physics_Player.cpp:522-566`），又发现两处遗漏：**上浮/下潜是跳跃键和蹲下键对称控制的**（按跳跃上浮、按蹲下键下潜），不是只有上浮没有下潜；**完全不按任何移动键时，角色会被动下沉**（`wishvel = gravityNormal * 60`），不是停在原地不动。补全后的版本：

```csharp
// PlayerController.cs 追加
[Export] public float SwimSpeedScale = 0.5f;    // 对应 PM_SWIMSCALE
private const float WaterAccelerate = 4.0f;       // 对应 PM_WATERACCELERATE
private const float WaterFriction = 1.0f;         // 对应 PM_WATERFRICTION
public bool IsInWater;   // 由水体 Area3D 的 BodyEntered/BodyExited 信号翻转这个字段

private void ApplyWaterMove(Vector2 rawInput, Vector3 wishDir, float wishSpeed, float dt)
{
    float speed = Velocity.Length();
    Vector3 vel = Velocity;
    if (speed > 0.001f)
    {
        float drop = speed * WaterFriction * dt;
        vel *= Mathf.Max(speed - drop, 0.0f) / speed;
    }

    bool noInput = rawInput.LengthSquared() < 0.001f && !Input.IsActionPressed("jump") && !Input.IsActionPressed("crouch");
    if (noInput)
    {
        // 完全不按任何键时被动下沉——源码这里同样是走 Accelerate(wishdir, wishspeed, PM_WATERACCELERATE)，
        // wishvel = gravityNormal * 60 只是意愿速度（wishdir），依然会被 wishSpeed * SwimSpeedScale 钳住上限，
        // 不是脱离加速度系统、不设上限地一路往下叠加
        vel = ApplyAcceleration(vel, Vector3.Down, wishSpeed * SwimSpeedScale, WaterAccelerate, dt);
        Velocity = vel;
        return;
    }

    Vector3 swimWish = wishDir;
    if (Input.IsActionPressed("jump")) swimWish += Vector3.Up;      // 上浮
    if (Input.IsActionPressed("crouch")) swimWish += Vector3.Down;   // 下潜——上一版遗漏的对称按键
    if (swimWish.LengthSquared() > 0.001f) swimWish = swimWish.Normalized();

    vel = ApplyAcceleration(vel, swimWish, wishSpeed * SwimSpeedScale, WaterAccelerate, dt);
    Velocity = vel;
}
```

在 `_PhysicsProcess` 里，`IsInWater` 为真时整段替换掉原本的地面/空中分支，改为调用 `ApplyWaterMove`，并把 `Input.GetVector(...)` 算出的原始 `Vector2` 一起传进去（用来判断"是不是完全没有输入"）。水体本身就是一个开着 `Monitoring` 的 `Area3D`，`BodyEntered`/`BodyExited` 分别把 `IsInWater` 设为 `true`/`false`，这部分接线本教程前面章节已经写过好几次同样的模式，不再重复代码。

> **一个明确不做的真实功能**：DOOM3 还有一个 `CheckWaterJump()`/`WaterJumpMove()`（`Physics_Player.cpp:1213-1249`）——朝着齐腰深水域的墙壁方向游过去、面前一点点距离内墙面以上是空气、墙面以下还是实心墙，就会自动判定"这是从水里爬上岸的边缘"，给一个固定的抛物线速度（`200*viewForward - 350*gravityNormal`）把角色甩到岸上，2 秒内不受正常游泳控制。这本质是一个只服务于"贴着水池边缘手动往上爬"这一个具体场景的特判，判定逻辑要连续探测墙面两个高度上的实体方块与空气交界，跟本教程 2.5 节"水体是一整个 Area3D"的简化模型不兼容（没有"沿墙面探测"的几何信息可用）。这不是漏看了源码，是明确评估过之后跳过的——大多数关卡设计能用一段简单的坡道或梯子达到同样的"离开水域"效果，这个特判本身也更像是 id Software 给某几个具体场景手工调的边缘情况，不是"通用游泳系统"必须有的一环。如果你的关卡确实需要贴墙起跳出水，参考上面给出的行数自己实现思路是现成的。

### 2.6 爬梯

> 这一节的第一版写错了，写成"只要抬头就会自动往上爬，不需要按任何键"——这个说法不对，直接去读了 `neo/d3xp/physics/Physics_Player.cpp::LadderMove()`（第 852-926 行）的真实源码才发现问题：DOOM3 原版的垂直爬升速度是 `wishvel = -0.9f * gravityNormal * upscale * scale * (float)command.forwardmove;`，**乘了 `command.forwardmove`（前后移动键的输入值）**——不按 W/S，这一项恒为 0，人不会动。视角俯仰角算出来的 `upscale` 只是"调节爬升方向和快慢"的系数（水平看着梯子时它已经接近 1，低头会让它变小甚至反向），不是唯一驱动力。这一版当时自称"按真实源码改正"，但后来把整个 `LadderMove()` 从头到尾又完整读了一遍，发现还漏了四块东西，之前那版远没有"对齐"源码：
>
> 1. **磁吸贴墙**：源码每帧都执行 `wishvel = -100.0f*ladderNormal; current.velocity = (gravityNormal·velocity)*gravityNormal + wishvel;`——先把速度里"不沿重力方向"的分量整个清零，再叠加一个固定的贴墙拉力，把角色往梯子表面吸。上一版完全没有这一步，理论上角色可以慢慢飘离梯子。
> 2. **跳跃/蹲下是独立的第二条垂直输入通道**：源码里还有 `wishvel += -0.5f*gravityNormal*scale*(jump ? 127 : (crouch ? -127 : 0));`——按跳跃键无条件往上爬、按蹲下键无条件往下爬，跟有没有按 W/S 完全无关。上一版"不按键 forwardInput 恒为 0，climbSpeed 恒为 0"这句话是错的，只在"没有额外按跳跃/蹲下"的前提下才成立。
> 3. **面朝方向决定左右翻转**：源码在加横移分量之前有一句 `if (ladderNormal·viewForward > 0.0f) right = -right;`——背对着梯子往右移动時，世界空间里的横移方向要整个翻过来，上一版的横移完全没做这个判断。
> 4. **走的是 Friction + Accelerate，不是 Lerp**：源码这里跟地面移动一样调用 `Friction()` 和 `Accelerate(wishvel, wishspeed, PM_ACCELERATE)`，把结果的垂直速度钳制在 `PM_LADDERSPEED`（100 单位/秒）以内，且在完全没有垂直意愿时会混入一部分重力。上一版直接用 `Velocity.Lerp(targetVel, dt*10f)` 逼近目标速度，是手感相似但机制不同的近似写法，不是源码真正的机制。
>
> 下面是补上这四块之后的版本，顺便复用了 2.2 节已经写好的 `Friction`/`Accelerate` 辅助函数。

```csharp
// PlayerController.cs 追加
public bool IsOnLadder;
public Vector3 LadderNormal;   // 由梯子的 Area3D 检测逻辑提供朝向，指向"墙外"（玩家所在的一侧）
private const float LadderSpeed = 2.5f;       // 对应源码 PM_LADDERSPEED = 100 单位/秒，按 2.2 节同样的比例换算成米/秒
private const float LadderMagnetPull = 3.0f;  // 对应源码 "wishvel = -100.0f * ladderNormal" 的贴墙拉力，同样按比例换算

private void ApplyLadderMove(Vector2 rawInput, Vector3 wishDir, float dt)
{
    // 第一步：磁吸贴墙——只保留速度里的垂直分量，横向分量整个清零，
    // 再叠加一个固定的贴墙拉力（沿 -LadderNormal，即"往墙里"的方向）
    Vector3 wishvel = new Vector3(0, Velocity.Y, 0) - LadderNormal * LadderMagnetPull;

    // upscale：跟上一版一样，camForward 与"上"方向的夹角决定爬升方向和快慢
    Vector3 camForward = -_camera.GlobalTransform.Basis.Z;
    float upscale = Mathf.Clamp((Vector3.Up.Dot(camForward) + 0.5f) * 2.5f, -1f, 1f);

    // 第二步：前后键驱动的爬升——这部分沿用上一版的公式
    float forwardInput = -rawInput.Y;
    wishvel += Vector3.Up * (-0.9f * upscale * forwardInput * RunSpeed);

    // 第三步：跳跃/蹲下是完全独立的第二条垂直输入通道，跟前后键无关，
    // 这是上一版遗漏、也是这次改正的重点之一
    if (Input.IsActionPressed("jump")) wishvel += Vector3.Up * (RunSpeed * 0.5f);
    else if (Input.IsActionPressed("crouch")) wishvel += Vector3.Down * (RunSpeed * 0.5f);

    // 第四步：左右横移——先滤掉沿梯子法线方向的分量只保留贴着梯子平面的部分，
    // 再根据"是不是正对着梯子"翻转左右方向
    Vector3 right = _camera.GlobalTransform.Basis.X;
    if (LadderNormal.Dot(camForward) > 0f) right = -right;
    Vector3 lateral = wishDir - wishDir.Project(LadderNormal);
    float rightAmount = lateral.Dot(right.Normalized());
    wishvel += right.Normalized() * rightAmount * RunSpeed;

    // 第五步：跟地面移动同一套 Friction + Accelerate，不再是简单粗暴地 Lerp 到目标速度
    Vector3 wishDirFinal = wishvel.LengthSquared() > 0.0001f ? wishvel.Normalized() : Vector3.Zero;
    float wishSpeedFinal = wishvel.Length();

    Vector3 vel = ApplyFriction(Velocity, dt);
    vel = ApplyAcceleration(vel, wishDirFinal, wishSpeedFinal, Accelerate, dt);

    // 垂直速度钳制在 LadderSpeed 以内，对应源码的 PM_LADDERSPEED
    vel.Y = Mathf.Clamp(vel.Y, -LadderSpeed, LadderSpeed);

    // 完全没有垂直意愿（没按 W/S，也没按跳跃/蹲下）时混入一部分重力，
    // 让角色贴着梯子慢慢往下滑，而不是无限期悬空不动——源码在这里对重力做了特殊处理
    bool noVerticalWish = Mathf.Abs(forwardInput) < 0.001f && !Input.IsActionPressed("jump") && !Input.IsActionPressed("crouch");
    if (noVerticalWish)
    {
        vel.Y -= Gravity * 0.25f * dt;
    }

    Velocity = vel;
}
```

同样在 `_PhysicsProcess` 顶部加一个 `IsOnLadder` 分支，优先级比游泳和地面/空中都高，调用时把 `Input.GetVector(...)` 算出的原始 `Vector2` 和 `wishDir` 一起传进去。梯子的检测可以用一个贴着梯子表面的窄 `Area3D`，进入时记录法线方向、置位 `IsOnLadder`。

这次的错误提醒了一件事：**本教程前面几轮"完全参考"的修订，一部分是我直接去读源码验证的，另一部分是根据更早之前研究这份源码时留下的文字总结转述的——后者存在被我自己转述错、或者当时的总结本身就不够精确的风险**。如果你在后面章节看到某个说法感觉不对劲、或者行为跟直觉明显冲突，最可靠的做法就是像这次一样直接要求我去读对应的源码文件核实，而不是默认我转述的一定准确。

### 2.7 台阶步进：楼梯不该让角色一顿一顿地跳

`MoveAndSlide()` 本身能处理"撞到矮台阶就自动上去"这类简单情况，但台阶如果比较陡或者移动速度比较快，会出现明显的一顿一顿的抖动感。DOOM 3 在 `SlideMove()` 里专门处理了这个问题：先在当前高度试走、发现被挡住了，再垫高试走、确认垫高确实有用，最后把结果贴回台阶实际的高度。

> 这一节的第一版代码写得不完整，只做了"垫高之后走一下"，没有先判断"当前高度到底有没有被挡住"——结果是角色在完全平坦的地面上走路也会被这段代码不断顶高，因为条件判断只测了"垫高之后能不能走"，平地上这个条件几乎永远成立。改正版本补上了被漏掉的两步：**先确认当前高度确实被挡住了才有必要垫高**，以及**垫高之后要往下探一次、贴回台阶的实际高度**，不能直接假设台阶正好有 `StepHeight` 那么高。

> 这里还有一处遗漏：真实源码（`Physics_Player.cpp:225-276`）只在 `nearGround` 为真时才会尝试整套台阶逻辑，而 `nearGround` 不是简单的"是否贴地"，还包括"虽然在空中，但离地面很近"的情况——源码在判断之前先做了一次向下的预探测，注释原话是"贴近地面时做台阶检测，能让玩家在跳跃的同时也平滑地走上楼梯"。下面代码里的 `if (!IsOnFloor()) return;` 直接把这整个空中-但-贴近地面的情况排除掉了，结果是跳着上楼梯时台阶步进完全不生效、只能靠 `MoveAndSlide()` 自己那套不太平滑的处理。改正版本加了一次向下的短距离探测来补上这一支：

```csharp
// PlayerController.cs 追加
[Export] public float StepHeight = 0.3f;

// 这次垫高实际抬升了多少、什么时候发生的——3.2 节的视角平滑（对应源码 BobCycle() 里的
// stepUpDelta 处理）要读这两个字段，不属于台阶步进本身的逻辑，只是顺手在这里记一下
private float _stepUpDelta;
private float _stepUpElapsed;

private void ApplyStepUp(Vector3 horizontalMotion, float dt)
{
    if (horizontalMotion.LengthSquared() < 0.0001f) return;

    // nearGround：贴地本身算数；不贴地时再做一次向下的短距离探测（探测距离用 StepHeight），
    // 只要能摸到地面也算数——这样跳跃上升途中掠过一级台阶边缘时，台阶步进依然会生效，
    // 不会等到完全落地才开始工作，跳着上楼梯才不会顿一下
    bool nearGround = IsOnFloor();
    if (!nearGround)
    {
        var groundCheckParams = new PhysicsTestMotionParameters3D { From = GlobalTransform, Motion = Vector3.Down * StepHeight };
        var groundCheckResult = new PhysicsTestMotionResult3D();
        nearGround = PhysicsServer3D.BodyTestMotion(GetRid(), groundCheckParams, groundCheckResult);
    }
    if (!nearGround) return;

    Vector3 motion = horizontalMotion * dt;

    // 第一步：当前高度试走一下，看看有没有被挡住
    var flatParams = new PhysicsTestMotionParameters3D { From = GlobalTransform, Motion = motion };
    var flatResult = new PhysicsTestMotionResult3D();
    PhysicsServer3D.BodyTestMotion(GetRid(), flatParams, flatResult);
    float flatTravel = flatResult.GetTravel().Length();

    if (flatTravel >= motion.Length() * 0.99f)
    {
        return;   // 当前高度就走完了，前面没东西挡路，不需要垫高
    }

    // 第二步：假装垫高 StepHeight，再试一次同样的移动
    Transform3D raisedTransform = GlobalTransform;
    raisedTransform.Origin += Vector3.Up * StepHeight;
    var raisedParams = new PhysicsTestMotionParameters3D { From = raisedTransform, Motion = motion };
    var raisedResult = new PhysicsTestMotionResult3D();
    PhysicsServer3D.BodyTestMotion(GetRid(), raisedParams, raisedResult);
    float raisedTravel = raisedResult.GetTravel().Length();

    if (raisedTravel <= flatTravel)
    {
        return;   // 垫高也没能多走，说明前面是墙，不是台阶，不该垫高
    }

    // 第三步：确实是台阶——垫高之后走到的这个点，再往下探，贴回台阶实际的高度，
    // 而不是直接假设台阶正好有 StepHeight 那么高
    Vector3 steppedPos = raisedTransform.Origin + raisedResult.GetTravel();
    var downParams = new PhysicsTestMotionParameters3D
    {
        From = new Transform3D(GlobalTransform.Basis, steppedPos),
        Motion = Vector3.Down * StepHeight
    };
    var downResult = new PhysicsTestMotionResult3D();
    PhysicsServer3D.BodyTestMotion(GetRid(), downParams, downResult);
    float previousY = GlobalPosition.Y;
    GlobalPosition = steppedPos + downResult.GetTravel();

    // 记下这次垫高实际抬升的高度、重置计时——3.2 节会拿这两个字段做视角平滑，
    // 不做的话角色走上台阶那一刻摄像机会跟着位置瞬间"跳"一下，很生硬
    _stepUpDelta = GlobalPosition.Y - previousY;
    _stepUpElapsed = 0f;

    // 这一帧的水平移动已经在上面这几步里手动做完了（垫高、走过去、再贴回台阶高度），
    // 如果紧接着调用的 MoveAndSlide() 还拿同一份 Velocity 再走一次，水平方向就会
    // 被重复叠加，实际移动距离变成两倍。这里把水平分量清空，只留 Y 方向（重力/跳跃）
    // 交给 MoveAndSlide() 去处理。
    Velocity = new Vector3(0, Velocity.Y, 0);
}
```

> 这一步之前还有一个问题：把 `GlobalPosition` 直接设成了包含完整水平位移的 `steppedPos`，但紧接着 `_PhysicsProcess` 里还会用同一份 `horizontalVelocity` 调用一次 `MoveAndSlide()`——水平方向等于走了两遍，这一帧实际移动的距离会变成正常的两倍，虽然平时不容易注意到（因为只在真正踩上台阶那一帧发生），但快速贴墙走楼梯时能明显感觉到"一步窜出去很远"。修法不是不做水平位移（那样台阶又量不到该往上垫多高），而是在 `ApplyStepUp` 自己已经手动完成这一帧的水平移动之后，把 `Velocity` 的水平分量清空，让随后的 `MoveAndSlide()` 只处理垂直方向。

在 `_PhysicsProcess` 里 `MoveAndSlide()` **之前**、算完地面加速度之后调用 `ApplyStepUp(horizontalVelocity, dt);`——三步走完之后，只有真正遇到台阶时角色才会被垫高并贴回台阶表面，平地上完全不会触发，也不会凭空往上飘。注意 `ApplyStepUp` 里读写的都是 `Velocity`（`CharacterBody3D` 自带的那个属性），传进来的 `horizontalVelocity` 参数只是用来做碰撞测试用的一份拷贝，函数末尾清空的是真正驱动 `MoveAndSlide()` 的那个 `Velocity`。

> **`ApplyStepUp` 只做了一半：上台阶，没做下台阶**——回头看 `SlideMove()` 完整的函数签名 `SlideMove( bool gravity, bool stepUp, bool stepDown, bool push )`，`WalkMove()` 调用它时 `stepUp`/`stepDown` 两个参数都传了 `true`（244 行附近）。`stepDown` 对应的是 `SlideMove()` 末尾单独的一段代码（405-415 行）：贴地状态下，水平移动结束之后，**总是**再往下探 `maxStepHeight` 距离，只要探到了地面就直接把角色贴过去。这解决的是和"上台阶"对称的另一个问题：走下楼梯、或者沿着一个比较陡但还没陡到 `MIN_WALK_NORMAL` 之外的下坡走的时候，角色的水平速度会让它在每一级台阶的边缘飞出去一小段抛物线，再落到下一级——这就是很多简陋移动实现里"下楼梯一顿一顿地往下跳"的来源，跟 2.7 节开头说的"上楼梯抖动"其实是同一个问题在下坡方向的镜像。
>
> `ApplyStepUp` 里手写的三步测试（试走、垫高再试走、贴回台阶高度）完全没有覆盖这个方向——它只在"水平方向被挡住、垫高之后能走通"时触发，正常走下坡根本不会被挡住，自然不会进这段逻辑。这个问题不需要照抄 DOOM3 的 `stepDown` 探测再手写一遍：Godot 的 `CharacterBody3D` 本来就有一个功能上对应的内置机制——`FloorSnapLength`，作用完全就是"贴地状态下，每次移动后允许角色贴着地面向下吸附的最大距离"，本质就是 `stepDown` 那段代码在引擎层面的等价实现。问题在于**这个属性不configure就等于没有**（不同 Godot 版本的默认值不完全一致，不能假设默认值刚好够用），本教程至今没有一处显式设置过它，相当于下坡台阶步进这半边功能一直是"看运气"：
>
> ```csharp
> // PlayerController.cs 的 _Ready() 里追加，和 2.2 节的 FloorMaxAngle 放在一起显式配置
> public override void _Ready()
> {
>     // ...原有初始化...
>     FloorSnapLength = StepHeight;   // 至少要不小于 StepHeight，才能跟 ApplyStepUp 的上台阶幅度对称
> }
> ```
>
> 这里 `FloorSnapLength` 放在 `_Ready()` 而不是每帧动态改，是因为它和 `StepHeight` 应该始终保持一致——如果你的关卡有些区域台阶特别高，与其动态调这个值，不如给那些区域单独放一个"楼梯"专用的 `StepHeight` 更大的变体角色设置，两者不该在运行时频繁切换（`FloorSnapLength` 变化太频繁会让贴地判定本身变得不稳定）。

### 2.8 跳跃：一个大家都以为很简单、其实藏了好几个坑的动作

> 前面 2.1、2.2 节里，跳跃只用了一行代码搪塞过去：`velocity.Y = JumpVelocity`。跟 2.4/2.5/2.6 节一样的剧本——先给一个能跑的最简版本让你能看到效果，欠的账现在还。直接读 `Physics_Player.cpp::CheckJump()`（1174-1206 行）和它在 `WalkMove()`（644-669 行）里的调用点，会发现这一行代码背后至少漏了四件事。

**1. 起跳是叠加到当前速度上的，不是直接覆盖**

```cpp
current.velocity += addVelocity;
```

不是 `current.velocity = addVelocity`。平时感觉不出区别（起跳前 `velocity.Y` 通常本来就接近 0），但只要垂直方向上已经有别的东西在起作用（比如台阶步进残留的一点速度、或者别的系统在同一帧也想改 `velocity.Y`），用 `=` 会把那部分直接吃掉。改正：

```csharp
velocity.Y += JumpVelocity;
```

**2. 跳跃速度不该是一个手调的数字，该由"想跳多高"反推出来**

DOOM3 从不直接写死一个跳跃速度常量。真正的输入是 `maxJumpHeight`（对应 cvar `pm_jumpheight`，默认 48 个世界单位），起跳速度是每次现算的：

```cpp
addVelocity = 2.0f * maxJumpHeight * -gravityVector;
addVelocity *= idMath::Sqrt( addVelocity.Normalize() );
current.velocity += addVelocity;
```

`-gravityVector` 是重力的反方向（"上"）；`idTech4` 里 `Normalize()` 的返回值是"归一化前的原始长度"（副作用才是真正做归一化），所以 `addVelocity.Normalize()` 拿到的其实是 `2gh`，开根号、乘回已经变成单位向量的 `addVelocity`，整个式子就是最基础的抛体公式 **v = √(2gh)**：给定"想跳多高"，反推起跳瞬间该给多大的垂直速度。好处是改跳跃高度只需要改一个直觉数字，重力变了跳跃高度也不用重新配平——而且这套写法用的是活的 `gravityVector` 而不是写死的坐标轴，DOOM3 里有些区域重力方向不常规，这个公式一样成立。

Godot 项目通常不需要"任意方向重力"，可以简化成假设重力沿 -Y：

```csharp
[Export] public float JumpHeight = 1.1f;   // 想跳多高（米），不是速度
[Export] public float Gravity = 20.0f;

private float JumpVelocity => Mathf.Sqrt(2f * Gravity * JumpHeight);
```

把 2.1/2.2 节里 `[Export] public float JumpVelocity = 6.0f;` 换成上面这两行——`JumpVelocity` 变成一个只读的计算属性，`JumpHeight` 才是真正该调的旋钮。

**3. 不能在蹲着的时候起跳**

```cpp
if ( current.movementFlags & PMF_DUCKED ) {
    return false;
}
```

`CheckJump()` 开头就把这条堵死了，跟 2.4 节的 `_isDucked` 是同一个状态：

```csharp
if (Input.IsActionJustPressed("jump") && !_isDucked)
```

**4. 起跳那一帧，水平移动要立刻按"空中"算，不能等下一帧**

这条最容易漏。`WalkMove()` 的调用顺序（644-669 行）：`CheckJump()` 是这个函数最先做的事，一旦成功，**立刻**分支进 `AirMove()` 并直接返回，根本不会走到后面地面摩擦力/加速度那部分代码——起跳这一帧，水平移动从头到尾都是按空中规则算的，不存在"这一帧先按地面走、下一帧才切到空中"的过渡。

而 2.2 节现在的写法是拿 `IsOnFloor()` 去判断该走哪个分支——问题是 Godot 的 `IsOnFloor()` 是"上一次 `MoveAndSlide()` 算出来的结果"，起跳这一帧你还没调用这次的 `MoveAndSlide()`，读到的还是跳之前那次的 `true`，结果水平方向会先走一次地面分支，跟 DOOM3 对不上。修法是自己记一个"这一帧刚跳了"的标志，覆盖掉这一帧的地面判断：

```csharp
bool useAirMove = !IsOnFloor() || justJumped;
```

**完整版**：把 2.2 节的 `_PhysicsProcess` 换成这个版本，四处修正全部合并进去了（`ApplyFriction`/`ApplyAcceleration` 内容不变，见 2.2 节；`_isDucked` 是 2.4 节引入的字段）：

```csharp
[Export] public float WalkSpeed = 4.0f;
[Export] public float RunSpeed = 7.0f;
[Export] public float JumpHeight = 1.1f;
[Export] public float Gravity = 20.0f;

private const float Accelerate = 10.0f;
private const float AirAccelerate = 1.0f;
private const float Friction = 6.0f;
private const float StopSpeed = 1.0f;

private float JumpVelocity => Mathf.Sqrt(2f * Gravity * JumpHeight);

public override void _PhysicsProcess(double delta)
{
    float dt = (float)delta;
    Vector3 velocity = Velocity;
    bool justJumped = false;

    if (IsOnFloor())
    {
        if (Input.IsActionJustPressed("jump") && !_isDucked)
        {
            velocity.Y += JumpVelocity;
            justJumped = true;
        }
    }
    else
    {
        velocity.Y -= Gravity * dt;
    }

    Vector2 inputDir = Input.GetVector("move_left", "move_right", "move_forward", "move_back");
    Vector3 wishDir = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();
    // *SpeedModifier 是 2.2 节留的增益系统接口，6.3 节会说明它什么时候真正被改
    float wishSpeed = (Input.IsActionPressed("sprint") ? RunSpeed : WalkSpeed) * SpeedModifier;

    Vector3 horizontalVelocity = new Vector3(velocity.X, 0, velocity.Z);
    bool useAirMove = !IsOnFloor() || justJumped;

    if (useAirMove)
    {
        horizontalVelocity = ApplyAcceleration(horizontalVelocity, wishDir, wishSpeed, AirAccelerate, dt);
    }
    else
    {
        horizontalVelocity = ApplyFriction(horizontalVelocity, dt);
        horizontalVelocity = ApplyAcceleration(horizontalVelocity, wishDir, wishSpeed, Accelerate, dt);
    }

    velocity.X = horizontalVelocity.X;
    velocity.Z = horizontalVelocity.Z;

    Velocity = velocity;
    MoveAndSlide();
}
```

**DOOM3 里不存在、但你可能会想加的东西**：二段跳、跳跃消耗体力、跳跃打断后摇——`CheckJump()` 里完全没有这些，而且它只能从 `WalkMove()`（地面状态）进入，意味着**在空中再按跳跃键什么都不会发生**，没有"还剩几次跳跃机会"这种计数器。想要二段跳是你在 DOOM3 基础上主动做的设计延伸，不是教程漏写了，加的时候记得别让二段跳的判断也去卡 `IsOnFloor()`（二段跳的定义就是在空中也能跳一次）。

**兔子跳成立还有第三个原因，藏在这一节的代码顺序里**：2.3 节讲过兔跳依赖两个条件（空中摩擦力为0、加速度用投影而不是矢量差值），但这两条只解释了"空中怎么持续加速"，没解释另一半——**为什么落地之后立刻再跳一次，水平速度不会被地面摩擦力先咬掉一口**。答案在 `WalkMove()`（644-669行）的调用顺序本身：

```cpp
void idPhysics_Player::WalkMove() {
    ...
    if ( idPhysics_Player::CheckJump() ) {
        // jumped away
        ...
        return;
    }

    idPhysics_Player::Friction();
    ...
}
```

`CheckJump()` 在 `Friction()` **前面**调用，一旦这一帧起跳成功，函数立刻 `return`，`Friction()` 根本没有机会执行。也就是说：**只要你是在贴地的这一帧按下跳跃键，这一帧就完全不会经过摩擦力计算**，水平速度原封不动地带进空中；但凡你晚个几帧才按跳跃键，这几帧里 `Friction()` 已经在正常吃你的速度了，只是没落地时那一下摔停得那么狠。这也是为什么高手连跳讲究"落地那一刻就按下一次跳跃"，不是"落地之后随便什么时候跳都一样"——每晚一帧，摩擦力就多啃一点。

这一点你在 2.8 节写的完整版 `_PhysicsProcess` 里已经是对的，不用再改代码，只是要理解**为什么** `useAirMove = !IsOnFloor() || justJumped;` 这一行必须把 `justJumped` 也算进去：`justJumped` 为真的时候强制走 `AirAccelerate` 那个分支，跳过了 `ApplyFriction()`——这正是在复刻 `WalkMove()` 里"`CheckJump()` 成功就直接 `return`、绕开 `Friction()`"这个顺序。如果当初写成"先摩擦力、再判断跳不跳"，这一帧的水平速度就会先被削一刀，兔跳会明显没那么跟手。

（Source 引擎——Half-Life 2、CS:GO 这条线——的玩家移动代码是同一个 Quake 血统的另一分支，`gamemovement.cpp` 公开资料里描述的也是同一个原理：跳跃检测在摩擦力计算之前处理，成功起跳就跳过地面摩擦这一步。这里没有 Source 引擎的源码可以逐行核对，只能说这是同一条技术脉络下的通用做法，不像上面 DOOM3 那部分是直接读代码验证过的。）

### 2.9 弹板与连续起跳：DOOM3 的离地检测还藏着这两手

想做弹板（踩上去把人弹飞）和连续起跳（落地立刻能再跳，手感要脆），光靠 2.7 节的 `IsOnFloor()` 判断是不够的。直接读 `Physics_Player.cpp::CheckGround()`（959-1061行）会发现，DOOM3 在"贴地/离地"这件事上，比一句 `IsOnFloor()` 多想了两层：

**第一层：离地够快，直接判定"被弹飞"，不等下一帧**

```cpp
// 检查是不是正在被弹开
if ( (current.velocity * -gravityNormal) > 0.0f && ( current.velocity * groundTrace.c.normal ) > 10.0f ) {
    groundPlane = false;
    walking = false;
    return;
}
```

这段在 `CheckGround()` 里，每帧都会跑：如果角色正在往"上"（重力反方向）移动、且速度沿地面法线方向的分量够大，立刻把 `groundPlane` 清成 `false`，不管这一帧碰撞检测本身有没有把它判成"贴地"。为什么需要这个：弹板给玩家一个瞬间的向上冲量之后，玩家的**位置**这一帧可能还没真正离开弹板的碰撞体积，如果这时候还按"贴地"处理，重力/摩擦力那一套逻辑会立刻把刚给出去的冲量吃掉一部分，弹起来的力道会打折扣。

Godot 的 `IsOnFloor()` 天生没有这个机制——它是**上一次 `MoveAndSlide()` 的缓存结果**，弹板给速度的那一帧，这次的 `MoveAndSlide()` 还没跑，`IsOnFloor()` 读到的还是弹起来之前的 `true`。解决办法不是在 `IsOnFloor()` 上做文章（做不到），而是**弹起的瞬间由弹板自己主动把"贴地"状态清掉**，不指望引擎下一帧自己反应过来：

```csharp
// PlayerController.cs 追加
public void ApplyLaunch(Vector3 velocity)
{
    Velocity = velocity;
    _isGrounded = false;   // 立刻清掉，不等下一帧IsOnFloor()自己更新
}
```

`_isGrounded` 是你自己在 `_PhysicsProcess` 里缓存的"这一帧是不是在地面"的字段（2.2-2.8 节的判断逻辑都可以统一改成读这个字段，而不是每处都单独调用一次 `IsOnFloor()`）。

**第二层：摔得够狠，标记一次硬着陆**

```cpp
// 如果上一帧没有地面接触
if ( !hadGroundContacts ) {
    // 如果只是顺着斜坡滑下来的，不算硬着陆
    if ( (current.velocity * -gravityNormal) < -200.0f ) {
        current.movementFlags |= PMF_TIME_LAND;
        current.movementTime = 250;
    }
}
```

刚落地（上一帧不是贴地、这一帧是）那一刻，如果下落速度超过阈值，标记 `PMF_TIME_LAND`，`movementTime` 记 250 毫秒。

> **这里要纠正上一版一个想当然的说法**：上一版写的是"250 毫秒内不能再跳"，理由是这个标志位注释写着"movementTime is time before rejump"。但只把 `CheckGround()` 读完是不够的——把 `PMF_TIME_LAND`/`current.movementTime` 在整个 `Physics_Player.cpp` 里的每一处用到的地方都搜出来读一遍，会发现一个反直觉的结果：`CheckJump()` 自己从头到尾都**没有**检查这个标志位或 `current.movementTime`，`movementTime` 真正被检查的地方只有三处——`CheckLadder()` 开头（贴地硬着陆的短暂时间内不允许再抓梯子）、`CheckWaterJump()` 开头（同理，不允许立刻再触发水跳）、以及 `SetKnockBack()` 开头（不让一次新的击退打断还没结束的上一次击退计时）。也就是说，**真实的 DOOM3 里，哪怕你从很高的地方摔下来触发了"硬着陆"动画和摔落伤害，落地那一刻依然可以立刻再跳一次，没有任何强制的跳跃冷却**——那句注释描述的是设计意图，但实际代码从来没有把它接到 `CheckJump()` 上，是一个"名不副实"的标志位命名。
>
> 下面 `LandRecoveryTimer`/`CanJump` 这套"硬着陆之后短时间不能跳"的机制，因此**不能再算是"完全参考 DOOM3"**——它是本教程主动追加的手感强化，跟 2.8 节结尾提到的二段跳是同一类东西：DOOM3 本身没有，但很多现代动作类游戏（尤其强调"落地要有份量感"的第三人称/平台跳跃类）会加这类"硬着陆恢复窗口"，用来让摔落的冲击感在操作上也有反馈，不只是视觉上的镜头下沉（3.3 节）。这不是缺陷——是"照抄 DOOM3 的字面行为"和"做一个手感扎实的现代商业级动作系统"之间一个明确的分岔口，本教程选择保留这个强化，只是要诚实地说清楚它的来源不是源码，而是设计判断。如果你想要跟 DOOM3 逐帧一致的行为，把下面 `CanJump` 这个限制去掉、`Input.IsActionJustPressed("jump") && CanJump` 改回不带 `CanJump` 即可，`LandRecoveryTimer` 这套字段可以整体删掉。
>
> 注意 `PMF_TIME_LAND` 虽然不管跳跃，但**不是没用**——它驱动的是 3.3 节 `landChange`/`landTime` 那套摄像机镜头下沉效果，这部分本教程的 `TrackFallImpact`/`UpdateLandingDip` 已经完全参考、没有这个"想当然"的问题。

Godot 版本，两层写法可以一起放进角色控制器：

```csharp
// PlayerController.cs 追加
[Export] public float HardLandVelocity = -8.0f;      // 触发硬着陆的下落速度阈值
[Export] public float LandRecoveryTime = 0.25f;       // 硬着陆之后多久不能跳

private bool _isGrounded;
private float _fallVelocity;
private float _landRecoveryTimer;
private bool CanJump => _landRecoveryTimer <= 0f;

private void UpdateGround(float dt)
{
    bool wasGrounded = _isGrounded;
    _isGrounded = IsOnFloor();

    if (_isGrounded)
    {
        // 刚落地：按摔之前积累的下落速度判定是不是硬着陆
        if (!wasGrounded)
        {
            if (_fallVelocity < HardLandVelocity)
                _landRecoveryTimer = LandRecoveryTime;
            _fallVelocity = 0f;
        }
    }
    else
    {
        _fallVelocity = Mathf.Min(_fallVelocity, Velocity.Y);
    }

    if (_landRecoveryTimer > 0f)
        _landRecoveryTimer = Mathf.Max(0f, _landRecoveryTimer - dt);
}

public void ApplyLaunch(Vector3 velocity)
{
    Velocity = velocity;
    _isGrounded = false;
    _fallVelocity = Mathf.Min(0f, velocity.Y);   // 弹起来的瞬间不该被当成"正在硬摔"
}
```

`UpdateGround(dt)` 放在 `_PhysicsProcess` 最前面调用（在 2.2/2.8 节的重力/跳跃判断之前），把后面所有原本直接调 `IsOnFloor()` 的地方统一换成读 `_isGrounded`；跳跃那一行的判断从 `Input.IsActionJustPressed("jump")` 改成 `Input.IsActionJustPressed("jump") && CanJump`。

**弹板本身**：一个 `Area3D`，进碰撞区域就调用玩家身上的 `ApplyLaunch`：

```csharp
// LaunchPad.cs
using Godot;

public partial class LaunchPad : Area3D
{
    [Export] public float LaunchSpeed = 12f;
    [Export] public Vector3 LaunchDirection = Vector3.Up;
    [Export] public float Cooldown = 0.3f;   // 避免角色还没完全离开触发区域就被连续弹射

    private double _lastLaunchTime = -999.0;

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
    }

    private void OnBodyEntered(Node3D body)
    {
        if (body is not PlayerController player) return;

        double now = Time.GetTicksMsec() / 1000.0;
        if (now - _lastLaunchTime < Cooldown) return;

        _lastLaunchTime = now;
        player.ApplyLaunch(LaunchDirection.Normalized() * LaunchSpeed);
    }
}
```

**连续跳跃/弹板链**为什么能直接成立，不需要额外处理：`ApplyLaunch` 走的是 `Area3D` 触发，跟落地缓冲计时、地面判定完全是两条独立的路径——角色被弹起、还没落地就飞进下一个弹板的触发区域，第二次 `ApplyLaunch` 照样会执行，不会被"上一次落地还没缓冲完"卡住（因为角色压根还没落地，`_landRecoveryTimer` 根本没被触发过）。真正会限制连续弹跳手感的，只有 `Cooldown` 这一个参数——调小一点，弹板之间挨得近也能连续弹起来。

**受击顶退：玩家挨打时完全没有物理反馈，这在商业级 FPS 里说不过去**——回头看第 4/5 章会发现一件事：子弹/爆炸命中 `RigidBody3D` 会施加物理冲量把箱子推开，但命中玩家自己时，`TakeDamage` 只扣血，玩家的 `Velocity` 完全不受影响，站在原地一动不动地硬吃火箭弹。翻 `idPlayer::Damage()`（`Player.cpp:8456-8488`）会发现真实 DOOM3 完全不是这样：

```cpp
// 决定击退
int knockback = 0;
damageDef->dict.GetInt( "knockback", "20", knockback );

if ( knockback != 0 && !fl.noknockback ) {
    idVec3 kick = dir;
    kick.Normalize();
    kick *= g_knockback.GetFloat() * knockback * attackerPushScale / 200.0f;
    physicsObj.SetLinearVelocity( physicsObj.GetLinearVelocity() + kick );

    // 设置计时器，让玩家没法立刻把这段位移完全抵消掉
    physicsObj.SetKnockBack( idMath::ClampInt( 50, 200, knockback * 2 ) );
}
```

每次伤害定义（`damageDef`）都带一个 `knockback` 数值（默认 20），换算成速度叠加到玩家当前速度上——注意是**叠加**（`+= kick`），不是覆盖，这跟 2.8 节起跳的 `velocity.Y += JumpVelocity` 是同一个原则。真正有意思的是紧跟着的 `SetKnockBack()`：它把 `PMF_TIME_KNOCKBACK` 标志位和一小段 `movementTime`（50-200 毫秒，随击退力度变化）写进玩家状态，这段窗口内 `WalkMove()` 会做两件跟平时不一样的事（回看 2.2 节引用过的这段源码）：

```cpp
if ( ( groundMaterial && groundMaterial->GetSurfaceFlags() & SURF_SLICK ) || current.movementFlags & PMF_TIME_KNOCKBACK ) {
    accelerate = PM_AIRACCELERATE;   // 击退窗口内，就算贴地，加速度也降到空中那一档
}
...
if ( ... || current.movementFlags & PMF_TIME_KNOCKBACK ) {
    current.velocity += gravityVector * frametime;   // 击退窗口内，就算贴地，也照样叠加重力
}
```

这两行才是"击退感"真正的来源：不是单纯给一个速度就完了，而是在这段窗口内，玩家**暂时失去了地面移动那种"随时能用满额加速度把自己刹停/转向"的抓地力**——加速度被临时降到空中那一档（本来就比地面弱十倍，2.3 节讲过），且哪怕人还站在地上，也会像在空中一样持续被重力往下拽一点。效果是挨了一下之后，这一小段时间里角色会明显地被打得一个趔趄、身不由己地往后出去一截，而不是"瞬间贴住地面纹丝不动"或者"被打飞出去失控翻滚"两个极端。

Godot 版本，复用 2.2 节已经写好的 `Accelerate`/`AirAccelerate` 两档：

```csharp
// PlayerController.cs 追加
private float _knockbackTimer;
private bool InKnockback => _knockbackTimer > 0f;

// impulse 是要叠加的速度，不是要覆盖的速度；lockoutSeconds 对应源码 50-200ms 那个窗口，
// 击退越重传的值应该越大——具体怎么从伤害量换算成这两个参数，留给 4/8 章的伤害系统决定，
// 这里只负责"收到一次击退请求之后，物理层该怎么响应"
public void ApplyKnockback(Vector3 impulse, float lockoutSeconds)
{
    Velocity += impulse;
    _knockbackTimer = Mathf.Max(_knockbackTimer, lockoutSeconds);
}
```

在 `_PhysicsProcess` 里，`UpdateGround(dt)` 之后加一行 `_knockbackTimer = Mathf.Max(0f, _knockbackTimer - dt);`；地面分支选加速度系数的地方（2.8 节"完整版" `_PhysicsProcess` 里 `useAirMove` 之外的 `else` 分支）额外接入 `InKnockback`：

```csharp
if (useAirMove || InKnockback)
{
    horizontalVelocity = ApplyAcceleration(horizontalVelocity, wishDir, wishSpeed, AirAccelerate, dt);
    if (InKnockback && IsOnFloor()) velocity.Y -= Gravity * dt;   // 击退窗口内，贴地也照样叠加一部分重力
}
else
{
    horizontalVelocity = ApplyFriction(horizontalVelocity, dt);
    horizontalVelocity = ApplyAcceleration(horizontalVelocity, wishDir, wishSpeed, Accelerate, dt);
}
```

`ApplyKnockback` 本身不知道、也不需要知道调用者是谁——4/8 章真正接线的时候，玩家的 `TakeDamage` 只需要在扣血之外，按命中方向和伤害类型算一个 `impulse`（方向来自攻击者到玩家的连线或者子弹飞行方向，大小可以参考真实源码的比例：`g_knockback`（默认 1000）乘以 `damageDef` 的 `knockback` 值（默认 20）除以 200——换算成米制单位、按 2.2 节的量纲比例缩小，一次普通命中大概是 2-3 m/s 量级的一次性速度叠加，不是把人打飞出去几十米那种夸张力度）再调用 `player.ApplyKnockback(impulse, lockoutSeconds)` 即可，这也是 6.3 节马上要做的事——近战命中玩家自己（比如联机对战）时就是照这个方式接的。

到这里，第 2 章的移动状态机已经完整覆盖了 DOOM 3 `idPhysics_Player` 的全部移动模式（走/跑/蹲/跳跃/弹飞/受击顶退/空中/游泳/爬梯/台阶步进），是时候进入视角部分了。

---

## 3. 视角与摄像机：让移动"看起来"对

### 3.1 鼠标视角

回到 `PlayerController.cs`，加上视角控制。FPS 视角的标准做法：**左右转向（yaw）转动角色本身，上下看（pitch）只转动摄像机**——这样角色的移动方向（`Transform.Basis`，第 2 节用过）会跟着左右视角走，但不会因为你抬头低头而奇怪地倾斜。

```csharp
// PlayerController.cs 追加——注意这里只列新增的部分，2.2-2.7 节已经加上的
// WalkSpeed/RunSpeed/Accelerate/ApplyFriction/ApplyAcceleration 等等都还在，不要被下面
// 这段截图误导成"整个类只剩这些字段了"
[Export] public float MouseSensitivity = 0.003f;

private Node3D _head;
private Camera3D _camera;
private float _pitch;

public override void _Ready()
{
    _head = GetNode<Node3D>("Head");
    _camera = GetNode<Camera3D>("Head/Camera3D");
    Input.MouseMode = Input.MouseModeEnum.Captured;   // 隐藏鼠标、锁定在窗口内，FPS 标配
}

public override void _UnhandledInput(InputEvent @event)
{
    if (@event is InputEventMouseMotion mouseMotion && Input.MouseMode == Input.MouseModeEnum.Captured)
    {
        RotateY(-mouseMotion.Relative.X * MouseSensitivity);   // 左右转向：转动整个角色

        _pitch -= mouseMotion.Relative.Y * MouseSensitivity;
        _pitch = Mathf.Clamp(_pitch, Mathf.DegToRad(-85), Mathf.DegToRad(85));
        _head.Rotation = new Vector3(_pitch, 0, 0);            // 上下看：只转动头部/摄像机
    }

    if (@event.IsActionPressed("ui_cancel"))   // Esc 键，方便调试时把鼠标放出来
    {
        Input.MouseMode = Input.MouseMode == Input.MouseModeEnum.Captured
            ? Input.MouseModeEnum.Visible
            : Input.MouseModeEnum.Captured;
    }
}
// _PhysicsProcess 内容不变，此处省略——见 2.2-2.7 节
```

`RotateY()` 是 `Node3D` 自带的方法，直接绕角色自身的上轴转动，这也是为什么第 2 节的 `Transform.Basis * inputDir` 现在真的有意义了：角色转向之后，"相对角色的前方"这个概念也跟着转了，`wishDir` 自动就是"当前面朝的方向"，不需要额外处理。

`Input.MouseMode = Input.MouseModeEnum.Captured` 这一行很重要，没有它鼠标移动只会移动屏幕上的光标，不会转视角。

**这一节现在的 `MouseSensitivity` 是"能用"，但离一个商业级 FPS 该给玩家的鼠标设置差得远**——去读 DOOM3 真正处理鼠标输入的地方（不在 `d3xp/` 里，是更底层的 `framework/UsercmdGen.cpp`，`idUsercmdGenLocal::MouseMove()`，第 440-485 行附近）会发现几件事，值得挨个对照：

1. **灵敏度是纯线性缩放，没有非线性加速曲线**——`mx = mouseDx * sensitivity.GetFloat()`，`viewangles[YAW] -= m_yaw.GetFloat() * mx`，就是原始像素位移乘几个常数，仅此而已。这不是 DOOM3 偷懒，是**FPS 品类的行业共识**：竞技/精确瞄准场景几乎都拒绝非线性的"鼠标加速度"（移动越快每像素转的角度越多），因为它会破坏"肌肉记忆里同样的手部位移=同样的转向角度"这个前提，这也是为什么几乎所有 Windows 系统自带的"提高指针精确度"（系统级鼠标加速）选项，PC 端 FPS 都会建议玩家关掉。本节现有的 `MouseSensitivity` 就是纯线性缩放，这一点已经是对的，不用改——但也正因为这是一个真实存在、容易被问到的设计决策，这里明确写出来，比让读者自己猜"是不是漏做了"要好。
2. **有一个可选的原始位移平滑**——`m_smooth` cvar（1-8，默认 1），语义是"把最近 N 次鼠标事件的位移取平均再用"（`history[historyCounter&7]` 是个 8 帧环形缓冲）。默认值 1 等于"不平滑，直接用当前这一次"；调大能缓解低端鼠标或不稳定轮询率带来的转向抖动，代价是引入一点点输入延迟。这是一个**只处理"设备噪声"、不改变整体灵敏度曲线**的功能，跟第 1 点的"不要加速度曲线"并不矛盾。
3. **有独立的反转 Y 轴开关**——`in_mouseInvertLook`，`viewangles[PITCH] += m_pitch.GetFloat() * (in_mouseInvertLook.GetBool() ? -my : my)`。反转 Y 轴不是一个可有可无的偏好选项，是相当一部分玩家（尤其从飞行模拟/操纵杆时代过来的老玩家）离不开的可访问性设置，商业游戏基本没有不给这个开关的。

本教程目前完全没有第 2、3 点，只补第 3 点（性价比最高，实现成本几乎为零）和一个简化版的第 2 点：

```csharp
// PlayerController.cs 追加
[Export] public bool InvertMouseY = false;
[Export(PropertyHint.Range, "1,8,1")] public int MouseSmoothingSamples = 1;   // 对应 m_smooth，默认 1 = 不平滑

private readonly Vector2[] _mouseHistory = new Vector2[8];
private int _mouseHistoryIndex;

public override void _UnhandledInput(InputEvent @event)
{
    if (@event is InputEventMouseMotion mouseMotion && Input.MouseMode == Input.MouseModeEnum.Captured)
    {
        Vector2 rawDelta = mouseMotion.Relative;

        // m_smooth 的简化版：把最近 MouseSmoothingSamples 次的原始位移取平均。
        // 默认 MouseSmoothingSamples=1 时，下面这段等价于直接用 rawDelta，不引入任何延迟
        _mouseHistory[_mouseHistoryIndex % _mouseHistory.Length] = rawDelta;
        _mouseHistoryIndex++;
        int n = Mathf.Min(MouseSmoothingSamples, _mouseHistory.Length);
        Vector2 smoothedDelta = Vector2.Zero;
        for (int i = 0; i < n; i++)
        {
            smoothedDelta += _mouseHistory[(_mouseHistoryIndex - 1 - i + _mouseHistory.Length) % _mouseHistory.Length];
        }
        smoothedDelta /= n;

        RotateY(-smoothedDelta.X * MouseSensitivity);

        float pitchDelta = smoothedDelta.Y * MouseSensitivity * (InvertMouseY ? 1f : -1f);
        _pitch += pitchDelta;
        _pitch = Mathf.Clamp(_pitch, Mathf.DegToRad(-85), Mathf.DegToRad(85));
        _head.Rotation = new Vector3(_pitch, 0, 0);
    }

    if (@event.IsActionPressed("ui_cancel"))
    {
        Input.MouseMode = Input.MouseMode == Input.MouseModeEnum.Captured
            ? Input.MouseModeEnum.Visible
            : Input.MouseModeEnum.Captured;
    }
}
```

> **一个没法在这里彻底回答的开放问题**：DOOM3 能放心用"纯线性缩放"而不做任何操作系统级修正，是因为它通过 DirectInput/裸设备读数（`sys/win32/win_input.cpp`）拿到的是不经 Windows 指针加速度处理的原始位移。Godot 的 `InputEventMouseMotion.Relative` 在鼠标被 `Captured` 模式锁定时是不是同样绕开了操作系统的指针加速度、拿到的是真正的原始设备位移——这取决于具体平台和 Godot 版本的 `DisplayServer` 实现，本教程没有能力从这份 DOOM3 源码里替你确认 Godot 引擎内部这一层的行为。如果你实测发现灵敏度在不同鼠标 DPI/系统指针速度设置下手感不一致，这就是要去查的方向，而不是先怀疑上面这段代码写错了。
>
> 另外，上面 `_pitch` 的俯仰角限制沿用了原来的 ±85°，而 DOOM3 的默认值 `pm_maxviewpitch`/`pm_minviewpitch` 是 ±89°（`SysCvar.cpp:242-243`）——这个数字本节从没声称是照抄源码，±85° 单纯是一个更保守、不容易让摄像机转到接近垂直时出现万向节相关观感问题的常见取值，如果想要跟源码数值对齐，改成 89 即可。

### 3.2 视角晃动（View Bob）：完整移植 DOOM 3 的 `BobCycle()`

现在角色能走能看了，但站着不动和走路时摄像机是完全静止的，感觉发"飘"。这一节**完整照抄** `Player.cpp::BobCycle()` 的公式，不是一个近似效果——DOOM 3 的 view bob 实际上由**四个**独立部分叠加而成：一个走/跑速率不同的周期性正弦位置起伏、一个用"脚的奇偶"翻转符号做出的左右交替角度摇摆、一个跌落/落地时的独立冲击下沉（3.3 节），以及一个容易被忽略的第四部分——**台阶步进的视角平滑**：`BobCycle()`（`Player.cpp:5977-5997`）里专门检测 `physicsObj.HasSteppedUp()`，一旦上一帧发生了台阶垫高（对应本教程 2.7 节的 `ApplyStepUp`），就会把这次垫高的高度差存下来，用 `STEPUP_TIME`（200ms）的时间线性衰减掉，抵消掉角色被瞬间垫高的那一截位移，效果是走上台阶时摄像机看起来是"平滑升上去"，而不是跟着碰撞体一起瞬间弹一下。之前的版本只写了前三个部分，第四个是这次补上的。逐个实现：

```csharp
// PlayerController.cs 追加字段
[Export] public float WalkBobRate = 0.6f;     // 对应 pm_walkbob
[Export] public float RunBobRate = 0.8f;      // 对应 pm_runbob
[Export] public float CrouchBobRate = 1.0f;   // 对应 pm_crouchbob
[Export] public float BobUpAmount = 0.01f;    // 对应 pm_bobup，垂直位置起伏幅度
[Export] public float BobPitchAmount = 0.004f; // 对应 pm_bobpitch，点头角度幅度
[Export] public float BobRollAmount = 0.004f;  // 对应 pm_bobroll，左右摇摆角度幅度
[Export] public float RunPitchAmount = 0.004f; // 对应 pm_runpitch，纯速度驱动的前后倾（非周期性）
[Export] public float RunRollAmount = 0.01f;   // 对应 pm_runroll，纯速度驱动的左右倾（非周期性）
private const float MinBobSpeed = 0.3f;        // 对应 MIN_BOB_SPEED：低于这个速度直接清零，不产生 bob
private const float StepUpTime = 0.2f;         // 对应 STEPUP_TIME，台阶步进平滑窗口，200ms

private float _bobCycle;
private Vector3 _viewBobOffset;
private Vector3 _viewBobAngles;

private void UpdateViewBob(float dt)
{
    // 台阶步进的计时器要独立于下面"是否在走路"的分支持续前进——哪怕这一帧玩家已经停下，
    // 上一次垫高留下的高度差依然要按自己的时间线衰减完，不能被走路分支的提前 return 打断
    _stepUpElapsed += dt;

    Vector3 horizontalVel = new Vector3(Velocity.X, 0, Velocity.Z);
    float xySpeed = horizontalVel.Length();

    if (!IsOnFloor() || xySpeed <= MinBobSpeed)
    {
        // 腾空或几乎静止时直接清零、不是渐隐——DOOM3 原版就是这样处理的，
        // 保证玩家站定瞄准时摄像机绝对静止
        _bobCycle = 0;
        _viewBobOffset = _viewBobOffset.Lerp(Vector3.Zero, dt * 10f);
        _viewBobAngles = _viewBobAngles.Lerp(Vector3.Zero, dt * 10f);
        ApplyBobToCamera();
        return;
    }

    // 按当前状态（蹲/走/跑）选择不同的周期速率
    // 这里要老实说明一处简化：DOOM3 原版的 bobmove 不是简单按"按没按跑步键"二选一，
    // 而是 pm_walkbob*(1-bobFrac) + pm_runbob*bobFrac 的连续插值，bobFrac 来自体力值
    // （体力耗尽会让 bobFrac 从 1 往 0 走，跑步的 bob 手感逐渐退回走路的样子）——
    // 这依赖一套本教程压根没做的体力系统，所以这里用"是否按住 sprint"这个二值判断代替，
    // 是一处有意为之、且原因明确的简化，不是漏看了源码
    float bobRate = IsCrouchingNow() ? CrouchBobRate : (Input.IsActionPressed("sprint") ? RunBobRate : WalkBobRate);
    _bobCycle += bobRate * dt * Mathf.Tau;   // 走完一个完整周期 = 2π，和三角函数的周期对齐

    float bobFracSin = Mathf.Abs(Mathf.Sin(_bobCycle));    // 恒非负的折叠正弦波，对应 bobfracsin
    bool secondHalf = Mathf.Sin(_bobCycle) < 0;             // 对应 bobFoot 的奇偶——决定这一步是"左脚"还是"右脚"

    float crouchMultiplier = IsCrouchingNow() ? 3.0f : 1.0f;   // DOOM3 原版：蹲下时点头/摇摆幅度 ×3

    // 位置：垂直起伏，钳制上限（DOOM3 原版钳制在 6 个世界单位，这里按比例换算成米级上限）
    float vertical = Mathf.Min(bobFracSin * xySpeed * BobUpAmount, 0.08f);

    // 角度：点头分量（恒正）+ 左右摇摆分量（按脚的奇偶翻转符号——这是把恒正的正弦波
    // 变成真正 "左右左右" 交替摇摆的关键，直接省略这一步的话画面只会朝一个方向倾斜再回正）
    float speedForAngles = Mathf.Max(xySpeed, 2.0f);
    float pitchBob = bobFracSin * BobPitchAmount * speedForAngles * crouchMultiplier;
    float rollBob = bobFracSin * BobRollAmount * speedForAngles * crouchMultiplier;
    if (secondHalf) rollBob = -rollBob;

    // 额外叠加一层非周期性的、纯粹由瞬时速度驱动的倾斜——前后加减速带来轻微低头/抬头，
    // 左右平移带来轻微侧倾，这一层和上面周期性的 bob 是两个独立的角度来源，直接相加
    Vector3 localVel = Transform.Basis.Inverse() * Velocity;
    float runPitch = localVel.Z * RunPitchAmount;
    float runRoll = -localVel.X * RunRollAmount;

    _viewBobOffset = new Vector3(0, vertical, 0);
    _viewBobAngles = new Vector3(pitchBob + runPitch, 0, rollBob + runRoll);

    ApplyBobToCamera();
}

private void ApplyBobToCamera()
{
    // 台阶步进平滑：_stepUpElapsed 在 ApplyStepUp（2.7 节）真正垫高的那一帧被清零，
    // _stepUpDelta 记的是那一帧实际抬升的高度。在 StepUpTime 窗口内，反着补一个
    // 随时间线性衰减到 0 的偏移，让摄像机看起来是慢慢升上去的，而不是跟着碰撞体一起瞬间弹一下——
    // 对应源码 BobCycle() 里 stepUpDelta*(STEPUP_TIME-deltaTime)/STEPUP_TIME 那部分
    Vector3 stepUpOffset = Vector3.Zero;
    if (_stepUpElapsed < StepUpTime)
    {
        float remain = (StepUpTime - _stepUpElapsed) / StepUpTime;
        stepUpOffset = new Vector3(0, -_stepUpDelta * remain, 0);
    }

    Vector3 camPos = _viewBobOffset + _landingDipOffset + stepUpOffset;   // _landingDipOffset 见 3.3 节
    _camera.Position = camPos;
    _camera.Rotation = new Vector3(_viewBobAngles.X, 0, _viewBobAngles.Z);
}

private bool IsCrouchingNow()
{
    var capsule = (CapsuleShape3D)_collisionShape.Shape;
    return capsule.Height < (StandHeight + CrouchHeight) * 0.5f;
}
```

在 `_PhysicsProcess` 最后加一行 `UpdateViewBob(dt);`。

逐条说明这里"完全参考"到底参考了什么：

- **`bobRate` 按蹲/走/跑三档区分**——这是 DOOM3 原版真实存在、但很容易被简化掉的一个细节：蹲下移动的 bob 周期速率（`pm_crouchbob`）、走路（`pm_walkbob`）、跑步（`pm_runbob`）三者不同，不是同一个数字缩放。
- **`bobFracSin` 恒为非负**——用 `Mathf.Abs(Mathf.Sin(...))` 折叠出的包络，配合 `secondHalf` 判断（对应原版 `bobFoot & 1`）去翻转横向摇摆的符号——**这正是上一版教程漏掉的、把"单调的正弦波"变成"真正左右交替摇摆"的那个关键技巧**，没有这一步，摇摆看起来只会朝一个方向倾斜再弹回来，不会有"左右左右"的交替感。
- **周期性 bob 和纯速度驱动的倾斜是两个独立的角度来源**（`pitchBob`/`rollBob` 来自 bob 周期，`runPitch`/`runRoll` 直接来自瞬时局部速度），原版里这是两套完全不同的机制，简单相加，不是同一个公式套两次。
- **蹲下时点头/摇摆幅度 ×3**（`crouchMultiplier`）——这个细节容易被忽略，但是它是"蹲下移动时晃动感明显更强"这个手感的直接来源。
- **台阶步进平滑**（`_stepUpDelta`/`_stepUpElapsed`，见下面 `ApplyBobToCamera` 里的处理）——对应 `BobCycle()` 里 `HasSteppedUp()` 那一段，源码用 `STEPUP_TIME` 把垫高的高度差线性衰减掉；这一步依赖 2.7 节 `ApplyStepUp` 在真正垫高时把高度差和计时器写进这两个字段，两节代码要配合着看。

> 这里要老实说明一处没标注过的数值出入：上面的 `BobUpAmount`/`BobPitchAmount`/`BobRollAmount` 分别是 0.01/0.004/0.004，而 DOOM3 原版对应的 `pm_bobup`/`pm_bobpitch`/`pm_bobroll`（`SysCvar.cpp:258-262`）分别是 0.005/0.002/0.002，正好是原版的两倍。这不是换算误差——前面 2.2 节的换算是"量纲换算"（英寸转米），这几个 bob 幅度系数跟 2.2 节里 `Accelerate`/`Friction` 那几个无量纲比例系数一样，理论上不需要跟着单位换算改变。这里是特意调大了一倍，因为直接抄原版数值在 Godot 的这套摄像机/单位设置下摇摆几乎感觉不到，纯粹是为了可感知的手感调的，不是"完全参考"的一部分——如果你想要跟原版数值上更接近的手感，把这三个数各减半即可。**补一句之前漏说的**：上面代码里 `RunPitchAmount`/`RunRollAmount`（0.004/0.01）同样是 `pm_runpitch`/`pm_runroll`（`SysCvar.cpp:258-259`，0.002/0.005）的整整两倍，跟上面三个是同一个"调大一倍换取可感知手感"的决定，只是上一版写这条说明的时候只列了前三个受影响的常量，漏提了这两个——五个常量全部统一按同样的比例放大，不是只放大了一部分。
>
> 还有一处这一节代码没处理、但真实源码有处理的边界情况，诚实地留在这里当一个开放问题：`BobCycle()` 判断"要不要把 `bobCycle` 清零重新开始"的条件是 `(!usercmd.forwardmove && !usercmd.rightmove) || (xyspeed <= MIN_BOB_SPEED)`——**没有输入**和**速度太低**是两个独立的、用"或"连起来的条件，任意一个满足就清零。上面 `UpdateViewBob` 只判断了后半句（`xySpeed <= MinBobSpeed`），没有单独判断"是否完全没有移动输入"。两者通常等价（松开按键之后摩擦力很快会把速度降到阈值以下），唯一会分道扬镳的场景是角色完全没有输入、但因为某种原因还在高速滑动——比如站在一个只受重力驱动持续下滑的陡坡上，或者被 2.9 节的击退顶飞之后短暂失控滑行。这两种场景下，真实 DOOM3 会立刻把视角 bob 归零（因为没有主动输入），而上面这版代码会因为速度仍然高于 `MinBobSpeed` 继续播放行走摇摆动画——玩家明明没在自己走路，镜头却还在做走路的周期摇摆，观感上会有点奇怪。这本教程目前没有可靠的斜坡材质/击退滑行系统去精确复现这个边界，所以先诚实地记录这个开放问题，而不是假装处理好了。

### 3.3 落地冲击：跌落速度越快、镜头下沉越明显

DOOM 3 的 `CrashLand()` 按跌落冲击力度分四档，用快速下沉+缓慢回弹两段式给出反馈，这是一个和上面周期性 bob**完全独立**的第三个偏移源，两者在 `ApplyBobToCamera()` 里简单相加：

> 这里要老实说明一处简化：真实的 `CrashLand()`（`Player.cpp:5763-5883`）判断"摔得多重"用的不是"落地那一刻的峰值下落速度"，而是解一个二次方程算出来的**子帧冲击量** `delta = (vel + t*acc)^2 * 0.0001`（`t` 是这一帧里实际用于下落的那一小段时间，`acc` 是重力加速度），四档阈值（单人游戏下 `softDelta=30`、`hardDelta=45`、`fatalDelta=65`）都是卡在这个 `delta` 上，不是直接卡下落速度。下面 `severity = Mathf.Abs(_lastFallVelocity)` 用峰值下落速度做近似替代，四档阈值也是照着"感觉对应 -8/-16/-24/-32"手动挑的，跟源码这套子帧冲击量的计算不是一回事，只是效果上大致对应。

```csharp
// PlayerController.cs 追加
private Vector3 _landingDipOffset;
private float _landingDipStrength;
private double _landingDipStartTime;
private const float LandDeflectTime = 0.15f;   // 对应 LAND_DEFLECT_TIME
private const float LandReturnTime = 0.3f;     // 对应 LAND_RETURN_TIME
private float _lastFallVelocity;

// 在 _PhysicsProcess 里，每帧记录腾空时的下落速度，落地那一帧触发
private void TrackFallImpact()
{
    if (!IsOnFloor())
    {
        _lastFallVelocity = Mathf.Min(_lastFallVelocity, Velocity.Y);
        return;
    }
    if (_lastFallVelocity < -6.0f)   // 有意义的下落速度才触发，轻微的台阶步进不该有反馈
    {
        // 按冲击力度分四档——对应 DOOM3 原版 -8/-16/-24/-32 那四个档位，这里按比例换算
        float severity = Mathf.Abs(_lastFallVelocity);
        _landingDipStrength = severity switch
        {
            > 16f => 0.18f,
            > 12f => 0.12f,
            > 9f => 0.07f,
            _ => 0.03f,
        };
        _landingDipStartTime = Time.GetTicksMsec() / 1000.0;
    }
    _lastFallVelocity = 0;
}

private void UpdateLandingDip()
{
    if (_landingDipStrength <= 0.0001f)
    {
        _landingDipOffset = Vector3.Zero;
        return;
    }
    double elapsed = Time.GetTicksMsec() / 1000.0 - _landingDipStartTime;
    if (elapsed < LandDeflectTime)
    {
        float t = (float)(elapsed / LandDeflectTime);
        _landingDipOffset = new Vector3(0, -_landingDipStrength * t, 0);
    }
    else if (elapsed < LandDeflectTime + LandReturnTime)
    {
        float t = (float)((elapsed - LandDeflectTime) / LandReturnTime);
        _landingDipOffset = new Vector3(0, -_landingDipStrength * (1 - t), 0);
    }
    else
    {
        _landingDipStrength = 0;
        _landingDipOffset = Vector3.Zero;
    }
}
```

在 `_PhysicsProcess` 里，`TrackFallImpact()` 放在重力/地面判断附近每帧调用，`UpdateLandingDip()` 在 `UpdateViewBob(dt)` 之前调用（因为 `ApplyBobToCamera()` 里要用到刚算好的 `_landingDipOffset`）。

现在运行游戏：走路应该能看到清晰的左右交替摇摆和点头，蹲着移动摇摆明显更夸张，从高处跳下落地那一刻镜头应该有一个先快速下沉、再缓慢回弹的冲击感——这三层效果（周期 bob、速度倾斜、落地冲击）叠加在一起，就是 DOOM 3 玩家视角"扎实"这个直觉印象背后的全部数学。

> **一个明确划在本章范围之外的真实效果**：`idPlayer::GetViewPos()`（`Player.cpp:8950-8973`）算最终视角角度时，除了 `viewAngles + viewBobAngles`（本章处理的这套），还加了一项 `playerView.AngleOffset()`——这是另一个完全独立的系统（`PlayerView.cpp`），驱动的是"挨打时镜头猛地歪一下""爆炸在附近时画面震动"这类**跟战斗反馈绑定、而不是跟移动绑定**的镜头效果。本章的标题是"让移动看起来对"，`viewBob`/`landChange` 这一套都是移动/物理状态驱动的相机效果；`AngleOffset()` 那一套是伤害/战斗事件驱动的，性质上更接近第 4/8 章要处理的"打击反馈"，不属于这一章要完全参考的范围，本教程目前也没有在别处实现它——如果之后要做，应该按"独立于 view bob 的第三个角度偏移源，同样在相机最终角度那一步简单相加"的思路去接，跟 `_landingDipOffset` 是同一种叠加模式。

**先把这一步的手感调到自己满意再往下走**——移动和视角是玩家花在游戏里时间最长、最容易被感知到细微差别的部分，值得现在就花时间打磨。

---

## 4. 你的第一把枪：开火与命中判定

### 4.1 挂一把枪在摄像机前面

在 `Head/Camera3D` 下面新建一个 `Node3D`，改名 `WeaponHolder`，放一个占位的 `MeshInstance3D`（随便一个长方体，权当枪的模型，后面会换成真的模型）。位置大概摆在屏幕右下方，比如 `(0.3, -0.3, -0.5)`。

```
Player (CharacterBody3D)
└── Head
    └── Camera3D
        └── WeaponHolder (Node3D)
            └── GunMesh (MeshInstance3D，占位方块)
            └── Muzzle (Marker3D，放在枪口位置，等下用来算子弹发射点)
```

### 4.2 开火：一条射线，检测打中了什么

FPS 里"打枪"最常见的实现方式不是真的模拟一颗子弹飞行，而是**打一条射线**（业界俗称 hitscan）——按下鼠标的瞬间，直接从枪口/摄像机往前发射一条射线，看它撞到什么，立刻判定命中，视觉上再配一个子弹拖尾特效制造"子弹飞过去了"的错觉。新建 `Weapon.cs`，挂在 `WeaponHolder` 上：

> 这一节的 `query.Exclude` 那一行之前多写了一层 `GetParent()`——按 4.1 节的场景树，`Weapon.cs` 挂在 `WeaponHolder` 上，往上数三层（`WeaponHolder → Camera3D → Head → Player`）刚好是玩家节点，之前的代码写了四层 `GetParent()`，会跑到 `Player` 的父节点（关卡根节点）去做类型转换，`GetParent<CharacterBody3D>()` 直接抛异常。已经改成三层。

```csharp
using Godot;

public partial class Weapon : Node3D
{
    [Export] public float Damage = 20.0f;
    [Export] public float Range = 100.0f;
    [Export] public Node3D Muzzle;
    [Export] public Camera3D Camera;   // 拖入 Head/Camera3D

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("fire"))
        {
            Fire();
        }
    }

    private void Fire()
    {
        var spaceState = GetWorld3D().DirectSpaceState;
        Vector3 from = Camera.GlobalPosition;
        Vector3 to = from + (-Camera.GlobalTransform.Basis.Z) * Range;   // 摄像机的 -Z 方向就是"往前看"

        var query = PhysicsRayQueryParameters3D.Create(from, to);
        query.Exclude = new Godot.Collections.Array<Rid> { GetParent<Node3D>().GetParent<Node3D>().GetParent<CharacterBody3D>().GetRid() };

        var result = spaceState.IntersectRay(query);

        if (result.Count > 0)
        {
            Node3D hitObject = (Node3D)result["collider"];
            Vector3 hitPoint = (Vector3)result["position"];
            GD.Print($"打中了：{hitObject.Name}，位置：{hitPoint}");

            // 如果被打中的物体实现了受伤接口，就造成伤害——第 8 章做怪物的时候会用到这个
            if (hitObject.HasMethod("TakeDamage"))
            {
                hitObject.Call("TakeDamage", Damage);
            }
        }
    }
}
```

**关于 `query.Exclude` 那一行的说明**：射线检测默认什么都会撞，包括发射者自己的碰撞体（玩家的 `CharacterBody3D`）。上面那行层层 `GetParent()` 找到玩家节点、排除掉它——这行写法很丑，是故意先用最直白的方式让你看到"为什么需要排除自己"这个问题，第 4.3 节马上会给出更干净的写法。

运行游戏，对着地板或墙开枪，应该能在输出面板看到"打中了：xxx"的打印。这就是命中判定的核心——**一个物体只要有一个叫 `TakeDamage` 的方法，就能被任何武器伤害**，不需要它继承什么特定的基类或实现什么接口声明，这是 C# 里也能用的"鸭子类型"式调用（`HasMethod`/`Call` 是 Godot 提供的反射式调用，绕开了 C# 本身编译期强类型检查的限制，专门用于这种"我不关心你是什么类型，只关心你有没有这个方法"的场景）。

### 4.3 更干净的写法：用碰撞层，而不是层层 GetParent

Godot 的物理系统有"碰撞层（Collision Layer）"和"碰撞掩码（Collision Mask）"的概念——每个物理体属于哪些层、检测哪些层，是解决"射线不要打到自己人"这类问题的标准做法，比手动排除节点更可靠（尤其是关卡里东西一多，`Exclude` 列表很快会写不过来）。

在项目设置里给碰撞层取几个名字（`项目 -> 项目设置 -> 图层名称 -> 3D 物理`）：Layer 1 = `World`，Layer 2 = `Player`，Layer 3 = `Enemy`，Layer 4 = `Projectile`。玩家的 `CharacterBody3D` 碰撞层设为 `Player`；武器射线的检测掩码设为 `World + Enemy`（不检测 `Player` 层，自然就打不到自己，不需要手动 Exclude）：

```csharp
private void Fire()
{
    var spaceState = GetWorld3D().DirectSpaceState;
    Vector3 from = Camera.GlobalPosition;
    Vector3 to = from + (-Camera.GlobalTransform.Basis.Z) * Range;

    var query = PhysicsRayQueryParameters3D.Create(from, to);
    query.CollisionMask = 0b0101;   // 二进制：只检测第 1 层(World)和第 3 层(Enemy)，不检测第 2 层(Player)

    var result = spaceState.IntersectRay(query);
    if (result.Count > 0)
    {
        Node3D hitObject = (Node3D)result["collider"];
        if (hitObject.HasMethod("TakeDamage"))
        {
            hitObject.Call("TakeDamage", Damage);
        }
        SpawnImpactEffect((Vector3)result["position"], (Vector3)result["normal"]);
    }
}

private void SpawnImpactEffect(Vector3 position, Vector3 normal)
{
    // 占位：先打印，第 6 章讲完命中反馈后再换成真正的粒子/贴花
    GD.Print($"命中特效：{position}");
}
```

从现在开始的所有射线/物理查询，都用碰撞层来控制"谁能打谁"，不再用手动 `Exclude` 列表——这是本教程唯一一处需要你现在就去项目设置里点几下鼠标的地方，做好之后后面章节可以完全不用再操心这个问题。

### 4.3.1 霰弹枪：一次开火打出一圈散射（可选）

一个尖锐的问题：DOOM 3 里有没有"一枪打出多条弹道"的武器？**有**——散弹枪不是靠"打一发大威力子弹"实现的，而是货真价实地一次开火发射多颗弹丸，每颗弹丸独立判定命中、独立算伤害。这个逻辑不在 `Weapon.cpp` 里单独写了一份"散弹枪专用代码"，而是整个引擎的开火函数本来就是**多发**的：`idWeapon::Event_LaunchProjectiles(int num_projectiles, float spread, ...)`（`Weapon.cpp:3520`）从来都接受一个"这次开火打几发"的参数，手枪/步枪只是把它传成 `1`，散弹枪传成一个更大的数字（比如 8~13 发，具体数值在 `.def` 里配置），本质上是同一套函数在处理，不是两套独立的开火路径——这跟本教程第 5.3 节反复强调的"武器差异是数据配置的差异，不是代码分支的差异"是同一件事，只是这次体现在开火函数的参数上，而不是 `IsHitscan` 这样的开关上。

值得照抄的不是"多发"这个想法本身（这个显而易见），而是**这些弹丸具体怎么在锥形范围内撒开**。`Event_LaunchProjectiles` 里每一发弹丸的方向是这样算的（`Weapon.cpp:3616-3621`）：

```cpp
float spreadRad = DEG2RAD( spread );
for ( i = 0; i < num_projectiles; i++ ) {
    ang = idMath::Sin( spreadRad * gameLocal.random.RandomFloat() );
    spin = (float)DEG2RAD( 360.0f ) * gameLocal.random.RandomFloat();
    dir = muzzleAxis[0] + muzzleAxis[2] * ( ang * idMath::Sin( spin ) ) - muzzleAxis[1] * ( ang * idMath::Cos( spin ) );
    dir.Normalize();
}
```

这里的技巧在于：不是简单地把"水平散射"和"垂直散射"各自加一个独立的随机偏移量（那样撒出来的弹着点在准星周围会是一个方形/菱形分布，边角比中心密度还高，肉眼能看出不自然）。真实做法是先随机一个**半径**（`ang`，由随机数过一遍 `sin` 得到，偏向集中在锥心附近）、再随机一个**旋转角**（`spin`，覆盖整个 360°），用极坐标的方式在一个圆锥内撒点，这样撒出来的弹着点才是肉眼看起来自然的"圆形散布，中心密、边缘疏"。这个公式可以照抄进 Godot：

```csharp
// Weapon.cs 追加字段
[Export] public int PelletCount = 1;       // 大于 1 就是霰弹枪式散射；手枪/步枪保持 1，行为跟 4.3 节完全一样
[Export] public float SpreadDegrees = 0f;  // 单发的最大散射半角（锥形范围的顶角一半），单位是度

private void Fire()   // 这一版替换 4.3 节的 Fire()——到第 5.3 节接入投射物武器时，这个函数会被改名成 FireHitscan()
{
    var spaceState = GetWorld3D().DirectSpaceState;
    Vector3 forward = -Camera.GlobalTransform.Basis.Z;
    Vector3 right = Camera.GlobalTransform.Basis.X;
    Vector3 up = Camera.GlobalTransform.Basis.Y;

    int pellets = Mathf.Max(1, PelletCount);
    float spreadRad = Mathf.DegToRad(SpreadDegrees);

    for (int i = 0; i < pellets; i++)
    {
        Vector3 dir = forward;
        if (SpreadDegrees > 0f)
        {
            // 照抄 Event_LaunchProjectiles()（Weapon.cpp:3616-3621）的散射算法：
            // 极坐标撒点，不是给水平/垂直分别加独立随机偏移——后者会撒成方形，不自然
            float ang = Mathf.Sin(spreadRad * GD.Randf());        // 随机半径，偏向锥心附近
            float spin = Mathf.Tau * GD.Randf();                  // 随机旋转角，覆盖整个圆周
            dir = (forward + right * (ang * Mathf.Sin(spin)) - up * (ang * Mathf.Cos(spin))).Normalized();
        }

        Vector3 from = Camera.GlobalPosition;
        Vector3 to = from + dir * Range;

        var query = PhysicsRayQueryParameters3D.Create(from, to);
        query.CollisionMask = 0b0101;   // 同 4.3 节：World + Enemy

        var result = spaceState.IntersectRay(query);
        if (result.Count > 0)
        {
            Node3D hitObject = (Node3D)result["collider"];
            if (hitObject.HasMethod("TakeDamage"))
            {
                hitObject.Call("TakeDamage", Damage);   // 每颗弹丸独立算一次伤害，不是总伤害除以弹丸数再统一打一下
            }
            SpawnImpactEffect((Vector3)result["position"], (Vector3)result["normal"]);
        }
    }
}
```

注意 `Damage` 这个字段现在是"每一颗弹丸的伤害"，不是"这次开火的总伤害"——如果散弹枪一枪打 10 发、每发 4 点，总伤害上限是 40（近距离全部命中的情况），这跟真实 DOOM 3 的散弹枪调数值的方式一致：策划调的是单发伤害和弹丸数量两个独立数字，不是先定一个"总伤害"再除。

**这一节不打算做的事**：真实源码的 `num_projectiles`/`spread` 传参路径最终是从每把武器各自的 `.def` 文件读出来的（脚本里调用 `LaunchProjectiles(numProjectiles, spread, ...)` 时传的是 `.def` 里配置好的常量），意味着散射角度、弹丸数量这些都是纯数据、不需要碰代码——上面 `[Export]` 字段已经做到了同样的效果，不需要再额外抽一层配置文件。另外真实弹丸沿用的仍然是"生成一个 `idProjectile` 实体"的路径（只是 `net_instanthit` 标记为真时用于网络同步优化），跟本教程"霰弹枪也是若干条射线"的实现在概念上是一回事——**打点的方式**（生成实体 vs. 打射线）不同，但**散射的数学**是完全一样的，这也是这一节真正想让你照抄的部分。

### 4.4 命中部位缩放（可选）：爆头为什么伤害更高

DOOM 3 的伤害系统里，命中扫描/近战都会带上"打中了哪个部位"的信息，配合一张按部位缩放的表（比如打中头部伤害 ×3），这是完整实现的一部分，这里说明**为什么这一节标了"可选"、以及要做到什么程度才算数**：这个效果依赖被打中的角色**有细分的碰撞体积**（每个身体部位是独立的碰撞形状，或者角色骨骼上挂了带命名的物理骨骼），而本教程第 8 章的怪物目前只有一个整体的 `CapsuleShape3D`——射线打中它，`result["collider"]` 永远是同一个节点，物理查询层面根本拿不到"打中的是头还是脚"这个信息，不是漏写了判断逻辑，是碰撞体积的精细度还不支持。

> **"爆头"只是举的一个例子，不是唯一的一档**：读一下真实的 `idActor::SetupDamageGroups()`（`Actor.cpp:2463`）会发现，DOOM 3 的部位倍率表根本不是"头/身体"两档写死的枚举，而是从怪物 `.def` 里读两类前缀键动态建出来的：`damage_zone <组名> <骨骼匹配规则>` 把一批骨骼归到一个命名分组（可以是 `head`，也可以是 `larm`/`rarm`/`legs` 随便取名字），再用 `damage_scale <组名> <倍率>` 给每个分组单独定一个倍率，`idActor::GetDamageForLocation()`（`Actor.cpp:2512`）按命中的关节号查这张表——理论上一个怪物可以有任意多个命中区域，各自倍率互不相同（打头 ×3、打腿 ×0.5 之类都是同一套机制的不同配置），不是只有"头"这一个特殊分支被硬编码优待。下面第一条路径的 `Area3D` 方案已经是按这个思路设计的（可以摆不止一个区域），只是这里明确点破一下，避免以为"命中部位"这个概念天然只有爆头一档。

想要这个效果，有两条路：

1. **粗糙但省事**：给怪物加几个手动摆放的小 `Area3D`"命中区域"（不止头部一个——可以再摆一个套在四肢/腿部的区域），判定优先级更高，命中扫描先测试这些区域再测试主碰撞体，每个命中区域自带一个独立的 `DamageMultiplier` 导出字段，对应真实源码里一个 `damage_zone` 分组配一个 `damage_scale` 倍率。
2. **精细但需要更多前期工作**：走真正的骨骼命中检测——如果角色用的是带骨骼的模型且已经配置了第 10 章要讲的物理骨骼，Godot 的物理查询命中 `PhysicalBone3D` 时结果里能带出具体命中了哪根骨骼（`result["collider"]` 直接就是那根骨骼对应的物理体），可以按骨骼名字查一张倍率表（骨骼名 → 分组名 → 倍率，两层查表，对应上面 `damage_zone`/`damage_scale` 两个前缀键各自的作用）——这跟 DOOM 3 的原始实现（命中扫描击中演员身上具体某根骨骼，转换成关节句柄，查 `damage_zone`/`damage_scale` 表）是同一个思路，只是要先有骨骼化的角色模型才谈得上做这件事。

本教程从第 8 章开始一直用单个胶囊体做怪物碰撞体，是为了先把感知/寻路/状态机这些更核心的系统讲清楚，不代表"命中部位缩放不重要、可以永远不做"——如果你的美术资源已经到位（带骨骼的怪物模型），在第 10 章接入物理骨骼之后，回头把上面第二条路径接进 `Fire()`/`Melee()`/怪物近战判定，是完全可行、且直接复用已有射线检测代码的一次扩展，不需要重新设计伤害系统。

---

## 5. 武器系统进阶：弹药、换弹、切枪

### 5.1 弹药与弹匣

真枪不是无限连点的，加一个弹匣/储备弹药的概念：

```csharp
using Godot;

public partial class Weapon : Node3D
{
    [Export] public float Damage = 20.0f;
    [Export] public float Range = 100.0f;
    [Export] public Node3D Muzzle;
    [Export] public Camera3D Camera;

    [Export] public int ClipSize = 12;
    [Export] public float ReloadTime = 1.5f;
    [Export] public float FireRate = 0.15f;   // 两次开火之间的最短间隔，防止无限连点变成机关枪

    public int CurrentAmmo { get; private set; }
    public int ReserveAmmo = 60;

    private double _lastFireTime = -999;
    private bool _isReloading;

    public override void _Ready()
    {
        CurrentAmmo = ClipSize;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("fire"))
        {
            TryFire();
        }
        if (@event.IsActionPressed("reload"))
        {
            TryReload();
        }
    }

    private void TryFire()
    {
        if (_isReloading) return;
        double now = Time.GetTicksMsec() / 1000.0;
        if (now - _lastFireTime < FireRate) return;

        if (CurrentAmmo <= 0)
        {
            TryReload();   // 没子弹了，自动换弹——很多玩家习惯这个行为
            return;
        }

        _lastFireTime = now;
        CurrentAmmo--;
        Fire();
    }

    private async void TryReload()
    {
        if (_isReloading || CurrentAmmo >= ClipSize || ReserveAmmo <= 0) return;

        _isReloading = true;
        GD.Print("装弹中...");

        // await 一个计时器信号，是 C# 里"等一段时间再继续"最标准的写法——
        // 注意方法签名必须是 async void（或者 async Task），普通方法里不能直接 await
        await ToSignal(GetTree().CreateTimer(ReloadTime), SceneTreeTimer.SignalName.Timeout);

        int needed = ClipSize - CurrentAmmo;
        int taken = Mathf.Min(needed, ReserveAmmo);
        CurrentAmmo += taken;
        ReserveAmmo -= taken;
        _isReloading = false;
        GD.Print($"装弹完成：{CurrentAmmo}/{ReserveAmmo}");
    }

    // Fire()/SpawnImpactEffect() 内容不变，见第 4 章
}
```

记得去输入映射里加一个 `reload` 动作（建议绑 R 键）。

`async void TryReload()` 这个写法要特别注意——C# 的 `async`/`await` 在 Godot 里的作用，跟很多现代脚本语言的协程是同一个概念：**方法执行到 `await` 那一行会"暂停"，但不会阻塞游戏的其他部分继续跑**，计时器到点之后再从暂停的地方接着往下执行，就像整个函数只是被按下了暂停键一样。这比你自己维护一个"是否正在装弹"的计时器变量、在 `_Process` 里手动倒计时要直观得多。**这里稍微超前提一句**：这种"看起来像同步代码、实际上跨越了很多帧"的写法，正是第 13 章要讲的架构话题的一个引子——你已经在用它了，只是还没意识到这背后是个值得展开讲的东西。

> `ReserveAmmo` 现在是挂在这把枪自己身上的字段——先这样写是故意的，只有一把武器的时候看不出问题。但一个尖锐的问题是：如果玩家同时带着手枪和步枪，而两者都打 9mm 子弹，捡到的一盒子弹应该同时喂饱两把枪，还是各算各的？真实 DOOM 3 的答案是前者——`idInventory` 用一个按**弹药类型**（不是按武器）索引的储备数组，每把武器的 `.def` 只声明自己用哪种 `ammoType`（`Player.cpp:726` `AmmoIndexForWeaponClass()`，内部读 `decl->dict.GetString("ammoType")`），多把武器共享同一个类型对应的同一份储备。现在只有一把武器，这个区别还显不出来，`ReserveAmmo` 先留在 `Weapon` 上不算错——但等 5.2 节 `WeaponManager` 出现、场上有不止一把武器之后，这个简化会变成一个真实的手感问题（步枪打光了子弹，手枪的储备弹药却毫无变化，两把枪各自为政），5.2.2 节会回头把它收编成共享弹药池。

### 5.2 多把武器：切换——参照 `idPlayer::Weapon_Combat()` 的"意图/当前状态分离"模型

一个玩家通常不止一把枪，而 DOOM 3 的切枪不是"点一下立刻换"，而是有真实的收枪/举枪动画：必须先把当前武器完整收起（`PutAway` → `IsHolstered()`），再举起新武器（`Raise`）。这是 `idPlayer` 里管理武器切换那部分职责的核心逻辑，值得从一开始就做对，而不是先写一个"直接切换"的简化版本再回头改。

> 这里先老实说一下"参照"到底参照到什么程度：下面这套"`_idealSlot` 表达意图、每帧轮询推进"的分离思路，是照着 `Weapon_Combat()` 的真实结构写的；但 DOOM3 原版的举枪/收枪不是固定时长的计时器，而是由武器的**脚本状态机**驱动、举枪动作实际播完哪一帧算完全由动画和脚本事件决定，`RaiseTime`/`LowerTime` 这两个固定秒数只是拿计时器近似替代了这套逐帧驱动的脚本系统。另外原版还有三块这里没做的簿记：**`weaponGone`**（切枪过程中武器实体临时"不存在"的中间状态标记）、**`NextBestWeapon()`**（当前武器打空弹药时自动切到下一把能用的武器）、以及 **`previousWeapon` 的"按键切回上一把"** 记忆——这几个都属于"锦上添花但不影响核心切换逻辑对不对"的部分，本教程不实现，需要的话可以照着这个思路自己加。

先给每把武器一个状态机（`Weapon.cs` 追加）：

```csharp
// Weapon.cs 追加
public enum WeaponState { Holstered, Raising, Idle, Firing, Reloading, OutOfAmmo, Lowering }
public WeaponState State { get; private set; } = WeaponState.Holstered;

[Export] public float RaiseTime = 0.4f;
[Export] public float LowerTime = 0.3f;

public bool IsHolstered => State == WeaponState.Holstered;

public async void Raise()
{
    State = WeaponState.Raising;
    Visible = true;
    // 真实项目里这里应该播放举枪动画、等动画播完的信号，先用计时器模拟
    await ToSignal(GetTree().CreateTimer(RaiseTime), SceneTreeTimer.SignalName.Timeout);
    if (State == WeaponState.Raising)   // 防止这段等待期间武器又被要求切走
    {
        State = WeaponState.Idle;
    }
}

public async void PutAway()
{
    State = WeaponState.Lowering;
    await ToSignal(GetTree().CreateTimer(LowerTime), SceneTreeTimer.SignalName.Timeout);
    if (State == WeaponState.Lowering)
    {
        State = WeaponState.Holstered;
        Visible = false;
    }
}
```

`WeaponManager.cs` 现在不再是"直接切换"，而是每帧比较"我想要哪把武器"（`_idealSlot`）和"当前手上是哪把、状态如何"，照抄 DOOM3 原版的轮询逻辑：

```csharp
using Godot;
using System.Collections.Generic;

public partial class WeaponManager : Node3D
{
    private readonly Dictionary<int, Weapon> _weapons = new();
    private int _currentSlot = -1;
    private int _idealSlot = -1;   // 对应 idealWeapon：玩家"想要"切到的武器，不代表当前手上就是它

    public override void _Ready()
    {
        foreach (Node child in GetChildren())
        {
            if (child is Weapon weapon)
            {
                int slot = _weapons.Count + 1;
                _weapons[slot] = weapon;
                weapon.Visible = false;
            }
        }
        _idealSlot = 1;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("weapon_1")) SelectWeapon(1);
        if (@event.IsActionPressed("weapon_2")) SelectWeapon(2);
    }

    // 对应 idPlayer::SelectWeapon()——只是记录"意图"，不直接做任何切换动作
    private void SelectWeapon(int slot)
    {
        if (!_weapons.ContainsKey(slot)) return;
        _idealSlot = slot;
    }

    // 对应 idPlayer::Weapon_Combat() 每帧的轮询——这是真正驱动切换发生的地方
    public override void _PhysicsProcess(double delta)
    {
        if (_idealSlot == _currentSlot) return;

        if (_currentSlot == -1)
        {
            // 第一次装备武器，没有"当前武器"要收起，直接举起目标武器
            RaiseIdealWeapon();
            return;
        }

        Weapon current = _weapons[_currentSlot];
        if (current.State == Weapon.WeaponState.Holstered)
        {
            RaiseIdealWeapon();
            return;
        }

        // 只有武器处于"已经举起、可以正常使用"的状态时才允许开始收枪——对应源码 IsReady()
        // （status == WP_RELOAD || WP_READY || WP_OUTOFAMMO），特意不包含 WP_RISING：
        // 如果举枪动画还没播完就按了切枪键，真实游戏会等它自己播完、变成"就绪"状态之后
        // 才开始收枪，不会半路打断举枪动画——这是上一版遗漏的地方，之前 Raising 中途也会被打断
        bool isReadyToPutAway = current.State == Weapon.WeaponState.Idle
            || current.State == Weapon.WeaponState.Firing
            || current.State == Weapon.WeaponState.Reloading
            || current.State == Weapon.WeaponState.OutOfAmmo;

        if (isReadyToPutAway)
        {
            current.PutAway();
        }
        // 处于 Raising（还没播完，等它自己完成）或者已经在 Lowering（已经在收了）时，
        // 这里什么都不做，下一帧继续检查
    }

    private void RaiseIdealWeapon()
    {
        _currentSlot = _idealSlot;
        _weapons[_currentSlot].Raise();
    }

    public Weapon CurrentWeapon => _currentSlot != -1 ? _weapons[_currentSlot] : null;
}
```

同样记得加 `weapon_1`/`weapon_2` 输入映射（建议绑数字键 1/2）。这个版本和"直接切换"的区别，运行起来才能真正感受到：切枪时会先看到当前武器收下去、再看到新武器举起来，而不是瞬间换脸。**这个"意图状态和当前状态分离、靠每帧轮询推进过渡"的模式**，本教程后面第 9 章做怪物移动目标（`NavigationAgent3D.TargetPosition` 表达意图，实际移动每 tick 逐步逼近）用的是同一个思路——这不是巧合，是"任何一个不能瞬间完成、需要一段真实过渡时间的状态改变"的通用解法。

第 6 章的 `Melee()` 目前没有检查 `State` 是不是 `Idle`——回头在开头加一行 `if (State != WeaponState.Idle) return;`，防止武器还在收起/举起过程中就能开火（这一步教程后面到 6.3 节会替你补上）。

### 5.2.1 收编 5.1 节的换弹逻辑：`_isReloading` 应该并入 `State`，不该是独立字段

5.1 节的 `TryFire()`/`TryReload()` 是在状态机出现之前写的，用了一个孤立的 `_isReloading` 布尔字段自己管"是不是在装弹"，跟这一节刚建好的 `State` 完全是两套独立的东西——这是本教程故意先留的一个"债"：**先让读者用最直白的方式做出装弹能用的效果，等状态机这个更完整的模型出现之后，再回头把之前的临时方案收编进来**，而不是一开始就要求你理解一整套状态机才能实现装弹。现在状态机有了，是收编的时候了。

删掉 `_isReloading` 字段，`TryFire()`/`TryReload()` 改成全部走 `State`，顺带把 DOOM3 `WP_OUTOFAMMO` 对应的"没弹药"状态也正式接进来（而不是只在 `TryFire()` 里做一次性判断）：

```csharp
// Weapon.cs —— 用这一版替换 5.1 节的 TryFire()/TryReload()，删掉 _isReloading 字段
private void TryFire()
{
    if (State != WeaponState.Idle && State != WeaponState.OutOfAmmo) return;
    double now = Time.GetTicksMsec() / 1000.0;
    if (now - _lastFireTime < FireRate) return;

    if (CurrentAmmo <= 0)
    {
        State = WeaponState.OutOfAmmo;
        TryReload();
        return;
    }

    _lastFireTime = now;
    CurrentAmmo--;
    State = WeaponState.Firing;
    Fire();

    // Firing 是一个短暂状态，撑满一次开火间隔之后自动回到 Idle——复用 FireRate 这个已有数值当
    // "开火动作占用多久"，不需要为这一件事再单独引入一个新的计时字段
    GetTree().CreateTimer(FireRate).Timeout += () =>
    {
        if (State == WeaponState.Firing) State = WeaponState.Idle;
    };
}

private async void TryReload()
{
    if (State == WeaponState.Reloading || CurrentAmmo >= ClipSize || ReserveAmmo <= 0) return;

    State = WeaponState.Reloading;
    GD.Print("装弹中...");

    await ToSignal(GetTree().CreateTimer(ReloadTime), SceneTreeTimer.SignalName.Timeout);

    int needed = ClipSize - CurrentAmmo;
    int taken = Mathf.Min(needed, ReserveAmmo);
    CurrentAmmo += taken;
    ReserveAmmo -= taken;

    // 跟 Raise()/PutAway() 里同样的防御性判断：装弹这几百毫秒里，如果武器已经被切走
    // （State 变成了 Lowering），不能在这里把它硬掰回 Idle，覆盖掉切枪逻辑已经做的事
    if (State == WeaponState.Reloading)
    {
        State = WeaponState.Idle;
    }
    GD.Print($"装弹完成：{CurrentAmmo}/{ReserveAmmo}");
}
```

`TryFire()` 开头 `State != WeaponState.Idle && State != WeaponState.OutOfAmmo` 这个判断值得多看一眼：**没弹药也允许触发 `TryFire()`**（因为它自己会转去 `TryReload()`），但 `Raising`/`Lowering`/`Reloading` 这几个状态一律拒绝——你按开火键，武器正在收起过程中，这一枪就该被吃掉，不该排队等切枪动画播完再补开一枪，这跟 DOOM3 `WP_HOLSTERED`/`WP_RISING`/`WP_LOWERING` 状态下开火请求被直接忽略是同一个设计。

> 但这里对 `Reloading` 的处理跟真实源码的枚举语义并不完全一致，需要老实说明一下：DOOM3 的 `IsReady()`（`Weapon.cpp:1832`）判定为"可以开火"的状态是 `WP_RELOAD || WP_READY || WP_OUTOFAMMO`——**装弹中也允许开火**，经典例子是霰弹枪的"边压弹边打断重新开火"（打一发就把还没压完的这次装弹取消掉）。这一版教程的 `TryFire()` 把 `Reloading` 排除在外，是刻意选择的简化：真正的取消装弹需要能中途打断 `TryReload()` 里那个 `await` 掉的计时器、并且保证"还没加进弹匣的那部分子弹不能被提前加上"，这需要重新组织 `TryReload()` 的异步流程（比如换成可取消的 token 而不是直接 `await` 计时器），为了不把这一节的状态机搞复杂，这里先不实现装弹被开火打断的行为——如果你想要这个手感，思路是在 `TryFire()` 里对 `Reloading` 也放行，同时给 `TryReload()` 加一个取消标记，在计时器醒来时先检查有没有被取消，被取消就不再加弹药、直接把 `State` 交给开火逻辑接管。

这一步做完之后，`Weapon` 全部的"我现在在干嘛"都只有 `State` 这一个真相来源，不会再出现"状态机说是 Idle，但其实还在装弹"这种两套状态互相打架的情况。

### 5.2.2 收编 5.1 节的弹药：应该按类型共享，不是每把枪一份

5.1 节结尾留了个疑问：`ReserveAmmo` 挂在 `Weapon` 自己身上，只有一把武器时看不出问题，但现在 `WeaponManager` 已经把多把武器管起来了，是时候照真实源码的做法把它收编掉。`idInventory` 管储备弹药的方式（`Player.cpp:674` 起）不是"每把武器一个数字"，而是一个按**弹药类型**索引的共享数组：每把武器的 `.def` 只声明自己用的 `ammoType`（比如 `"bullets"`），`AmmoIndexForWeaponClass()`（`Player.cpp:726`）把这个字符串解析成数组下标，换弹、拾取弹药全部读写同一个下标——手枪和步枪只要都填 `"bullets"`，两者天然共享同一份储备，代码里完全不需要为"这两把枪打同一种子弹"这件事写任何特殊判断，纯粹是数据层面自动生效的结果。

储备弹药池应该放在哪？现在 `WeaponManager` 是所有武器共同的父节点，是天然的"库存"归属地——`Weapon` 自己不该再持有一份独立的储备数字：

```csharp
// WeaponManager.cs 追加：按弹药类型共享的储备弹药池，对应 idInventory 的共享数组 + ammoType
private readonly Dictionary<string, int> _reserveAmmo = new()
{
    { "bullets", 60 },
    { "shells", 16 },
    { "rockets", 6 },
};   // 弹药类型 -> 储备数量，跟具体哪把枪无关；只要 AmmoType 相同的武器，取的就是同一份储备

public int GetReserveAmmo(string ammoType) => _reserveAmmo.GetValueOrDefault(ammoType, 0);

public void AddReserveAmmo(string ammoType, int amount)
{
    _reserveAmmo[ammoType] = GetReserveAmmo(ammoType) + amount;   // 捡弹药补给用这个，本教程还没做拾取系统，先留好接口
}

// 换弹时从共享储备里"取"一部分，返回实际取到的数量——储备不够就只给这么多，不会取出负数
public int TakeReserveAmmo(string ammoType, int amount)
{
    int have = GetReserveAmmo(ammoType);
    int taken = Mathf.Min(have, amount);
    _reserveAmmo[ammoType] = have - taken;
    return taken;
}
```

`Weapon.cs` 这边删掉 `public int ReserveAmmo`，换成一个类型标签和一个指向父节点的引用：

```csharp
// Weapon.cs —— 删掉 5.1 节的 ReserveAmmo 字段
[Export] public string AmmoType = "bullets";   // 对应 .def 的 ammoType 键；手枪/步枪都填 "bullets" 就会共享同一份储备

private WeaponManager _manager;

public override void _Ready()
{
    CurrentAmmo = ClipSize;
    _manager = GetParent<WeaponManager>();   // 5.2 节的场景结构里，Weapon 就是 WeaponManager 的直接子节点
}

public bool HasAnyAmmo => CurrentAmmo > 0 || (_manager != null && _manager.GetReserveAmmo(AmmoType) > 0);   // 5.2.3 节切枪要用
```

`TryReload()` 改成向 `_manager` 要弹药，不再自己扣自己的字段：

```csharp
// Weapon.cs —— 用这一版替换 5.2.1 节的 TryReload()
private async void TryReload()
{
    if (State == WeaponState.Reloading || CurrentAmmo >= ClipSize) return;
    if (_manager.GetReserveAmmo(AmmoType) <= 0) return;

    State = WeaponState.Reloading;
    GD.Print("装弹中...");

    await ToSignal(GetTree().CreateTimer(ReloadTime), SceneTreeTimer.SignalName.Timeout);

    int needed = ClipSize - CurrentAmmo;
    int taken = _manager.TakeReserveAmmo(AmmoType, needed);
    CurrentAmmo += taken;

    if (State == WeaponState.Reloading)
    {
        State = WeaponState.Idle;
    }
    GD.Print($"装弹完成：{CurrentAmmo}/{_manager.GetReserveAmmo(AmmoType)}");
}
```

去 Inspector 里把每把武器的 `AmmoType` 填好：手枪、步枪都填 `"bullets"`，散弹枪填 `"shells"`，5.3 节马上要做的火箭筒填 `"rockets"`——这个字段跟 5.2.3 节的循环切枪、5.3 节的火箭筒弹药消耗都会直接用到，不需要再改一次。

### 5.2.3 滚轮循环切枪：wraparound，并跳过没弹药的武器

数字键 1/2 只能"点名"切到指定武器，很多玩家习惯用滚轮在武器之间循环——DOOM 3 里对应 `idPlayer::NextWeapon()`/`PrevWeapon()`（`Player.cpp:4481`/`4530`），核心是两个细节：**越界要绕回另一端**（从最后一把往后滚，绕回第一把，反过来也一样），以及**跳过打光了弹药的武器**（`inventory.HasAmmo(weap, true, this)` 检查不通过就跳过，继续找下一把，而不是切过去之后发现是把打不响的空枪）。这两点直接搬到 `WeaponManager` 上：

```csharp
// WeaponManager.cs 追加
public override void _UnhandledInput(InputEvent @event)
{
    if (@event.IsActionPressed("weapon_1")) SelectWeapon(1);
    if (@event.IsActionPressed("weapon_2")) SelectWeapon(2);
    if (@event.IsActionPressed("weapon_next")) CycleWeapon(1);
    if (@event.IsActionPressed("weapon_prev")) CycleWeapon(-1);
}

// 对应 NextWeapon()/PrevWeapon()：从当前"意图槽位"开始按方向一格格找下一把还有弹药的武器，
// 越过数组末尾/开头时绕回另一端（wraparound）；转了一整圈回到起点还没找到，就放弃、留在原地
private void CycleWeapon(int direction)
{
    if (_weapons.Count == 0) return;

    int w = _idealSlot;
    for (int i = 0; i < _weapons.Count; i++)
    {
        w += direction;
        if (w > _weapons.Count) w = 1;    // 越过末尾绕回槽位 1
        if (w < 1) w = _weapons.Count;    // 越过开头绕回最后一个槽位

        if (w == _idealSlot) break;       // 转了一圈回到起点，没找到别的能用的，放弃

        if (_weapons.TryGetValue(w, out Weapon candidate) && candidate.HasAnyAmmo)
        {
            _idealSlot = w;
            return;
        }
    }
    // 一圈下来没有任何一把武器还有子弹——真实 DOOM3 这时会退回拳头（近战武器，不耗弹药，永远能用）。
    // 本教程目前没有一把"打不光"的武器可以退，所以这里就地什么都不做，停留在当前武器上，
    // 不会切到另一把同样没子弹的枪——如果你已经做了近战武器（第 6 章 Melee()），
    // 可以把它当成永远满足 HasAnyAmmo 的兜底槽位，思路和源码的拳头完全一致
}
```

同样记得加 `weapon_next`/`weapon_prev` 输入映射（建议绑鼠标滚轮上下）。

### 5.3 投射物武器：火箭筒——直接命中和范围伤害是两件独立的事

到现在为止，`Weapon.Fire()` 只有一种打法：一条射线，打中即判定，没有飞行时间。这在手枪/步枪上没问题，但火箭筒、等离子炮这类武器不该是"瞬间命中"——它们需要一个真的在世界里飞行、会被躲开、命中后炸出范围伤害的**投射物实体**。

这一节要"完全参考"的是 DOOM 3 `neo/d3xp/Projectile.cpp` 里 `idProjectile::Collide()`（约 554-724 行）和 `Explode()` 的设计，核心是一个容易被忽略的点：**直接命中伤害和范围爆炸伤害，是两次独立的判定，不是二选一**。一发火箭打中一个怪物，会先对这个怪物单独算一次"直接命中"伤害，然后**无论有没有命中任何东西**都会引爆，再对爆炸半径内的所有实体算一遍范围伤害——已经吃过直接命中的那个目标要从范围伤害里排除掉，不然会被炸两次。线性衰减的伤害公式也是照抄的，跟 `Game_local.cpp:3897` 附近 `RadiusDamage` 的算法一致。

> 这里还有一个容易漏掉、但会直接影响手感的点：真实的 `RadiusDamage`（`Game_local.cpp:3890`）对每一个候选目标，除了距离衰减，还会额外做一次 `ent->CanDamage(origin, damagePoint)`——一次爆心到目标的**遮挡检测**，隔着墙的目标不给范围伤害，爆炸不能穿墙杀人。下面的 `Explode()` 第一版只用一次球形 `IntersectShape` 查询圈出半径内的所有目标，完全没有做遮挡判断，意味着躲在墙后面的怪物一样会被隔墙炸到——这不对，补上一条遮挡射线：

> **一个尖锐的问题**：火箭筒贴脸炸自己怎么办，DOOM 3 是不是设了个"离目标太近就不引爆"的最小引信距离？读完 `Collide()`/`Explode()`/`RadiusDamage()` 全文，答案是**没有**——DOOM3 完全没有"最小引爆距离"或者"触发引信 vs. 定时引信"这种区分（`fuse` 字段只用于会弹跳的手雷/等离子球那类武器的自毁计时，跟"离自己多近才炸"无关），火箭筒贴着墙打自己脚下一样会正常爆炸。真正解决"自己炸自己"手感问题的机制是 `RadiusDamage()`（`Game_local.cpp:3812`）读的一个 `.def` 参数 `attackerDamageScale`（默认 `0.5`）：射手自己吃自己这发爆炸的范围伤害时打五折，而不是把射手从范围伤害判定里整个排除掉——`ent == attacker` 时伤害倍率额外乘一次这个系数（`Game_local.cpp:3898`），伤害循环本身仍然会遍历到射手自己。至于击退力，源码走的是另一条路径：`RadiusPush()`（`Game_local.cpp:3948`）明确跳过所有 `idPlayer` 类型的实体（注释原文"players use knockback in idPlayer::Damage"），玩家自己的击退效果是 `idPlayer::Damage()` 内部单独算的，不经过这条给非玩家刚体用的推力路径——这部分已经超出这一节想让你照抄的范围，下面的教程版本只搬"自伤打折、不是整个排除"这一条最直接影响手感的规则。

先做投射物本体，新建场景 `Rocket.tscn`：

```
Rocket (Area3D)
├── CollisionShape3D (SphereShape3D，半径很小，比如 0.1，够用来触发碰撞检测就行)
└── MeshInstance3D (占位：一个小圆柱体或者胶囊)
```

用 `Area3D` 而不是 `RigidBody3D`——火箭的飞行轨迹通常是"匀速直线飞向发射方向"，不需要真的参与物理仿真（不会被别的物体撞飞、不受碰撞反弹力影响），`Area3D` 的 `body_entered`/`area_entered` 信号足够检测"飞行路径上撞到了什么"，比接一整套刚体物理简单得多，也更容易保证命中判定稳定（`RigidBody3D` 高速穿过薄物体时容易发生"隧穿"漏检，`Area3D` 配合下面这种"每帧自己挪动 + 检测"的写法能避开这个问题）。

```csharp
// Rocket.cs
using Godot;
using System.Collections.Generic;

public partial class Rocket : Area3D
{
    [Export] public float Speed = 25.0f;
    [Export] public float DirectDamage = 60.0f;
    [Export] public float SplashDamage = 40.0f;
    [Export] public float SplashRadius = 4.0f;
    [Export] public float DirectPushForce = 8.0f;
    [Export] public float SplashPushForce = 12.0f;
    [Export] public float LifeTime = 5.0f;   // 飞出去太远还没撞到东西，超时自毁，防止永远飞下去
    [Export] public float AttackerDamageScale = 0.5f;   // 对应源码 attackerDamageScale：自己被自己这发爆炸炸到时打的折扣，不是免疫

    public Node3D Owner3D;   // 发射者
    private Vector3 _direction;
    private double _spawnTime;

    public void Launch(Vector3 direction, Node3D owner)
    {
        _direction = direction.Normalized();
        Owner3D = owner;
        _spawnTime = Time.GetTicksMsec() / 1000.0;
        LookAt(GlobalPosition + _direction, Vector3.Up);
    }

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
    }

    public override void _PhysicsProcess(double delta)
    {
        GlobalPosition += _direction * Speed * (float)delta;

        if (Time.GetTicksMsec() / 1000.0 - _spawnTime > LifeTime)
        {
            Explode(GlobalPosition, null);   // 超时自毁，没有直接命中目标，null 表示范围伤害不用排除任何人
        }
    }

    private void OnBodyEntered(Node3D body)
    {
        // 对应源码 Collide() 里 ent == owner.GetEntity() 的那个分支：火箭在离开枪口的瞬间就贴着发射者的
        // 碰撞体，直接命中判定要跳过发射者自己，不然枪一响自己就先挨一发直接命中——但注意这里只跳过
        // "直接命中"这一步，范围伤害（下面 Explode() 的爆炸半径）不会因此把发射者整个排除掉，
        // 见 5.3 节前面那条关于 attackerDamageScale 的说明
        if (body == Owner3D) return;

        // 步骤一：直接命中伤害，只作用于撞上的这一个目标
        if (body.HasMethod("TakeDamage"))
        {
            body.Call("TakeDamage", DirectDamage);
        }
        if (body is RigidBody3D rigidBody)
        {
            rigidBody.ApplyImpulse(_direction * DirectPushForce, GlobalPosition - rigidBody.GlobalPosition);
        }

        // 步骤二：不管上面有没有命中，都要引爆——直接命中和范围伤害是两回事
        Explode(GlobalPosition, body);
    }

    private void Explode(Vector3 explodePosition, Node3D directHitTarget)
    {
        GD.Print($"爆炸于 {explodePosition}");   // 占位：这里之后接第 6 章讲过的 SpawnImpactEffect 思路，换成真正的爆炸特效

        var spaceState = GetWorld3D().DirectSpaceState;
        var query = new PhysicsShapeQueryParameters3D
        {
            Shape = new SphereShape3D { Radius = SplashRadius },
            Transform = new Transform3D(Basis.Identity, explodePosition),
            CollisionMask = 0b0110   // 检测 Player(第2层) + Enemy(第3层)——注意故意包含 Player 层：
                                      // 真实 RadiusDamage() 不会把攻击者从候选目标里剔除，只是伤害打折（见下面 AttackerDamageScale），
                                      // 如果这里漏掉 Player 层，效果就变成了"完全免疫自己的爆炸"，跟源码行为不一致
        };

        var results = spaceState.IntersectShape(query);
        var alreadyDamaged = new HashSet<Node3D>();
        if (directHitTarget != null) alreadyDamaged.Add(directHitTarget);   // 已经直接命中过的目标，范围伤害要排除，不能炸两次

        foreach (var result in results)
        {
            Node3D hitObject = (Node3D)result["collider"];
            if (alreadyDamaged.Contains(hitObject)) continue;
            alreadyDamaged.Add(hitObject);

            // 遮挡检测：对应源码 CanDamage() 那次视线判定——爆心到目标之间被世界几何挡住的话，
            // 这个目标就不该吃到范围伤害，不然爆炸会隔着墙杀人。只测世界几何（第1层），
            // 不测生物层，避免目标自己的碰撞体把这条射线挡住
            var occlusionQuery = PhysicsRayQueryParameters3D.Create(explodePosition, hitObject.GlobalPosition);
            occlusionQuery.CollisionMask = 0b0001;
            occlusionQuery.Exclude = new Godot.Collections.Array<Rid> { GetRid() };
            if (spaceState.IntersectRay(occlusionQuery).Count > 0) continue;   // 被墙挡住，跳过这个目标

            // 简单的距离衰减：越靠近爆心伤害越高，边缘接近 0——DOOM 3 的 RadiusDamage 也是同一个思路
            float distance = hitObject.GlobalPosition.DistanceTo(explodePosition);
            float falloff = 1.0f - Mathf.Clamp(distance / SplashRadius, 0.0f, 1.0f);

            // 对应源码 RadiusDamage() 里 ent == attacker 时额外乘一次 attackerDamageScale（Game_local.cpp:3898）：
            // 打中别人是全额（乘 falloff）的范围伤害，但爆心波及发射者自己时要再打一次折扣，不是完全免疫——
            // 贴脸开火自己也会掉血，只是掉得比炸中别人少
            float selfScale = hitObject == Owner3D ? AttackerDamageScale : 1.0f;

            if (hitObject.HasMethod("TakeDamage"))
            {
                hitObject.Call("TakeDamage", SplashDamage * falloff * selfScale);
            }
            if (hitObject is RigidBody3D rigidBody)
            {
                Vector3 pushDir = (rigidBody.GlobalPosition - explodePosition).Normalized();
                rigidBody.ApplyImpulse(pushDir * SplashPushForce * falloff, rigidBody.GlobalPosition - explodePosition);
            }
        }

        QueueFree();
    }
}
```

`Weapon.cs` 这边不需要为投射物武器另开一套开火函数——沿用 DOOM 3 的思路（第 1 节提过的"一个通用发射逻辑 + 数据决定是子弹还是投射物"），给 `Weapon` 加一个开关：

> 这里有一个容易踩的坑，跟 4.2 节那个"层层 `GetParent()`"的问题是同一类：拿发射者节点还是得用到从 `Weapon` 往上数到 `Player` 的层数，而 5.2 节引入 `WeaponManager` 之后，场景树比 4.1 节多了一层（`Weapon` 现在挂在 `WeaponManager` 下面，不再直接挂在 `WeaponHolder` 上），层数已经从三层变成了四层。如果照抄 4.2 节"数三层"的写法，会正好错开一层，"炸自己"这种 bug 会在测试的时候才暴露，而且不一定每次都能立刻看出是场景树层数算错了。与其每处需要拿发射者的地方各自现算一遍 `GetParent()` 链条（数错一层就出问题，场景树只要再改一次结构就全部要重新数），不如让 `WeaponManager` 直接曝光一个字段，一次性配置好，别处不再自己数层数：
>
> ```csharp
> // WeaponManager.cs 追加
> [Export] public CharacterBody3D PlayerBody;   // 编辑器里手动拖入 Player 节点，不再靠 GetParent() 链条现算
> ```
>
> 这不是"照抄源码"能解决的问题——DOOM 3 的 `idWeapon` 本来就直接持有一个 `owner` 指针（`idEntityPtr<idPlayer> owner`，在 `idWeapon::SetOwner()` 里赋值一次），从来不会去做"网络地址往上数几层就是玩家"这种事，这里出的坑纯粹是本教程用 `GetParent()` 链条模拟"谁是我的主人"这件事本身就比较脆弱——**这一节顺手把它改成跟源码同样的做法：赋值一次，然后一直用这个引用，不再依赖场景树的具体深度**。

```csharp
// Weapon.cs 追加
[Export] public bool IsHitscan = true;
[Export] public PackedScene ProjectileScene;   // 拖入 Rocket.tscn，IsHitscan = false 时使用
[Export] public float ProjectileSpeed = 25.0f;

private void Fire()
{
    if (IsHitscan)
    {
        FireHitscan();   // 原来 4.2/4.3/4.3.1 节的射线判定逻辑（含霰弹枪散射），改个名字
    }
    else
    {
        FireProjectile();
    }
}

private void FireProjectile()
{
    var rocket = ProjectileScene.Instantiate<Rocket>();
    GetTree().Root.AddChild(rocket);
    rocket.GlobalPosition = Muzzle.GlobalPosition;
    Vector3 direction = -Camera.GlobalTransform.Basis.Z;
    rocket.Launch(direction, _manager.PlayerBody);   // 传入发射者，用于排除自伤——不再手动数 GetParent() 层数
}
```

在编辑器里把火箭筒的 `IsHitscan` 打勾去掉、`ProjectileScene` 拖入 `Rocket.tscn`，手枪/步枪保持 `IsHitscan = true` 不用改任何代码——**这就是"武器差异是数据配置的差异，不是代码分支的差异"这条原则的具体体现**，跟第 4 章开始就在强调的"鸭子类型式调用"是同一种设计取向：让尽量多的差异落在 Inspector 面板能调的字段上，而不是散落在一堆 `if (weaponType == ...)` 分支里。

### 5.4（可选进阶）追踪导弹：会自己转向的投射物

如果想要一把"追踪导弹"武器，在 `Rocket.cs` 基础上加一个转向逻辑就够了，不需要另起一个类——这是 DOOM 3 `idGuidedProjectile::Think()`（`Projectile.cpp:1651-1721`）的简化版：每帧朝目标方向转一点，转向速率有上限（不能瞬间掉头，不然追踪导弹的飞行轨迹会显得很假）。这里明确标"简化版"，具体简化掉了哪些东西也要说清楚，不能让"简化版"三个字自己含糊过去：

- **没有随机抖动**：真实源码每 200ms 刷新一个随机偏移角 `rndAng` 叠加到瞄准方向上，让导弹在远处飞得"不那么精准"，越接近目标这个抖动幅度越小——远处明显能看出导弹在扭，快到跟前时基本就稳定瞄着目标了。下面的版本永远精确瞄准，没有这个由远到近逐渐收紧的抖动。
- **没有"抵近脱锁"（`burstMode`）**：真实源码导弹快接近目标时会切换成不再转向的直线加速冲刺，纯粹靠这一下加速来保证命中/砸出冲击力，不是全程都在追踪修正。这里没有实现这一段。
- **瞄准点做了近似**：真实源码瞄的是目标的**眼睛位置再往下偏移 12 个单位**，不是目标的原始世界坐标（通常对应脚底或碰撞体中心）。这个点比较容易实现，下面顺手加上了。

```csharp
// Rocket.cs 追加
[Export] public bool IsGuided = false;
[Export] public float TurnRateDegreesPerSec = 90.0f;
[Export] public float TargetAimOffset = 1.3f;   // 近似"目标眼睛位置往下一点"——真实源码是眼位置再往下偏移 12 个单位，
                                                  // 这里没有目标身上真正的"眼睛"骨骼可用，用一个固定高度偏移粗略模拟同样效果
public Node3D Target;   // 发射时指定要追踪的目标（比如玩家开火时，锁定准星下最近的怪物）

public override void _PhysicsProcess(double delta)
{
    float dt = (float)delta;

    if (IsGuided && Target != null && IsInstanceValid(Target))
    {
        Vector3 targetPoint = Target.GlobalPosition + Vector3.Up * TargetAimOffset;
        Vector3 toTarget = (targetPoint - GlobalPosition).Normalized();
        float maxRadians = Mathf.DegToRad(TurnRateDegreesPerSec) * dt;
        _direction = _direction.Slerp(toTarget, Mathf.Min(1.0f, maxRadians / _direction.AngleTo(toTarget)));
        LookAt(GlobalPosition + _direction, Vector3.Up);
    }

    GlobalPosition += _direction * Speed * dt;

    if (Time.GetTicksMsec() / 1000.0 - _spawnTime > LifeTime)
    {
        Explode(GlobalPosition, null);
    }
}
```

`Vector3.Slerp` 配合按最大转向角度限制插值比例这一段写法有点绕，逐字拆开看：`_direction.AngleTo(toTarget)` 是当前方向和目标方向之间的夹角（弧度），`maxRadians / 夹角` 算出"这一帧允许转过的角度占总夹角的比例"，`Mathf.Min(1.0f, ...)` 防止这个比例超过 1（夹角本身很小、一帧就能转完的情况）——效果就是"匀速转向，转到位为止，不会转过头"。这个模式不止能用在导弹上，第 9 章讲怪物转身面向玩家时如果想要更平滑的转向（而不是瞬间 `LookAt`），也是同一个写法。

两个容易被现代 FPS 玩家问到、但读完真实源码之后能明确回答的问题：

- **导弹会不会因为被挡住太久就丢失锁定？**不会，`idGuidedProjectile::Think()` 全文没有任何视线检测——它每一帧都直接读 `enemy` 指针算方向、转向，完全不关心这中间隔没隔着墙。玩家绕柱子躲导弹，导弹会贴着柱子拐弯追过来（受 `turn_max` 限制转不过特别急的角，可能因此撞墙自爆，但这是"转向跟不上"撞上去的，不是"判定丢失目标"主动放弃）。所以这里不实现"挡住太久就脱锁"，不是漏做，是真实源码本来就没有这个机制——如果你想要这个手感，是在真实设计之上做的**原创扩展**，不是照抄。
- **最大转向速率要怎么权衡"追得上玩家"和"给玩家留躲避空间"？**源码里 `turn_max` 是每种导弹在 `.def` 里各自写死的固定数值，不存在什么根据玩家移动速度反过来动态调节转向速率的平衡算法——策划怎么调这个数字、值多大算"公平"，源码里找不到答案，是纯粹的关卡/难度调试问题。**这里有一个开放问题**：`TurnRateDegreesPerSec` 具体该定多少，取决于你的关卡尺度、玩家移动速度、遮蔽物密度这些本教程管不到的因素，没有一个能照抄的"正确答案"，需要你自己试出来。

---

## 6. 武器手感：后坐力、摇摆、近战

这一章不加新玩法，只打磨"已经能开火的枪，摸起来像不像样"——这是最容易被新手开发者跳过、但最影响游戏第一印象的一类细节。

### 6.1 开火后坐：完整照抄 `MuzzleRise()` 的时间窗口模型

> 这一节第一版也有问题：写成了"每次开火 `_kickEndTime = 当前时间 + RecoilTime`"，这其实是**每次都重置到同一个固定窗口**，不是真正的叠加——连续快速开火只会反复顶到同一个最大角度，不会像原版那样越打越往上顶。去读了真实的 `MuzzleRise()`（`Weapon.cpp:2091-2116`）和调用它前那段"延长 `kick_endtime`"的代码（`Weapon.cpp:3595-3602`）才发现，真正的叠加逻辑需要两个独立的时间常数：**每次开火把 `kick_endtime` 往后"顶"一段固定增量**（不是重置成"现在+固定值"），且这个"往后顶"的动作有一个总的上限，不能无限累加。改正版本：

```csharp
// Weapon.cs 追加字段
[Export] public float RecoilKickDegrees = 3.0f;
[Export] public float MuzzleKickTime = 0.08f;      // 每次开火，kick_endtime 往后顶多少——这是"叠加速度"
[Export] public float MuzzleKickMaxTime = 0.35f;   // kick_endtime 最多能顶到多远的将来——叠加的总上限

private double _kickEndTime;

// Fire() 里，扣弹药之后加一行
private void Fire()
{
    // ...原有的射线检测逻辑...
    double now = Time.GetTicksMsec() / 1000.0;
    // 关键修正：往后"顶"一段增量，而不是重置成固定窗口——这样连续快速开火，
    // kick_endtime 会被一次次往后推，最终撞到 MuzzleKickMaxTime 这个上限才封顶，
    // 效果就是越打越往上顶、直到顶满为止，不是每次都顶到同一个高度
    if (_kickEndTime < now) _kickEndTime = now;
    _kickEndTime += MuzzleKickTime;
    if (_kickEndTime > now + MuzzleKickMaxTime) _kickEndTime = now + MuzzleKickMaxTime;
}

// 每帧读取"kick_endtime 距离现在还有多久"，换算成当前应该顶到的角度
private float CurrentRecoilAngle()
{
    double now = Time.GetTicksMsec() / 1000.0;
    double remaining = _kickEndTime - now;
    if (remaining <= 0) return 0;
    if (remaining > MuzzleKickMaxTime) remaining = MuzzleKickMaxTime;
    float amount = (float)(remaining / MuzzleKickMaxTime);
    return RecoilKickDegrees * amount;
}
```

**注意这里操作的是武器模型自己的局部旋转，不是摄像机**——后坐力应该只让枪看起来在动，不应该真的影响玩家的实际瞄准方向。`CurrentRecoilAngle()` 会在 6.2 节被叠加进武器最终的角度里，不需要单独一个 `_Process`。试着用一把 `FireRate` 很短的武器连续开火，`_kickEndTime` 会一次次被往后推，直到撞上 `MuzzleKickMaxTime` 封顶——这才是"越打越往上顶、顶到一定程度就不再涨"的真实效果，跟"每次都重置到同一个角度"是两种不同的手感。

（DOOM3 原版 `MuzzleRise()` 除了角度还会叠加一个 `muzzle_kick_offset` 位置偏移，本节只做了角度这一半，位置偏移部分本教程略过——如果想要更完整，可以照同样的"剩余时间比例"逻辑再加一个 `Vector3` 位置偏移，思路和角度完全一样。）

### 6.2 武器摇摆：完整照抄 `CalculateViewWeaponPos()`

> 这一节第一版遗漏了整整一层效果——去读 `idPlayer::CalculateViewWeaponPos()`（`Player.cpp:8793-8855`）才发现，武器最终的角度其实是**四个**来源叠加，不是三个：除了"加速度拖尾位置偏移""鼠标转向角度滞后""恒定呼吸感"这三层，还有一层**跟视角 Bob 同源的周期性摇摆角度**——这层我之前完全没写。这四层各自独立、简单相加，缺哪一层"完全参考"都不成立。逐层补完：

1. **加速度触发的位置拖尾**——玩家开始移动/停止移动/跳跃的瞬间，武器会先反向摆动一下再跟上。
2. **鼠标转向触发的角度滞后**——转动视角时，武器会滞后一点点再跟上转向。
3. **跟脚步 Bob 同源的周期性摇摆角度**（上一版遗漏的部分）——用的是 3.2 节 `BobCycle()` 算出的同一个 `bobFracSin`/`xySpeed`，但换了一套独立的常数、且同样按"脚的奇偶"翻转符号。
4. **恒定的待机呼吸感**——哪怕完全不动也有一个缓慢的正弦飘动。

外加一个同样被漏掉的细节：**武器落地时也会有一次冲击下沉**（3.3 节那个 `_landingDipOffset` 的 0.25 倍强度）。这里要纠正一个直觉上很容易搞反的说法：**武器的落地冲击不是比摄像机自己的更轻，实际上比摄像机晃得更狠**。源码里 `CalculateViewWeaponPos()`（`Player.cpp:8834-8839`）算武器位置时，用的基准 `firstPersonViewOrigin`（`Player.cpp:8993`）本来就已经带上了摄像机**完整强度**的落地冲击（`viewBob` 里已经烤进了完整的 `landChange`），然后这个函数又在这个基准之上**再叠加一份 `landChange*0.25f`**——也就是说武器的落地冲击 = 摄像机已经继承到的完整落地冲击 + 额外 0.25 倍的独立冲击，两者是加在一起的，不是"摄像机的一部分"。

下面 `UpdateSway()` 里的 `Position = _basePosition + accelOffset + landingDipOffset * 0.25f;` 恰好就是这个"额外 0.25 倍"的部分——之所以不需要在这里再手动加一遍"完整强度的落地冲击"，是因为按 4.1 节搭的场景树，`WeaponHolder`（挂着这个 `Weapon.cs` 的节点）本身就是 `Camera3D` 的子节点，而 `Camera3D.Position` 已经在 `ApplyBobToCamera()` 里被设成了包含完整 `_landingDipOffset` 的 `camPos`——武器作为子节点，会通过节点树的父子变换**自动继承**摄像机这份完整强度的落地冲击，不需要代码里重复处理，这里的 `landingDipOffset * 0.25f` 才是源码里真正"额外叠加"的那一份。**这也是为什么这份场景层级不能随便改**：如果你以后把武器/`WeaponHolder` 从摄像机节点下面挪出去（比如为了做武器不跟着摄像机晃的某种效果），会同时悄悄丢掉两样东西——继承来的周期性 view bob，以及继承来的完整强度落地冲击——而这里的 `* 0.25f` 只是叠加的那一小部分，补不回丢掉的大头。完整实现：

```csharp
// Weapon.cs 追加
[Export] public float AccelOffsetScale = 0.02f;
[Export] public float AccelOffsetTime = 0.4f;      // 对应 weaponOffsetTime
[Export] public float TurnSwayScale = 0.15f;
[Export] public float TurnSwayMaxDegrees = 6.0f;
[Export] public int TurnSwayAverageFrames = 10;     // 对应 weaponAngleOffsetAverages
[Export] public float BobRollScale = 0.005f;        // 对应 CalculateViewWeaponPos 里硬编码的 0.005f
[Export] public float BobYawScale = 0.01f;          // 对应硬编码的 0.01f
[Export] public float BobPitchScale = 0.005f;       // 对应硬编码的 0.005f

private Vector3 _basePosition;
private Vector3 _baseRotationDegrees;

private class AccelEvent { public double Time; public Vector3 Dir; }
private readonly List<AccelEvent> _accelEvents = new();
private Vector2 _lastMoveInput;

// 视角历史用数组+游标模拟环形缓冲，而不是 Queue——这样可以像原版一样按"帧号"取值，
// 而不是每帧都做一次昂贵的出队/入队
private readonly Vector2[] _viewAngleHistory = new Vector2[64];
private int _viewAngleWriteIndex;
private int _viewAngleFrameCount;

public override void _Ready()
{
    CurrentAmmo = ClipSize;
    _basePosition = Position;
    _baseRotationDegrees = RotationDegrees;
}

// 由 PlayerController 每个物理帧调用，传入这一帧的移动输入、当前视角角度、
// 以及 3.2 节 BobCycle() 已经算好的 bobFracSin/xySpeed/secondHalf（同一份数据两处复用，不重新算一遍）
public void UpdateSway(Vector2 moveInput, Vector2 viewAngleDegrees, bool justJumped,
    float bobFracSin, float xySpeed, bool secondHalf, Vector3 landingDipOffset)
{
    LogAccelEvents(moveInput, justJumped);
    LogViewAngle(viewAngleDegrees);

    Vector3 accelOffset = ComputeAccelOffset();
    Vector3 turnOffset = ComputeTurnOffset(viewAngleDegrees);
    Vector3 bobAngle = ComputeBobAngle(bobFracSin, xySpeed, secondHalf);
    Vector3 idleBreath = ComputeIdleBreath(xySpeed);

    Position = _basePosition + accelOffset + landingDipOffset * 0.25f;   // 武器自己的落地冲击，强度是摄像机那份的 0.25 倍
    RotationDegrees = _baseRotationDegrees + bobAngle + turnOffset + idleBreath
        + new Vector3(-CurrentRecoilAngle(), 0, 0);   // 6.1 节的后坐角度也叠加在这里，五层角度/位置来源相加
}

// 对应 loggedAccel[] 环形缓冲——只在输入发生变化(即加速度)时记一笔，不是每帧都记。
// 这里做了一处简化：原版对"前后"和"左右"变化分别独立记两条事件，这里合并成一条 2D 事件，
// 效果上基本等价（都是"记录一次输入变化、按同一个衰减窗口淡出"），只是少了一条记录
private void LogAccelEvents(Vector2 moveInput, bool justJumped)
{
    double now = Time.GetTicksMsec() / 1000.0;
    if (justJumped)
    {
        _accelEvents.Add(new AccelEvent { Time = now, Dir = new Vector3(0, 1, 0) * 0.4f });
    }
    if (moveInput != _lastMoveInput)
    {
        Vector2 deltaInput = moveInput - _lastMoveInput;
        _accelEvents.Add(new AccelEvent { Time = now, Dir = new Vector3(deltaInput.X, 0, deltaInput.Y) });
        _lastMoveInput = moveInput;
    }
    _accelEvents.RemoveAll(e => now - e.Time > AccelOffsetTime);
}

// 对应 GunAcceleratingOffset()：每个未过期的事件用余弦衰减窗口(0 -> -1 -> 0)叠加一次位置偏移
private Vector3 ComputeAccelOffset()
{
    double now = Time.GetTicksMsec() / 1000.0;
    Vector3 offset = Vector3.Zero;
    foreach (var e in _accelEvents)
    {
        float t = (float)((now - e.Time) / AccelOffsetTime);
        float f = (Mathf.Cos(t * Mathf.Tau) - 1.0f) * 0.5f;
        offset += f * AccelOffsetScale * e.Dir;
    }
    return offset;
}

private void LogViewAngle(Vector2 viewAngleDegrees)
{
    _viewAngleHistory[_viewAngleWriteIndex % _viewAngleHistory.Length] = viewAngleDegrees;
    _viewAngleWriteIndex++;
    _viewAngleFrameCount = Mathf.Min(_viewAngleFrameCount + 1, _viewAngleHistory.Length);
}

// 对应 GunTurningOffset()：取最近 N 帧视角平均值与当前视角的差，钳制幅度，做出"滞后"效果。
// 这次补上了上一版遗漏的偏航角(Y)跨 0/360 度边界的处理——不做这个处理，
// 玩家转到背后视角跨过 0 度线的瞬间，摇摆会突然跳一下
private Vector3 ComputeTurnOffset(Vector2 currentViewAngle)
{
    if (_viewAngleFrameCount == 0) return Vector3.Zero;

    int n = Mathf.Min(TurnSwayAverageFrames, _viewAngleFrameCount);
    Vector2 avg = currentViewAngle;
    for (int j = 1; j < n; j++)
    {
        int idx = (_viewAngleWriteIndex - 1 - j + _viewAngleHistory.Length * 4) % _viewAngleHistory.Length;
        Vector2 sample = _viewAngleHistory[idx];
        float yawDelta = sample.Y - currentViewAngle.Y;
        if (yawDelta > 180f) yawDelta -= 360f;
        else if (yawDelta < -180f) yawDelta += 360f;
        avg += new Vector2(sample.X - currentViewAngle.X, yawDelta) / n;
    }

    Vector2 diff = (avg - currentViewAngle) * TurnSwayScale;
    diff.X = Mathf.Clamp(diff.X, -TurnSwayMaxDegrees, TurnSwayMaxDegrees);
    diff.Y = Mathf.Clamp(diff.Y, -TurnSwayMaxDegrees, TurnSwayMaxDegrees);
    return new Vector3(diff.X, diff.Y, 0);
}

// 对应 CalculateViewWeaponPos() 里那三行硬编码的 bob 角度——跟 3.2 节的视角 Bob
// 用的是同一份 bobFracSin/secondHalf，但换了一套独立常数，且多了一个 yaw 分量
// （视角 bob 只有 pitch/roll，武器 bob 还会左右轻微转向）
private Vector3 ComputeBobAngle(float bobFracSin, float xySpeed, bool secondHalf)
{
    float scale = secondHalf ? -xySpeed : xySpeed;
    float roll = scale * bobFracSin * BobRollScale;
    float yaw = scale * bobFracSin * BobYawScale;
    float pitch = xySpeed * bobFracSin * BobPitchScale;   // pitch 不受 secondHalf 影响，跟 3.2 节视角 bob 的处理一致
    return new Vector3(pitch, yaw, roll);
}

// 对应"哪怕站着不动也在动"的恒定呼吸感——只作用于武器，不作用于玩家摄像机本身
private Vector3 ComputeIdleBreath(float xySpeed)
{
    float scale = xySpeed + 1.0f;
    float t = scale * Mathf.Sin(Time.GetTicksMsec() / 1000.0f) * 0.5f;
    return new Vector3(t, t, t);
}
```

记得在文件顶部加 `using System.Collections.Generic;`。`PlayerController` 需要把 3.2 节 `UpdateViewBob()` 里已经算好的 `bobFracSin`/`xySpeed`/`secondHalf`（以及 3.3 节的 `_landingDipOffset`）一起传给 `weapon.UpdateSway(...)`——不要在武器脚本里重新算一遍视角 Bob 的相位，两处必须共用同一份计算结果，否则武器摇摆和视角摇摆会不同步、看起来是两套独立的抖动而不是"同一个身体在动"。

现在是四层位置/角度来源（加速度拖尾、转向滞后、脚步 bob 角度、待机呼吸）加上落地冲击、加上后坐力，一共六个独立的偏移源简单相加——这跟 DOOM3 原版的分层方式是一致的：把其中任意一层的强度单独调到 0，能确认这一层各自的贡献，这对调手感非常有用，比把它们全绑在一个"晃动强度"参数里要好排查得多。

> **一个之前没标注过的数值出入，跟 3.2 节 bob 幅度那处是同一类问题**：`ComputeIdleBreath` 里 `float scale = xySpeed + 1.0f;` 这一行，真实源码（`Player.cpp:8842-8846`）写的是 `scale = xyspeed + 40.0f`。这个 `40.0f` 不是随手写的经验数字，它的作用是"就算完全站定不动（`xyspeed=0`），呼吸感也要有一个不为零的最低摆动幅度"，只是原版的 `xyspeed` 是以每秒约 320 单位为量级的速度，`40` 相对它是一个小基数；换算到本教程米制单位下站立速度是 0 、跑步顶速才 7 左右，如果直接照抄 `40.0f`，这个常数会完全淹没 `xySpeed` 本身的贡献，呼吸感变成跟移动速度几乎无关的固定频率抖动——这不是"完全参考"该有的效果。这里改成 `1.0f` 是按跟 3.2 节 `BobUpAmount` 等常数同样的思路：保留原版"哪怕站定也有最低摆动幅度"这个设计意图，但重新在新的单位量级下选一个让效果既能感知到、又不会盖过速度变化的基数，属于同一类"结构完全参考、具体数值按手感重新配平"的处理，值得跟 3.2 节那条放在一起被同样诚实地记录下来，而不是让读者以为这是漏抄的数字。
>
> **另一件明确要澄清"不做"的事**：DOOM3 有一个 `BUTTON_ZOOM`/`weapon.GetZoomFov()` 驱动的"变焦"（`CalcFov()`，`Player.cpp:8666-8691`），按住某些武器（比如狙击步枪）的瞄准键会把视野角收窄，营造"举枪瞄准"的效果。但这跟现代商业 FPS 里常说的"ADS"（Aim Down Sights：举枪、准星移到屏幕正中、武器摇摆/后坐力大幅降低、往往还有武器模型整体位移到眼前）不是同一件事——DOOM3 的变焦只改 FOV 这一个数字，不改变上面六层偏移源里的任何一层，武器摇摆和后坐力在变焦时和不变焦时完全一样。也就是说，"给这套 DOOM3 风格的武器摇摆系统加一个能让摇摆/后坐力大幅降低的瞄准状态"，是一个 DOOM3 原版根本没有的现代商业 FPS 标准配置，不是本节遗漏了什么——如果你的项目确实需要 ADS 手感，思路是给上面 `UpdateSway` 六层偏移源各自的强度系数（`TurnSwayScale`、`BobRollScale`/`BobYawScale`/`BobPitchScale`、`RecoilKickDegrees` 等）整体乘上一个"是否正在瞄准"的插值系数，而不是另起一套摇摆逻辑，但这已经完全超出"忠实复刻 DOOM3"的范围，是一处明确的、需要你自己决定要不要做的现代化扩展。

### 6.3 近战攻击：伤害与物理冲击分开，并预留增益倍率接口

近战本质上和第 4 章的开火是同一件事——一条射线，只是距离短得多，不消耗弹药。但要"完全参考"DOOM 3 的近战，还有两个不能省略的细节：

1. **物理冲击和伤害数值是两条完全独立的调用**——命中之后既要造成伤害，也要给被打中的物体（如果是刚体）一个推开的冲量，两者互不依赖。
2. **伤害倍率要经过一个集中的增益修饰符查询点**，而不是直接用固定数值——哪怕你现在还没做狂暴/双倍伤害之类的增益道具，也应该先把这个查询点留出来，这是 DOOM 3 `idPlayer::PowerUpModifier()` 的设计：所有会影响战斗数值的增益，都通过同一个函数查询，而不是散落地在各处判断"当前是否处于某个状态"。

> 这条"所有会影响战斗数值的增益都要过这个查询点"的原则，下面第一版代码自己没有完全遵守：真实源码里近战的推力（`Weapon.cpp:4004`）也会乘上 `owner->PowerUpModifier(SPEED)`，而下面的 `MeleePushForce` 一开始完全没有接查询点，直接用了写死的数值——这跟本节自己强调的设计原则矛盾。改正版本给推力也接上了同一个查询点（复用 `PowerupState` 现有的 `MoveSpeed` 档位，源码这里用的也是速度类增益，不是单独开的一档）。

```csharp
// PowerupState.cs —— 挂在玩家身上，现在先放一个恒定返回 1.0 的占位实现
using Godot;
using System.Collections.Generic;

public partial class PowerupState : Node
{
    public enum Modifier { MeleeDamage, MeleeRange, ProjectileDamage, MoveSpeed }

    private readonly Dictionary<Modifier, double> _berserkUntil = new();

    // 之后做狂暴道具时，调用 ActivatePowerup(Modifier.MeleeDamage, 30.0, 10.0) 这样的接口即可接入，
    // 现在没有任何增益道具，GetModifier 对所有请求都返回 1.0——但调用方（Weapon.cs）从今天起就应该
    // 通过这个接口取倍率，而不是直接用写死的伤害数值，这样以后加增益不需要回头改战斗代码
    public float GetModifier(Modifier mod)
    {
        return 1.0f;
    }
}
```

```csharp
// Weapon.cs 追加
[Export] public float MeleeRange = 2.0f;
[Export] public float MeleeDamage = 40.0f;
[Export] public float MeleePushForce = 4.0f;
[Export] public PowerupState OwnerPowerups;   // 拖入玩家身上的 PowerupState 节点

public override void _UnhandledInput(InputEvent @event)
{
    if (State != WeaponState.Idle) return;   // 5.2 节补上的状态检查：收放枪过程中不响应任何输入
    if (@event.IsActionPressed("fire")) TryFire();
    if (@event.IsActionPressed("reload")) TryReload();
    if (@event.IsActionPressed("melee")) Melee();
}

private void Melee()
{
    float rangeScale = OwnerPowerups?.GetModifier(PowerupState.Modifier.MeleeRange) ?? 1.0f;
    float damageScale = OwnerPowerups?.GetModifier(PowerupState.Modifier.MeleeDamage) ?? 1.0f;
    // 推力也要过查询点——对应源码 owner->PowerUpModifier(SPEED)，复用 MoveSpeed 这一档
    float pushScale = OwnerPowerups?.GetModifier(PowerupState.Modifier.MoveSpeed) ?? 1.0f;

    var spaceState = GetWorld3D().DirectSpaceState;
    Vector3 from = Camera.GlobalPosition;
    Vector3 to = from + (-Camera.GlobalTransform.Basis.Z) * (MeleeRange * rangeScale);

    var query = PhysicsRayQueryParameters3D.Create(from, to);
    query.CollisionMask = 0b0101;
    var result = spaceState.IntersectRay(query);

    if (result.Count == 0) return;

    Node3D hitObject = (Node3D)result["collider"];
    Vector3 hitPoint = (Vector3)result["position"];
    Vector3 hitNormal = (Vector3)result["normal"];

    if (hitObject.HasMethod("TakeDamage"))
    {
        hitObject.Call("TakeDamage", MeleeDamage * damageScale);
    }

    // 物理冲击和伤害是两件独立的事——一个纯装饰性的、没有 TakeDamage 方法的刚体，
    // 挨了一拳依然应该被推开。推开方向用命中点的表面法线，不是用视线方向——对应源码
    // Event_Melee()（Weapon.cpp:4004）的 impulse = -push * PowerUpModifier(SPEED) * tr.c.normal，
    // 用的是 tr.c.normal（命中表面的法线），不是攻击者到目标的连线方向。正面近距离命中时
    // 两者几乎重合，但斜着擦到一个有角度的表面时（比如打中一个斜放的箱子的棱角），表面法线
    // 才是"应该往哪个方向弹开"更准确的物理直觉——用视线方向在这种情况下会把物体往错误的
    // 斜角推出去，用法线才会让它看起来是"被拳头撞飞"而不是"沿着你的准星方向平移"
    Vector3 pushDir = -hitNormal;

    if (hitObject is RigidBody3D rigidBody)
    {
        rigidBody.ApplyImpulse(pushDir * MeleePushForce * pushScale, hitPoint - rigidBody.GlobalPosition);
    }
    // 打中的如果是另一个玩家自己（比如 17 章要做的联机合作/对战），推力走的是完全不同的一套
    // 物理接口——CharacterBody3D 没有 ApplyImpulse，得用 2.9 节新加的 ApplyKnockback。这也是
    // 源码里 ent->ApplyImpulse(...) 为什么对任意实体类型都通用的原因：idPhysics_Player 自己
    // 实现了一份 ApplyImpulse（Physics_Player.cpp:1832 附近），跟 RigidBody 的物理接口是分开的
    // 两套实现，但对调用者（这里的 Melee()）暴露的是同一个"给我推一下"的意图
    else if (hitObject is PlayerController player)
    {
        player.ApplyKnockback(pushDir * MeleePushForce * pushScale, 0.15f);
    }
}
```

记得加 `melee` 输入映射（建议绑 V 键或鼠标中键）。你可能已经注意到，`Fire()`、`Melee()`、下一章要写的怪物近战判定，全部长得差不多——都是"从某个点往某个方向打一条射线，检测多远，命中了就调用 `TakeDamage`"。这不是偶然，也不是本教程偷懒——**几乎所有 FPS 里的近战攻击，本质上都是一条射程很短的"子弹"**，没有必要为它单独设计一套碰撞体积检测。这个观察本身也是第 13 章要讲的"什么时候该抽公共代码"的一个具体例子，先记住这个感觉，到时候会讲怎么把这几处重复的射线检测代码收拢成一个共享函数（到时候 `CombatUtil.RaycastAttack` 也会顺带把 `damageScale`/推力这两个参数一起纳入，不会漏掉这一节加的东西）。

顺带解决一个从 2.2 节就留到现在的接口：`PowerupState` 现在真实存在了，2.2 节为 `wishSpeed` 留的 `SpeedModifier` 字段也该有地方接上了——以后实现狂暴一类"移动速度也一起提升"的增益时，激活的地方（`PowerupState` 未来的 `ActivatePowerup`）除了记录倍率供 `GetModifier` 查询，还应该顺手把 `player.SpeedModifier = GetModifier(Modifier.MoveSpeed)` 同步一份到玩家控制器上——`PowerupState` 只负责"回答倍率是多少"，不负责"把倍率经常性地推给移动代码"，这一步同步逻辑现在没有实现（现在 `GetModifier` 恒返回 1.0，同步了也没有可感知的效果），但接口双方现在都已经就位，以后接增益道具时不需要再回头改 2.2 节的移动代码。

### 6.4 视图模型与世界模型：其他人看到的武器，和你自己看到的不是同一个

到目前为止，`WeaponHolder` 下面只有一个模型，这在单机游戏里通常够用——但如果你的项目以后可能有第三人称观察（过场动画摄像机、死亡后观察队友、联机），需要处理一个问题：**第一人称视角看到的武器模型和其他人看到你身上挂着的武器模型，是两个独立的物体**，只是动画同步播放。

> 这里要澄清一下"参照对象"：下面用 Godot 的渲染层（`Layers`）+ 摄像机剔除遮罩（`CullMask`）来实现"我自己看到一份模型、别人看到另一份模型"，这是 Godot 里解决这个问题的一种顺手的做法，但**不是 DOOM 3 实际的解法，架构完全不同**——真实的 id Tech 4 是让世界模型（`idAnimatedEntity* worldModel`）当成一个完全独立的、绑定到角色手部骨骼的实体单独生成出来，可见性是按 surface 逐个控制的：每个 surface 有一个 `renderEntity_t::suppressSurfaceInViewID` 字段，设成"武器持有者自己的 view ID"就表示"这个 surface 对持有者自己的摄像机隐藏"（而不是"只对某个渲染层可见"这种统一开关的思路），第一人称武器视图另外还走它自己独立的一个渲染 pass。这里借用的只是 DOOM 3"两份模型、各自可见性不同"这个问题定义，具体实现是 Godot 惯用法，不是照抄 id Tech 4 的机制。

```
Player (CharacterBody3D)
└── Head
    └── Camera3D
        └── WeaponHolder
            └── ViewModel (只有本地玩家自己的摄像机能看到)
└── Skeleton3D
    └── HandBone (BoneAttachment3D)
        └── WorldModel (其他视角能看到的、真正挂在角色手上的武器)
```

第一人称模型和世界模型分离靠的是 Godot 的**渲染层（`VisualInstance3D.Layers`）和摄像机的剔除遮罩（`Camera3D.CullMask`）**组合：

```csharp
// Weapon.cs 追加，在 _Ready() 里
public override void _Ready()
{
    // ...原有初始化...
    GetNode<Node3D>("ViewModel").Layers = 1u << 19;   // 独立渲染层，只有本地第一人称摄像机会看
    GetNode<Node3D>("WorldModel").Layers = 1u;         // 普通渲染层，所有摄像机都看得到
}
```

```csharp
// PlayerController.cs 的 _Ready() 里
_camera.SetCullMaskValue(20, true);   // 本地摄像机额外看到第 20 层（对应上面的 1u << 19）
```

播放动画（举枪/开火/换弹）时，两个模型的 `AnimationPlayer` 需要同步播放同一段动画名——具体接线方式和第 5.2 节的武器状态机是同一个模式（`ViewModel` 和 `WorldModel` 各自的 `AnimationPlayer.Play(animName)` 在 `Weapon.SetState()` 里一起调用），这里不重复贴代码。**如果你确定这辈子这个项目都不会有第三人称视角**，这一节可以完全跳过——`ViewModel`/`WorldModel` 分离解决的是一个特定问题（"别人看到的和你自己看到的不该是同一个渲染细节"），纯单人、永远第一人称视角的游戏不需要这个复杂度。

---

## 7. 物理与可交互物体：箱子、门、电梯

### 7.1 一个能被打飞的箱子

Godot 里"真正参与物理仿真、会被撞、会因为受力而移动"的物体用 `RigidBody3D`。做一个能被子弹打飞的木箱：

```
Crate (RigidBody3D)
├── CollisionShape3D (BoxShape3D)
└── MeshInstance3D (BoxMesh)
```

新建 `Crate.cs`：

```csharp
using Godot;

public partial class Crate : RigidBody3D
{
    [Export] public float Health = 30.0f;

    public void TakeDamage(float amount)
    {
        Health -= amount;
        if (Health <= 0)
        {
            QueueFree();
        }
    }
}
```

**先把碰撞层设对，不然下面这一切都不会发生**：第 4.3 节把武器射线的 `CollisionMask` 设成了 `0b0101`（只检测 `World` 层和 `Enemy` 层）。Godot 新建 `RigidBody3D` 默认落在第 1 层，凑巧等于这里的 `World`——但这是巧合，不是保证：如果你之前调整过默认物理层，或者是照着自己的习惯另外分配的层号，这里很容易对不上。把 `Crate` 节点的 `Collision Layer` 显式勾选成 `World`，确认它落在武器射线已有的检测范围内；不然子弹会直接穿过箱子打中它后面的墙，`TakeDamage` 永远不会被调用，而且不会有任何报错提示你哪里错了。后面 7.2 的门、7.3 的电梯是同样的道理——凡是"应该能被子弹打中/应该能挡住子弹"的场景物件，都建议放在 `World` 层，本章后面不再重复这条。

现在开枪打这个箱子，它会掉血、扣到 0 之后消失。但你会发现子弹命中时箱子不会被"打飞"——`TakeDamage` 只是扣血，没有施加物理冲量。回到 `Weapon.cs` 的 `Fire()`，命中后补一行：

```csharp
if (result.Count > 0)
{
    Node3D hitObject = (Node3D)result["collider"];
    Vector3 hitPoint = (Vector3)result["position"];
    Vector3 hitNormal = (Vector3)result["normal"];

    if (hitObject.HasMethod("TakeDamage"))
    {
        hitObject.Call("TakeDamage", Damage);
    }

    // 如果打中的是一个刚体，额外施加一个物理冲量——伤害和物理冲击是两件独立的事
    if (hitObject is RigidBody3D rigidBody)
    {
        Vector3 impulseDir = (to - from).Normalized();
        rigidBody.ApplyImpulse(impulseDir * 5.0f, hitPoint - rigidBody.GlobalPosition);
    }
}
```

**这里有一个值得记住的设计判断**：伤害（`TakeDamage`）和物理冲击（`ApplyImpulse`）是**两次分开的调用**，一个物体完全可以"只掉血不被打飞"，或者"被打飞但不掉血"（比如一个纯装饰性的空罐子，没有 `TakeDamage` 方法，但依然是 `RigidBody3D`，一样会被子弹撞飞）。不要把这两件事写成互相依赖的逻辑，分开处理会让后面加新物体类型时轻松很多——不过"完全独立"这个说法需要打个折扣：真实源码里这两个数值通常**共享同一份武器伤害定义**（同一个 `damageDef` 里的 `push` 和 `damage` 两个 key 一起读出来，同一把武器打在任何物体上，冲量和伤害的比例是固定的），不是两个可以随便各自取值的独立常数。本教程为了简单，上面 `Fire()` 里直接写死了一个 `5.0f` 给所有武器共用，没有像源码那样让每把武器自己的数据决定冲量大小，如果你想更贴近源码，可以把这个 `5.0f` 也做成 `Weapon` 的一个 `[Export]` 字段。

另外，真实的 `idMoveable::Killed()`（箱子对应的类）死亡时并不是直接消失——它会把模型换成一个专门准备的 `brokenModel`（碎裂后的残骸模型），残骸依然留在物理世界里继续参与仿真，不是像上面 `Crate.TakeDamage()` 这样直接 `QueueFree()` 瞬间消失。如果想要这个效果，思路很简单：血量归零时不调用 `QueueFree()`，改成把 `MeshInstance3D`/`CollisionShape3D` 换成"碎裂"版本的资源（比如换一个更小、更破碎的 `BoxMesh`/`BoxShape3D`，或者干脆换成一个准备好的碎片场景），残骸继续留在场景里被物理仿真接管。

**一个值得先问清楚的问题：满地都是箱子会不会把帧率拖垮？**不会，而且这不是运气——Godot 的 `RigidBody3D` 默认开启睡眠（`CanSleep = true`）：静止一段时间之后引擎会把它标记为休眠，跳过后续的物理积分，直到有东西碰它或者对它施加力才会被唤醒。这和真实源码 `idPhysics_RigidBody` 的机制是同一件事：`TestIfAtRest()`（`Physics_RigidBody.cpp:274`）逐帧检查线速度、角速度、接触点数量是否都低于阈值，一旦满足就调用 `Rest()`（`Physics_RigidBody.cpp:720`）把物体钉住不再模拟；`ApplyImpulse()`（`Physics_RigidBody.cpp:1075`）在施加冲量的同时总是紧接着调用一次 `Activate()`（`Physics_RigidBody.cpp:751`）把物体重新唤醒。上面 `Weapon.cs` 里"命中之后调用 `ApplyImpulse`"这一步天然对应了这个唤醒动作——Godot 的 `RigidBody3D.ApplyImpulse()` 同样会自动唤醒一个正在休眠的刚体，所以一屋子睡着的箱子被打中时能正常醒过来、被打飞，不需要你手动处理"先唤醒再施力"，两边引擎在这一点上做的是同一件事，不用加任何代码。

真实的 `idMoveable` 还有一件本教程目前没做的事：**被打飞的箱子撞到别的东西，会反过来对那个东西造成伤害**。`idMoveable::Collide()`（`Moveable.cpp:275`）在箱子撞上别的实体时，用撞击速度和一个 `minDamageVelocity` 阈值（默认 300）算出一个伤害倍率（`f = sqrt(v - min) / sqrt(max - min)`，速度越快伤害越接近满值，但不是线性增长），再调用 `ent->Damage(...)` 把这份伤害转嫁给撞上的东西——这正是很多商业 FPS/物理沙盒游戏里"把可破坏道具当武器扔"这种手感的来源：一个被爆炸掀飞的箱子砸中敌人，敌人是真的会掉血的，不只是被撞飞而已。给 `Crate.cs` 补上这个行为：

```csharp
using Godot;

public partial class Crate : RigidBody3D
{
    [Export] public float Health = 30.0f;
    [Export] public float MinDamageVelocity = 4.0f;   // 对应源码的 minDamageVelocity，单位换算成米/秒的经验值，具体数值要按场景尺度自己试
    [Export] public float MaxDamageVelocity = 10.0f;
    [Export] public float ImpactDamage = 15.0f;

    public override void _Ready()
    {
        ContactMonitor = true;
        MaxContactsReported = 4;   // 不开这两个，_IntegrateForces 里就拿不到接触信息
    }

    public void TakeDamage(float amount)
    {
        Health -= amount;
        if (Health <= 0)
        {
            QueueFree();
        }
    }

    // 对应 idMoveable::Collide()：撞击速度超过阈值，就对撞到的东西造成伤害——
    // 源码用 sqrt 曲线让伤害在阈值附近快速爬升、之后逐渐放缓，这里为了简单改成线性插值
    public override void _IntegrateForces(PhysicsDirectBodyState3D state)
    {
        float speed = LinearVelocity.Length();
        if (speed < MinDamageVelocity) return;

        float t = Mathf.Clamp((speed - MinDamageVelocity) / (MaxDamageVelocity - MinDamageVelocity), 0f, 1f);

        for (int i = 0; i < state.GetContactCount(); i++)
        {
            if (state.GetContactColliderObject(i) is Node3D collider && collider.HasMethod("TakeDamage"))
            {
                collider.Call("TakeDamage", ImpactDamage * t);
            }
        }
    }
}
```

`MinDamageVelocity`/`MaxDamageVelocity`没有一个通用的"标准答案"——取决于你场景里 1 米对应现实世界多大尺度、箱子质量、`ApplyImpulse` 力度这几个相互关联的参数，需要在编辑器里跑起来试出手感。

> **一个不打算实现的开放问题**：真实的 `idMoveable` 还能被 *Resurrection of Evil* 资料片新增的引力枪（Grabber，`Weapon_Grabber.cpp`）抓取悬浮、拖着满地图走——但这不是 `idMoveable` 类自带的能力，是那把武器单方面用物理约束把目标钉在准星前面实现的。本教程没有引力枪这一章，箱子在这里始终只是"会被撞飞、会掉血、现在还会反过来撞人"的道具，不能被拾取/搬运。如果以后想加类似能力，思路上更接近临时给箱子加一个约束节点或者每帧手动把它的位置钉到准星前方，而不是指望 `idMoveable`/`RigidBody3D` 本身有什么隐藏接口。

### 7.2 一扇会开的门

门不需要真的参与物理仿真（它不该被子弹撞开），但它需要能移动、并且移动时能正确地把站在旁边/上面的玩家一起带走，不能让玩家卡进门里。Godot 为这种"运动学移动、但依然正确参与物理交互"的物体准备了专门的节点类型：`AnimatableBody3D`。

```
Door (AnimatableBody3D)
├── CollisionShape3D
└── MeshInstance3D
```

`Door.cs`：

```csharp
using Godot;

public partial class Door : AnimatableBody3D
{
    [Export] public Vector3 OpenOffset = new Vector3(0, 3, 0);
    [Export] public float MoveTime = 1.0f;
    [Export] public float WaitTime = 3.0f;   // 开门后等待多久自动关闭

    private Vector3 _closedPosition;
    private bool _isOpen;
    private Tween _tween;

    public override void _Ready()
    {
        _closedPosition = Position;
        SyncToPhysics = true;   // 关键设置，见下方说明
    }

    public void Activate()
    {
        _isOpen = !_isOpen;
        Vector3 target = _isOpen ? _closedPosition + OpenOffset : _closedPosition;

        // 按剩余距离等比例缩短这一次运动的时长，而不是无论从哪个位置出发都用同一个 MoveTime——
        // 这样从半路被再次触发反向时（比如门刚开到一半，玩家又按了一次开关，或者第 11/12 章的
        // 触发器/按钮被连续按了两下），门的运动速度是连续的，不会出现"半路突然变速"的突兀感
        float totalDistance = OpenOffset.Length();
        float remainingDistance = (target - Position).Length();
        float duration = totalDistance > 0.0f ? MoveTime * (remainingDistance / totalDistance) : MoveTime;

        // 杀掉上一个还没播完的 tween 再开新的——如果不这样做，快速连续触发会导致两个 tween
        // 同时抢着写 Position，表现为门在两个目标之间抖动甚至瞬移，而不是干净地掉头
        _tween?.Kill();
        _tween = CreateTween();
        _tween.SetProcessMode(Tween.TweenProcessMode.Physics);   // 见下方说明
        _tween.SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
        _tween.TweenProperty(this, "position", target, duration);

        // 开门之后过 WaitTime 秒自动关闭——真实源码里门是通过延迟事件（PostEventSec）实现的，
        // 这里用一次性的 SceneTreeTimer 代替，效果等价：如果这段时间里门又被手动关闭了
        // （_isOpen 变回 false），这个回调触发时什么都不做
        if (_isOpen)
        {
            GetTree().CreateTimer(MoveTime + WaitTime).Timeout += () =>
            {
                if (_isOpen) Activate();
            };
        }
    }
}
```

这段代码里有两个容易被忽略、但只要写错就会在实机上现形的细节，值得展开说：

**第一个是 `_tween?.Kill()`**。`CreateTween()` 每次调用都会造出一个全新的、独立的 `Tween` 实例，Godot 不会替你管理"这个节点身上是不是已经有一个 tween 在动同一个属性"——如果 `Activate()` 在上一次的 tween 还没播完时被再次调用（这在只有单扇门、没有队伍系统的现在就能触发：连续两次触发这扇门，比如后面章节的按钮被手快的玩家连按两下），旧 tween 和新 tween 会在同一帧里各写一次 `Position`，谁在这一帧的 tween 更新顺序里排在后面就"赢"，肉眼看到的是门抖动甚至瞬移，而不是干脆利落地掉头往回走。用一个 `_tween` 字段记住当前正在播放的 tween、下次触发前先 `Kill()` 掉它，是 Godot 里"允许一个动画中途被打断重新开始"的标准写法。

**第二个是 `SetProcessMode(Tween.TweenProcessMode.Physics)`**。`Tween` 默认的 `ProcessMode` 是 `Idle`——也就是跟着渲染帧（`_Process`）推进，不是跟着物理帧（`_PhysicsProcess`）。但 `SyncToPhysics` 让 `AnimatableBody3D` 把"这一次物理步进里我的位置变化了多少"读出来、转换成一个速度传给物理引擎，`CharacterBody3D` 站在上面时正是靠这个速度被"带着走"的——如果 tween 只在渲染帧更新 `Position`，物理引擎在两次渲染帧之间的那几次物理步进读到的位置根本没变，算出来的"平台速度"就会一格一格地跳变而不是平滑连续，高刷新率显示器（渲染帧比物理帧密）下这个问题格外明显，表现为站在门/电梯上的玩家轻微抖动、甚至在门刚开始动的一瞬间被轻微"甩"一下。把 tween 的 `ProcessMode` 显式设成 `Physics`，让它跟物理帧同步推进，这个问题就没有了——这是很多 Godot 教程在演示"用 Tween/AnimationPlayer 做移动平台"时会漏掉的一步，因为不开这个选项在低刷新率、帧率稳定的录屏环境下很难注意到，一到玩家自己的机器上就会暴露。

`SyncToPhysics = true` 这一行本身也**非常容易被漏掉、漏掉之后的表现是"门确实动了，但站在门上的玩家会穿模掉下去"**——这是 Godot 物理系统一个真实存在的坑：`AnimatableBody3D` 默认不会把自己的运动通报给物理引擎，导致 `CharacterBody3D` 感知不到"我脚下的平台在动"。开了这个开关之后，还需要在 `PlayerController.cs` 里告诉玩家"哪些碰撞层算作可以站上去被带走的地面"：

```csharp
// PlayerController.cs 的 _Ready() 里加一行
PlatformFloorLayers = 1;   // 假设门/电梯所在的碰撞层是第 1 层
```

顺带一提：这里的"第 1 层"和 7.1 节强调过的碰撞层是同一件事——`Door` 节点的 `Collision Layer` 也应该包含武器射线检测的那个 `World` 层，不然子弹会直接穿过关着的门打中门后面的东西，而不是像真实世界那样被一扇关着的门挡住。

现在给门加一个触发它开关的方式——先用最简单的：给 `Door` 节点再加一个 `Area3D` 子节点（一个稍大的检测区域），玩家走进去就自动触发：

```csharp
// Door.cs 追加
public override void _Ready()
{
    _closedPosition = Position;
    SyncToPhysics = true;
    var triggerArea = GetNode<Area3D>("TriggerArea");
    triggerArea.BodyEntered += OnBodyEntered;
}

private void OnBodyEntered(Node3D body)
{
    if (body.IsInGroup("player") && !_isOpen)
    {
        Activate();
    }
}
```

记得把玩家节点加进 `player` 组（选中 `Player` 节点，`节点 -> 组`，添加 `player`）。

**给以后的钥匙系统留一个挂钩**：真实的 `idDoor` 从一开始就带着一个"这扇门要求什么"的字段——`requires`（`Mover.h:413`，从 `.def` 里配置成某个物品的名字），`Use()`（`Mover.cpp:3560-3572`）触发门之前先调用 `gameLocal.RequirementMet(activator, requires, removeItem)` 检查触发者的背包里有没有这个物品，没有就直接不动，`IsLocked()`（`Mover.cpp:3634`）单独暴露出来给关卡脚本和 AI 判断"这扇门现在能不能走"。第 11 章会做拾取物、但目前还没有背包/物品系统，没法现在就把"检查玩家是否持有某把钥匙"这部分接上——先在 `Door.cs` 留一个最简单的布尔开关，把入口占住，等背包系统做出来之后再把 `GD.Print` 换成真正的检查：

```csharp
// Door.cs 追加
[Export] public bool Locked = false;

public void Activate()
{
    if (Locked)
    {
        // 真实源码这里会检查 activator 的背包里有没有 requires 指定的钥匙物品（RequirementMet），
        // 没有就播放"锁着"的提示音，什么都不做——本教程还没有背包系统，先占住这个位置
        GD.Print("这扇门被锁住了");
        return;
    }

    // ...原有的 _isOpen 翻转、tween、自动关闭逻辑不变...
}
```

> **这里有一个开放问题，本教程不会展开实现**：真实源码里 `SetAASAreaState()`（`Mover.cpp:3454`）会把门的包围盒标记成 `AREACONTENTS_OBSTACLE`，写进关卡预计算好的 AAS（AI 寻路用的区域连通图）里，让第 9 章那种寻路系统知道"这块地方现在走不通"——但仔细读调用点会发现，这个标记主要是跟着 `Lock()`/`IsLocked()` 走的（`door->SetAASAreaState(f != 0)`，`Mover.cpp:3619`），而不是跟着门"现在是开是关"这个瞬时状态走：一扇没上锁的门，哪怕此刻正关着，在 AAS 里也不算障碍——因为源码里的怪物 AI 本来就有"自己走到门前触发它"的行为，没必要在寻路层面把关着的门当墙。反观本教程第 9 章的寻路是 Godot 内置的 `NavigationRegion3D`，导航网格是在编辑器里一次性"烘焙"出来的静态数据，不会因为一扇门运行时开关而自动更新——如果你的怪物需要绕过或者穿过一扇门，现在的做法要么是烘焙时把门的位置留出一条通道（等于假设门永远不构成障碍，这对于常开的门没问题，但一扇需要被主动触发的门会被怪物直接当成空气走过去），要么是自己教怪物在寻路目标前方检测到门就调用 `Door.Activate()`（模拟"AI 会自己开门"）。这两种做法都不是"改一行配置"能解决的，分别涉及关卡烘焙策略和 AI 行为树的改动，超出本章"物理与可交互物体"的范围，留给你在真正遇到这个需求时再决定怎么处理。

### 7.3 电梯：同样的原理，多一个状态

> 这里要先纠正一个说反了的说法：不是"电梯就是门的代码结构、只是多个状态"。真实源码里，跟门代码结构真正一样、不多不少的，其实是一种简单的**两站式升降台**（`idPlat`，`Mover.h:442-470`）——它和 `idDoor` 一样是同一个 4 状态 FSM（`idMover_Binary`），只是把"开/关"换成了"上/下"，结构确实完全没差。真正带"多一个状态"的 `idElevator`（`Mover.h:214-267`，`INIT`/`IDLE`/`WAITING_ON_DOORS` 三个状态）是一个完全独立、大得多的协调器类，直接继承 `idMover`（不是 `idMover_Binary`），要管理多个楼层各自的门、等门关好才能真正启动、处理呼叫按钮排队——不是"门的代码加一个 flag"能概括的。下面这节做的 `Elevator.cs` 实际对应的是前者（两站式升降台），代码结构上确实和门一样；如果你想要那种带呼叫按钮、能停多个楼层、还要等门关好的"真电梯"，那是另一个规模大得多的系统，本教程不实现。

两站式升降台在两个位置之间往返移动，代码结构和门几乎一样，只是多了个"当前在哪一层"的状态和到达后暂停一下再返回的逻辑：

```csharp
using Godot;

public partial class Elevator : AnimatableBody3D
{
    [Export] public float TopOffset = 4.0f;
    [Export] public float MoveTime = 2.0f;
    [Export] public float WaitTime = 3.0f;
    [Export] public NodePath BlockZonePath;   // 怎么放、检测原理见 7.4 节双开门部分的说明——和门用的是同一套"挡住就反向"机制，这里先用上

    private Vector3 _bottomPosition;
    private bool _atTop;      // 表示"当前正朝哪个状态运动/已经到达的目标层"，不是"已经到达"的确认——和 7.4 节 Door 的 _isOpen 语义一致
    private bool _isMoving;
    private Tween _tween;

    public override void _Ready()
    {
        _bottomPosition = Position;
        SyncToPhysics = true;

        var blockZone = GetNodeOrNull<Area3D>(BlockZonePath);
        if (blockZone != null) blockZone.BodyEntered += OnBlocked;
    }

    public void Activate()
    {
        if (_isMoving) return;
        MoveTo(!_atTop);
    }

    private void MoveTo(bool goingUp)
    {
        _atTop = goingUp;
        _isMoving = true;

        Vector3 target = _atTop ? _bottomPosition + Vector3.Up * TopOffset : _bottomPosition;

        // 按剩余距离等比例缩短运动时长，保持反向时速度连续——道理和 7.2 节门的同一处理完全一样
        float remainingDistance = (target - Position).Length();
        float duration = TopOffset > 0.0f ? MoveTime * (remainingDistance / TopOffset) : MoveTime;

        _tween?.Kill();
        _tween = CreateTween();
        _tween.SetProcessMode(Tween.TweenProcessMode.Physics);   // 见 7.2 节说明：SyncToPhysics 要求位置更新跟着物理帧走
        _tween.SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
        _tween.TweenProperty(this, "position", target, duration);
        _tween.Finished += OnMoveFinished;
    }

    private async void OnMoveFinished()
    {
        // 到达之后暂停 WaitTime 秒再允许下一次触发——_isMoving 在等待期间继续保持 true，挡住重复触发
        await ToSignal(GetTree().CreateTimer(WaitTime), SceneTreeTimer.SignalName.Timeout);
        _isMoving = false;
    }

    // 对应 idPlat::Event_TeamBlocked（Mover.cpp:4433）：真实源码里 idPlat 和没打 crusher 标记的
    // idDoor 用的是完全同一套"挡住就反向"默认行为（都来自 idMover_Binary），电梯没道理不接上——
    // 具体检测方式沿用 7.4 节门的思路：门扇/平台运动路径前沿放一个薄 Area3D
    private void OnBlocked(Node3D body)
    {
        if (!_isMoving) return;
        MoveTo(!_atTop);
    }
}
```

**为什么把 `await ToSignal(tween, ...)` 的写法换成了 `tween.Finished += OnMoveFinished`**：上一版用 `async`/`await` 直接等 tween 播完，写法更直观，但一旦要支持"运动中途被挡住就反向"，这个写法会出问题——`OnBlocked` 需要调用 `_tween?.Kill()` 打断正在播放的旧 tween，而 `Tween.Kill()` 不会让被杀掉的 tween 发出 `Finished` 信号（它是被打断的，不是正常播完的）。如果代码还留在 `await ToSignal(tween, Tween.SignalName.Finished)` 这一行，这次 `await` 会永远等不到信号、永远不会恢复，后面"暂停 `WaitTime` 秒、把 `_isMoving` 置回 `false`"这几行代码就再也不会执行，电梯从此卡死在"正在移动"状态，之后不管怎么触发都没反应。换成事件回调（`tween.Finished += ...`）之后，"tween 播完了该干什么"和"tween 被中途打断了该干什么"是两条不会互相纠缠的独立路径，`OnBlocked` 可以放心地随时 `Kill()` 掉旧 tween、重新开始一个新的，不用担心留下一个永远等不到的 `await`。7.2/7.4 节的 `Door.cs` 从一开始就没用 `async`/`await`，原因也在这里——这是"要支持中途打断重新开始"的动作，就不能用 `await` 直接等一个可能被杀掉的信号，这一条以后写类似的可打断动画/tween 逻辑时都适用。

这一节的三个例子（箱子、门、电梯）分别对应物理系统里三种不同的"物体该怎么动"：**完全交给物理引擎仿真**（箱子）、**按预定轨迹运动、但要正确参与物理交互**（门/电梯）、以及第 2 章已经写过的**由玩家输入直接驱动**（角色）。这三种是几乎所有 FPS 里"会动的东西"的全部分类，之后做怪物（第 8 章）的时候，你会发现敌人的移动方式其实是第三种和第一种的某种混合。

### 7.4 多部件联动的门：双开门为什么不能各自独立触发

一扇"双开门"（两片门扇同时向两侧打开）如果给每一片各自独立触发，玩家触碰到其中一片、只有那一片会动，看起来很别扭。DOOM 3 的解法是给同一组门指定一个共同的"团队名字"，其中一个成员被触发时，会把动作转发给整个团队、并且所有成员用**同一个起始时间**开始运动，保证严格同步（不是"各自播放同一段动画"，是"共用一份运动的起止时间"，这个区别在两片门运动速度不同或者中途被打断反向时会体现出来）。

> **先说清楚"转发给队长"这个模式本身解决的是什么问题，也是接下来要补的东西**：真实源码里团队系统真正的价值不是"同步开始"这么简单——`idMover_Binary::SetBlocked()`（`Mover.cpp:3046-3060`）会把"被挡住了"这个状态沿着 `activateChain` 广播给团队里的每一个成员，`idDoor::Event_TeamBlocked`（`Mover.cpp:3846-3852`）收到这个通知后会让门**反向**：如果一片门被卡住的东西（比如被塞进门缝的箱子，或者被 AI 挡住）挡住走不动了，整个团队会一起掉头往回走，而不是"没被挡住的那一片继续往前开、被挡住的那一片傻站着"。上面刚才那句"这个区别在两片门运动速度不同或者中途被打断反向时会体现出来"就是在说这件事——但下面第一版代码只实现了"转发+同步触发"，完全没有实现"中途被挡住会反向"，团队系统真正被需要的那部分理由反而没兑现。补上这一块：

```csharp
// Door.cs 追加
[Export] public string TeamName = "";
[Export] public NodePath BlockZonePath;   // 指向门扇前沿一个薄的 Area3D（门自己的子节点），检测移动路径上有没有东西挡路
[Export] public bool IsCrusher = false;   // 见下方说明：压缩机关门不反向，直接对挡路者造成伤害
[Export] public float CrushDamage = 0f;

private static readonly Dictionary<string, List<Door>> Teams = new();
private bool _isTeamMaster;
private bool _isMovingNow;

public override void _Ready()
{
    _closedPosition = Position;
    SyncToPhysics = true;

    if (TeamName != "")
    {
        if (!Teams.ContainsKey(TeamName))
        {
            Teams[TeamName] = new List<Door>();
            _isTeamMaster = true;   // 同一队伍里第一个 _Ready() 的成为队长
        }
        Teams[TeamName].Add(this);
    }

    var blockZone = GetNodeOrNull<Area3D>(BlockZonePath);
    if (blockZone != null) blockZone.BodyEntered += OnBlocked;
}

// 场景卸载/重载时要把自己从静态字典里摘出去——这个字典是 static 的，跨场景实例存活，
// 不清理的话，重新加载同一个 TeamName 的门会在列表里越堆越多陈旧的引用（上一版遗漏的清理步骤）
public override void _ExitTree()
{
    if (TeamName != "" && Teams.ContainsKey(TeamName))
    {
        Teams[TeamName].Remove(this);
        if (Teams[TeamName].Count == 0) Teams.Remove(TeamName);
    }
}

public void Activate()
{
    if (Locked)
    {
        GD.Print("这扇门被锁住了");
        return;
    }

    if (TeamName == "")
    {
        DoActivate();
        return;
    }

    if (!_isTeamMaster)
    {
        // 从属成员把触发请求转发给队长，自己不直接处理——对应精读文档描述的
        // "所有控制 API 从属成员重定向到主控" 这个模式
        Teams[TeamName].Find(d => d._isTeamMaster)?.Activate();
        return;
    }

    // 队长按顺序对全队每个成员发起运动——不需要显式记录一个时间戳再"对齐"，
    // 这个 foreach 本身就在同一次方法调用、同一帧里同步跑完，每个成员的 CreateTween()
    // 天然就是同一帧启动的，这就已经是"同一个起始时间"了（上一版传了个 startTime 参数
    // 但函数体根本没用到它，属于中看不中用，这一版直接去掉）
    foreach (var member in Teams[TeamName])
    {
        member.DoActivate();
    }
}

private void DoActivate()
{
    _isOpen = !_isOpen;
    Vector3 target = _isOpen ? _closedPosition + OpenOffset : _closedPosition;

    float totalDistance = OpenOffset.Length();
    float remainingDistance = (target - Position).Length();
    float duration = totalDistance > 0.0f ? MoveTime * (remainingDistance / totalDistance) : MoveTime;

    _isMovingNow = true;
    _tween?.Kill();
    _tween = CreateTween();
    _tween.SetProcessMode(Tween.TweenProcessMode.Physics);
    _tween.SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
    _tween.TweenProperty(this, "position", target, duration);
    _tween.Finished += () => _isMovingNow = false;

    if (_isOpen)
    {
        GetTree().CreateTimer(MoveTime + WaitTime).Timeout += () =>
        {
            if (_isOpen) Activate();
        };
    }
}

// 对应 idDoor::Event_TeamBlocked（Mover.cpp:3846-3859）：移动路径上的检测区域探测到有东西挡路，
// 默认反向——复用 Activate() 本身的转发/同步逻辑，从属成员一样会把这次"反向触发"转给队长，
// 队长再广播给全队，不需要另外写一套反向专用的代码路径。但源码里这里其实有两条分支：
// "if (crusher) return;"（Mover.cpp:3850）——crusher 类型的门从不反向，而是靠
// Event_PartBlocked（Mover.cpp:3875-3878）对挡路者造成 damage_moverCrush 伤害，
// 这是压缩机关门（trash compactor 那种"故意压死你"的机关）的实现方式，跟会体贴地掉头的
// 普通门是两种完全不同的设计意图，不能一概而论
private void OnBlocked(Node3D body)
{
    if (!_isMovingNow) return;

    if (IsCrusher)
    {
        if (CrushDamage > 0 && body.HasMethod("TakeDamage"))
        {
            body.Call("TakeDamage", CrushDamage);
        }
        return;
    }

    Activate();
}
```

需要在文件顶部加 `using System.Collections.Generic;`。把双开门的两个 `Door` 实例 `TeamName` 都填成同一个字符串（比如 `"double_door_01"`），`OpenOffset` 分别指向左右两侧相反的方向，触发任意一片都会让整组同步开启；`BlockZonePath` 各自指向自己门扇前沿的一个薄 `Area3D`（贴着门扇运动方向的领先边缘摆一个大概几厘米厚的检测区域）。**"从属成员把请求转发给队长处理"这个重定向模式**，本教程后面不会再重复贴一遍代码，但如果以后遇到"一组东西需要被当成一个整体触发/控制"的场景（比如一组联动的灯光、一组必须同时启动的机关），都可以照抄这个思路：一个字符串分组键 + 队伍内选出一个队长 + 其余成员的操作全部转发给队长处理，现在加上了"挡路即反向"，这个思路才算真正把团队系统存在的理由用上。

**这套机制不是"组队门"的专属特权**：`OnBlocked` 和 `_isMovingNow` 从头到尾都没有检查 `TeamName`——一扇完全没填 `TeamName` 的单开门，只要在 Inspector 里给它接上 `BlockZonePath`，`Activate()` 分支会直接走 `TeamName == ""` 那条路径调用 `DoActivate()`，反向逻辑照样生效。换句话说，"门自动关闭时压到站在门口的玩家"这个更常见的场景（不需要双开门也会发生），从一开始就该给每一扇门都接上 `BlockZonePath`，不是只有组队门才需要——7.2 节介绍单扇门的时候没有引入这部分，是因为反向检测依赖"移动路径上有东西"这个概念，放在这里跟"为什么需要团队"一起讲更完整，但代码本身对单扇门同样适用。

**队伍规模也不是写死成两片的**：`Teams[TeamName]` 是一个 `List<Door>`，`Activate()` 广播时用 `foreach` 遍历整个列表——三片、四片门共用同一个 `TeamName` 一样能同步运动、一样能在任意一片被挡住时让全队反向，不需要改一行代码。真实源码里 `activateChain` 也是同样的设计：团队大小从来不是通过某个"两个"的硬编码假设支持的，而是一条可以挂任意多个成员的链表。

---

## 8. 做一只会打你的怪物

到这里你已经有了一个能打枪的玩家、一个有物理效果的世界。是时候给玩家一个打的对象了。这一章的目标很朴素：**做一只能看到你、会走过来、能打你、会死的怪物**——不追求智能，先追求"能玩"。第 9 章再回头把它变聪明。

### 8.1 一个会掉血、会死的怪物

```
Enemy (CharacterBody3D)
├── CollisionShape3D (CapsuleShape3D)
└── MeshInstance3D (随便一个能看出朝向的形状，比如一个圆锥)
```

`Enemy.cs`：

```csharp
using Godot;

public partial class Enemy : CharacterBody3D
{
    [Export] public float MaxHealth = 50.0f;
    [Export] public float MoveSpeed = 3.0f;
    [Export] public float Gravity = 20.0f;

    private float _health;

    public override void _Ready()
    {
        _health = MaxHealth;
    }

    public void TakeDamage(float amount)
    {
        _health -= amount;
        GD.Print($"{Name} 受到 {amount} 点伤害，剩余 {_health}");
        if (_health <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        GD.Print($"{Name} 死了");
        QueueFree();   // 先用最简单的方式——直接从场景里移除，第 10 章会换成有布娃娃效果的死亡
    }
}
```

这和第 7 章的 `Crate.TakeDamage` 长得几乎一样——这不是巧合，**"能被伤害的东西"这个能力，不应该关心自己是箱子还是怪物**。这个观察会在第 14 章被正式处理（把它抽成一个所有"能受伤的东西"共享的组件），这里先不急，让代码重复着，重复到你自己觉得"够了，该抽出来了"的那一刻，抽象的时机自然就到了——这也是编程里一条实用的经验：**过早抽象往往比暂时重复更浪费时间**，先让功能跑起来，等真的看到重复模式且它开始造成麻烦，再动手整理。

### 8.2 让怪物追你

```csharp
// Enemy.cs 追加
[Export] public float ChaseRange = 15.0f;
private Node3D _player;

public override void _Ready()
{
    _health = MaxHealth;
    _player = GetTree().GetFirstNodeInGroup("player") as Node3D;   // 用第 7 章加过的 "player" 组找到玩家
}

public override void _PhysicsProcess(double delta)
{
    float dt = (float)delta;
    Vector3 velocity = Velocity;

    if (!IsOnFloor())
    {
        velocity.Y -= Gravity * dt;
    }

    if (_player != null && GlobalPosition.DistanceTo(_player.GlobalPosition) < ChaseRange)
    {
        Vector3 direction = (_player.GlobalPosition - GlobalPosition);
        direction.Y = 0;
        direction = direction.Normalized();

        velocity.X = direction.X * MoveSpeed;
        velocity.Z = direction.Z * MoveSpeed;

        LookAt(new Vector3(_player.GlobalPosition.X, GlobalPosition.Y, _player.GlobalPosition.Z), Vector3.Up);
    }
    else
    {
        velocity.X = 0;
        velocity.Z = 0;
    }

    Velocity = velocity;
    MoveAndSlide();
}
```

运行游戏，靠近怪物，它会转身朝你直冲过来（会穿墙，因为现在完全没有寻路，只是"直线朝玩家坐标移动"——这是第 9 章要解决的问题，先接受这个简陋版本，能看到怪物"活了"是这一章的目标）。

### 8.3 让怪物打你

怪物追上你之后应该攻击。给怪物加一个近战判定——是的，又是一条短射线，跟第 6 章玩家的近战、第 4 章的开火，是同一个模式：

```csharp
// Enemy.cs 追加
[Export] public float AttackRange = 2.0f;
[Export] public float AttackDamage = 10.0f;
[Export] public float AttackCooldown = 1.5f;
private double _lastAttackTime = -999;

// 在 _PhysicsProcess 里，追击逻辑之后加：
private void TryAttack()
{
    if (_player == null) return;
    float distance = GlobalPosition.DistanceTo(_player.GlobalPosition);
    if (distance > AttackRange) return;

    double now = Time.GetTicksMsec() / 1000.0;
    if (now - _lastAttackTime < AttackCooldown) return;
    _lastAttackTime = now;

    if (_player.HasMethod("TakeDamage"))
    {
        _player.Call("TakeDamage", AttackDamage);
    }
    GD.Print($"{Name} 攻击了玩家");
}
```

在 `_PhysicsProcess` 的追击分支里调用 `TryAttack();`。现在玩家也需要一个 `TakeDamage` 方法——回到 `PlayerController.cs`：

```csharp
// PlayerController.cs 追加
[Export] public float MaxHealth = 100.0f;
public float Health { get; private set; }

public override void _Ready()
{
    Health = MaxHealth;
    // ...原有的 _head/_camera 初始化...
}

public void TakeDamage(float amount)
{
    Health -= amount;
    GD.Print($"玩家受到 {amount} 点伤害，剩余 {Health}");
    if (Health <= 0)
    {
        GD.Print("玩家死亡");
        // 死亡处理留到后面章节完善
    }
}
```

跑到这里，你已经有一个完整的"能打能被打"的最小闭环了：玩家能开枪/近战打怪物，怪物会追玩家、贴近了会还手，双方都有血量和死亡判定。**这是整个教程第一个真正意义上的"能玩"的里程碑**——花点时间实际玩一玩，感受一下现在的手感和数值（伤害、速度、攻击距离）舒不舒服，觉得不舒服就现在调，比后面系统更复杂了再回头调容易得多。

---

## 9. 敌人 AI 进阶：寻路、感知、状态机、难度

第 8 章的怪物会穿墙、感知不到"看不看得见玩家"（隔着墙也会来追你）、行为只有"追"和"打"两种状态。这一章把这三个问题逐个解决。

### 9.1 寻路：让怪物会绕路

Godot 内置了完整的导航系统，不需要自己写寻路算法。步骤：

1. 在关卡场景里加一个 `NavigationRegion3D` 节点，包住整个可行走区域。
2. 选中它，编辑器顶部会出现"烘焙导航网格"按钮，点一下——Godot 会自动分析场景里的碰撞体，生成一份"哪里能走"的导航网格数据。
3. 给 `Enemy` 加一个 `NavigationAgent3D` 子节点。

`Enemy.cs` 用寻路替换掉第 8.2 节那个"直线朝玩家走"的写法：

```csharp
// Enemy.cs 修改
private NavigationAgent3D _navAgent;

public override void _Ready()
{
    _health = MaxHealth;
    _player = GetTree().GetFirstNodeInGroup("player") as Node3D;
    _navAgent = GetNode<NavigationAgent3D>("NavigationAgent3D");
}

public override void _PhysicsProcess(double delta)
{
    float dt = (float)delta;
    Vector3 velocity = Velocity;

    if (!IsOnFloor())
    {
        velocity.Y -= Gravity * dt;
    }

    if (_player != null && GlobalPosition.DistanceTo(_player.GlobalPosition) < ChaseRange)
    {
        _navAgent.TargetPosition = _player.GlobalPosition;

        if (!_navAgent.IsNavigationFinished())
        {
            Vector3 nextPos = _navAgent.GetNextPathPosition();
            Vector3 direction = (nextPos - GlobalPosition);
            direction.Y = 0;
            direction = direction.Normalized();

            velocity.X = direction.X * MoveSpeed;
            velocity.Z = direction.Z * MoveSpeed;

            if (direction.LengthSquared() > 0.01f)
            {
                LookAt(new Vector3(GlobalPosition.X + direction.X, GlobalPosition.Y, GlobalPosition.Z + direction.Z), Vector3.Up);
            }
        }

        TryAttack();
    }
    else
    {
        velocity.X = 0;
        velocity.Z = 0;
    }

    Velocity = velocity;
    MoveAndSlide();
}
```

`_navAgent.TargetPosition = ...` 告诉寻路系统"我想去哪"，`GetNextPathPosition()` 每帧问一次"我现在这一步该往哪个方向走"——注意它**不是**返回一整条路径，只返回下一个该去的点，这是 Godot 导航系统故意这样设计的：真正复杂的"整条路怎么规划"在你看不到的地方每帧/每隔几帧算好，你只需要不断问"下一步去哪"，跟着走就行。

现在给关卡加几堵墙，怪物应该能绕过去追你，而不是直接怼着墙走。

### 9.1.1 动态障碍：门关上之后，怪物真的会绕开吗

上面这套寻路能绕开的，只有**烘焙导航网格的时候就已经在那儿**的静态几何——关卡搭建阶段摆好的墙、地形。一个很自然的问题是：第 7 章做的那扇门，运行时会开会关，算不算这套寻路系统能感知到的障碍物？答案是**完全不算**——门的碰撞体从来没有参与过 `NavigationRegion3D` 的烘焙过程（第 7 章的门是一个独立的 `Area3D`/`AnimationPlayer` 组合，压根不是拿来给寻路用的），对 `NavigationAgent3D` 来说，门所在的那块地板从始至终都是"可以走"的空地，不管门这一刻是开着还是关着。也就是说，**现在这套实现里，关着的门根本挡不住怪物寻路**——它规划出的路径会直接穿过门的位置，最多在物理碰撞层面撞在门的碰撞体上卡一下，不会主动绕开。

> **这里先说清楚为什么不能简单地"让门也参与烘焙"**：Godot 的 `NavigationObstacle3D` 确实有 `AffectNavigationMesh`/`CarveNavigationMesh` 这两个属性，可以让一个物体在烘焙导航网格的时候把自己所在的区域"挖空"，效果就是被这个物体占据的地方彻底从可行走区域里消失——但这套机制是**为烘焙这个动作设计的**，不是为运行时随时开关设计的：一扇门这一秒关着、下一秒被打开，每次状态变化都要求 `NavigationRegion3D` 重新完整烘焙一次导航网格，这是一个相对昂贵的操作，如果关卡里有好几扇会频繁开关的门，没人会希望每次开关门都触发一次导航网格重烘焙。

真正适合"运行时随时变化"这种场景的，是 Godot 导航系统里另一层机制——**RVO 动态避让**（`avoidance_enabled`），它不改导航网格本身，而是在寻路给出的路径之上再加一层"实时躲开附近障碍物"的速度调整，代价是每帧的，不是每次开关门都要重新烘焙一次那么贵。先给门加一个只在关闭状态下生效的 `NavigationObstacle3D`：

```csharp
// Door.cs（第 7 章的门脚本）追加——门下面挂一个 NavigationObstacle3D 子节点，
// 顶点大致围出门洞被门板堵住时的那块区域
private NavigationObstacle3D _navObstacle;

public override void _Ready()
{
    // ...原有初始化...
    _navObstacle = GetNodeOrNull<NavigationObstacle3D>("NavigationObstacle3D");
}

// 原有的开门/关门逻辑里追加：门关上时打开避让、门开着时关掉——
// 打开的门不该继续挡任何人的路
private void SetDoorOpen(bool open)
{
    // ...原有的开关门表现...
    if (_navObstacle != null) _navObstacle.AvoidanceEnabled = !open;
}
```

`Enemy` 这边要打开避让、并且**把原来那些直接拍 `Velocity` 的地方，改成先把"我想要的速度"交给避让系统过一遍**——这是必须的一步，不是可选的：`avoidance_enabled` 的工作方式是你每帧把"这一帧本来想要的速度"写进 `_navAgent.Velocity`（这是"意图速度"的输入端，不是角色实际的 `Velocity`），`NavigationServer` 参考周围的 `NavigationObstacle3D`/其他开了避让的 agent 算出一个"安全速度"，通过 `VelocityComputed` 信号异步传回来；如果只打开 `AvoidanceEnabled = true` 却从来不写 `_navAgent.Velocity`，等于一直在告诉避让系统"我想要的速度是零"，算出来的安全速度自然也毫无意义——这是接入这套系统时很容易漏掉、但漏了就等于完全没生效的一步：

```csharp
// Enemy.cs 追加
private Vector3 _avoidanceSafeVelocity;

public override void _Ready()
{
    // ...原有初始化...
    _navAgent.AvoidanceEnabled = true;
    _navAgent.Radius = 0.4f;   // 跟怪物自己的碰撞体半径大致对应
    _navAgent.VelocityComputed += v => _avoidanceSafeVelocity = v;
}

// 把"寻路算出来的期望速度"交给避让系统，返回上一次算出来的安全速度——
// 注意这里有一帧的延迟：VelocityComputed 是异步信号，这一帧写进去的 desiredVelocity，
// 算出来的安全速度要等到之后才会通过回调更新 _avoidanceSafeVelocity，这一帧能读到的
// 只是"上一次"算出来的结果。怪物移动速度通常不快，这一帧的延迟在观感上可以忽略，
// 但如果你的怪物移动很快、或者对精确的躲避时机有要求，这一点需要知道
private Vector3 ApplyAvoidance(Vector3 desiredVelocity)
{
    _navAgent.Velocity = desiredVelocity;
    return _avoidanceSafeVelocity;
}
```

以 `ChaseAndAttack()`（9.4.3 节）为例，接入方式是把原来 `Velocity = new Vector3(moveDir.X * MoveSpeed + strafeVel.X, ...)` 这一行里、寻路贡献的那部分速度过一遍 `ApplyAvoidance()`：

```csharp
// ChaseAndAttack() 里原来这一行：
// Velocity = new Vector3(moveDir.X * MoveSpeed + strafeVel.X, Velocity.Y, moveDir.Z * MoveSpeed + strafeVel.Z);
// 改成：
Vector3 avoided = ApplyAvoidance(new Vector3(moveDir.X * MoveSpeed, 0, moveDir.Z * MoveSpeed));
Velocity = new Vector3(avoided.X + strafeVel.X, Velocity.Y, avoided.Z + strafeVel.Z);
```

> **这里要老实说明这套方案覆盖到什么程度、没覆盖到什么程度**：上面只演示了 `ChaseAndAttack()` 一处的接入方式——`TickReposition()`、巡逻移动、9.3 节改版 `TickAlert()` 的搜索移动，但凡是"从 `GetNextPathPosition()` 算出方向再乘 `MoveSpeed`"的地方，都是同一个改法（把结果丢进 `ApplyAvoidance()`），但本教程没有把每一处都改一遍贴出来——这是本教程刻意没有全部展开的一处工作量，逐处修改机械但繁琐，读者应该已经能照着上面这一处的模式自己补完其余几处。即便全部接上，`avoidance_enabled` 提供的也只是"实时躲避"，不是"重新规划路径"：`NavigationAgent3D` 心里想去的目标点和它规划出的那条路径本身完全不知道有一扇门刚刚关上，避让层只是在执行这条路径的过程中，一旦即将撞上这个 `NavigationObstacle3D`，把这一帧的期望速度往旁边推一点。如果走廊够宽，怪物会绕着关闭的门走一圈过去，看起来跟"重新规划了路径"没什么区别；但**如果这扇门正好堵在一条只有它一条路可走的窄走廊中间**，怪物会被推得贴着墙、在门前面反复试图挤过去而挤不过去，卡在原地打转，而不是掉头走另一条路——因为避让层根本不知道"另一条路"存不存在，那是路径规划层的职责，而路径规划层这里没有被通知门已经关上了。**这是一个明确的开放问题，本教程没有给出完美方案**：真正彻底的解法是门状态变化时调用 `NavigationRegion3D.BakeNavigationMesh()` 让整张导航网格感知到这个变化，但这个操作足够贵，不适合频繁触发的门；折中方案包括给容易被完全堵死的门额外配置一条"备用路径"标记点，或者干脆在关卡设计上避免"只有一扇门能过"的死胡同布局。选哪一种取决于你的关卡设计，这里不替你决定。

### 9.2 感知：看不见就不该追

现在的怪物只要在 `ChaseRange` 范围内就会追，哪怕隔着一堵墙。加一个视线检测：

```csharp
// Enemy.cs 追加
[Export] public float FieldOfViewDegrees = 100.0f;
private Vector3? _lastKnownPlayerPos;   // C# 的可空值类型（Vector3?）就能表达"要么有一个位置，要么什么都没有"，
                                         // 不需要为了偷懒判空而借用引用类型——这里直接用最直白的写法

private bool CanSeePlayer()
{
    if (_player == null) return false;

    Vector3 toPlayer = _player.GlobalPosition - GlobalPosition;
    float distance = toPlayer.Length();
    if (distance > ChaseRange) return false;

    // 视野角检测：玩家是否在怪物面前的视野锥范围内
    Vector3 forward = -GlobalTransform.Basis.Z;
    float angleToPlayer = forward.AngleTo(toPlayer.Normalized());
    if (Mathf.RadToDeg(angleToPlayer) > FieldOfViewDegrees * 0.5f) return false;

    // 视线检测：中间有没有挡着东西
    var spaceState = GetWorld3D().DirectSpaceState;
    var query = PhysicsRayQueryParameters3D.Create(
        GlobalPosition + Vector3.Up, _player.GlobalPosition + Vector3.Up * 0.5f);
    query.CollisionMask = 0b0011;   // World + Player 层，不检测其他怪物
    var result = spaceState.IntersectRay(query);

    // 视线是否畅通：没打到任何东西，或者打到的第一个东西就是玩家本人
    bool visible = result.Count == 0 || (Node3D)result["collider"] == _player;

    // 只要这次真的看见了，就把这一刻的位置记下来——对应真实 idAI 在 CanSee() 命中之后
    // 调用 SetEnemyPosition() 更新 lastVisibleEnemyPos 的那一步（AI.cpp:3899-3905）。
    // _lastKnownPlayerPos 现在不再是一个只声明不使用的字段：9.2.1 节的听觉、9.3 节改版的
    // TickAlert()都会读它，作为"去看一眼最后出现的地方"这个搜索行为的目标点
    if (visible) _lastKnownPlayerPos = _player.GlobalPosition;

    return visible;
}
```

把 `_PhysicsProcess` 里 `GlobalPosition.DistanceTo(_player.GlobalPosition) < ChaseRange` 的判断换成 `CanSeePlayer()`。**这里有一个性能上的考量值得提前说一句**：`CanSeePlayer()` 每帧对每只怪物都做一次射线检测，怪物一多会有开销。第 12 章讲感知优化的时候会回头处理这个问题，现在关卡里怪物不多，先不用担心。

### 9.2.1 听觉：不是所有警觉都得靠眼睛

现在的 `CanSeePlayer()` 只处理"看见"这一种感知——但真实 DOOM 3 的 AI 会对声音起反应，最典型的场景就是你在拐角外开了一枪，隔着墙、根本看不到你的怪物却应声警觉。这不是靠怪物"听力范围内有没有响动"这种通用声音传播模拟做的，源码里的实现比想象中朴素得多：`idGameLocal::AlertAI(ent)`（`Game_local.cpp:3777-3786`）维护的是**一个全局的、只记"最近一次是谁弄出了动静"的单一槽位**——武器开火（`Weapon.cpp:3579,3762`）、投射物命中（`Projectile.cpp:1077`）、怪物受伤或死亡时反击的那个攻击者（`AI.cpp:3299` 的 `Pain()`、`AI.cpp:3430` 的 `Killed()`）都会调用这一个函数，把"是谁""什么时候"记到这一个全局槽位里，槽位只在**下一帧**内有效（`lastAIAlertTime = time + 1`）。每只 AI 每个 think 周期通过 `Event_HeardSound()`（`AI_events.cpp:512-526`）去问"这一帧全局槽位里记的这个人，在不在我的听觉范围（`AI_HEARING_RANGE`，`AI.h:45` 定义为 2048 个引擎单位）内、我对它是不是 `ATTACK_ON_SIGHT` 反应"，问到了就把这个实体当成"我听见的目标"处理。

> **这里有个容易想岔的地方要先说清楚**：这套机制**不区分"谁弄出的动静离怪物耳朵有多近"这件事之外的任何空间因素**——没有对声音做隔墙衰减、没有考虑传播路径会不会被建筑结构挡住（只用直线距离比较），也不会因为一次响动同时让好几只不同远近的怪物听到不同"清晰度"的声音。全局槽位一次只能记一个人、一帧只维持一帧，本质上是一套刻意从简的近似，不是真的听觉物理模拟。DOOM 3 之所以这样做也说得通：这是 2004 年的引擎，AI 数量和关卡复杂度都在可控范围内，没必要为声音单独跑一套传播模型。

把这套"全局槽位广播"原样搬过来做一个 Godot 版本，是一个独立的静态类——设计上刻意保持它跟 DOOM 3 一样简单，**不模拟隔墙衰减，只按直线距离判断**：

```csharp
// AIPerception.cs —— 独立文件，不属于任何一个 Enemy 实例；对应 idGameLocal::AlertAI/GetAlertEntity
// 这一整套"全局唯一噪声槽位"机制，不是每只怪物各自维护一份
using Godot;

public static class AIPerception
{
    private static Vector3 _noisePosition;
    private static double _noiseExpireTime = -1;

    // 武器开火、投射物命中、怪物死亡的惨叫——任何"值得让附近怪物听见"的事件都从这里报进来。
    // 有效期只有很短一瞬（这里用 0.15 秒模拟源码里"只在下一帧有效"的效果——Godot 的物理帧
    // 之间间隔比 DOOM3 引擎帧短很多，直接照抄"一帧"在这里没有意义，用一个小的固定时间窗替代）
    public static void RaiseNoise(Vector3 position)
    {
        _noisePosition = position;
        _noiseExpireTime = Time.GetTicksMsec() / 1000.0 + 0.15;
    }

    // listenerPos：这只怪物自己的位置；hearingRange 对应 AI_HEARING_RANGE，但做成可配置的，
    // 不同怪物、不同噪声大小理应有不同的听觉半径，不像源码写死一个全局常量
    public static bool TryGetRecentNoise(Vector3 listenerPos, float hearingRange, out Vector3 noisePos)
    {
        noisePos = _noisePosition;
        if (Time.GetTicksMsec() / 1000.0 > _noiseExpireTime) return false;
        return listenerPos.DistanceSquaredTo(_noisePosition) < hearingRange * hearingRange;
    }
}
```

要让这套系统真正触发，需要在两个地方各加一行调用：第 4/5 章开火命中判定成功的地方（不管命中与否，开枪这个动作本身就该被听见），以及本章 `Die()` 死亡的地方（对应源码 `Killed()` 里那次 `AlertAI`——同伴死亡的惨叫是很强的警觉信号）：

```csharp
// WeaponManager.cs（或你存放开火逻辑的地方）—— 在真正扣动扳机、播放枪声的那一行旁边加：
AIPerception.RaiseNoise(GlobalPosition);

// Enemy.cs 的 Die() 开头加：
AIPerception.RaiseNoise(GlobalPosition);
```

`Enemy` 这边只需要在没看见玩家的分支里加一次查询，9.3 节的状态机会把这次查询结果接到 `TickIdle()`（听到动静，从待机转警觉）里，这里先只展示查询本身长什么样，不重复贴 9.3 节已经写过的状态切换代码：

```csharp
// Enemy.cs 追加——9.3 节的 TickIdle() 会调用这个方法
private bool TryHearPlayer(out Vector3 noisePos)
{
    return AIPerception.TryGetRecentNoise(GlobalPosition, ChaseRange, out noisePos);
}
```

**这里也顺带回答一个常见问题**：这套机制算不算"怪物之间会互相通知"的群体协作？某种程度上算，但跟"A 怪物看见了玩家，主动告诉 B 怪物"完全是两回事——源码里没有任何"看见就广播"的逻辑，`AlertAI` 只在开火/受击/死亡这些**会产生响动**的事件上触发，纯粹靠"听力范围内共享同一个全局槽位"这个巧合让多只怪物同时反应过来，不是真正意义上的信息传递。9.4.4 节还会再回来讨论"怪物之间要不要协作"这个问题，那里要处理的是走位层面的协作，跟这里的听觉层面是两件不同的事。

### 9.3 状态机：从"追/打"两态，扩展成一套完整流程

真正的敌人行为不是非黑即白的"看见了就一直追"，而是一套流程：**待机（巡逻或站桩）→ 警觉（听到/瞥见动静，但还没确认）→ 战斗（确认看到目标，追击/攻击）→ 目标丢失后回到待机**。用一个枚举 + `switch` 实现：

```csharp
using Godot;

public partial class Enemy : CharacterBody3D
{
    private enum State { Idle, Alert, Combat, Dead }
    private State _state = State.Idle;

    // ...前面章节的字段不变，追加：
    [Export] public float AlertDuration = 2.0f;   // 警觉状态下，多久没看到目标就放弃、回到待机
    [Export] public float SearchArriveDistance = 1.0f;   // 搜索最后已知位置时，走到离这个点多近就算"到了"
    private double _lastSeenTime;

    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;
        Vector3 velocity = Velocity;
        if (!IsOnFloor()) velocity.Y -= Gravity * dt;
        Velocity = velocity;

        switch (_state)
        {
            case State.Idle:
                TickIdle();
                break;
            case State.Alert:
                TickAlert();
                break;
            case State.Combat:
                TickCombat(dt);
                break;
            case State.Dead:
                MoveAndSlide();
                return;
        }

        MoveAndSlide();
    }

    private void TickIdle()
    {
        if (CanSeePlayer())
        {
            _state = State.Combat;
            _lastSeenTime = Time.GetTicksMsec() / 1000.0;
            GD.Print($"{Name}：发现目标，进入战斗状态");
        }
        else if (TryHearPlayer(out Vector3 noisePos))
        {
            // 9.2.1 节的听觉：看不见，但听见了枪声/同伴惨叫——先不直接进战斗（毕竟还没亲眼确认），
            // 转警觉，去响动发生的地方看一眼
            _state = State.Alert;
            _lastSeenTime = Time.GetTicksMsec() / 1000.0;
            _lastKnownPlayerPos = noisePos;
            GD.Print($"{Name}：听到动静，前去查看");
        }
    }

    private void TickAlert()
    {
        if (CanSeePlayer())
        {
            _state = State.Combat;
            _lastSeenTime = Time.GetTicksMsec() / 1000.0;
            return;
        }

        double now = Time.GetTicksMsec() / 1000.0;
        if (now - _lastSeenTime > AlertDuration)
        {
            _state = State.Idle;
            _lastKnownPlayerPos = null;
            Velocity = new Vector3(0, Velocity.Y, 0);
            GD.Print($"{Name}：搜索无果，恢复待机");
            return;
        }

        // 警觉状态不再是"原地站桩等超时"——去 _lastKnownPlayerPos 记的最后位置看一眼，
        // 这是本教程自己设计的搜索策略：源码里"要不要去搜、搜多久放弃"这套决策写在
        // 拿不到的 .script 文件里，这里借用的只是真实存在的引擎原语——
        // "记住最后一次确认目标所在的位置"这个概念本身（对应 idAI::lastVisibleEnemyPos）
        if (_lastKnownPlayerPos.HasValue)
        {
            Vector3 target = _lastKnownPlayerPos.Value;
            float distToTarget = GlobalPosition.DistanceTo(target);
            if (distToTarget > SearchArriveDistance)
            {
                _navAgent.TargetPosition = target;
                if (!_navAgent.IsNavigationFinished())
                {
                    Vector3 nextPos = _navAgent.GetNextPathPosition();
                    Vector3 dir = (nextPos - GlobalPosition); dir.Y = 0; dir = dir.Normalized();
                    Velocity = new Vector3(dir.X * MoveSpeed * 0.6f, Velocity.Y, dir.Z * MoveSpeed * 0.6f);
                    if (dir.LengthSquared() > 0.01f)
                        LookAt(new Vector3(GlobalPosition.X + dir.X, GlobalPosition.Y, GlobalPosition.Z + dir.Z), Vector3.Up);
                    return;
                }
            }
            // 已经走到（或者本来就够近）——原地观望，把剩下的 AlertDuration 当"看了一圈没找到人"的耐心值花掉，
            // 而不是一到目标点就立刻掉头回待机，那样看起来会像"瞬移式"的机械感
            Velocity = new Vector3(0, Velocity.Y, 0);
        }
    }

    private void TickCombat(float dt)
    {
        if (CanSeePlayer())
        {
            _lastSeenTime = Time.GetTicksMsec() / 1000.0;
            ChaseAndAttack(dt);
        }
        else
        {
            Velocity = new Vector3(0, Velocity.Y, 0);
            _state = State.Alert;
            GD.Print($"{Name}：看不见目标了，转为警觉");
        }
    }

    private void ChaseAndAttack(float dt)
    {
        _navAgent.TargetPosition = _player.GlobalPosition;
        if (!_navAgent.IsNavigationFinished())
        {
            Vector3 nextPos = _navAgent.GetNextPathPosition();
            Vector3 direction = (nextPos - GlobalPosition);
            direction.Y = 0;
            direction = direction.Normalized();
            Velocity = new Vector3(direction.X * MoveSpeed, Velocity.Y, direction.Z * MoveSpeed);
            if (direction.LengthSquared() > 0.01f)
            {
                LookAt(new Vector3(GlobalPosition.X + direction.X, GlobalPosition.Y, GlobalPosition.Z + direction.Z), Vector3.Up);
            }
        }
        TryAttack();
    }
}
```

`_state = State.Dead` 那个分支现在还没有真正被设置（`Die()` 方法目前还是直接 `QueueFree()`），先把架子搭好，第 10 章讲布娃娃死亡的时候会用到"进入 Dead 状态但先不移除节点，播放完死亡表现再移除"这个流程，到时候回头改 `Die()`。

上面的 `TickAlert()` 已经不是最初那种"原地站着等超时"的写法了——丢了视线之后，怪物会真的走到 `_lastKnownPlayerPos` 记录的最后位置去看一眼，找不到人才会在 `AlertDuration` 耗尽后放弃、回到待机，这比"一断线就傻站着，掉线秒数一到就瞬间恢复原样"看起来更像"在找你"而不是一个卡在计时器上的木偶。

这个状态机现在只有 4 个状态、转移条件也很简单，但这已经是一个**可以无限扩展的骨架**——想加"受伤后短暂僵直"这类行为，都是往 `switch` 里加一个新状态分支，不需要推倒重来（9.5 节马上就会把"受伤后短暂僵直"这一条填上）。

**这里主动回答一个读者可能会问、但本教程没有实现的状态**：要不要加一个"血量低于某个比例就转身逃跑"的 `Flee` 状态？先说源码事实——查遍这份源码目录的 `AI.cpp`/`AI.h`/`Actor.cpp`/`Actor.h`，**找不到任何跟"逃跑"相关的字段、状态或 spawnArg**（没有 `flee`、没有 `Fly_Turn`、没有类似的东西）。这不代表 DOOM 3 里所有怪物都是"死战到底"——但凡真有哪只怪物会在濒死时转身逃跑，这个行为只可能写在拿不到的 `.script` 文件里，C++ 引擎层没有为这件事提供任何专门支持，纯粹是脚本作者自己在状态机里判断血量、切换动画和移动目标做出来的效果，跟其他"够不着的招式就走位"这类判断没有本质区别，只是没有一个专门的引擎原语。所以本教程选择不做这个状态，不是因为它没用，而是因为**它不需要任何新的引擎层支持，加不加纯粹是内容量的取舍**——如果你想要，思路是：在 9.5 节 `TryPain()` 或者 `TakeDamage()` 里加一个"血量低于阈值且这只怪物标记为'可逃跑'"的判断，命中就把 `_state` 切到一个新的 `Flee` 状态，`TickFlee()` 里把 `_navAgent.TargetPosition` 设成"背离玩家方向、离得尽可能远的一个可行走点"（可以简单地取"当前位置沿着背离玩家的方向延伸一段距离"这条线段的终点，交给 `NavigationAgent3D` 去规划怎么走到附近），这个状态不需要引入任何新概念，全部是已经搭好的寻路 + 状态机骨架的重新组合。

### 9.4 完整近战判定：两阶段检测 + 保底不死

第 8.3 节的 `TryAttack()` 只做了一个简单的距离比较——这是"能跑起来"的最简版本，现在要把它换成 DOOM 3 `idAI::TestMelee()`/`AttackMelee()` 的完整逻辑，包含两个不能省略的东西：**两阶段检测**（先用一个粗糙的包围盒快速排除明显够不着的情况，通过了再做一次真正的视线连线确认），以及**保底不死机制**（低难度下，如果这一下会直接把玩家打死，有几率强制打空，避免"莫名其妙被秒杀"的挫败感）：

```csharp
// Enemy.cs 替换 TryAttack()——AttackDamage/AttackCooldown/_lastAttackTime 这三个字段
// 8.3 节已经声明过了，这里不重复声明，只追加两个新字段
private double _lastSavingThrowTime = -999;
private const double SavingThrowWindow = 5.0;   // 对应 DOOM3 的 SAVING_THROW_TIME：同一玩家 5 秒内只会被保底一次

// 对应 idAI::TestMelee()：第一阶段——粗糙的包围盒重叠检测
private bool TestMeleeBounds()
{
    if (_player == null) return false;
    Vector3 toPlayer = _player.GlobalPosition - GlobalPosition;
    bool withinHorizontal = new Vector2(toPlayer.X, toPlayer.Z).Length() <= AttackRange;
    bool withinVertical = Mathf.Abs(toPlayer.Y) <= 1.0f;
    return withinHorizontal && withinVertical;
}

// 第二阶段——粗过滤通过之后，再做一次真正的视线确认，理由和 9.2 节的 CanSeePlayer() 一致：
// 包围盒重叠不代表中间没有东西挡着（比如玩家正好绕着柱子躲）
private bool TestMeleeLineOfSight()
{
    var spaceState = GetWorld3D().DirectSpaceState;
    var query = PhysicsRayQueryParameters3D.Create(GlobalPosition + Vector3.Up, _player.GlobalPosition + Vector3.Up * 0.5f);
    query.CollisionMask = 0b0011;
    var result = spaceState.IntersectRay(query);
    return result.Count == 0 || (Node3D)result["collider"] == _player;
}

private void TryAttack()
{
    if (_player == null || !TestMeleeBounds() || !TestMeleeLineOfSight()) return;

    double now = Time.GetTicksMsec() / 1000.0;
    if (now - _lastAttackTime < AttackCooldown) return;
    _lastAttackTime = now;

    float finalDamage = AttackDamage * DifficultySettings.EnemyDamageMultiplier();

    // 保底不死：只在新兵/普通两档最低难度生效，且这一下必须会直接打死玩家才触发判断
    if (DifficultySettings.Current <= DifficultySettings.Level.Normal
        && _player.Call("WouldDieFrom", finalDamage).AsBool())
    {
        // 这一段逻辑对照源码（AI.cpp:4479-4496）修正过一次：不是单纯"5 秒内只保底一次"，
        // 而是"超过 5 秒没保底过，就重置计时器、这次一定保底；5 秒内如果距离上一次保底不到 1 秒，
        // 这次也保底（允许短时间内多个致命判定连续被保底）；否则（1-5 秒之间）正常命中，不再手下留情"
        double t = now - _lastSavingThrowTime;
        if (t > SavingThrowWindow)
        {
            _lastSavingThrowTime = now;
            t = 0;
        }
        if (t < 1.0)
        {
            GD.Print($"{Name}：保底判定生效，这一下打空了");
            return;
        }
    }

    if (_player.HasMethod("TakeDamage"))
    {
        _player.Call("TakeDamage", finalDamage);
    }
    GD.Print($"{Name} 攻击了玩家，造成 {finalDamage} 点伤害");
}
```

`PlayerController.cs` 需要补一个 `WouldDieFrom` 方法给上面的保底判断调用：

```csharp
// PlayerController.cs 追加
public bool WouldDieFrom(float incomingDamage)
{
    return Health - incomingDamage <= 0;
}
```

**这里的"两阶段"和"保底不死"都是真实存在于 DOOM 3 源码里、而不是本教程编出来凑数的机制**——包围盒粗筛 + 视线连线确认，是为了避免每次判断近战都做一次相对更贵的射线检测；保底不死则是一条明确写在源码里的"反挫败感"规则，只在最低两档难度生效，且同一玩家 5 秒内只保底一次。

### 9.4.1 招式表：一只怪物不该只有一个 AttackDamage

上面 9.4 节（往前追溯到 8.3 节）里，`Enemy` 全程只有一组孤零零的 `AttackDamage`/`AttackRange` 字段——这在"一只怪物只有一种打法"的时候够用，但这其实是一处刻意先留的简化，现在到了要还账的时候：真实 DOOM 3 里几乎没有怪物只有一种攻击。`idAI::AttackMelee(meleeDefName)`（`AI.cpp:4456`）、`Event_MeleeAttackToJoint(jointname, meleeDefName)`（`AI_events.cpp:812`）、`LaunchProjectile(jointname, target, clampToAttackCone)`（`AI.h:532`）、`Event_LaunchProjectile(entityDefName)`（`AI_events.cpp:682`）——这几个函数全都接受一个"名字"或"关节"参数，就是为了让同一只怪物的脚本能在多种攻击之间挑：一只怪物完全可以近战有"一拳"和"一抓"两种不同伤害/范围的打法，远程同时有普通子弹和会追踪的火球，各自绑在骨骼上不同的发射点、用不同的判定方式算命中。上面那种"一只怪物只有一个数"的写法，从一开始就没办法表达这件事——这也是接下来 9.8/9.9 节要动的地方：远程攻击和冲锋攻击都要基于同一张表来选，不能再各自抱着一套自己的字段。

把攻击信息拆成一张表，`Enemy` 挂几条这样的条目，而不是几个孤立字段：

```csharp
// AttackDefinition.cs —— 单独的 Resource，方便在编辑器里为同一只怪物配置多份，
// 也方便把同一份攻击定义在不同怪物之间共享
using Godot;

[GlobalClass]
public partial class AttackDefinition : Resource
{
    [Export] public string AttackName;
    [Export] public float Damage;
    [Export] public float Range;

    // 眼到眼连线（对应 TestMelee 那种全身通用的粗判定）还是从指定关节算起
    // （对应 Event_MeleeAttackToJoint 的 jointname 参数，比如"这一下是用爪子挥的，判定要从爪子的位置算"）
    public enum HitTraceType { EyeToEye, JointOrigin }
    [Export] public HitTraceType TraceType = HitTraceType.EyeToEye;

    [Export] public Node3D JointOrigin;    // TraceType 为 JointOrigin 时使用：挂在怪物骨骼下的 BoneAttachment3D，
                                            // 是 Godot 里最接近 DOOM3 jointname 参数的东西——一个具名的、跟着骨骼动的空间点
    [Export] public string AnimationName;  // 这次攻击对应哪一段动画/触发帧，留给动画系统去消费，这里不展开
    [Export] public bool AllowSavingThrow = true;   // 这次攻击要不要走 9.4 节的保底不死判定——9.9 节的冲锋攻击会绕开它
    [Export] public PackedScene ProjectileScene;    // 留空 = 近战；非空 = 远程，实例化这个场景当投射物
}
```

> 这里有个 Godot 特有的小别扭要提一句：`Resource` 本来的设计意图是"可以在多个实例之间共享的资产数据"，而 `JointOrigin` 这种直接指向某个具体场景节点的引用，理论上更应该用 `NodePath`（真正用到的时候再相对怪物自身 `GetNode<Node3D>(path)` 取出来），不然一份 `AttackDefinition` 资源实际上就绑死给了某一个具体的怪物实例，没法真的被多只怪物复用。这里为了跟上面列出的字段结构保持一致，先按 `Node3D` 写；如果你想要"资源真的能被复用"这个特性，把 `JointOrigin` 换成 `NodePath` 是更地道的做法。

`Enemy.cs` 把散落的单一攻击字段换成一张表：

```csharp
// Enemy.cs —— 用这个字段替换掉 8.3 节引入的 AttackDamage/AttackRange
[Export] public Godot.Collections.Array<AttackDefinition> Attacks = new();
```

`TestMeleeBounds()`/`TestMeleeLineOfSight()`/`TryAttack()` 也要跟着换成按表挑选，而不是读那几个已经不存在的单一字段——下面是这三个函数对应 9.4 节版本的替换版：

```csharp
// Enemy.cs —— 替换 9.4 节的 TestMeleeBounds()/TestMeleeLineOfSight()/TryAttack()
private bool TestMeleeBounds(AttackDefinition attack)
{
    if (_player == null) return false;
    Vector3 toPlayer = _player.GlobalPosition - GlobalPosition;
    bool withinHorizontal = new Vector2(toPlayer.X, toPlayer.Z).Length() <= attack.Range;
    bool withinVertical = Mathf.Abs(toPlayer.Y) <= 1.0f;
    return withinHorizontal && withinVertical;
}

private bool TestMeleeLineOfSight(AttackDefinition attack)
{
    // TraceType 决定射线从哪里发出——EyeToEye 用怪物自己头顶附近的位置（原来的写法），
    // JointOrigin 则改用这条攻击指定的关节位置，比如"这一下是爪击，判定要从爪子那里算"
    Vector3 origin = (attack.TraceType == AttackDefinition.HitTraceType.JointOrigin && attack.JointOrigin != null)
        ? attack.JointOrigin.GlobalPosition
        : GlobalPosition + Vector3.Up;

    var spaceState = GetWorld3D().DirectSpaceState;
    var query = PhysicsRayQueryParameters3D.Create(origin, _player.GlobalPosition + Vector3.Up * 0.5f);
    query.CollisionMask = 0b0011;
    var result = spaceState.IntersectRay(query);
    return result.Count == 0 || (Node3D)result["collider"] == _player;
}

// 从 Attacks 表里挑一条当前可用的近战招式——远程条目（ProjectileScene 非空）留给 9.8 节的
// PickRangedAttack() 处理，这里只看近战条目
private AttackDefinition PickMeleeAttack()
{
    if (_player == null) return null;
    foreach (var attack in Attacks)
    {
        if (attack.ProjectileScene != null) continue;
        if (TestMeleeBounds(attack) && TestMeleeLineOfSight(attack)) return attack;
    }
    return null;
}

// 把"挑出一条能用的招式"和"真正执行这条招式、算保底不死"拆成两个函数——
// 9.9 节的冲锋攻击会复用 TestMeleeBounds/TestMeleeLineOfSight，但不会调用这个函数，
// 因为冲锋伤害不走保底判定这条路（原因见 9.9 节）
private void ExecuteMeleeAttack(AttackDefinition attack)
{
    float finalDamage = attack.Damage * DifficultySettings.EnemyDamageMultiplier();

    if (attack.AllowSavingThrow
        && DifficultySettings.Current <= DifficultySettings.Level.Normal
        && _player.Call("WouldDieFrom", finalDamage).AsBool())
    {
        double now = Time.GetTicksMsec() / 1000.0;
        double t = now - _lastSavingThrowTime;
        if (t > SavingThrowWindow) { _lastSavingThrowTime = now; t = 0; }
        if (t < 1.0)
        {
            GD.Print($"{Name}：保底判定生效，这一下打空了");
            return;
        }
    }

    if (_player.HasMethod("TakeDamage")) _player.Call("TakeDamage", finalDamage);
    GD.Print($"{Name} 使用 {attack.AttackName} 攻击了玩家，造成 {finalDamage} 点伤害");
}

private void TryAttack()
{
    var attack = PickMeleeAttack();
    if (attack == null) return;

    double now = Time.GetTicksMsec() / 1000.0;
    if (now - _lastAttackTime < AttackCooldown) return;
    _lastAttackTime = now;

    ExecuteMeleeAttack(attack);
}
```

这一版 `TryAttack()` 还只会挑近战条目——9.8 节马上会把它再扩展一层，加上远程分支。

### 9.4.2 攻击选择：多种招式该怎么选

上面 `PickMeleeAttack()` 的挑选规则其实只有一条："表里第一条距离够得着、视线也通的近战条目，就用它"。这在只有一种攻击的时候没问题，但只要一只怪物同时有两种够得着的招式——比如近距离的爪击和稍远一点的地面震击——这种写法会导致它几乎永远只用排在 `Attacks` 数组里靠前的那一条：只要两者的可用距离有重叠，前面那条永远先满足条件、先被返回，后面那条形同摆设。这不是真的"选择"，只是"数组顺序优先"，玩家打几次就会发现这只怪物其实只有一种真正会用的招式。

真实 DOOM 3 确实有一个跟这个问题相关的查询：`idAI::Event_EnemyRange()`/`Event_EnemyRange2D()`（`AI_events.cpp:1460-1491`，声明在 `AI.h:623-624`）。**这里要先纠正一处容易读错的细节**：这两个函数本身**返回的是一个原始浮点距离值**（`idThread::ReturnFloat(dist)`，就是怪物到目标的欧几里得距离，2D 版本只是去掉了高度分量），并不会在 C++ 这一层就把距离"归"成近战/中距/远距这样的档位——真正的分档判断（比如"距离小于 X 算近战、大于 Y 算远程"）是脚本代码拿到这个原始距离之后自己写的 if/else，而脚本文件不在本教程能拿到的这份源码目录里。所以准确的说法是：**C++ 引擎层只提供了"查距离"这一个原语，"按距离分档"这件事完全是脚本层的策略**，不是 `Event_EnemyRange` 自己做的。

> **要老实说清楚这里的边界**：既然分档逻辑本身不可见，下面新增的 `MinRange`（下限）配合 9.4.1 已有的 `Range` 字段（现在承担上限的角色）这套分档字段，是拿 `Event_EnemyRange` 这个真实存在的"查询怪物到目标距离"原语作为出发点，在 Godot 这边自己设计的一套通用分档系统——具体的数值边界和"档位内怎么再挑"这套权重随机策略，都是本教程自己定的合理选择，不是从源码脚本里扒出来的。如果你手上有这些怪物真正的 `.script` 文件，应该以那里面写的分档边界和挑选规则为准。

把这两件事——"这条攻击够不够得着"和"这条攻击是不是还在冷却"——都变成 `AttackDefinition` 自己的属性，再加一个权重字段，给 9.4.1 定义的 `AttackDefinition` 追加三个字段：

```csharp
// AttackDefinition.cs —— 追加到 9.4.1 节的类定义里
[Export] public float MinRange = 0f;   // 这条攻击只在 [MinRange, Range] 这个区间内才算"够得着"——
                                        // Range 字段沿用 9.4.1 的定义，现在承担"区间上限"这个角色
[Export] public float Cooldown = 1.5f; // 这条攻击自己的冷却时间，不再共用 Enemy 身上那一个全局冷却
[Export] public float Weight = 1.0f;   // 同一时刻有多条攻击都够格时，按这个权重做加权随机挑选
```

`Cooldown` 是这条攻击"多久能再打一次"的设计时长，但"这条攻击上一次是什么时候打的"是运行时才有的状态，不能直接存在 `AttackDefinition` 上——`AttackDefinition` 是 `Resource`，同一份资源理论上可能被多只怪物实例共享（9.4.1 节已经提过这一点），如果把"上次使用时间"这种运行时状态也塞进 `Resource` 里，多只怪物共用同一条 `AttackDefinition` 时就会互相污染对方的冷却计时。所以运行时的"上次使用时间"改存在 `Enemy` 自己身上，用一个以 `AttackDefinition` 为 key 的字典：

```csharp
// Enemy.cs 追加
using System.Collections.Generic;   // Dictionary<TKey, TValue> 需要这个命名空间，别忘了加在文件顶部

private readonly Dictionary<AttackDefinition, double> _attackReadyTime = new();

private bool IsAttackOffCooldown(AttackDefinition attack, double now)
{
    return !_attackReadyTime.TryGetValue(attack, out double readyAt) || now >= readyAt;
}

private void MarkAttackUsed(AttackDefinition attack, double now)
{
    _attackReadyTime[attack] = now + attack.Cooldown;
}

// 上一次真正打出去的是哪一条攻击——WeightedPick 用它做"不要连续两次挑同一招"的抑制。
// 这条状态跟 _attackReadyTime 一样存在 Enemy 身上，不存 AttackDefinition：理由相同，
// 避免多只怪物共享同一份 Resource 时互相污染
private AttackDefinition _lastUsedAttack;

// 从一组已经确认合格的候选里按 Weight 做加权随机挑选——候选只有 0/1 个时直接短路，不用真的掷骰子。
// **这一条"压低上一次用过的招式权重"的规则不是从源码里扒出来的**：DOOM3 的 C++ 层完全没有
// "记住上一次用了哪招、这次少选它"这种概念（AI.h/AI.cpp 通篇没有类似 lastAttack 的字段），
// 这件事如果存在，只会是某只怪物的 .script 自己写的、我们拿不到。这里加它纯粹是因为
// 玩家对"同一只怪物反复甩同一个动作"的重复感很敏感，是本教程从"这套系统已经是自己设计的
// 引申"这个既有边界内，为了打起来更耐玩而加的一条工程经验规则，不是在冒充源码行为
private static AttackDefinition WeightedPick(List<AttackDefinition> candidates, AttackDefinition lastUsed)
{
    if (candidates.Count == 0) return null;
    if (candidates.Count == 1) return candidates[0];

    const float RepeatPenalty = 0.35f;   // 上一次用过的招式，这一轮权重打这个折扣，不是直接排除——
                                          // 排除会导致"只有这一招够格"时它反而永远选不中，打折才对

    float totalWeight = 0f;
    foreach (var c in candidates)
    {
        float w = Mathf.Max(c.Weight, 0f);
        if (c == lastUsed) w *= RepeatPenalty;
        totalWeight += w;
    }
    if (totalWeight <= 0f) return candidates[(int)(GD.Randi() % (uint)candidates.Count)];   // 权重全是 0，退化成纯随机

    float roll = (float)GD.RandRange(0.0, (double)totalWeight);
    float cumulative = 0f;
    foreach (var c in candidates)
    {
        float w = Mathf.Max(c.Weight, 0f);
        if (c == lastUsed) w *= RepeatPenalty;
        cumulative += w;
        if (roll <= cumulative) return c;
    }
    return candidates[^1];   // 浮点误差兜底
}
```

有了这两块之后，`TestMeleeBounds()`/`PickMeleeAttack()`/`TryAttack()` 也要跟着换成认区间、认冷却、认权重的版本：

```csharp
// Enemy.cs —— 替换 9.4.1 节的 TestMeleeBounds()/PickMeleeAttack()/TryAttack()
// TestMeleeLineOfSight() 不用变，这次改动跟视线判定无关
private bool TestMeleeBounds(AttackDefinition attack)
{
    if (_player == null) return false;
    Vector3 toPlayer = _player.GlobalPosition - GlobalPosition;
    float horizontalDistance = new Vector2(toPlayer.X, toPlayer.Z).Length();
    // 现在同时检查下限：太近也可能打不到（比如一把设计上不该贴脸使用的地面震击）
    bool withinRange = horizontalDistance >= attack.MinRange && horizontalDistance <= attack.Range;
    bool withinVertical = Mathf.Abs(toPlayer.Y) <= 1.0f;
    return withinRange && withinVertical;
}

private AttackDefinition PickMeleeAttack()
{
    if (_player == null) return null;
    double now = Time.GetTicksMsec() / 1000.0;

    var eligible = new List<AttackDefinition>();
    foreach (var attack in Attacks)
    {
        if (attack.ProjectileScene != null) continue;   // 远程条目交给 9.8 节的 PickRangedAttack()
        if (!IsAttackOffCooldown(attack, now)) continue;
        if (!TestMeleeBounds(attack) || !TestMeleeLineOfSight(attack)) continue;
        eligible.Add(attack);
    }
    return WeightedPick(eligible, _lastUsedAttack);
}

// 挑出一条攻击、标记它进入冷却、执行——三件事收在一起，避免"挑了但没用上却已经标记冷却"这种不一致
private AttackDefinition TryAttack()
{
    var attack = PickMeleeAttack();
    if (attack == null) return null;

    double now = Time.GetTicksMsec() / 1000.0;
    MarkAttackUsed(attack, now);
    _lastUsedAttack = attack;
    ExecuteMeleeAttack(attack);
    return attack;
}
```

`TryAttack()` 的返回类型从 `void` 换成了 `AttackDefinition`（成功打出的那一条，没打出就是 `null`）——之前所有把它当 `TryAttack();` 这样单独一行调用的地方（8.2/9.3 的 `ChaseAndAttack()`）不用改，C# 允许忽略一个方法的返回值；9.4.3 节的走位逻辑会开始用上这个返回值。

**这里有一个值得记住的设计判断**：没有区间和权重之前，一只同时有爪击（近）和地震波（稍远）的怪物，实际上打起来只会用爪击——因为两者射程有重叠、爪击排在数组前面，地震波永远选不到。加上 `MinRange`/`Range` 分档之后，贴脸时段两者可能都够格，靠 `Weight` 做加权随机，怪物不会每次都用同一招；拉开到只有地震波够格的距离时，爪击因为超出了自己的 `Range` 自然被过滤掉，地震波才会被选中。同一套判断逻辑，不需要写"这只怪物是不是该用远一点的招式"这种专门的 if 分支。

至此，8.3/9.4 节引入的 `Enemy.AttackCooldown`/`_lastAttackTime` 这一对共享冷却字段已经完全没有代码在用了（9.4.1 节的 `TryAttack()` 还引用过它们，但这里已经被整体替换掉）——可以直接从 `Enemy.cs` 里删掉，冷却现在完全由 `_attackReadyTime` 字典按每条攻击各自记账。9.8 节的远程分支和它自己的 `PickRangedAttack()`，也要跟着换成同一套区间 + 冷却 + 权重的判断，下面 9.8 节的代码已经同步改过来了。

### 9.4.3 走位：不是每次都该站着打

**先把这一节的定位说清楚，避免造成"这是在照抄 DOOM 3 源码"的误解**：这一节实现的实时走位系统是本教程自己设计的，不是 DOOM 3 源码的移植。真实 DOOM 3 的怪物定位系统是 `idCombatNode`（`AI.h:702`）——关卡设计师在编辑器里手动摆放一个个"战斗节点"，每个节点标注好朝向、可视范围、这个节点适合用来打近战还是远程，怪物通过 `Event_FindEnemyInCombatNodes()`/`Event_GetCombatNode()` 去找一个当前能用的节点、走过去、站在那儿打——本质上是**关卡设计师预先摆好的静态位置**，不是怪物自己实时计算"我现在该往哪挪"。这一节选择不做 `idCombatNode` 那一套（做那一套意味着教程的关卡搭建部分要多一大块"如何摆放战斗节点"的内容，也意味着怪物离开预先设计好的战斗节点密集区域就完全不会走位），而是设计一套不依赖关卡预置数据、单靠上面 9.4.2 节引入的距离分档概念就能实时算出该往哪走的系统。这套系统在**"用距离分档决定行为"这个思路**上是受 `Event_EnemyRange` 启发的，但具体走位的实时计算方式没有任何源码依据，请不要把接下来的代码当成 `idCombatNode` 的还原来理解。

现在 9.3 节的状态机里，`TickCombat()` 只会做两件事：追过去、够得着就打。但如果一只纯远程怪物的目标欺身贴到脸上、或者一只纯近战怪物够不到目标，`Attacks` 表里没有一条能用的招式，怪物应该先挪到"能打"的距离上，而不是站在原地干等目标自己送上门（或者傻乎乎地继续追、追到贴脸了才发现自己是个只会打远处的怪物）。给状态机加一个 `Reposition` 状态：

```csharp
// Enemy.cs —— 状态枚举追加 Reposition，_PhysicsProcess() 的 switch 追加对应分支
private enum State { Idle, Alert, Combat, Reposition, Dead }

// _PhysicsProcess() 里的 switch 追加一支：
//     case State.Reposition:
//         TickReposition(dt);
//         break;
// （插入位置在 case State.Combat 和 case State.Dead 之间，其余分支不变）
```

判断"该不该走位"，靠的是 9.4.2 节引入的区间 + 冷却概念——不看某一条具体攻击够不够格（那是 `PickMeleeAttack`/`PickRangedAttack` 内部的事），只看**整张 `Attacks` 表里，有没有任何一条在当前距离的区间内、且没在冷却**：

```csharp
// Enemy.cs 追加——不区分近战/远程，只要区间和冷却都满足就算数，
// 视线/包围盒这些更细的判定交给真正执行攻击时的 TestMeleeBounds/TestMeleeLineOfSight
private bool HasEligibleAttack(float distance, double now)
{
    foreach (var attack in Attacks)
    {
        if (distance < attack.MinRange || distance > attack.Range) continue;
        if (!IsAttackOffCooldown(attack, now)) continue;
        return true;
    }
    return false;
}
```

`TickCombat()` 现在多一步判断：够不到任何招式就转去 `Reposition`；够得到就照常追打，并且——这是这一节要解决的第二个问题——**哪怕这一刻能打，也不该每次都笔直冲上去站定不动地打**。怪物刚打出一发攻击、这条攻击进了冷却的瞬间，有一定概率不站着傻等冷却转好，而是侧身移动一小段时间，让自己看起来像在寻找角度而不是靶场里的固定木桩：

```csharp
// Enemy.cs 替换 9.3 节的 TickCombat()，ChaseAndAttack() 也一并替换
[Export] public float StrafeChance = 0.35f;    // 每次成功打出一次攻击后，触发侧移的概率
[Export] public float StrafeDuration = 0.6f;   // 侧移持续多久
[Export] public float StrafeSpeed = 2.0f;      // 侧移的横向速度，刻意比 MoveSpeed 慢一些，别让它看起来在平移传送
private double _strafeUntil;
private int _strafeDir = 1;   // -1 = 向左，1 = 向右

private void TickCombat(float dt)
{
    if (!CanSeePlayer())
    {
        Velocity = new Vector3(0, Velocity.Y, 0);
        _state = State.Alert;
        GD.Print($"{Name}：看不见目标了，转为警觉");
        return;
    }

    _lastSeenTime = Time.GetTicksMsec() / 1000.0;
    double now = _lastSeenTime;
    float distance = GlobalPosition.DistanceTo(_player.GlobalPosition);

    if (!HasEligibleAttack(distance, now))
    {
        _state = State.Reposition;
        GD.Print($"{Name}：当前距离没有能用的招式，开始走位");
        return;
    }

    ChaseAndAttack(dt);
}

private AttackDefinition ChaseAndAttack(float dt)
{
    Vector3 moveDir = Vector3.Zero;
    _navAgent.TargetPosition = _player.GlobalPosition;
    if (!_navAgent.IsNavigationFinished())
    {
        Vector3 nextPos = _navAgent.GetNextPathPosition();
        moveDir = (nextPos - GlobalPosition); moveDir.Y = 0; moveDir = moveDir.Normalized();
    }

    double now = Time.GetTicksMsec() / 1000.0;
    Vector3 strafeVel = Vector3.Zero;
    if (now < _strafeUntil)
    {
        Vector3 toPlayer = _player.GlobalPosition - GlobalPosition; toPlayer.Y = 0;
        Vector3 lateral = toPlayer.Normalized().Cross(Vector3.Up) * _strafeDir;
        strafeVel = lateral * StrafeSpeed;
    }

    Velocity = new Vector3(moveDir.X * MoveSpeed + strafeVel.X, Velocity.Y, moveDir.Z * MoveSpeed + strafeVel.Z);

    Vector3 faceDir = _player.GlobalPosition - GlobalPosition; faceDir.Y = 0;
    if (faceDir.LengthSquared() > 0.01f)
        LookAt(new Vector3(GlobalPosition.X + faceDir.X, GlobalPosition.Y, GlobalPosition.Z + faceDir.Z), Vector3.Up);

    var fired = TryAttack();
    if (fired != null) MaybeStartStrafe(now);   // 这一下打出去了，赌一把接下来要不要侧移
    return fired;
}

private void MaybeStartStrafe(double now)
{
    if (now < _strafeUntil) return;         // 已经在侧移窗口里，不重复触发
    if (GD.Randf() > StrafeChance) return;
    _strafeUntil = now + StrafeDuration;
    _strafeDir = GD.Randf() < 0.5f ? -1 : 1;
}
```

> **这里要老实标一处简化**：上面的侧移只是在寻路给出的前进方向上叠加一个横向速度分量，本身没有再单独做一次墙体检测——之所以敢这么简化，是因为侧移幅度小（`StrafeSpeed` 刻意调得比 `MoveSpeed` 慢）、持续时间短（`StrafeDuration` 默认 0.6 秒），贴墙侧移撞墙的后果通常只是"这半秒卡在墙边空转轮子"，不会走出很离谱的路线。如果你的关卡有很多贴身的窄道或者掩体，想让侧移也严格不穿墙、不躲进会挡视线的掩体后面，可以在 `strafeVel` 生效前再加一次沿侧移方向的短距离射线/胶囊体检测（做法跟 9.9 节 `IsChargePathClear()` 的胶囊体扫掠是同一个思路），这里为了保持这一节的重点在"该不该走位、怎么决定走位方向"上，没有把这层检测也叠上去。

再实现"够不到任何招式，该往哪挪"这部分——`TickReposition()`。它复用 9.1 节的 `NavigationAgent3D` 寻路（保证不会穿墙走直线），只是把寻路目标从"玩家所在的位置"换成"玩家和自己连线上、落在某条还没冷却的招式的可用区间里的一个点"：

```csharp
// Enemy.cs 追加
[Export] public float RepositionStuckTimeout = 3.0f;   // 走位走了这么久还是够不到任何招式，就认定是被困住了
private double _repositionSince = -1;

private void TickReposition(float dt)
{
    if (!CanSeePlayer())
    {
        Velocity = new Vector3(0, Velocity.Y, 0);
        _state = State.Alert;   // 走位途中跟丢了视线，跟 9.3 节 TickCombat() 一样转警觉，而不是继续瞎走
        _repositionSince = -1;
        return;
    }

    double now = Time.GetTicksMsec() / 1000.0;
    float distance = GlobalPosition.DistanceTo(_player.GlobalPosition);

    if (HasEligibleAttack(distance, now))
    {
        _state = State.Combat;   // 已经挪到能打的距离了，交回 TickCombat() 正常追打
        _repositionSince = -1;
        return;
    }

    if (_repositionSince < 0) _repositionSince = now;

    // 被困住的兜底——走位走了超过 RepositionStuckTimeout 还是找不到能打的距离（典型场景：
    // 被玩家逼到死角、周围没有空间可退，NavigationAgent3D 规划出的目标点根本走不到），
    // 与其让怪物永远站在原地当一动不动的活靶子，不如豁出去用够得上视线判定的招式硬打一下——
    // 忽略冷却和区间下限，只要 TestMeleeBounds/TestMeleeLineOfSight 还成立就行。
    // 这条兜底目前只看近战条目——9.8 节加入远程之后，会把它换成同时也看远程条目的版本
    if (now - _repositionSince > RepositionStuckTimeout)
    {
        var desperate = PickDesperateMeleeAttack();
        if (desperate != null)
        {
            GD.Print($"{Name}：走位被困住了，豁出去用 {desperate.AttackName} 硬打一下");
            MarkAttackUsed(desperate, now);
            _lastUsedAttack = desperate;
            ExecuteMeleeAttack(desperate);
            _repositionSince = now;   // 打完这一下重新计时，别每帧都硬打
            return;
        }
    }

    // 在所有还没冷却的招式里，找一个"离当前距离最近"的区间去靠——
    // 太近就退到某条招式的 MinRange，太远就冲到某条招式的 Range 以内
    float targetDistance = distance;
    float bestDelta = float.MaxValue;
    bool found = false;
    foreach (var attack in Attacks)
    {
        if (!IsAttackOffCooldown(attack, now)) continue;
        float clamped = Mathf.Clamp(distance, attack.MinRange, attack.Range);
        float delta = Mathf.Abs(clamped - distance);
        if (delta < bestDelta) { bestDelta = delta; targetDistance = clamped; found = true; }
    }

    if (!found)
    {
        // 所有招式都在冷却，没有"该往哪挪"的依据——原地不动等某条招式冷却转好，
        // 比朝着一个瞎猜的方向乱走更合理
        Velocity = new Vector3(0, Velocity.Y, 0);
        return;
    }

    Vector3 toPlayer = _player.GlobalPosition - GlobalPosition; toPlayer.Y = 0;
    Vector3 desiredPos = _player.GlobalPosition - toPlayer.Normalized() * targetDistance;

    _navAgent.TargetPosition = desiredPos;   // 交给 9.1 节的寻路系统去规划怎么走到这个点，不会直接穿墙
    if (!_navAgent.IsNavigationFinished())
    {
        Vector3 nextPos = _navAgent.GetNextPathPosition();
        Vector3 dir = (nextPos - GlobalPosition); dir.Y = 0; dir = dir.Normalized();
        Velocity = new Vector3(dir.X * MoveSpeed, Velocity.Y, dir.Z * MoveSpeed);
        if (dir.LengthSquared() > 0.01f)
            LookAt(new Vector3(GlobalPosition.X + dir.X, GlobalPosition.Y, GlobalPosition.Z + dir.Z), Vector3.Up);
    }
    else
    {
        Velocity = new Vector3(0, Velocity.Y, 0);
    }
}

// 兜底攻击：无视冷却和 MinRange，只要包围盒 + 视线这套最基本的命中判定还成立就行——
// 目的不是"选出最合适的招式"，是"总得打点什么，不能傻站着挨打"
private AttackDefinition PickDesperateMeleeAttack()
{
    foreach (var attack in Attacks)
    {
        if (attack.ProjectileScene != null) continue;
        if (TestMeleeBounds(attack) && TestMeleeLineOfSight(attack)) return attack;
    }
    return null;
}
```

这个状态和 9.1/9.2 节已经搭好的地基严丝合缝地接上了：走位目标点是交给 `NavigationAgent3D` 去规划的，不是自己算一条直线杵过去，所以不会出现"为了拉开距离一头扎进墙里"这种情况；跟丢视线时统一走 `State.Alert` 这条已有的路径，不需要在 `Reposition` 状态里单独写一套"目标去哪了"的处理逻辑。至此，`Enemy` 的状态机变成了 `Idle → Alert → Combat ⇄ Reposition → Dead` 这五态，`Combat` 和 `Reposition` 之间可以来回切换，具体切哪边完全由"当前距离有没有一条还没冷却的招式够得着"这一个统一的判断驱动。

**这里回答一个容易被忽略的边界情况**：如果一只怪物被逼到角落、`HasEligibleAttack()` 一直是 `false`（比如所有招式都在冷却，或者所有招式的 `MinRange` 都大于当前能退到的最大距离），没有 `RepositionStuckTimeout` 这道兜底的话，它会永远停在 `Reposition` 状态里反复计算"该往哪挪"却又哪儿都挪不动，变成一个只会挨打不会还手的活靶子——这在设计上是站不住脚的：玩家会很快发现"把这只怪物逼到墙角"是一个稳赢的技巧，而不是靠正常输出赢下战斗。加上这道超时兜底之后，被困住太久就会豁出去硬打一下，至少不会看起来像卡死了。

### 9.4.4 群体：不止一只怪物同时走位会怎样

前面几节的走位系统全程只考虑了"这一只怪物和玩家之间"的关系——`TickReposition()` 算 `desiredPos` 只看自己到玩家的连线，完全不知道场景里还有没有其他怪物、它们此刻在往哪儿挪。这在只有一只怪物的战斗里没问题，但 FPS 里非常常见的"一群怪物同时围上来"场景会立刻暴露问题：如果两三只同一种怪物几乎同时判定"该走位了"，而它们各自到玩家的连线方向差不多，`Mathf.Clamp(distance, attack.MinRange, attack.Range)` 算出来的 `targetDistance` 也差不多，几只怪物会不约而同地朝着玩家周围**同一小片区域**扎堆过去——`NavigationAgent3D` 只保证"这只怪物不会穿墙走到目标点"，不保证"这只怪物不会和另一只走到同一个目标点的怪物叠在一起"，观感上会是好几只怪物挤成一团、模型互相穿插，一点都不像在包抄玩家。

**这里要先说清楚：DOOM 3 源码里没有任何东西可以照抄来解决这个问题**——9.4.3 节已经交代过，走位这整套系统本身就是本教程的自建扩展，源码里对应的 `idCombatNode` 是关卡设计师手摆的静态节点，节点数量有限，天然就不会出现"好几只怪物涌向同一个点"这种问题（因为能站的点位一开始就是设计师控制好的）。既然走了实时计算这条路，"怎么让多只怪物不挤在一起"就必须是本教程自己给出方案的部分——不解决的话，对一份自称追求商业级质感的教程来说是说不过去的，这是一个真实存在、必须正面处理的问题，不能挂个"开放问题"的牌子就绕过去。

处理思路分两层，分别针对"目标点选得太集中"和"就算目标点不同，路上也可能撞在一起"这两件事：

**第一层：给每只怪物一个稳定的、各不相同的角度偏移**，让它们不是都直奔玩家和自己连线上的那个点，而是分散到玩家周围不同的方位角上去：

```csharp
// Enemy.cs 追加
// 用节点自己的 InstanceId 生成一个稳定的、每只怪物各不相同的角度偏移——不需要关卡设计师
// 手动给每只怪物分配包抄角度，同一只怪物每次调用这个方法结果都一样（不会一会儿一个方向）
private float PackAngleOffset()
{
    ulong id = GetInstanceId();
    float normalized = (id % 1000) / 1000.0f;
    return (normalized - 0.5f) * Mathf.Pi * 0.6f;   // 映射到大约 ±54°的偏移范围，不是随机值，每次调用结果都一样
}
```

`TickReposition()` 算 `desiredPos` 那一行，从"直接沿连线退/进"改成"沿连线偏转一个角度之后再退/进"：

```csharp
// TickReposition() 里原来这两行：
// Vector3 toPlayer = _player.GlobalPosition - GlobalPosition; toPlayer.Y = 0;
// Vector3 desiredPos = _player.GlobalPosition - toPlayer.Normalized() * targetDistance;
// 改成：
Vector3 toPlayer = _player.GlobalPosition - GlobalPosition; toPlayer.Y = 0;
Vector3 spreadDir = toPlayer.Normalized().Rotated(Vector3.Up, PackAngleOffset());
Vector3 desiredPos = _player.GlobalPosition - spreadDir * targetDistance;
```

这一步解决的是"目标点选得太集中"——不同 `InstanceId` 算出不同的偏移角度，几只怪物即便一开始站的位置、面对玩家的方向都差不多，走位目标点也会散开到玩家周围一圈不同的方位，而不是全部涌向同一个点。

**第二层：就算目标点不同，路上仍然可能因为起点接近而短暂挤在一起**——这一层交给 9.1.1 节刚引入的 `avoidance_enabled`/`ApplyAvoidance()` 处理。这不是巧合：`NavigationAgent3D` 的 RVO 避让本来就不是只避让 `NavigationObstacle3D` 这种静态/半静态障碍物，**同样开启了 `avoidance_enabled` 的其他 agent 之间也会互相避让**——也就是说，只要每只怪物都在 9.1.1 节接上了 `ApplyAvoidance()`，多只怪物同时走位时，靠得太近的两只会被 RVO 自动推开一点距离，不需要再单独写一套"怪物之间保持间距"的分离逻辑。这也是为什么 9.1.1 节的动态避让不只是为了解决关门的问题——它本身就是"多个移动中的 agent 该怎么不叠在一起"这个更通用问题的解法，关闭的门和别的怪物，对 RVO 来说只是"避让列表"里的两种不同条目而已。

> **老实说明这两层能解决到什么程度**：这套方案能让怪物**看起来**不再无脑地涌向同一点、也不再硬挤在一起，但它依然不是真正的"编队"或者"协作走位"——每只怪物的决策完全是独立的，`PackAngleOffset()` 只是给了一个静态的、跟其他怪物无关的偏移量，不会根据"这个方位这一刻已经站了另一只怪物"去动态调整；RVO 避让解决的也只是"别叠在一起"，不会主动帮怪物群体形成包围圈或者分配"谁去正面、谁去侧翼"这种战术分工。如果你想要更接近真正战术协作的效果（比如显式地在几只怪物之间分配不同的包抄方位、确保不会有两只选中同一个扇区），需要引入一个更高层的"小队"概念——一个共享的黑板/管理器，怪物加入战斗时去这个管理器"预定"一个方位区间，离开战斗（死亡/跟丢目标）时归还——这已经超出这一节想解决的范围，这里只指出这是进一步值得做的方向，不展开实现。
### 9.5 疼痛打断：受伤会不会打断当前动作

现在的怪物挨打只会掉血，不会有任何"被打疼了一下"的反应。DOOM 3 的疼痛系统有三道门，缺一个都不对：**冷却时间**（疼痛反应之间有最短间隔，不会每一下都触发）、**免打断窗口**（脚本/攻击逻辑可以主动申请"接下来这段时间不许被打断"，比如重击动作的起手阶段）、以及**伤害下限**（这是一个绝对数值门槛，不是概率——很多人凭直觉以为这是"一定概率被打退"，实际上 DOOM 3 里这一下伤害没达到阈值，疼痛反应根本不会触发，达到了就必定触发，不掷骰子）。

> 去读 `idActor::Pain()`（`Actor.cpp:2368` 起）发现还有一处容易漏掉的细节：**冷却计时器的重置，和"疼痛动画到底播不播"是两件独立的事**——只要冷却时间够了，疼痛提示音**总会播放**（并且冷却计时器立刻重置），哪怕接下来 `allowPain`/伤害门槛这两道门把疼痛**动画**挡住了。如果像下面这样把 `_lastPainTime = now` 也一起挡在最后（只有全部门都通过才重置冷却），会导致"这一下被免打断窗口挡住了"之后，冷却计时器根本没重置，下一次伤害立刻又能重新触发判断——这跟原版"冷却只看时间，不管动画播没播"的行为不一样。改正版本：

```csharp
// Enemy.cs 追加
[Export] public float PainThreshold = 8.0f;    // 绝对伤害门槛，不是概率
[Export] public float PainDebounce = 0.5f;      // 两次疼痛反应之间的最短间隔
[Export] public float StaggerDuration = 0.4f;   // 疼痛硬直的真正时长——见下面的说明
private double _lastPainTime = -999;
private bool _painAllowed = true;
private double _painPreventedUntil;
private double _staggerUntil;

public void TakeDamage(float amount)
{
    if (_state == State.Dead) return;
    _health -= amount;

    if (_health <= 0)
    {
        Die();
        return;
    }

    TryPain(amount);
}

private void TryPain(float amount)
{
    double now = Time.GetTicksMsec() / 1000.0;
    if (now - _lastPainTime < PainDebounce) return;   // 冷却未到，连疼痛音效都不播

    // 冷却计时器在这里就重置，不等后面两道门通过——对应源码里
    // "pain_debounce_time = gameLocal.time + pain_delay" 紧跟在冷却判断之后、
    // 早于 allowPain/阈值判断执行这个顺序
    _lastPainTime = now;

    // 疼痛提示音：只要冷却过了就播，不受下面两道门影响
    GD.Print($"{Name} 发出疼痛提示音");

    if (!_painAllowed || now < _painPreventedUntil) return; // 处于免打断窗口，不播放疼痛动画
    if (amount < PainThreshold) return;                     // 没达到伤害门槛，不播放疼痛动画

    GD.Print($"{Name} 播放疼痛动画/短暂僵直");
    _staggerUntil = now + StaggerDuration;   // 这一行才是"僵直"这个词真正兑现的地方，见下面的说明
}

// 供攻击逻辑在起手帧调用："接下来这段时间不许被打断"——比如一个大动作攻击的前摇
public void PreventPain(float duration)
{
    _painPreventedUntil = Time.GetTicksMsec() / 1000.0 + duration;
}

public void SetPainAllowed(bool allowed)
{
    _painAllowed = allowed;
}

public bool IsStaggered() => Time.GetTicksMsec() / 1000.0 < _staggerUntil;
```

**这里要老实指出上一版遗留的一个问题**：`GD.Print($"{Name} 播放疼痛动画/短暂僵直");` 这一行本身只是打印了一句话，"短暂僵直"这四个字在代码里没有对应的任何实际后果——`TryAttack()`/`TickCombat()`/`TickReposition()` 该怎么跑还是怎么跑，播不播这个"动画"对游戏逻辑毫无影响，是纯装饰性的一句日志。真实源码里 `AI_PAIN` 这个标志（`AI.cpp:3279-3306` 的 `idAI::Pain()`）之所以能让怪物真的"愣一下"，靠的不是这个标志本身，而是脚本状态机看到这个标志之后**整个跳进一个专门的疼痛状态**，那个状态运行期间正常的攻击/移动判断逻辑根本不会被执行——真正的"打断"是靠切换到另一个状态、暂停了原来的状态在做的事情，不是靠一个只用来触发动画的布尔量。这个决定性的环节（脚本层的状态切换）恰好是我们拿不到的部分，但**它会不会有实际的移动/攻击后果，不需要靠那部分才能判断**——这件事只要在 Godot 这边接上一个真正会被读取的"僵直中"标记就行。所以上面的 `IsStaggered()` 不是装饰，`_PhysicsProcess()`（9.3 节）要跟着改一版，在 `switch` 之前插入一次检查：

```csharp
// Enemy.cs —— 替换目前的 _PhysicsProcess()（9.3 节写的初版，9.4.3 节又追加过 Reposition 分支）：
// 只在原有逻辑前面插入一次僵直检查，switch 内部的各个 Tick 方法、已经加过的 Reposition 分支
// 一个字都不用改——下面这版把 9.4.3 节加的那一支也一并列出来，避免读者对照的时候漏看
public override void _PhysicsProcess(double delta)
{
    float dt = (float)delta;
    Vector3 velocity = Velocity;
    if (!IsOnFloor()) velocity.Y -= Gravity * dt;
    Velocity = velocity;

    if (_state != State.Dead && IsStaggered())
    {
        // 僵直期间：水平方向完全不动，也不会进 switch 执行任何一个 Tick 方法——
        // 也就是说这段时间里不会攻击、不会走位、寻路目标也不会更新，是真的"愣住了"
        Velocity = new Vector3(0, Velocity.Y, 0);
        MoveAndSlide();
        return;
    }

    switch (_state)
    {
        case State.Idle: TickIdle(); break;
        case State.Alert: TickAlert(); break;
        case State.Combat: TickCombat(dt); break;
        case State.Reposition: TickReposition(dt); break;
        case State.Dead: MoveAndSlide(); return;
    }

    MoveAndSlide();
}
```

`PreventPain(duration)`/`SetPainAllowed(allowed)` 这两个原语现在还是不会在本节被直接调用——9.9 节的冲锋攻击会是第一个真正用到它们的地方：冲锋途中若被普通攻击打疼，直接愣住会显得非常怪（冲一半忽然定住），那里会在冲锋开始时调 `SetPainAllowed(false)`、结束时调 `SetPainAllowed(true)`，让冲锋成为一段"不会被疼痛打断"的动作。

### 9.6 完整的难度系统

第一版的 `DifficultySettings` 只有两个乘数，DOOM 3 实际的难度系统还包含护甲吸收比例（难度越高，护甲能挡住的伤害比例反而越低）和一个可选的、随难度整体缩放怪物强度的开关：

```csharp
using Godot;

public static class DifficultySettings
{
    public enum Level { Recruit, Normal, Veteran, Nightmare }
    public static Level Current = Level.Normal;

    public static float DamageTakenMultiplier()
    {
        return Current switch
        {
            Level.Recruit => 0.5f,
            Level.Veteran => 1.7f,
            Level.Nightmare => 3.5f,
            _ => 1.0f,
        };
    }

    public static float EnemyDamageMultiplier()
    {
        return Current switch
        {
            Level.Recruit => 0.7f,
            Level.Veteran => 1.3f,
            Level.Nightmare => 1.6f,
            _ => 1.0f,
        };
    }

    // 护甲吸收比例：难度越高，护甲能挡住的伤害比例反而越低——这是 DOOM3 里一个容易被忽略、
    // 但很影响体验的反直觉设定，"你以为的护甲"在高难度下没那么可靠
    public static float ArmorProtection()
    {
        return Current <= Level.Normal ? 0.4f : 0.2f;
    }

    // 可选：怪物强度是否随难度整体缩放。DOOM3 原版实际上不这样做（原版靠关卡设计师
    // 手动为不同难度增减怪物摆放/弹药数量），这里作为一个可选的备用方案给出，
    // 两种做法的取舍在第 18 章的对照表后面会再提一句
    public static float MonsterHealthScale()
    {
        return Current switch
        {
            Level.Recruit => 0.8f,
            Level.Veteran => 1.3f,
            Level.Nightmare => 1.6f,
            _ => 1.0f,
        };
    }
}
```

`PlayerController.TakeDamage` 要把护甲吸收也算进去，才是完整的伤害结算：

```csharp
// PlayerController.cs
[Export] public float Armor;

public void TakeDamage(float amount)
{
    float scaled = amount * DifficultySettings.DamageTakenMultiplier();
    float protection = DifficultySettings.ArmorProtection();

    float armorAbsorbed = Mathf.Ceil(scaled * protection);
    armorAbsorbed = Mathf.Min(armorAbsorbed, Armor);

    float finalDamage;
    if (scaled <= 0)
    {
        armorAbsorbed = 0;
        finalDamage = 0;
    }
    else if (armorAbsorbed >= scaled)
    {
        // 护甲原本足够挡下全部伤害——但只实际消耗 "伤害-1" 这么多护甲，保底 1 点血必须扣，
        // 不能让护甲值也跟着多扣（这是 CalcDamagePoints() 里容易被忽略的一处记账细节）
        armorAbsorbed = scaled - 1;
        finalDamage = 1;
    }
    else
    {
        finalDamage = scaled - armorAbsorbed;
    }

    Armor -= armorAbsorbed;
    Health -= finalDamage;
    GD.Print($"玩家受到 {finalDamage} 点伤害（护甲吸收 {armorAbsorbed}），剩余 {Health}");
    if (Health <= 0)
    {
        GD.Print("玩家死亡");
    }
}
```

护甲永远不能把伤害完全挡到 0——哪怕护甲值远超本次伤害，也保底扣 1 点血，这是 DOOM 3 明确的设计。但要注意护甲**消耗量**也要跟着少扣：护甲原本要吸收的量超过了伤害本身，就只按"伤害减 1"来实际扣护甲，不能让护甲凭空多损耗——这是第一版遗漏的记账细节，之前的版本会在这种情况下多扣掉一些不该扣的护甲值。

**这里补一条设计笔记，回答一个读到 9.4.2/9.4.3 节之后很自然会冒出来的问题**：难度会不会影响攻击选择的"聪明程度"或者走位的"积极程度"，而不只是数值？——先说源码事实：查遍这份 DOOM 3 BFG 源码目录能找到的 `g_skill` 用法，只有 `AttackMelee()` 里那一处保底不死判断（`AI.cpp:4480`）。**没有任何地方能找到"低难度让 AI 决策变笨、高难度让 AI 决策变精"这类逻辑**——DOOM 3 的难度分档，就纯粹是玩家承受的伤害倍率、怪物造成的伤害倍率、护甲吸收比例，再加上这一个保底判定，AI 本身"怎么想"在所有难度下是同一套代码、同一套权重、同一套走位规则，跑出来的行为在直觉上"聪明不聪明"完全没有差别，差的只是数值。9.4.2 节的 `MinRange`/`Range`/`Weight`、9.4.3 节的 `StrafeChance`/`RepositionStuckTimeout`，这套系统本身既然是本教程自己设计的，理论上确实可以让 `DifficultySettings` 去调它们——比如高难度下 `StrafeChance` 更高、`RepositionStuckTimeout` 更短（被逼急了更快掏出兜底攻击）、`WeightedPick` 的反重复惩罚更轻（高难度不介意看起来"精于算计"地连续使用最优招式）。**但这不是 DOOM 3 的做法，是不是要加，取决于你想做的是"忠实小品"还是"商业化打磨"**：不少商业 FPS（尤其是有明确难度分级市场的射击游戏）确实会让高难度的 AI 决策本身更凶、更少犯错，不只是玩家更脆、敌人更肉；但这么做的代价是每个难度都要单独调一遍这几个已经不算少的旋钮，且很容易在某个难度上调出"看起来在作弊"的观感（比如反应快到不自然）。本教程选择跟 DOOM 3 保持一致：`DifficultySettings` 只影响数值，不影响 9.4.2/9.4.3 这套系统本身的任何一个判断——如果你想加，上面提到的那几个 `[Export]` 字段都是现成的旋钮，改法跟 `EnemyDamageMultiplier()` 是同一个模式，这里不替你做这个决定。

### 9.7 巡逻路点：填上 `TickIdle()` 一直空着的那部分

9.3 节的 `TickIdle()` 到现在为止什么都不做——怪物只是站着等玩家出现。DOOM 3 的怪物在没发现目标时通常会沿着关卡设计师放置的一串路点巡逻，而不是傻站着。做一个路点节点和对应的巡逻逻辑：

```csharp
// PatrolPoint.cs —— 场景里放几个 Marker3D，挂这个脚本，在编辑器里把它们首尾连起来
using Godot;

public partial class PatrolPoint : Marker3D
{
    [Export] public PatrolPoint Next;
    [Export] public float WaitTime = 2.0f;
}
```

```csharp
// Enemy.cs 追加——替换 9.2.1/9.3 节那一版 TickIdle()
// using System.Threading.Tasks;   // Task 需要这个命名空间，别忘了加在文件顶部
[Export] public PatrolPoint PatrolStart;
private PatrolPoint _currentPatrolPoint;
private bool _isPatrolling;

// **这一版顺手修掉一个前一版本就存在、没在这本教程里被点破过的重入问题**：_PhysicsProcess() 的
// switch 每一帧都会调用一次 TickIdle()。旧版把 TickIdle() 直接写成 async void，也就是说只要
// 状态还是 Idle，每一帧都会重新触发一次全新的异步调用——多数帧里这次新调用只是把 CanSeePlayer()
// 又测一遍（正在巡逻的那个协程内部循环自己也在测），白白多付出一次每帧都要做的射线检测
// （9.2 节已经提过这是一笔开销）。更麻烦的是：如果这次"外层新调用"先一步判定发现了玩家、
// 把 _state 改成 Combat，而巡逻协程本体要等到它自己下一次 await 恢复时才会跟着退出循环、
// 把 _isPatrolling 重置回 false——中间有一帧的窗口，_isPatrolling 是"过期地"停留在 true。
// 改法是把"发起巡逻"和"巡逻本体"拆成两个函数，_isPatrolling 为 true 时 TickIdle() 直接
// 让协程自己处理，不再重复检查
private void TickIdle()
{
    if (_isPatrolling) return;   // 协程在跑，它自己每帧都会检查视觉/听觉，外层不用再查一遍

    if (CanSeePlayer())
    {
        _state = State.Combat;
        _lastSeenTime = Time.GetTicksMsec() / 1000.0;
        GD.Print($"{Name}：发现目标，进入战斗状态");
        return;
    }

    if (TryHearPlayer(out Vector3 noisePos))
    {
        _state = State.Alert;
        _lastSeenTime = Time.GetTicksMsec() / 1000.0;
        _lastKnownPlayerPos = noisePos;
        GD.Print($"{Name}：听到动静，前去查看");
        return;
    }

    if (PatrolStart == null) return;
    _ = RunPatrol();   // fire-and-forget：协程内部自己会在状态变化时退出，不需要持有返回的 Task
}

private async Task RunPatrol()
{
    _isPatrolling = true;
    _currentPatrolPoint ??= PatrolStart;

    while (_state == State.Idle)
    {
        _navAgent.TargetPosition = _currentPatrolPoint.GlobalPosition;
        while (!_navAgent.IsNavigationFinished() && _state == State.Idle)
        {
            if (CanSeePlayer())   // 巡逻途中发现玩家，立刻中断巡逻
            {
                _state = State.Combat;
                _lastSeenTime = Time.GetTicksMsec() / 1000.0;
                _isPatrolling = false;
                return;
            }
            if (TryHearPlayer(out Vector3 noisePos))   // 巡逻途中听见动静，转警觉去查看
            {
                _state = State.Alert;
                _lastSeenTime = Time.GetTicksMsec() / 1000.0;
                _lastKnownPlayerPos = noisePos;
                _isPatrolling = false;
                return;
            }
            Vector3 nextPos = _navAgent.GetNextPathPosition();
            Vector3 dir = (nextPos - GlobalPosition); dir.Y = 0; dir = dir.Normalized();
            Velocity = new Vector3(dir.X * MoveSpeed * 0.5f, Velocity.Y, dir.Z * MoveSpeed * 0.5f);
            await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
        }
        Velocity = new Vector3(0, Velocity.Y, 0);
        await ToSignal(GetTree().CreateTimer(_currentPatrolPoint.WaitTime), SceneTreeTimer.SignalName.Timeout);
        _currentPatrolPoint = _currentPatrolPoint.Next ?? PatrolStart;   // 走完一圈没有下一个点就回到起点，形成循环
    }
    _isPatrolling = false;
}
```

这段协程式的巡逻循环，和第 5.1 节介绍过的 `async`/`await`（"方法执行到 `await` 那一行会暂停，但不阻塞游戏其他部分"）是同一套机制，只是这里用 `while` 循环 + `await get_tree().physics_frame` 表达一段跨越多帧、能被外部条件随时打断的行为——`while (_state == State.Idle)` 这个循环条件，一旦怪物发现玩家、`_state` 被外部改成 `Combat`，下一次循环检查就会自然退出。`RunPatrol()` 声明成 `async Task` 而不是上一版的 `async void`，是为了让 `TickIdle()` 能显式地用 `_ = RunPatrol();` 表达"我知道这是有意不等它、不是忘了 `await`"——`async void` 在 C# 里本来就应该只留给事件处理器用，像这种由自己代码主动发起、不需要跟外部事件系统打交道的协程，`async Task` + 显式丢弃返回值是更规范的写法，额外的好处是协程内部如果抛异常，`async Task` 版本的异常至少还能被 `.NET` 的未观察异常机制捕获到，`async void` 版本则会直接让异常在无法预测的地方冒出来。

**巡逻恢复到哪一个点，也是一个容易被读者忽略、这里明确说一下的细节**：`_currentPatrolPoint` 是 `Enemy` 实例自己的字段，不会因为进了 `Combat`/`Reposition` 打了一架又跑回 `Idle` 而被重置——`RunPatrol()` 里那句 `_currentPatrolPoint ??= PatrolStart;` 只在这个字段还是 `null`（第一次巡逻）时才会赋成起点，后续每次重新触发 `RunPatrol()`，都会从上次打断时停留的那个路点继续走，而不是每次都从 `PatrolStart` 重新出发——这才是"巡逻被打断、打完架回来接着巡逻"该有的行为，不是回到起点、也不是永远停在原地不再动。

### 9.8 远程攻击：不是所有怪物都该贴脸近战

第 8.3 节给怪物做的攻击只有一种：贴近了才能打的近战判定。这里要先纠正上一版一句说错的话：**近战和远程在真实 DOOM 3 里不是共用同一个方法**——近战是即时的连线检测（`AttackMelee()`/`TestMelee()`），远程是生成一个真正会飞行的投射物实体，两者在引擎里是结构上完全独立的两套系统。真正"共用同一个方法"的，是远程攻击内部的不同子类型：子弹、火球、火箭这些不同的投射物，全部通过同一个 `idAI::LaunchProjectile(jointname, target, clampToAttackCone)`（`neo/d3xp/ai/AI.h:532`）发射出去，区别只在于传给它的投射物定义是哪一份——这句话原本描述的是"远程内部的子类型共用一个发射方法"，不是"近战和远程共用一个方法"，上一版把这两件事混在一起说了。

这一节要做的是把 9.4.1 节搭好的 `Attacks` 表用起来：近战和远程条目并存在同一张表里（用 `ProjectileScene` 是否为空区分），`TryAttack()` 从表里挑，而不是靠一个"这只怪物是近战还是远程"的枚举开关去分派——一只怪物完全可以同时拥有近战和远程两种打法，靠近了用近战、拉开距离换远程，这在"非此即彼"的枚举写法下是表达不出来的。

```csharp
// Enemy.cs 追加
[Export] public Node3D ProjectileSpawnPoint;      // 投射物从哪里生成，通常是怪物的"嘴"或"手"位置
[Export] public float AttackConeDegrees = 30.0f;  // 目标偏出这个角度就不发射，避免"背对着打中"的怪异画面

// 从 Attacks 表里挑一条当前可用的远程条目（ProjectileScene 非空）——近战条目由
// 9.4.1/9.4.2 节的 PickMeleeAttack() 处理，两者共存在同一张表里，互不干扰。
// 判断标准跟 9.4.2 节的 PickMeleeAttack() 保持一致：区间（MinRange/Range）+ 冷却 +
// 权重随机，而不是"表里第一条够得着的就用"
private AttackDefinition PickRangedAttack()
{
    if (_player == null || !IsPlayerInAttackCone()) return null;
    double now = Time.GetTicksMsec() / 1000.0;
    float distance = GlobalPosition.DistanceTo(_player.GlobalPosition);

    var eligible = new List<AttackDefinition>();
    foreach (var attack in Attacks)
    {
        if (attack.ProjectileScene == null) continue;
        if (distance < attack.MinRange || distance > attack.Range) continue;
        if (!IsAttackOffCooldown(attack, now)) continue;
        eligible.Add(attack);
    }
    return WeightedPick(eligible, _lastUsedAttack);
}

// 对应 idAI::Event_EntityInAttackCone()（AI_events.cpp:1367-1399）：一次硬性的是/否判定，
// 目标偏出这个锥角就直接不发射。**这里要纠正上一版的另一处错误引用**：这个硬性放行/拒绝的判定
// 不是 LaunchProjectile 的 clampToAttackCone 参数做的——clampToAttackCone 实际做的是把瞄准方向
// "掰"到锥角边缘上照样开火（`AI.cpp:4289-4298`），不会真的拒绝这次攻击。下面这种"判定不通过就
// 直接不开火"的硬性检测，对应的真实函数是 Event_EntityInAttackCone，代码本身不用改，只是引错了名字
private bool IsPlayerInAttackCone()
{
    Vector3 toTarget = (_player.GlobalPosition - GlobalPosition); toTarget.Y = 0;
    float angle = Mathf.RadToDeg(GlobalTransform.Basis.Z.AngleTo(toTarget.Normalized()));
    return 180.0f - angle <= AttackConeDegrees;   // GlobalTransform.Basis.Z 是"身后"方向，这里换算成"正面偏离角度"
}

private void AttackRanged(AttackDefinition attack)
{
    if (attack.ProjectileScene == null || ProjectileSpawnPoint == null) return;

    var projectile = attack.ProjectileScene.Instantiate<Rocket>();
    GetTree().Root.AddChild(projectile);
    projectile.GlobalPosition = ProjectileSpawnPoint.GlobalPosition;
    projectile.DirectDamage = attack.Damage;   // 伤害也从这条 AttackDefinition 读，不再是投射物场景自己写死的值

    Vector3 direction = (_player.GlobalPosition - ProjectileSpawnPoint.GlobalPosition).Normalized();
    projectile.Launch(direction, this);   // this：把怪物自己传进去当 Owner3D，防止爆炸伤到自己
    GD.Print($"{Name} 使用 {attack.AttackName} 发射了一枚投射物");
}

// Enemy.cs —— 替换 9.4.2 节的 TryAttack()：现在近战/远程条目都从同一张 Attacks 表里挑，
// 优先挑近战（够近就贴脸打，不浪费弹药），挑不到再看有没有能用的远程条目。注意这里不再有
// 一个统一的 "now - _lastAttackTime < AttackCooldown" 前置判断——9.4.2 节已经把冷却下放到
// 每条 AttackDefinition 自己身上，PickMeleeAttack()/PickRangedAttack() 内部各自会检查
private AttackDefinition TryAttack()
{
    if (_player == null) return null;
    double now = Time.GetTicksMsec() / 1000.0;

    var melee = PickMeleeAttack();
    if (melee != null)
    {
        MarkAttackUsed(melee, now);
        _lastUsedAttack = melee;
        ExecuteMeleeAttack(melee);
        return melee;
    }

    var ranged = PickRangedAttack();
    if (ranged != null)
    {
        MarkAttackUsed(ranged, now);
        _lastUsedAttack = ranged;
        AttackRanged(ranged);
        return ranged;
    }
    return null;
}
```

**这一节真正的重点不是这段代码本身，是它带来的设计结果**：想做一只纯近战僵尸、一只纯远程暴风兵、或者一只"平时打子弹、贴脸了改用爪子"的混合型怪物，不需要写不同的怪物类，只需要在场景编辑器里往 `Attacks` 表里塞不同组合的条目——一条 `ProjectileScene` 留空的近战条目、一条指向 `Rocket.tscn` 的远程条目，两条都挂上就是混合型。**甚至可以把某条远程攻击的 `ProjectileScene` 换成不同配置的投射物场景**（改小 `SplashRadius`、换成 `IsGuided = true` 的追踪弹版本），做出"近战怪、普通远程怪、会追踪的远程怪、近战+远程混合怪"这些手感完全不同的敌人，`Enemy.cs` 一行都不用改。这正是第 14 章要系统讲的"数据驱动"的一次提前预演，也是上面 9.4.1 节把攻击拆成一张表而不是一堆孤立字段的真正回报。

`TickReposition()`（9.4.3 节）里那个"被困住超时就硬打一下"的兜底，到现在为止只看近战条目——这里跟着补上远程分支，否则一只纯远程怪物被逼到死角、`Attacks` 表里只有远程条目时，`PickDesperateMeleeAttack()` 永远找不到东西，兜底形同虚设：

```csharp
// Enemy.cs —— 替换 9.4.3 节的 PickDesperateMeleeAttack()，改名兼顾近战/远程
private AttackDefinition PickDesperateAttack()
{
    foreach (var attack in Attacks)
    {
        if (attack.ProjectileScene == null)
        {
            if (TestMeleeBounds(attack) && TestMeleeLineOfSight(attack)) return attack;
        }
        else if (IsPlayerInAttackCone())
        {
            return attack;   // 远程兜底不看 MinRange/Range——都被困住了，管不了那么多，能打多远打多远
        }
    }
    return null;
}
```

`TickReposition()` 里调用 `PickDesperateMeleeAttack()` 的那一行也要跟着改成 `PickDesperateAttack()`，判断到手之后按 `attack.ProjectileScene` 是否为空分派给 `ExecuteMeleeAttack()`/`AttackRanged()`——跟 `TryAttack()` 里已经写过的分派逻辑是同一个模式，这里不重复贴一遍完整代码。

**这里把 9.4.2/9.4.3 和这一节的远程攻击拼起来，实际走一遍读者可能会怀疑的那个场景**：一只纯远程怪物（`Attacks` 表里只有一条 `MinRange = 5, Range = 20` 的远程条目），玩家欺身贴到距离 2 的地方——会不会出现"够不着任何攻击，但也没有逻辑把它从贴脸位置解救出来"这种死锁？跟踪一遍：`TickCombat()` 每帧算 `HasEligibleAttack(distance=2, now)`，遍历 `Attacks` 表，`distance(2) < attack.MinRange(5)`，条件不成立，`HasEligibleAttack` 返回 `false`，状态切到 `Reposition`；`TickReposition()` 里同样调用不到任何"够格"的攻击，进入下面"在所有还没冷却的招式里找最近区间"这段——`Mathf.Clamp(2, 5, 20)` 结果是 `5`，`targetDistance = 5`，于是 `desiredPos` 被算成"沿着怪物到玩家连线，退到距离玩家 5 的位置"，交给 `NavigationAgent3D` 走过去。也就是说，**纯远程怪物被迫贴脸之后，会经由 `Reposition` 状态主动后退到自己的最小射程之外，不会卡死在"够不着任何攻击"的状态里**——这条路径在 9.4.2/9.4.3/9.8 三节各自写代码的时候没有专门为"贴脸的纯远程怪"做特判，全靠同一套"看当前距离有没有一条招式的区间覆盖它"的通用判断自然得出，这也是那两节强调"通用系统"而不是"专门给某种怪物写分支"的真正验证。唯一的前提条件是关卡里退路要有空间——如果纯远程怪物本身也被逼到墙角无路可退，就会落到上面刚讲过的 `RepositionStuckTimeout` 兜底，用远程攻击贴脸硬打一下，而不是无限卡在"想撤但撤不了"的状态里。

### 9.9（可选进阶）冲锋攻击：不是所有攻击都能站着放

有些怪物的攻击不是"站在原地打"，而是主动冲向你——DOOM 3 里这类攻击靠 `Event_ChargeAttack`/`Event_TestChargeAttack`（`neo/d3xp/ai/AI_events.cpp:1743-1802`）实现，冲锋前会先验证"冲过去这条路线走不走得通"，避免怪物一头冲进墙里卡住。这一节有两处容易想当然写错的地方，值得先说清楚再看代码：

**第一处：冲锋伤害不走保底不死判定**。真实源码的 `Event_ChargeAttack` 调用的是 `BeginAttack(damageDef)`，然后每个 tick 只要命中判定通过就直接 `DirectDamage(attack, enemy)`（`AI.cpp:4139-4150`、`4356-4392`）——这是一条完全绕开 `AttackMelee()` 的独立路径，而 9.4 节那个"低难度保底不死"的判断恰恰只存在于 `AttackMelee()` 内部。也就是说，**真实游戏里冲锋攻击在新兵/普通难度下一样能一下打死血量不多的玩家**，不享受近战的保底待遇——这是刻意的难度设计（冲锋通常伤害更高、也更容易被看出来提前躲开，游戏没打算再额外保护你），不是应该修的 bug。如果直接复用 9.4.1 节的 `ExecuteMeleeAttack()`，就会把这份本不该有的保护也带过来，跟源码的行为不一致。

**第二处：路径验证要验证的是"这条直线"，不是"存不存在某条路"**。真实的 `Event_TestChargeAttack`（`AI_events.cpp:1769-1802`）用的是 `PredictPath`，验证的是冲锋会走的那条**直线轨迹**本身有没有被挡住、会不会冲出悬崖——不是"从这里到玩家存不存在一条可达路径"。`NavigationAgent3D.IsTargetReachable()` 检测的是后者：哪怕这条路要绕开一根柱子，只要绕得过去就算 `true`。但冲锋不会绕路，它是径直冲过去的，一头撞上那根"反正绕得过去"的柱子——用 `IsTargetReachable()` 验证冲锋路线，验证的根本不是同一件事。改正版本换成一次直线方向的胶囊体扫掠，只信这条直线本身。

**第三处：这一节最初写出来的那版代码有一个没被点破的架构问题，这里一并说清楚再往下写**——冲锋原本被实现成一段独立于 `_state` 状态机之外的 `async void` 协程，靠一个 `_isCharging` 布尔值自己标记"现在是不是在冲"。问题是 `_PhysicsProcess()` 每一帧仍然会照常按 `_state`（这时候还停留在 `Combat`）分派给 `TickCombat()`/`ChaseAndAttack()`，那两个函数一样会在同一帧写 `Velocity`——冲锋协程和正常追打逻辑变成两段互相不知道对方存在的代码在同一帧抢着写同一个 `Velocity`，谁的赋值发生在后面完全取决于协程 `await` 的信号什么时候恢复，这是实现细节，不是可以放心依赖的顺序保证。正确的做法是让冲锋也成为状态机里正式的一个状态——跟 9.4.3 节加 `Reposition` 状态时同样的思路：

```csharp
// Enemy.cs —— 状态枚举追加 Charging，_PhysicsProcess() 的 switch 追加对应分支
private enum State { Idle, Alert, Combat, Reposition, Charging, Dead }

// _PhysicsProcess() 里的 switch 追加一支：
//     case State.Charging:
//         break;   // 冲锋期间 Velocity 完全由下面 RunCharge() 协程自己控制，这一帧什么都不用做——
//                  // 重力和 MoveAndSlide() 仍然由 _PhysicsProcess() 统一处理，跟其他状态一样
```

```csharp
// Enemy.cs 追加
[Export] public float ChargeSpeed = 10.0f;
[Export] public float ChargeAttackRange = 6.0f;
[Export] public float ChargeCooldown = 4.0f;   // 冲锋自己的冷却——跟 9.4.2 节每条 AttackDefinition 各自的
                                                // Cooldown 是分开记账的，因为冲锋不经过 PickMeleeAttack() 那套挑选
private double _chargeReadyTime = -999;

// 由 TickCombat() 每帧调用，判断"这一刻要不要发起冲锋"——一次同步的是/否判断，
// 真正冲出去的过程交给下面的 RunCharge() 协程。返回 true 说明这一帧已经把状态切进
// Charging 了，调用方应该直接 return，不要再执行正常的追打/走位逻辑
private bool TryStartCharge()
{
    double now = Time.GetTicksMsec() / 1000.0;
    if (now < _chargeReadyTime || _player == null) return false;

    // 冲锋伤害借用 Attacks 表里的一条近战条目当基础数值（伤害、判定用的范围），
    // 但下面执行时不会调用 9.4.1 节的 ExecuteMeleeAttack()——原因见上面第一处说明
    var chargeAttack = Attacks.FirstOrDefault(a => a.ProjectileScene == null);
    if (chargeAttack == null) return false;

    float distance = GlobalPosition.DistanceTo(_player.GlobalPosition);
    if (distance > ChargeAttackRange || distance < chargeAttack.Range) return false;   // 太远冲不到、太近没必要冲，直接近战就行

    // 对应 Event_TestChargeAttack：验证的是直线路径本身，不是"存不存在某条能到达的路"——
    // 见上面第二处说明，这里换成一次沿冲锋方向的胶囊体扫掠
    if (!IsChargePathClear())
    {
        GD.Print($"{Name}：冲锋路线走不通，放弃");
        return false;
    }

    _chargeReadyTime = now + ChargeCooldown;
    _state = State.Charging;
    _ = RunCharge(chargeAttack);
    return true;
}

private async Task RunCharge(AttackDefinition chargeAttack)
{
    GD.Print($"{Name} 开始冲锋！");
    double chargeStart = Time.GetTicksMsec() / 1000.0;

    // 9.5 节埋下的 PreventPain()/SetPainAllowed() 原语，这是本教程第一次真正用上它们：
    // 冲锋途中要是被普通攻击的疼痛判定打中，直接愣在半路会显得很怪（冲一半忽然定住），
    // 所以冲锋期间关掉疼痛硬直，冲完（不管是命中、超时还是中途被打断）再打开
    SetPainAllowed(false);

    while (Time.GetTicksMsec() / 1000.0 - chargeStart < 1.5 && _state == State.Charging)
    {
        Vector3 direction = (_player.GlobalPosition - GlobalPosition); direction.Y = 0;
        direction = direction.Normalized();
        Velocity = new Vector3(direction.X * ChargeSpeed, Velocity.Y, direction.Z * ChargeSpeed);

        // 命中判定复用 9.4.1 节的两阶段检测（TestMeleeBounds/TestMeleeLineOfSight），
        // 但伤害直接结算，不经过 ExecuteMeleeAttack() 里的保底不死判断——
        // 对应源码 BeginAttack()+DirectDamage() 这条独立于 AttackMelee() 的路径
        if (TestMeleeBounds(chargeAttack) && TestMeleeLineOfSight(chargeAttack))
        {
            float finalDamage = chargeAttack.Damage * DifficultySettings.EnemyDamageMultiplier();
            if (_player.HasMethod("TakeDamage")) _player.Call("TakeDamage", finalDamage);
            GD.Print($"{Name} 的冲锋直接命中，造成 {finalDamage} 点伤害（不经过保底判定）");
            break;
        }
        await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
    }

    SetPainAllowed(true);   // 不管上面是命中 break、超时结束，还是中途状态被外部改掉，都要把疼痛许可还回去——
                             // 否则一旦某次冲锋中途被打断（比如冲一半玩家把怪物打死），_painAllowed 会
                             // 一直卡在 false，这只怪物从此再也不会有疼痛反应，是一个很容易漏掉的资源泄漏式 bug

    // 只有状态还停留在自己设的 Charging 时才收尾改回 Combat——避免覆盖掉冲锋途中
    // 已经被外部改成的其他状态（比如冲锋没冲完就被玩家一枪打死，_state 已经是 Dead 了）
    if (_state == State.Charging)
    {
        Velocity = new Vector3(0, Velocity.Y, 0);
        _state = State.Combat;
    }
}

// 沿冲锋方向做一次胶囊体扫掠，只测世界几何（第 1 层），不测生物层——
// 只要沿途会撞上静态几何，就说明这条直线冲锋路线走不通
private bool IsChargePathClear()
{
    Vector3 direction = (_player.GlobalPosition - GlobalPosition); direction.Y = 0;
    float distance = direction.Length();
    direction = direction.Normalized();

    var spaceState = GetWorld3D().DirectSpaceState;
    var query = new PhysicsShapeQueryParameters3D
    {
        Shape = new CapsuleShape3D { Radius = 0.4f, Height = 1.8f },
        Transform = GlobalTransform,
        Motion = direction * distance,
        CollisionMask = 0b0001
    };
    float[] result = spaceState.CastMotion(query);
    // CastMotion 返回 [安全比例, 不安全比例]：安全比例明显小于 1，说明扫掠中途会被世界几何挡住
    return result.Length < 2 || result[0] > 0.95f;
}
```

最后是把 `TryStartCharge()` 接进 `TickCombat()`——**这里直接回答"冲锋跟走位状态机冲不冲突、到底谁说了算"这个问题**：冲锋检查排在 `HasEligibleAttack()` 判断之前，一旦触发就直接切状态、`return`，这一帧不会再往下走到"该不该转 `Reposition`"这一步；`Charging` 是独立于 `Combat`/`Reposition` 之外的状态，两者不可能同时生效，`_PhysicsProcess()` 的 `switch` 每一帧只会进其中一支：

```csharp
// Enemy.cs —— 替换 9.4.3 节的 TickCombat()：只在原有逻辑前面插入一次冲锋判断，
// 其余部分（HasEligibleAttack 判断、ChaseAndAttack 调用）原样不动
private void TickCombat(float dt)
{
    if (!CanSeePlayer())
    {
        Velocity = new Vector3(0, Velocity.Y, 0);
        _state = State.Alert;
        GD.Print($"{Name}：看不见目标了，转为警觉");
        return;
    }

    _lastSeenTime = Time.GetTicksMsec() / 1000.0;
    double now = _lastSeenTime;
    float distance = GlobalPosition.DistanceTo(_player.GlobalPosition);

    if (TryStartCharge()) return;   // 冲锋优先于走位判断——够条件就直接冲，不用先判断有没有"够格的招式"

    if (!HasEligibleAttack(distance, now))
    {
        _state = State.Reposition;
        GD.Print($"{Name}：当前距离没有能用的招式，开始走位");
        return;
    }

    ChaseAndAttack(dt);
}
```

这里"冲锋判断排在走位判断之前"是本教程自己定的优先级——源码里没有这层裁决可以参考（`Event_ChargeAttack`/`Event_TestChargeAttack` 什么时候该被调用，同样是脚本层的决定）。选择让冲锋优先，是因为冲锋本身通常就是"拉近距离"的一种手段，如果反过来让 `Reposition` 先把怪物挪到某条普通招式的可用区间，冲锋这个选项在很多帧里就再也没有机会被触发到；`TickReposition()` 里没有对称地加一次 `TryStartCharge()` 调用——这是有意的：冲锋和"正在小心翼翼地找角度走位"是两种不同的行为动机，混在一起容易出现"走位走到一半忽然冲锋"这种不连贯的观感，如果你想让走位途中也能触发冲锋，把同一行 `if (TryStartCharge()) return;` 加到 `TickReposition()` 开头就行，判断逻辑完全复用，不用另外写一份。

记得在文件顶部加 `using System.Linq;`（`Attacks.FirstOrDefault` 用到）和 `using System.Threading.Tasks;`（`Task` 用到，如果 9.7 节已经加过这一行，这里不用重复加）。

这一节标"可选"不是因为它不重要，是因为不是每种怪物都需要这个攻击方式——冲锋现在已经被收编成状态机里正式的一个状态，解决了最初那版跟 `Combat`/`Reposition` 抢 `Velocity` 写权的问题，但"选哪条攻击当冲锋基础数值"这件事依然没有走 9.4.2 节 `PickMeleeAttack()`/`WeightedPick()` 那一整套区间 + 冷却 + 权重的挑选流程——`TryStartCharge()` 里 `Attacks.FirstOrDefault(a => a.ProjectileScene == null)` 永远只挑表里第一条近战条目，这是本教程为了不把 `AttackDefinition` 的字段进一步复杂化（比如再加一个"能不能被当冲锋用"的标记）而留下的一处不对称。如果你想让冲锋也完全数据驱动、支持同一只怪物有好几种不同的冲锋打法，可以在 `AttackDefinition` 上加一个 `IsChargeAttack` 布尔字段，`TryStartCharge()` 改成跟 `PickMeleeAttack()` 一样走一遍"筛选候选 + `WeightedPick`"的流程，而不是 `FirstOrDefault`。

### 9.10（可选进阶）Boss：多阶段怪物

**先把这一节的定位说清楚**：真实 DOOM 3 里**没有一个单独的 Boss AI C++ 类**——查遍 `neo/d3xp/ai/` 目录，找不到任何叫 `idAI_Boss`、`Cyberdemon`、`Guardian` 之类的类。Boss 在源码结构上就是一只普通的 `idAI` 实例，跟本教程从第 8 章开始做的怪物用的是同一个引擎层类型，区别全部体现在它挂的那份体积大得多的 `.script` 脚本文件里——更多的攻击、更复杂的阶段判断逻辑，这些脚本文件同样不在本教程能拿到的这份源码目录里。所以这一节要做的，从头到尾都是**本教程自己在已经搭好的架构上做的合理扩展**：把 9.4.1 的 `Attacks` 表、9.4.2 的攻击选择、9.4.3 的走位、9.6 的难度系统这几块拼到一起，做出一个"血量降到某个百分比就换一套打法"的多阶段系统，不是在照抄某段 Boss 源码——因为那段源码根本不存在于本教程能看到的范围里。

延续 9.8 节结尾提过的思路——不同的怪物手感应该靠往 `Attacks` 表里塞不同的数据组合来表达，而不是靠新写一个继承自 `Enemy` 的子类。Boss 也一样：不新开一个 `Boss : Enemy` 的子类，而是继续往 `Enemy` 身上加数据，用"`Phases` 数组是否为空"来区分"这是普通怪物还是 Boss"。先做一份阶段配置：

```csharp
// BossPhase.cs —— 单独的 Resource，一只 Boss 挂几份，在编辑器里按血量阈值从高到低排开
using Godot;

[GlobalClass]
public partial class BossPhase : Resource
{
    [Export] public string PhaseName;
    [Export] public float HealthThreshold = 1.0f;   // 血量降到 MaxHealth 的这个比例以下（0~1），就切换到这一阶段
    [Export] public Godot.Collections.Array<AttackDefinition> PhaseAttacks = new();   // 这一阶段能用的招式子集，
                                                                                        // 可以跟其他阶段重叠，也可以完全独占
    [Export] public float MoveSpeedMultiplier = 1.0f;   // Boss 通常后期阶段更快、更凶——这里就是那个"更快"的旋钮
}
```

`Enemy` 挂一份 `Phases` 数组，并且要在受伤时判断该不该切阶段：

```csharp
// Enemy.cs 追加
using System;   // event 需要这个命名空间

[Export] public Godot.Collections.Array<BossPhase> Phases = new();
[Export] public float PhaseTransitionInvulnerability = 1.0f;   // 切阶段瞬间的强制无敌窗口，给表现（音效/镜头/动画）留出反应时间

public event Action<BossPhase> PhaseChanged;   // 挂钩，读者可以在这里接一段过场镜头/播报语音之类的表现
                                                 // （这里先用最基础的 C# event——本教程要到第 13 章才正式介绍
                                                 // Godot 的 [Signal] 机制，这一节写的时候还不适合提前用它；
                                                 // 如果你已经看过第 13 章，把这里换成 [Signal] 效果是一样的）

private BossPhase _activePhase;
private double _phaseInvulnerableUntil;

// 返回值：这一次调用有没有真的切换阶段——TakeDamage() 需要知道这件事，见下面的说明
private bool UpdateBossPhase()
{
    if (Phases.Count == 0) return false;
    float healthFraction = _health / MaxHealth;

    // 在所有"阈值 >= 当前血量比例"的阶段里，挑阈值最低的那一个——
    // 也就是"当前血量刚好够得着的、最往后的那一阶段"。各阶段的 HealthThreshold
    // 理应互不重叠、由挂 Phases 的人自己按从高到低的顺序摆好
    BossPhase target = null;
    foreach (var phase in Phases)
    {
        if (healthFraction <= phase.HealthThreshold
            && (target == null || phase.HealthThreshold < target.HealthThreshold))
        {
            target = phase;
        }
    }

    if (target != null && target != _activePhase)
    {
        _activePhase = target;
        _phaseInvulnerableUntil = Time.GetTicksMsec() / 1000.0 + PhaseTransitionInvulnerability;
        GD.Print($"{Name}：进入阶段「{target.PhaseName}」，{PhaseTransitionInvulnerability} 秒强制无敌");
        PhaseChanged?.Invoke(target);
        return true;
    }
    return false;
}
```

`TakeDamage()`（9.5 节定义）要多做两件事：切阶段瞬间的无敌窗口要挡伤害，受伤之后要检查是不是该换阶段了：

```csharp
// Enemy.cs —— 替换 9.5 节的 TakeDamage()
public void TakeDamage(float amount)
{
    if (_state == State.Dead) return;
    if (Phases.Count > 0 && Time.GetTicksMsec() / 1000.0 < _phaseInvulnerableUntil) return;   // 阶段切换的强制无敌窗口

    _health -= amount;

    if (_health <= 0)
    {
        Die();
        return;
    }

    // 同一下伤害如果刚好触发了换阶段，就不要再叠一次疼痛硬直——见下面的说明
    bool phaseChanged = Phases.Count > 0 && UpdateBossPhase();
    if (!phaseChanged) TryPain(amount);
}
```

**这里要主动检查一个容易被忽略的交叉情况，而不是等读者自己踩到**：如果不加 `phaseChanged` 这层判断，`TakeDamage()` 原本的写法是"切完阶段之后，不管切没切，`TryPain(amount)` 照常执行"——设想一下，玩家打出的这一下伤害刚好把血量砍过了某个 `HealthThreshold`，于是 `UpdateBossPhase()` 触发了阶段切换、给了 `PhaseTransitionInvulnerability` 这么长一段强制无敌窗口，紧接着同一帧、同一次 `TakeDamage()` 调用里，`TryPain(amount)` 还是照常跑了一遍——如果这一下伤害同时也过了 `PainThreshold`，Boss 会在"理应无敌、播放阶段转换表现"的这个时刻**同时**触发一次疼痛硬直（9.5 节的 `_staggerUntil`），两套反应叠在一起，观感上会是"Boss 一边放着阶段转换的特效一边像被打疼了一样抖一下"，很容易让玩家觉得这是穿模或者时序错误，而不是设计好的效果。改成"这一下如果换了阶段，就不再额外触发疼痛"之后，阶段转换本身（连同你挂在 `PhaseChanged` 上的表现）就是这一下伤害唯一的反应，不会有第二套反应来抢戏。

阶段切换本身只做了两件"轻量"的事——挡一小段时间的伤害，和抛一个事件出去让读者自己接表现——**这是有意保持简单的**：真要做的话，"强制打断当前动作，播一段专属的阶段转换动画/台词，镜头给个特写"这些都可以挂在 `PhaseChanged` 这个事件上，但那属于具体项目的表现层工作，这里只搭这根挂钩，不替你把动画和运镜也写了。

最后是把 9.4.2 的攻击选择和 9.4.3 的走位接到"当前阶段"上——这两节写的 `PickMeleeAttack()`/`PickRangedAttack()`/`HasEligibleAttack()`/`TickReposition()`，以及 9.4.3/9.8 节后来为"被困住兜底"加的 `PickDesperateAttack()`、9.9 节 `TryStartCharge()` 里 `Attacks.FirstOrDefault(...)` 那一行，但凡出现 `Attacks` 的地方，只需要把它换成下面这个新属性，其余判断逻辑（区间/冷却/权重/视线）一个字都不用动：

```csharp
// Enemy.cs 追加——Phases 为空（普通怪物）时退回 9.4.1 节的 Attacks 表，
// Phases 非空时只从当前激活阶段的 PhaseAttacks 里选
private Godot.Collections.Array<AttackDefinition> CurrentAttackPool()
{
    return (Phases.Count > 0 && _activePhase != null) ? _activePhase.PhaseAttacks : Attacks;
}

// 同理，凡是直接用 MoveSpeed 的地方（ChaseAndAttack()、TickReposition()、TickIdle() 的巡逻移动），
// 都换成这个属性——Boss 没进入任何阶段时（Phases 为空）行为跟普通怪物完全一样
private float EffectiveMoveSpeed => MoveSpeed * (Phases.Count > 0 && _activePhase != null ? _activePhase.MoveSpeedMultiplier : 1.0f);
```

**这就是先把 9.4.2/9.4.3 做成通用系统、而不是给某只怪物写死的真正回报**：Boss 后期阶段移动更快、可用招式变了，9.4.3 节的走位逻辑不需要专门为 Boss 再写一套——它本来就是照着"当前有没有一条够得着的招式""该多快移动"这两个通用问题来决定行为的，阶段一变，这两个问题的答案自动跟着变，走位自然就适配了新阶段，不需要多出一行"如果这是 Boss 就……"这样的特判代码。9.6 节的 `DifficultySettings.EnemyDamageMultiplier()` 也是同样的道理——`ExecuteMeleeAttack()`/`AttackRanged()` 已经在用它，Boss 的攻击一样会经过这一层难度缩放，不需要再单独处理。`Reposition` 状态也是同一套故事：`TickReposition()` 内部但凡出现 `Attacks` 的地方都已经换成 `CurrentAttackPool()`，Boss 进入某个"这一阶段没有远程招式、只剩贴身近战"的阶段之后，被拉开距离照样会触发走位逼近，不需要为 Boss 专门再判断一次"这是不是我这个阶段该用的招式"。

**开放问题：要不要加一个"激怒计时器"（enrage timer）**——这是本教程主动提出、但没有替你做决定的一个设计问题。不少商业 FPS/ARPG 的 Boss 战会有这么一条规则：战斗进行到某个时长还没打完，Boss 直接获得一次性的大幅增益（攻击力/攻速暴涨，甚至进入必杀阶段），目的是防止玩家用"绕着 Boss 一直跑，靠地形和距离磨死它"这种不需要正面承伤的打法把战斗拖到无限长。**这不是从 DOOM 3 里来的**——前面已经说过，DOOM 3 源码里根本没有独立的 Boss 类，自然也没有 enrage 相关的字段或逻辑，这纯粹是本教程基于"通用商业 Boss 战设计经验"提出的一个候选功能，不是补充源码遗漏。要不要加，取决于你的 Boss 战节奏设计：如果 Boss 本身就有 9.9 节的冲锋、够快的 `MoveSpeed`，玩家很难长期保持"只跑不打"的距离，enrage 计时器可能是画蛇添足；如果 Boss 是一只纯站桩输出、移动缓慢的类型，没有这道保险确实存在被无限风筝的风险。如果决定要加，实现思路很简单，直接复用已经搭好的骨架、不需要引入新概念：

```csharp
// Enemy.cs 追加——可选，是否启用取决于你的 Boss 设计
[Export] public float EnrageTime = 0f;   // <= 0 表示不启用；否则战斗持续这么久（秒）后强制激怒
[Export] public float EnrageDamageMultiplier = 1.5f;
[Export] public float EnrageMoveSpeedMultiplier = 1.3f;
private double _combatStartTime = -1;
private bool _enraged;

// 在 TickCombat() 开头（CanSeePlayer() 判断之后）追加一行 CheckEnrage(); 即可接入，
// 不需要新增状态——激怒只是叠加在现有阶段系统之上的一层数值修正，不改变走位/攻击选择的判断逻辑
private void CheckEnrage()
{
    if (EnrageTime <= 0 || _enraged || Phases.Count == 0) return;
    if (_combatStartTime < 0) _combatStartTime = Time.GetTicksMsec() / 1000.0;
    if (Time.GetTicksMsec() / 1000.0 - _combatStartTime > EnrageTime)
    {
        _enraged = true;
        GD.Print($"{Name}：激怒！");
    }
}
```

`ExecuteMeleeAttack()`/`AttackRanged()` 算 `finalDamage` 的地方，以及 `EffectiveMoveSpeed`，各自再乘一个 `(_enraged ? EnrageDamageMultiplier/EnrageMoveSpeedMultiplier : 1.0f)` 就接上了——这里不重复贴一遍这两个函数的完整代码，改法跟接入 `DifficultySettings.EnemyDamageMultiplier()` 是完全一样的模式，一行乘法。**这个计时器从进入战斗（`_state` 第一次变成 `Combat`）开始计，而不是从阶段切换开始计**——如果你想要"每进入一个新阶段就重新计时"这种效果（常见于分阶段 Boss，逼玩家在限定时间内打完这一阶段），把 `_combatStartTime` 的重置移到 `UpdateBossPhase()` 里、阶段真正切换的那一刻，而不是只在 `_combatStartTime < 0` 时赋值一次。

---

## 10. 死亡的分量：布娃娃与肢解

现在怪物死亡是"血量归零，`QueueFree()` 瞬间消失"——很敷衍。这一章让死亡"有分量"：身体应该像真的失去控制一样瘫倒。

### 10.1 准备一具布娃娃骨架

布娃娃需要一个带骨骼的模型（`Skeleton3D`），这部分需要你在建模软件（Blender 等）里做好骨骼绑定并导入 Godot——本教程不讲建模，假设你已经有一个带骨骼的敌人模型。导入后，Godot 提供了一个编辑器工具，能帮你自动在每根骨骼上生成对应的物理碰撞体：选中场景里的 `Skeleton3D` 节点，编辑器顶部工具栏会出现"创建物理骨骼"这类选项，点击后 Godot 会在骨架下自动生成一整套 `PhysicalBoneSimulator3D` + 每根骨骼对应的 `PhysicalBone3D`，并帮你猜测每个骨骼碰撞体的大小/关节限制——生成后通常需要手动微调几个明显不合理的碰撞体大小，但省去了从零手搭关节链的功夫。

### 10.2 触发布娃娃

```csharp
// Enemy.cs 修改 Die()
private PhysicalBoneSimulator3D _ragdollSimulator;
private AnimationPlayer _animPlayer;

public override void _Ready()
{
    // ...原有初始化...
    _ragdollSimulator = GetNode<PhysicalBoneSimulator3D>("Skeleton3D/PhysicalBoneSimulator3D");
    _animPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
}

private void Die()
{
    _state = State.Dead;
    GD.Print($"{Name} 死了");

    // 关闭角色本身的碰撞和移动逻辑——从现在开始身体完全交给布娃娃物理接管
    GetNode<CollisionShape3D>("CollisionShape3D").Disabled = true;
    SetPhysicsProcess(false);

    // 布娃娃继承死亡瞬间的速度，避免"正在跑步的怪物一死就诡异地定在原地瘫倒"
    Vector3 deathVelocity = Velocity;

    _ragdollSimulator.PhysicalBonesStartSimulation();

    foreach (Node child in _ragdollSimulator.GetChildren())
    {
        if (child is PhysicalBone3D bone)
        {
            bone.LinearVelocity = deathVelocity;
        }
    }
}
```

`PhysicalBonesStartSimulation()` 这一次调用，就是整个死亡表现从"被动画/代码控制"切换到"完全交给物理引擎接管"的开关——之前身体的每一帧姿态是由动画数据决定的，这一行调用之后变成由重力、碰撞、关节约束实时计算出来的，这就是"瘫倒"这个视觉效果的全部原理。

### 10.2.1 慢动作沉降：DOOM 3 布娃娃标志性的"先慢后快"

如果你玩过 DOOM 3，可能会记得那种敌人死亡瞬间身体先短暂地以慢动作瘫倒、然后逐渐恢复正常速度的效果——这不是错觉，是 DOOM 3 在布娃娃刚触发的一段时间内，专门给这具身体的物理仿真套了一个从慢到快的时间缩放渐变（`Actor.cpp:1663-1667`，`ragdoll_slomoStart`/`ragdoll_slomoEnd`，死亡前 1.6 秒到死亡后 0.8 秒这段区间内生效）。

> 这里要纠正一处编出来的细节：上一版说"配合关节/接触摩擦力也同步从低到高变化"，这个说法是错的，不是简化，是编错了。真实源码（`Actor.cpp:1669-1681`、`Physics_AF.cpp:4982-5001,5867-5903`）里摩擦力的变化不是单调的"从低到高"，而是一个**V 形的凹陷**：先是正常摩擦力，在自己这条时间线的**中点**跌到一个很低的值（0.1），再回升回正常水平——而且关节摩擦力和接触摩擦力走的是两条**完全独立、互不同步**的时间线：关节摩擦力的凹陷窗口是死亡后 0.2 秒到 1.2 秒，接触摩擦力的凹陷窗口是死亡后 1.0 秒到 2.0 秒，两者既不同步，也都不跟前面说的"-1.6 秒到 +0.8 秒"这条时间缩放窗口对齐——是三条各自独立的时间线在同时跑，不是"配合"着变化的一件事。

**这里要老实说明一个 Godot 和 DOOM 3 的真实差异，不能假装能完全照搬**：DOOM 3 的关节人偶系统是自己手写的约束求解器，可以给单具布娃娃单独设置时间缩放；Godot 内置物理引擎的 `PhysicalBoneSimulator3D` 没有暴露"让这一具骨架用比世界其他物体更慢的时间流速仿真"这个能力（`Engine.TimeScale` 是全局的，会拖慢整个游戏，不能只影响一具尸体）。所以下面这个实现**不是时间缩放，而是用一个从强到弱变化的阻尼（damping）去模拟类似的视觉效果**——物理上不是同一回事，但视觉上很接近"一开始软绵绵地沉降、逐渐恢复正常物理反应"这个效果。

再加一层老实话：就算不提时间缩放这件事，下面的阻尼曲线本身也只是对上面纠正过的"V 形凹陷、关节/接触各走各的独立时间线"这套机制的**单调近似**，不是真的复刻。`PhysicalBone3D` 的 `LinearDamp`/`AngularDamp` 也没有区分"关节摩擦力"和"接触摩擦力"这两个独立概念，Godot 这层 API 本身就不支持这种细粒度的拆分。下面这版用一个统一的阻尼值、在一段时间内从强单调衰减到 0，本教程认这是"为了简单，用一次性的阻尼上升近似代替源码真正的两段式凹陷+恢复"，不是逐段还原两条独立时间线的凹陷曲线——如果你想更贴近源码的观感，可以把这条曲线换成一个先降后升的三角形/正弦形状，且给关节和接触各开一条独立的计时器，这里为了控制复杂度没有这么做：

```csharp
// Enemy.cs 追加
[Export] public float RagdollSettleDuration = 1.0f;
[Export] public float RagdollSettleDamping = 4.0f;   // 沉降期间的额外阻尼，值越大越"软"

private async void ApplyRagdollSettleRamp()
{
    var bones = new List<PhysicalBone3D>();
    foreach (Node child in _ragdollSimulator.GetChildren())
    {
        if (child is PhysicalBone3D bone) bones.Add(bone);
    }

    double startTime = Time.GetTicksMsec() / 1000.0;
    while (true)
    {
        double elapsed = Time.GetTicksMsec() / 1000.0 - startTime;
        float t = Mathf.Clamp((float)(elapsed / RagdollSettleDuration), 0f, 1f);
        // t 从 0 到 1：阻尼从 RagdollSettleDamping 单调衰减到 0——这是对 DOOM3 真实的
        // "V 形凹陷、关节/接触摩擦力各走各的独立时间线" 这套机制的简化近似，不是逐段还原，
        // 只是让视觉上同样有"先软后硬"的沉降感
        float damping = Mathf.Lerp(RagdollSettleDamping, 0f, t);
        foreach (var bone in bones)
        {
            bone.LinearDamp = damping;
            bone.AngularDamp = damping;
        }
        if (t >= 1f) break;
        await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
    }
}
```

在 `Die()` 里 `PhysicalBonesStartSimulation()` 之后调用 `ApplyRagdollSettleRamp();`。这个函数每个物理帧都 `await` 一次、循环到时间跑完为止——这是第 13 章会正式讲到的"用协程表达一段跨越很多帧、但读起来像同步代码的过程"的又一个例子，这里提前用上了。

### 10.3 肢解：更夸张的死法

不是每次死亡都要炸得四分五裂——通常只在"过量伤害"（比如已经快死了还挨了一发火箭）时才触发。加一个判断：

```csharp
// Enemy.cs 追加
[Export] public float GibThreshold = -20.0f;   // 血量掉到这个值以下才触发肢解，而不是普通布娃娃
[Export] public PackedScene GibChunkScene;

public void TakeDamage(float amount)
{
    if (_state == State.Dead) return;   // 已经死了就不用再处理伤害
    _health -= amount;
    if (_health <= 0)
    {
        if (_health <= GibThreshold)
        {
            Gib();
        }
        else
        {
            Die();
        }
    }
}

private void Gib()
{
    _state = State.Dead;
    Visible = false;
    GetNode<CollisionShape3D>("CollisionShape3D").Disabled = true;
    SetPhysicsProcess(false);

    var rng = new RandomNumberGenerator();
    for (int i = 0; i < 5; i++)
    {
        var chunk = GibChunkScene.Instantiate<RigidBody3D>();
        GetParent().AddChild(chunk);
        chunk.GlobalPosition = GlobalPosition + Vector3.Up * 0.5f;
        Vector3 dir = new Vector3(rng.RandfRange(-1, 1), rng.RandfRange(0.3f, 1.0f), rng.RandfRange(-1, 1)).Normalized();
        chunk.ApplyImpulse(dir * 4.0f);

        // 碎块几秒后自动消失，不然关卡里打得越多、物理体越堆越多，性能会明显下滑
        GetTree().CreateTimer(4.0).Timeout += chunk.QueueFree;
    }

    QueueFree();   // 本体（原模型）直接移除，玩家看到的是飞散的碎块，不是普通布娃娃瘫倒
}
```

`GetTree().CreateTimer(4.0).Timeout += chunk.QueueFree;` 这一行是"定时器信号触发时执行某个方法"的另一种写法——跟第 5 章 `await ToSignal(...)` 做的是同一件事（等一段时间后做点什么），但这里用的是**订阅信号的事件语法**（`+=`）而不是 `await`。两种写法的取舍：如果你只是想"等一会儿，然后做一件事，做完这个函数也就没别的事了"，`await` 更直观；如果你想"订阅一个信号，以后不管什么时候触发都要响应"，用 `+=` 订阅事件更合适。这个小细节会在第 13 章被重新讲一遍，到时候你会看到它其实是一个更大的话题（游戏里的"事件"到底该怎么组织）的一角。

跑一下：正常死亡应该是布娃娃瘫倒，用大伤害一次打死（比如把 `Damage` 临时改成 100）应该看到肢解效果。**这个功能纯粹是锦上添花，不影响任何核心玩法**——如果你的美术资源还没准备好碎块模型，`GibChunkScene` 留空、跳过这一节完全不影响后面章节的学习，回头有资源了再补上就行。

> **对照真实源码，纠正一处可能的误解**：上面这版 `Gib()` 隐藏本体、瞬间 `QueueFree()`、另外单独生成几个不相干的碎块——这套"整体替换掉，不是从活着的骨架上一根一根拆骨头"的思路，**恰好是 DOOM 3 真实的做法，不是本教程的简化**。源码里 `idAFEntity_Gibbable::Gib()`（`AFEntity.cpp:1214-1256`）触发条件是 `health < -20 && spawnArgs.GetBool("gib")`——阈值 `-20` 和上面 `GibThreshold` 的默认值精确对应，不是巧合。触发之后，它做的也不是"从这具还在模拟的布娃娃身上摘掉一根骨骼、让约束链断开重连"，而是：整个渲染模型直接切换成一份预先做好的、独立的"肢解态模型"（`model_gib`），物理体的 `Contents` 换成 `CONTENTS_CORPSE`（不再被当作正常碰撞体处理），再额外调用 `SpawnGibs()`（`AFEntity.cpp:1164-1207`）生成一批独立的碎块实体（`idAFEntity_Base::DropAFs` + `idMoveableItem::DropItems`）往外飞散——跟上面 `Gib()` 里"本体消失 + 单独生成几个碎块 `RigidBody3D`"是同一个骨架，只是 Godot 这版把"预先做好的肢解态模型"简化成了"直接不显示"。也就是说，**`PhysicalBoneSimulator3D` 用不着支持"运行时从活体约束链上摘掉一根骨骼"这个能力，因为真实源码本来就没有走这条路**——如果你之前担心"摘掉一条手臂之后，肩关节的 `PhysicalBone3D` 约束会不会变成指向空气、报错"，这个担心不成立，因为无论是源码还是本教程，都没有实现"从活着的骨架上真的拆掉一根骨骼"这种玩法，两边都是用"整体切换/替换"绕开了这个问题。
>
> 还有两处细节，真实源码有、上面这版没有，值得知道（要不要补，取决于你的项目规模）：
> 1. **碎块生成有一个全局节流**：源码里 `GIB_DELAY = 200`（`AFEntity.h:43`，注释原文是"只保留一个较低的频率，免得一次炸一堆怪物时性能掉得太狠"），`gameLocal.GetGibTime()`/`SetGibTime()` 维护一个全局的"下一次允许真正生成碎块的时间"，同一帧内哪怕好几只怪物同时被打出肢解判定，实际生成碎块特效的频率也会被摊平到至少每 200 毫秒一次。上面这版 `Gib()` 里每只怪物各自独立生成 5 个碎块，没有这层全局节流——只要你的关卡不会出现"一枚火箭炸出一堆同时触发肢解的怪物"这种场景（比如用溅射伤害同时打死一群小怪），就不用管这一条；一旦会出现，没有节流的话那一瞬间的 `RigidBody3D` 生成数量可能会造成一次明显的帧率毛刺，值得照着 `GIB_DELAY` 的思路加一个全局冷却。
> 2. **本体不是瞬间消失的**：源码里 `Gib()` 最后一行 `PostEventSec(&EV_Gibbed, 4.0f)`，而 `EV_Gibbed` 绑定的处理函数就是 `Event_Remove`（`AFEntity.cpp:962`）——也就是说，切换成肢解态模型之后，这具残骸还会在原地**再保留 4 秒**（跟碎块的存活时间一致）才真正被移除，玩家看到的是"倒下的残骸躺一会儿，跟着碎块一起消失"，而不是上面这版"本体一肢解就立刻从场景里消失、只剩碎块在飞"。如果你想要更接近源码的观感，把 `Visible = false; ... QueueFree();` 换成"隐藏原模型，改显示一个静止的肢解态残骸网格（哪怕只是原模型不带动画地摆一个姿势），4 秒后再 `QueueFree()`"，用一个 `GetTree().CreateTimer(4.0).Timeout += QueueFree;` 就能接上，不需要新概念。

### 10.4 尸体的生命周期：别让战场变成永久坟场

上面 `SpawnGibs()` 生成的碎块，源码和本教程都用了一个固定的存活时间（4 秒）自动清理——这条没有争议，两边一致。但**普通死亡（没有触发肢解、只是 `PhysicalBonesStartSimulation()` 瘫倒）的布娃娃尸体，源码里完全没有清理机制**：翻遍 `AI.cpp`、`Physics_AF.cpp`、`AFEntity.cpp` 都找不到任何"尸体存在超过多久就自动移除""同时存在的尸体数量有上限"这类逻辑——`idAI::Killed()` 触发布娃娃之后，这具尸体会一直留在关卡里，直到玩家离开这张地图。这不是源码疏漏，是 2004 年那批关卡本身就是"打完一段、经过存档点或过场就切地图"的线性结构，一张地图能同时存在的尸体数量天然有上限，没必要专门处理。

**这里要老实说明：接下来这段是本教程主动加的，不是从源码扒出来的**——但如果你的关卡是"一大片开放区域、玩家可能在里面反复横跳打很久"这种非线性结构（这正是本教程从第 9 章开始鼓励的巡逻+导航网格这套关卡形态最容易导致的情况），普通死亡尸体没有任何清理机制会变成一个真实的性能问题：每一具瘫倒的布娃娃都是一整套还在被物理引擎持续模拟的 `PhysicalBone3D` 链（哪怕它们已经完全静止，Godot 的物理引擎通常也需要经过若干帧的低速运动才会把一个物理体标记为"休眠"，休眠之后开销才会显著降低），打得越久、地上堆的尸体越多，物理线程的负担只会单调上升，永远不会自己降下来。一个简单、通用的解法：用一个全局管理器给"同时存在的尸体数量"设一个上限，超过上限就按"最早倒下的先清理"的顺序淡出最老的一具：

```csharp
// CorpseManager.cs —— Autoload 单例，跟 EventBus（第 13 章）注册方式一样
using Godot;
using System.Collections.Generic;

public partial class CorpseManager : Node
{
    public static CorpseManager Instance { get; private set; }

    [Export] public int MaxCorpses = 20;   // 同时存在的（非肢解）尸体上限，超出部分按"最早倒下的先清理"淘汰

    private readonly List<Node3D> _corpses = new();

    public override void _Ready() => Instance = this;

    // Enemy.Die() 触发布娃娃之后调用这个，把自己登记进队列
    public void Register(Node3D corpse)
    {
        _corpses.Add(corpse);
        if (_corpses.Count > MaxCorpses)
        {
            Node3D oldest = _corpses[0];
            _corpses.RemoveAt(0);
            FadeAndRemove(oldest);
        }
    }

    // 尸体因为别的原因提前消失了（比如关卡自己清理），记得摘出队列，不然队列会越攒越大、
    // 而且可能因为引用了一个已经被释放的节点在下次 Register 触发淘汰时报错
    public void Unregister(Node3D corpse) => _corpses.Remove(corpse);

    private async void FadeAndRemove(Node3D corpse)
    {
        if (!IsInstanceValid(corpse)) return;
        // 直接摘除最老的尸体最简单，但会很突兀地"啪"一下从场景里消失；给最后半秒做个淡出，
        // 观感上更像"融进阴影里"而不是"贴图突然没了"——前提是怪物材质开启了透明混合（Transparency），
        // 没开的话这个 tween 不会有可见效果，直接瞬间移除也不算错，只是少了这个加分项
        var tween = CreateTween();
        bool hasFade = false;
        foreach (Node child in corpse.GetChildren())
        {
            if (child is MeshInstance3D mesh)
            {
                hasFade = true;
                tween.Parallel().TweenProperty(mesh, "transparency", 1.0f, 0.5f);
            }
        }
        if (hasFade) await ToSignal(tween, Tween.SignalName.Finished);
        if (IsInstanceValid(corpse)) corpse.QueueFree();
    }
}
```

`Enemy.Die()`（10.2 节）末尾追加一行 `CorpseManager.Instance?.Register(this);` 就接上了——用 `?.` 是因为如果你还没把这个脚本注册成 Autoload，调用不会直接报错崩掉，只是静默跳过淘汰逻辑，方便你按自己的节奏决定什么时候接入这套机制。**这条上限该设多大，没有一个放之四海而皆准的数字**：太小（比如 5）会让玩家在激烈战斗中亲眼看到"我刚打死的敌人凭空消失了"这种穿帮；太大又起不到控制物理开销的作用——具体数值取决于你关卡的战斗密度和目标平台性能，20 只是一个"先跑起来、后面自己按实测表现调"的起点，不是权威数字。10.3 节的肢解碎块因为已经有源码对齐的 4 秒定时清理，不需要接入这套按数量淘汰的系统，两套清理机制各自负责各自的对象，互不冲突。

---

## 11. 关卡工具箱：触发器、开关、拾取物

到目前为止，关卡里的一切都是你在场景编辑器里手摆的、代码里写死的。这一章开始搭一套**关卡设计师（哪怕这个设计师就是你自己）不用改代码就能拼关卡**的工具箱。

### 11.1 拾取物：弹药、血包

```
AmmoPickup (Area3D)
├── CollisionShape3D
└── MeshInstance3D
```

**先问一个尖锐的问题，再写代码**：如果玩家当前这种弹药的储备已经是满的，走进一发弹药拾取物会发生什么？上一版的写法是"不管当前储备多少，无条件加上去、然后拾取物消失/进入重生倒计时"——这意味着一个已经满弹的玩家路过一堆弹药，会眼睁睁看着它们一个个"被捡起"却什么都没发生，如果这堆弹药还带重生倒计时，相当于白白吃掉了一次原本可以留给队友、留给自己下一轮再来捡的资源。真实的直觉应该是反过来的：**捡不动的东西不该消失**。要让这件事成立，`AddReserveAmmo` 得先有一个上限的概念——5.2.2 节写的版本是没有的：

```csharp
// WeaponManager.cs——补一个上限，替换掉 5.2.2 节没有上限的 AddReserveAmmo
[Export] public int MaxReserveAmmoPerType = 200;   // 简化起见先用同一个上限套所有弹药类型；
                                                     // 如果不同弹药想要不同上限，把这个字段换成
                                                     // Godot.Collections.Dictionary<string, int> 按 ammoType 查表即可

// 返回值是这次实际补上了多少——可能因为快到上限而小于 amount，甚至是 0（已经满了）。
// 调用方（拾取物）需要知道这件事，才能决定要不要把自己留在场上
public int AddReserveAmmo(string ammoType, int amount)
{
    int current = GetReserveAmmo(ammoType);
    int newAmount = Mathf.Min(current + amount, MaxReserveAmmoPerType);
    int actuallyAdded = newAmount - current;
    _reserveAmmo[ammoType] = newAmount;
    return actuallyAdded;
}

// 拾取物调用的入口，跟 TakeDamage/Activate/Interact 是同一种"只认方法名"的鸭子类型调用
public int GiveAmmo(string ammoType, int amount) => AddReserveAmmo(ammoType, amount);
```

`AmmoPickup.cs`：

```csharp
using Godot;

public partial class AmmoPickup : Area3D
{
    [Export] public string AmmoType = "bullets";   // 跟 5.2.2 节 Weapon.AmmoType 是同一套字符串键——
                                                     // 拾取的是"这一类型弹药的共享储备"，不是某一把具体武器
    [Export] public int AmmoAmount = 20;
    [Export] public float RespawnTime = 15.0f;   // <=0 表示拾取后不再重生

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
    }

    private async void OnBodyEntered(Node3D body)
    {
        if (!body.IsInGroup("player")) return;
        if (!body.HasMethod("GiveAmmo")) return;

        int given = (int)body.Call("GiveAmmo", AmmoType, AmmoAmount);
        if (given <= 0)
        {
            // 一点都没吃进去——这种弹药的储备已经满了。拾取物原地不动、不进入任何"已拾取"状态，
            // 玩家能看见它、能再走过来试，直到储备腾出空间为止，而不是被无声无息地清空
            return;
        }

        SetPickedUp(true);

        if (RespawnTime > 0)
        {
            await ToSignal(GetTree().CreateTimer(RespawnTime), SceneTreeTimer.SignalName.Timeout);
            SetPickedUp(false);
        }
        else
        {
            QueueFree();
        }
    }

    private void SetPickedUp(bool pickedUp)
    {
        Visible = !pickedUp;
        GetNode<CollisionShape3D>("CollisionShape3D").Disabled = pickedUp;
    }
}
```

血包是完全同一个模式，唯一的区别是"满了不该拾取"的判断对象从弹药储备换成了血量。`PlayerController.cs` 补一个 `Heal` 方法（8 章定下来的 `Health`/`MaxHealth` 现在派上第二次用场）：

```csharp
// PlayerController.cs 追加
public int Heal(float amount)
{
    float before = Health;
    Health = Mathf.Min(Health + amount, MaxHealth);
    int healed = Mathf.RoundToInt(Health - before);
    if (healed > 0) EmitSignal(SignalName.HealthChanged, Health, MaxHealth);   // 13 章接好信号之后补上这一行
    return healed;
}
```

```csharp
// HealthPickup.cs —— 跟 AmmoPickup 是同一个骨架，判断"满了没有"的对象换成 Health/MaxHealth
using Godot;

public partial class HealthPickup : Area3D
{
    [Export] public int HealAmount = 25;
    [Export] public float RespawnTime = 20.0f;

    public override void _Ready() => BodyEntered += OnBodyEntered;

    private async void OnBodyEntered(Node3D body)
    {
        if (!body.IsInGroup("player") || !body.HasMethod("Heal")) return;

        int healed = (int)body.Call("Heal", HealAmount);
        if (healed <= 0) return;   // 满血——留在场上，不消失

        Visible = false;
        GetNode<CollisionShape3D>("CollisionShape3D").Disabled = true;
        if (RespawnTime > 0)
        {
            await ToSignal(GetTree().CreateTimer(RespawnTime), SceneTreeTimer.SignalName.Timeout);
            Visible = true;
            GetNode<CollisionShape3D>("CollisionShape3D").Disabled = false;
        }
        else
        {
            QueueFree();
        }
    }
}
```

**这里刻意留了一个不对称，说明一下原因**：`AddReserveAmmo` 返回"实际补上了多少"，`Heal` 也返回"实际治疗了多少"，两者都用这个返回值判断"要不要把自己留在场上"——但如果玩家的储备/血量只差一点点就满（比如差 2 点，捡到一发加 20 的弹药），现在的写法是"能补多少补多少，然后正常消失/进入重生"，不是"不够整数吃满就拒绝"。这是有意的：真实玩家不会因为"这发弹药没有 100% 利用" 而觉得不对劲，只有"完全没有效果却还是消失了"（`given == 0` 却依然判定为"已拾取"）才会显得像 bug。

### 11.2 触发体积：走进去，门就开了

第 7 章的门是"用一个绑在门自己身上的 `Area3D` 侦测玩家"，这样每扇门都要自己管理触发逻辑。更灵活的做法是把"触发体积"和"被触发的效果"拆成两个独立的东西——一个触发器可以同时激活好几个目标（一扇门 + 一盏灯 + 一段音效），互相之间不需要认识对方：

```csharp
using Godot;
using System.Collections.Generic;

public partial class TriggerZone : Area3D
{
    [Export] public NodePath[] Targets = System.Array.Empty<NodePath>();
    [Export] public bool OneShot = true;
    [Export] public float Wait = 0.5f;   // OneShot = false 时，两次触发之间至少间隔多久——见下面的说明

    private bool _fired;
    private double _nextTriggerTime;

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
    }

    private void OnBodyEntered(Node3D body)
    {
        if (_fired && OneShot) return;
        if (!body.IsInGroup("player")) return;

        double now = Time.GetTicksMsec() / 1000.0;
        if (now < _nextTriggerTime) return;   // 还在冷却，忽略这次触发

        _fired = true;
        _nextTriggerTime = now + Wait;
        foreach (NodePath path in Targets)
        {
            Node target = GetNode(path);
            if (target.HasMethod("Activate"))
            {
                target.Call("Activate");
            }
        }
    }
}
```

> **`Wait` 这个字段不是本教程自己拍的——DOOM 3 的触发体积原生就有这个概念，而且上一版这里完全没做，是一处真实的缺口**：`idTrigger_Multi`（`Trigger.cpp:257-336`）默认构造时 `wait = 0.0f`，`Spawn()` 里 `spawnArgs.GetFloat("wait", "0.5", wait)`——也就是说**默认值就是 0.5 秒**，跟上面 `Wait = 0.5f` 的默认值精确对应。真实的触发逻辑（`Event_Touch`，`Trigger.cpp:465-508`）第一件事就是检查 `nextTriggerTime > gameLocal.time`，是就直接返回、连响应都不触发——这正是"重复触发的开关需不需要防抖"这个问题的标准答案：**需要，而且这是引擎原生支持的能力，不是某个关卡遇到问题之后才加的补丁**。没有这层防抖，`OneShot = false` 的触发器会在玩家站在边界反复横跳、或者角色控制器一帧内因为碰撞体形状产生多次 `BodyEntered` 时被连续触发好几次——如果 `Targets` 指向的是一盏灯的开关，连续触发偶数次会让灯"看起来什么都没发生"（开了又关，肉眼来不及分辨），触发奇怪的次数则会让灯的状态跟玩家预期的相反，两种情况在没有防抖的版本里都真实存在。DOOM 3 甚至比这里做得更细：`random`/`random_delay` 两个字段能给 `wait`/`delay` 叠加一个随机扰动（`nextTriggerTime = gameLocal.time + SEC2MS(wait + random * gameLocal.random.CRandomFloat())`），让好几个并排的触发器不会看起来"整齐划一"地同时冷却完——这属于锦上添花，上面这版为了简单没有实现，如果你想要，思路是在 `_nextTriggerTime` 赋值那一行加一个 `+ (float)GD.RandRange(-RandomWait, RandomWait)`。

把第 7 章的 `Door.Activate()` 方法保留（正好已经叫这个名字），现在触发器可以直接在编辑器的 Inspector 面板里把 `Targets` 数组指向任意数量的门/灯/其他实现了 `Activate()` 方法的节点，**门自己不再需要关心是谁触发了它**。这个"只要有 `Activate` 方法就能被任何触发器调用"的模式，会在第 13 章被正式讨论——你现在已经不知不觉用了好几次这种"只关心方法名、不关心具体类型"的写法（`TakeDamage`、`GiveAmmo`、`Activate`），是时候回头看看这套模式到底是什么、能不能做得更规范了。

### 11.3 可重复触发的开关

`TriggerZone` 的 `OneShot = false` 已经支持重复触发，而且上一节刚补上的 `Wait` 防抖同样适用——两次自动触发之间至少要隔 `Wait` 秒。但如果你想要"必须主动按按钮才触发，而不是走进某个区域就自动触发"，需要另一种触发方式——留到第 12 章一起讲，那一章专门处理"主动交互"这类场景；第 12 章的 `Button3D` 同样需要一层防抖，原因和这里完全一样（连续两次 `Interact` 调用之间不该间隔为零），到时候会补上，不在这里重复实现。

---

## 12. 交互系统：按钮、终端、可点击的屏幕

第 11 章的触发器是"被动的"——玩家走进某个区域就自动触发，不需要玩家主动做任何操作。这一章加"主动交互"：玩家看着某个东西、按一个键，才会发生事情。

### 12.1 一个能看的"焦点"系统

```csharp
// PlayerController.cs 追加
[Export] public float InteractRange = 3.0f;
[Export(PropertyHint.Layers3DPhysics)] public uint InteractMask = 0b0001;   // 见下面的说明——只探测"可交互"层，不是全部物理层
private Node3D _currentFocus;

public override void _PhysicsProcess(double delta)
{
    // ...原有移动逻辑...
    UpdateFocus();
}

private void UpdateFocus()
{
    var spaceState = GetWorld3D().DirectSpaceState;
    Vector3 from = _camera.GlobalPosition;
    Vector3 to = from + (-_camera.GlobalTransform.Basis.Z) * InteractRange;

    var query = PhysicsRayQueryParameters3D.Create(from, to);
    query.CollisionMask = InteractMask;
    var result = spaceState.IntersectRay(query);

    Node3D newFocus = result.Count > 0 ? (Node3D)result["collider"] : null;

    if (newFocus != _currentFocus)
    {
        _currentFocus = newFocus;
        bool showPrompt = _currentFocus != null && _currentFocus.HasMethod("Interact");
        // 这里应该去通知 HUD 显示/隐藏交互提示文字——第 16 章会正式接上 HUD，先留个占位
        GD.Print(showPrompt ? $"可以交互：{_currentFocus.Name}" : "无交互目标");
    }
}

public override void _UnhandledInput(InputEvent @event)
{
    if (@event.IsActionPressed("interact") && _currentFocus != null && _currentFocus.HasMethod("Interact"))
    {
        _currentFocus.Call("Interact", this);
    }
}
```

记得加 `interact` 输入映射（建议绑 E 键）。

**上一版这里没有设置 `CollisionMask`，这是一处真实的缺口，不是无关紧要的细节**：不设置的话，`PhysicsRayQueryParameters3D` 默认检测**所有**物理层，这条准星射线会打中挡在路上的任何东西——包括第 9 章会动的怪物、第 10 章瘫倒在地上的布娃娃尸体、第 7 章的可推动箱子。设想一个很容易实际发生的场景：墙上有一块第 12 章要做的终端屏幕，恰好一具第 10 章打死后瘫倒的尸体挡在射线和屏幕之间——没有过滤的话，射线会先打中尸体（`Node3D`，但没有 `Interact` 方法），`_currentFocus` 变成这具尸体、`showPrompt` 判定为 `false`，玩家会觉得"我明明对着屏幕，为什么没有交互提示"，而真正的原因是一具挡路的尸体，肉眼很难第一时间看出来。加一条独立的物理层专门给"可交互物体"用（比如项目设置里新建一层叫 `Interactable`，`Button3D`、可点击的屏幕都放在这一层），`InteractMask` 只勾选这一层，射线就会直接穿过怪物、尸体、箱子这些不相关的物体，只在真正的交互目标上停下——这也顺带解决了另一个问题：现在 `UpdateFocus()` 用的是"这条射线打中的第一个东西"，只要交互目标本身没有被挡（不在同一层的物体不会挡路），这就已经是"你准星对着的、离你最近的那个可交互物体"，不需要额外去处理"视野里同时有好几个可交互物体该选哪个"——因为它们本来就不会同时出现在同一条射线上，一条射线的物理意义天然就是"我此刻准星正对着的那一个"。

### 12.2 一个按钮

```csharp
using Godot;

public partial class Button3D : StaticBody3D
{
    [Export] public NodePath[] Targets = System.Array.Empty<NodePath>();
    [Export] public float Wait = 0.5f;   // 跟 11.2 节 TriggerZone.Wait 是同一个防抖需求，理由也一样

    private double _nextInteractTime;

    public void Interact(Node whoInteracted)
    {
        double now = Time.GetTicksMsec() / 1000.0;
        if (now < _nextInteractTime) return;   // 冷却未到，忽略这次按下
        _nextInteractTime = now + Wait;

        GD.Print("按钮被按下");
        foreach (NodePath path in Targets)
        {
            Node target = GetNode(path);
            if (target.HasMethod("Activate"))
            {
                target.Call("Activate");
            }
        }
    }
}
```

**这里的防抖跟 11.2 节的 `TriggerZone.Wait` 是同一个真实存在的问题，只是触发方式不同**：`TriggerZone` 需要防抖是因为角色可能在边界反复横跳、或者一帧内触发多次 `BodyEntered`；`Button3D` 需要防抖的原因略有不同，但同样真实——`_UnhandledInput` 里的 `IsActionPressed` 本身在单次按键内只会触发一次，看起来不需要额外防抖，但真实项目里 `Interact` 完全可能被**别的路径**重复调用：比如手柄的按键连发、或者玩家在交互动画播放到一半时又按了一次（这在没有防抖的版本里会让 `Activate()` 被连续调用两次，如果目标是一扇门，两次调用可能刚好把"开"又立刻叠加成"关"，或者如果目标是加一次性奖励的机关，会被吃两份奖励）。加一层跟 `TriggerZone` 一样的冷却窗口，成本几乎为零，却能挡掉这一整类问题，没有理由不加。

注意 `Button3D.Interact()` 和上一节 `TriggerZone.OnBodyEntered()` 最后做的事情几乎一样——都是遍历一个目标列表，调用 `Activate()`。**触发方式不同（走进区域 vs 主动按键），但触发之后"该发生什么"是同一套逻辑**。这正是第 11 章末尾埋下的伏笔：不管是走进去触发、还是按键触发，最终都收敛到同一个"激活"动作上。

### 12.3 世界里的可交互屏幕（选学）

如果你想做那种"墙上有一块可以用准星点击的电脑屏幕"的效果（终端、电梯面板这类），Godot 的做法是用 `SubViewport`：把一整套 UI（按钮、文字）渲染到一个独立的"子视口"里，再把这个子视口的画面当贴图贴到一个 3D 网格上。玩家的准星射线打中这块网格时，把命中点换算成这块 UI 上的二维坐标，往子视口里注入一个"鼠标点在这里"的事件。上一版这里只描述了思路、没有给出真正能跑的代码——这一节的知识点确实比较独立（不影响后面任何一章），但"选学"不等于"可以只讲个大概"，下面是一份真正能跑起来的最小实现：

```
InteractiveScreen (StaticBody3D)                 # 挂在 InteractMask 那一层（12.1 节），
├── CollisionShape3D                              # 碰撞体大小跟屏幕网格对齐
├── MeshInstance3D                                # 一块 QuadMesh，材质的 Albedo 贴图指向 SubViewport 的画面
└── SubViewport (SubViewportContainer 也可以，但屏幕场景不需要显示在 2D 层，直接用 SubViewport 节点即可)
    └── Control（你的终端 UI：按钮、Label……）
```

```csharp
// InteractiveScreen.cs
using Godot;

public partial class InteractiveScreen : StaticBody3D
{
    [Export] public NodePath ScreenViewportPath;   // 指向上面那个 SubViewport 节点
    [Export] public NodePath ScreenMeshPath;        // 指向贴了 SubViewport 画面的 MeshInstance3D

    private SubViewport _viewport;
    private MeshInstance3D _mesh;

    public override void _Ready()
    {
        _viewport = GetNode<SubViewport>(ScreenViewportPath);
        _mesh = GetNode<MeshInstance3D>(ScreenMeshPath);
        // 子视口默认不会响应"从外部注入"的输入事件，必须显式打开这个开关，
        // 不然下面 Interact() 里发送的鼠标事件会被无声无息地丢弃——这是最容易漏掉的一步
        _viewport.HandleInputLocally = false;
    }

    // 由 12.1 节的焦点系统调用——参数是玩家准星射线在这块屏幕网格上的命中信息
    public void Interact(Node whoInteracted, Vector3 hitPointWorld)
    {
        Vector2 uv = WorldHitToUv(hitPointWorld);
        if (uv.X < 0 || uv.X > 1 || uv.Y < 0 || uv.Y > 1) return;   // 命中点换算出界，说明打在了网格上但不在有效贴图区域

        Vector2 viewportPos = uv * _viewport.Size;   // 按当前子视口的实际像素尺寸换算——不要写死分辨率，
                                                       // 不然子视口分辨率一改这里的坐标就全错了，见下面的说明

        var motion = new InputEventMouseMotion { Position = viewportPos, GlobalPosition = viewportPos };
        _viewport.PushInput(motion);   // 先发一次移动事件，让 Control 树里的悬停/焦点状态先更新对

        var press = new InputEventMouseButton
        {
            Position = viewportPos, GlobalPosition = viewportPos,
            ButtonIndex = MouseButton.Left, Pressed = true
        };
        _viewport.PushInput(press);

        var release = (InputEventMouseButton)press.Duplicate();
        release.Pressed = false;
        _viewport.PushInput(release);   // 子视口里的按钮通常在"按下+抬起都发生在同一个控件上"才算一次完整点击，
                                          // 只发按下不发抬起，按钮会卡在"按住"的视觉状态出不来
    }

    // 把世界坐标的命中点，转换成这块 QuadMesh 表面的 [0,1] UV 坐标——
    // 这一步假设 MeshInstance3D 用的是一个未经非均匀缩放的 QuadMesh，法线朝 -Z，
    // 且命中点已经在网格的局部空间内（调用方传世界坐标，这里转一次局部空间）
    private Vector2 WorldHitToUv(Vector3 hitPointWorld)
    {
        Vector3 local = _mesh.GlobalTransform.AffineInverse() * hitPointWorld;
        var quad = (QuadMesh)_mesh.Mesh;
        float u = (local.X + quad.Size.X * 0.5f) / quad.Size.X;
        float v = 1.0f - (local.Y + quad.Size.Y * 0.5f) / quad.Size.Y;   // Godot 的 UV 原点在左上，局部 Y 轴朝上，方向要反一下
        return new Vector2(u, v);
    }
}
```

12.1 节的 `UpdateFocus()`/`_UnhandledInput()` 需要相应地传入命中点世界坐标（`IntersectRay` 返回的字典里本来就有 `"position"` 这个键），调用 `Interact` 时如果目标实现的是 `Interact(Node, Vector3)` 这个重载，多传一个 `(Vector3)result["position"]`；本教程前面 12.1 节写的 `Interact(Node whoInteracted)` 是单参数版本，两者可以用 `HasMethod`/重载判断共存，这里不重复展开这层适配代码。

**三个容易被忽略、但会直接决定这套方案能不能用的细节，都值得提前知道**：

1. **分辨率/宽高比不是"设一次就不用管"的**：上面 `WorldHitToUv` 算出来的 UV 是跟 `QuadMesh` 的物理尺寸绑定的，而 `viewportPos = uv * _viewport.Size` 是跟 `SubViewport` 的像素尺寸绑定的——两者是两个独立的数字，只要 `QuadMesh` 的宽高比和 `SubViewport` 的宽高比对不上，UI 上的点击位置就会出现整体拉伸偏移（比如你点在按钮左边缘，实际点击点算到了按钮外面）。保证两者比例一致（比如网格 2:1，视口也开 2:1，哪怕分辨率本身是 512×256 还是 1024×512 都不影响，只要比例一致就没问题）是这套方案能正确工作的前提，不是可选的优化。
2. **半透明部分"点得穿"这件事，取决于你怎么处理射线检测，不是 `SubViewport` 自动帮你做的**：如果这块屏幕的材质本身有透明区域（比如一个圆形按钮贴图，四角是透明的），`IntersectRay` 默认只关心碰撞体的几何形状，不关心贴图的 Alpha 通道——玩家点在按钮之间的透明缝隙上，射线依然会命中整块 `QuadMesh` 的碰撞体，然后把这次点击转发给子视口里那个位置，如果那个位置底下恰好还有一层背景控件在监听点击，就会出现"点在看起来什么都没有的地方，却触发了东西"的违和感。真要做到"像素级别的可点击性由贴图透明度决定"，需要在 `WorldHitToUv` 算出 UV 之后，额外去读一次那张贴图对应像素的 Alpha 值（`Image.GetPixel`，前提是贴图在 CPU 端可读），Alpha 低于某个阈值就当作没点中——这一步会有明显的额外开销（每次交互都要做一次贴图采样），只有真的需要"缝隙不可点"这种精细手感时才值得做，大多数终端/面板类 UI 用矩形按钮摆开，不会紧贴到需要处理这个问题的程度。
3. **`SubViewportContainer` 和裸 `SubViewport`是两个不同的用法，这里用的是后者**：如果你是照着 Godot 官方"2D UI 叠加在 3D 场景上"的教程搭的，很容易看到 `SubViewportContainer` 包一层 `SubViewport` 的写法——那一套是给"UI 要显示在屏幕上"用的（`SubViewportContainer` 本身是个 2D `Control`，会被塞进 `CanvasLayer`）。这里的场景是"UI 只需要被渲染成一张贴图贴到 3D 网格上，玩家根本看不到、也不该看到 `SubViewportContainer` 本身"，所以直接用一个独立的 `SubViewport` 节点（不挂在任何 `CanvasLayer` 下），把它的 `GetTexture()` 结果丢给 `MeshInstance3D` 的材质当 Albedo 贴图就够了，多包一层 `SubViewportContainer` 反而是不需要的额外节点。

---

## 13. 为什么你需要一个事件系统

到这里为止，本教程刻意没有讲任何"架构"——没有提事件系统、没有提数据驱动设计，就是一路"要什么功能就直接写"。现在项目已经有十几个互相关联的系统了，是时候回头看看代码本身，因为它已经开始露出几个真实的问题。

### 13.1 先看看现在的代码有什么问题

翻回第 8 章，`Enemy.TryAttack()` 里有这么一行：

```csharp
if (_player.HasMethod("TakeDamage"))
{
    _player.Call("TakeDamage", AttackDamage);
}
```

这行代码能跑，但它有一个隐藏的假设：**`Enemy` 必须知道"玩家"这个具体对象存在，并且知道它有一个叫 `TakeDamage` 的方法**。反过来，`GetTree().GetFirstNodeInGroup("player")` 这行代码在 `Enemy.cs`、`AmmoPickup.cs` 里各出现了一次——如果以后你想支持"多个玩家"（分屏、联机），或者想让 NPC 也能被"某种东西"攻击（不只是玩家），这些写死的假设会一处一处地变成要改的地方。

再看 `HasMethod("TakeDamage")` / `HasMethod("Activate")` / `HasMethod("Interact")` 这几个模式——它们已经出现了不下六次，散落在 `Weapon.cs`、`Enemy.cs`、`TriggerZone.cs`、`Button3D.cs`、`PlayerController.cs` 里。每一处都在做同一件事："我不关心你是什么类型，只要你有这个方法，我就调用它"。这种写法能跑，但有两个真实的缺点：

1. **拼错方法名不会在编译期报错**——`HasMethod("TakeDamge")`（打错一个字母）不会有任何警告，运行时只是安安静静地"什么都没发生"，你要靠肉眼观察游戏行为不对才能发现，排查起来很烦。
2. **没有统一的地方能看到"这个游戏里到底有哪些事件、谁在响应它们"**——想知道"玩家受伤的时候，除了扣血还应该发生什么"，得去搜整个代码库里所有调用 `TakeDamage` 的地方，一个个看。

### 13.2 用 Godot 信号，把"谁调用谁"倒过来

Godot 的信号系统解决的正是第二个问题：**让"发生了什么事"和"谁来响应这件事"彻底解耦**——发出信号的一方完全不需要知道有多少个东西在监听它，监听的一方也不需要被发出信号的一方直接持有引用去调用。

把玩家的受伤逻辑，从"别人主动调用我的方法"，改成"我发生了这件事，我广播出去，谁关心谁自己去接"：

```csharp
using Godot;

public partial class PlayerController : CharacterBody3D
{
    [Signal] public delegate void HealthChangedEventHandler(float newHealth, float maxHealth);
    [Signal] public delegate void DiedEventHandler();

    [Export] public float MaxHealth = 100.0f;
    public float Health { get; private set; }

    public override void _Ready()
    {
        Health = MaxHealth;
        // ...
    }

    public void TakeDamage(float amount)
    {
        Health -= amount * DifficultySettings.DamageTakenMultiplier();
        EmitSignal(SignalName.HealthChanged, Health, MaxHealth);

        if (Health <= 0)
        {
            EmitSignal(SignalName.Died);
        }
    }
}
```

（这里为了聚焦在"信号"这个改动本身，`TakeDamage` 省略了 9.6 节已经写好的护甲吸收结算——实际项目里把这两行 `EmitSignal` 分别加进 9.6 节那份完整版本的对应位置即可，不需要重新实现一遍伤害结算。）

现在任何关心"玩家血量变化"的系统，只需要订阅这个信号，不需要每帧去查询玩家血量，也不需要玩家去主动通知它：

```csharp
// Hud.cs（第 16 章会正式写这个类，这里先展示订阅的写法）
public override void _Ready()
{
    var player = GetTree().GetFirstNodeInGroup("player");
    player.Connect(PlayerController.SignalName.HealthChanged, new Callable(this, MethodName.OnPlayerHealthChanged));
}

private void OnPlayerHealthChanged(float newHealth, float maxHealth)
{
    _healthLabel.Text = $"{newHealth}/{maxHealth}";
}
```

`[Signal] public delegate void HealthChangedEventHandler(float, float)` 这一行**自带类型检查**——如果你在 `EmitSignal` 时传错参数类型或数量，编译期就会报错，不会像 `HasMethod`/`Call` 那样悄无声息地失败到运行时。这是从"字符串约定"升级到"编译期契约"最直接的收益。

### 13.3 全局事件总线：给"跟谁都有关系"的事件一个统一的家

`HealthChanged` 这种信号，关心它的通常只有一两个具体的系统（HUD），直接订阅玩家节点本身没问题。但有些事件是**"谁都可能关心"**的，比如"某只怪物死了"——成就系统关心、任务系统关心、可能还有个"清空这个房间就开门"的关卡机关也关心。如果每个关心的系统都要自己去 `GetTree().GetFirstNodeInGroup(...)` 或者遍历场景树找到具体是哪只怪物、再挨个 `Connect`，会很啰嗦。

这种"广播给不确定是谁、不确定有多少个"的场景，适合用一个全局的事件总线——一个单例（Autoload）节点，专门负责转发这类"大家都可能关心"的事件：

```csharp
using Godot;

public partial class EventBus : Node
{
    public static EventBus Instance { get; private set; }

    [Signal] public delegate void MonsterDiedEventHandler(Node3D monster, Node killer);
    [Signal] public delegate void ObjectiveCompletedEventHandler(string objectiveId);
    [Signal] public delegate void DoorOpenedEventHandler(Node3D door);

    public override void _Ready()
    {
        Instance = this;
    }
}
```

把这个脚本挂到一个场景上，在 `项目 -> 项目设置 -> Autoload` 里把它注册成全局单例（勾选"全局变量"，节点名填 `EventBus`）。以后任何地方都能通过 `EventBus.Instance.EmitSignal(...)` 广播、通过 `EventBus.Instance.Connect(...)` 订阅，双方完全不需要互相持有引用：

```csharp
// Enemy.cs 的 Die() 里追加一行——killer 这里先传 null：TakeDamage(float amount) 从第 8 章
// 定下来就只接收伤害数值，没有带上"谁打的"这个信息，`MonsterDied` 信号预留了 killer 这个参数位，
// 但要真正填上它，需要回头把 TakeDamage 的签名改成带一个攻击者参数，并且改掉所有调用它的地方
// （Weapon.Fire()/Melee()、Enemy.TryAttack()）——如果你的成就/统计系统需要"谁杀的"这个信息，
// 这是值得做的一处改动，本教程为了不在这里牵连前面所有章节已经写好的调用点，先不做这个改动
EventBus.Instance.EmitSignal(EventBus.SignalName.MonsterDied, this, (Node)null);
```

```csharp
// 任何关心"有怪物死了"的系统，比如一个"清光这个房间就开门"的关卡脚本
public override void _Ready()
{
    EventBus.Instance.Connect(EventBus.SignalName.MonsterDied, new Callable(this, MethodName.OnMonsterDied));
}

private void OnMonsterDied(Node3D monster, Node killer)
{
    _remainingMonsters--;
    if (_remainingMonsters <= 0)
    {
        _door.Call("Activate");
    }
}
```

**什么时候该用"直接信号"，什么时候该用"全局事件总线"**：一个节点自己的状态变化（武器换弹完成、门到达开启位置），关心它的通常是一两个明确的、离得不远的系统，直接订阅那个节点自己的信号就够；"谁都可能关心、数量不确定"的全局性事件（怪物死亡、任务完成、关卡目标达成），才上事件总线。如果什么都往总线上塞，代码会变得难以追踪"这个信号到底谁在发、谁在收"——这是一个很容易走极端的地方，两种极端（完全不用总线 vs 什么都用总线）都不好，具体怎么权衡，随着项目变大你会逐渐有自己的判断。

### 13.4 把重复的射线检测收拢起来

第 6 章末尾埋下的伏笔，现在可以回收了——`Fire()`、`Melee()`、怪物的近战判定，都是"从一点往一个方向打一条射线，命中了调用 `TakeDamage`"。抽成一个静态工具方法：

```csharp
using Godot;

public static class CombatUtil
{
    public static bool RaycastAttack(Node3D shooter, Vector3 from, Vector3 to, float damage, uint collisionMask)
    {
        var spaceState = shooter.GetWorld3D().DirectSpaceState;
        var query = PhysicsRayQueryParameters3D.Create(from, to);
        query.CollisionMask = collisionMask;
        var result = spaceState.IntersectRay(query);

        if (result.Count == 0) return false;

        Node3D hitObject = (Node3D)result["collider"];
        if (hitObject.HasMethod("TakeDamage"))
        {
            hitObject.Call("TakeDamage", damage);
        }
        return true;
    }
}
```

`Weapon.Fire()`、`Weapon.Melee()`、`Enemy.TryAttack()` 现在都可以简化成调用 `CombatUtil.RaycastAttack(this, from, to, damage, mask)` 一行——这不是什么高深的架构模式，就是"发现好几处代码在做同一件事，抽成一个函数"这个最朴素的编程习惯，只是放到这一章讲，是因为**在这之前你可能还没意识到这几处代码之间有这层关系**，先让代码在不同章节里各自独立地存在，走到这一步回头一看才看得出规律，比一开始就要求你设计好这个抽象要自然得多。

### 13.5 信号的生命周期：订阅之后，什么时候该取消订阅

前面几节演示的每一处 `Connect`/`+=`，都只讲了"怎么订阅"，没有讲"这份订阅什么时候该结束"——这不是疏漏，是故意留到这里集中讲，因为这是一个**真实存在、几乎每个用 Godot 信号系统的人都会踩至少一次**的坑，值得单独拿出来，而不是在前面每一处订阅代码旁边都插一句提醒打断阅读节奏。

**先说清楚 Godot 帮你兜住了哪一半**：如果 A 订阅了 B 的信号（`B.SomeSignal += A.Handler`），然后 A 先被 `QueueFree()` 释放——这种情况 Godot 引擎层是安全的，A 被释放时，Godot 会自动清理掉所有以 A 为接收方的信号连接，B 之后正常 `EmitSignal`，不会尝试调用一个已经不存在的对象的方法，不会报错、也不会崩溃。这是 Godot 信号系统内置的保证，不需要你手动做任何事。

**但反过来的情况，Godot 帮不了你**：如果 B（信号的发出方）活得比 A（订阅方）久，而 A 只是被逻辑上"废弃"（比如从场景树移除、或者游戏逻辑上认为它已经"死"了，但 C# 对象因为某个地方还留着引用而没有被真正回收），A 之前订阅的这份连接会一直挂在 B 身上，只要 B 还在发信号，就会一直尝试调用 A 的处理函数。本教程里最容易撞上这个情况的正是第 13.3 节的 `EventBus`——它是一个 Autoload 单例，**生命周期覆盖整局游戏**，而 `Enemy`/`TriggerZone` 这些订阅它的节点，生命周期通常只有一次战斗、一个房间那么长。如果一只 `Enemy` 在 `_Ready()` 里订阅了 `EventBus.Instance.Connect(...)`，死亡后 `QueueFree()` 掉——前面说过，Godot 会自动断开"以这只 `Enemy` 为接收方"的连接，这一点本身没问题；真正的风险出现在**这只 `Enemy` 如果被对象池复用、而不是真正释放**（有些项目为了性能会这么做，本教程目前的写法是直接 `QueueFree()`，还没有引入对象池），或者**回调闭包里捕获了别的、比 `Enemy` 自己活得更久的外部状态**——这两种情况都不是"信号本身没断开"，而是"信号处理函数执行的时候，访问到的数据已经不是你以为的那个状态了"，排查起来比直接报错的空引用异常更隐蔽。

真正应该养成的习惯，是**不依赖引擎的自动清理是不是覆盖了你的场景，而是显式地在 `_ExitTree()` 里断开自己订阅的、生命周期比自己长的信号源**：

```csharp
// Enemy.cs
public override void _Ready()
{
    // ...
    EventBus.Instance.Connect(EventBus.SignalName.MonsterDied, new Callable(this, MethodName.OnMonsterDied));
}

public override void _ExitTree()
{
    // EventBus 是 Autoload，活得比这只 Enemy 久——它是不是已经真的没用了，
    // 由这只 Enemy 自己的生命周期决定，所以断开连接的责任也应该在这只 Enemy 自己身上，
    // 而不是指望 EventBus 那边知道"这个订阅者已经不需要了"
    if (EventBus.Instance != null && EventBus.Instance.IsConnected(EventBus.SignalName.MonsterDied, new Callable(this, MethodName.OnMonsterDied)))
    {
        EventBus.Instance.Disconnect(EventBus.SignalName.MonsterDied, new Callable(this, MethodName.OnMonsterDied));
    }
}
```

**一个判断标准，帮你决定什么时候需要写这段 `_ExitTree()`，什么时候不需要**：如果信号的发出方和接收方生命周期基本同步（比如 `Hud` 订阅 `PlayerController.HealthChanged`——两者通常从游戏开始活到游戏结束，或者至少同时被卸载），不用特意写这段清理代码，Godot 的自动断开已经够用；一旦发出方是 Autoload（`EventBus`、`SaveManager` 这类"活得比谁都久"的单例），而接收方是会被频繁创建/销毁的东西（怪物、拾取物、任何一次性关卡道具），就应该养成在 `_ExitTree()` 里显式断开的习惯——多写这几行代码的成本，远低于一次"游戏运行很久之后偶发的奇怪报错、但复现不出来"的排查成本。

**另一个容易被忽略的地方：重复订阅**。如果一个节点的 `_Ready()` 因为场景重新加载、或者节点被重新添加进树而执行了不止一次（比如你做了一个"重新进入这个房间就重置怪物"的系统，房间场景被卸载又重新实例化），每次 `_Ready()` 都无条件 `Connect` 一次，而没有先检查是否已经连接过，会导致**同一个信号的同一个处理函数被注册了好几份**——`EmitSignal` 一次，处理函数却被连续调用好几次（比如玩家血量变化的 UI 更新逻辑被触发两次，通常看不出明显的错误，但如果处理函数里有"每次触发加一点数值"这种累加逻辑，就会产生实实在在的数值 bug）。Godot 的 `Connect` 默认允许重复连接同一对信号源和处理函数（不会报错，也不会自动去重），所以这个责任同样在调用方自己身上：

```csharp
// 订阅前先检查，避免同一个处理函数被注册多次
var callable = new Callable(this, MethodName.OnMonsterDied);
if (!EventBus.Instance.IsConnected(EventBus.SignalName.MonsterDied, callable))
{
    EventBus.Instance.Connect(EventBus.SignalName.MonsterDied, callable);
}
```

**最后，顺带回答一下"信号的触发顺序有没有保证"这个问题**：Godot 的信号是**同步**的——`EmitSignal` 这一行代码会按"连接建立的先后顺序"依次、原地调用每一个订阅者的处理函数，全部执行完毕，`EmitSignal` 这一行才算返回，不存在"排队到下一帧再触发"这种异步语义（除非你自己在处理函数里主动 `await` 或者 `CallDeferred`）。这意味着两件事：第一，如果你订阅的处理函数里有耗时逻辑（比如同步读一个大文件），会直接卡住发出信号的那一次调用，进而卡住那一帧；第二，也是更容易踩到的一个坑——**如果某个处理函数在响应信号的过程中，把另一个订阅者所在的节点 `QueueFree()` 掉了**（比如 `MonsterDied` 的其中一个处理函数负责"清理关卡里跟这只怪物关联的其他对象"），而那个被清理的对象**恰好也订阅了同一个信号、且排在它后面**，`QueueFree()` 本身不会立即从场景树移除节点（要等到这一帧结束），所以理论上不会在同一次 `EmitSignal` 内触发"调用一个已经不存在的对象"的错误，但如果处理函数里做的是更直接的清理（比如手动断开连接、或者把某个字段置空），后续同一次广播里排在后面的处理函数访问到的可能已经是"半清理"状态。这种"一个处理函数的副作用影响同一次广播里另一个处理函数看到的状态"的情况并不常见，但一旦发生会很难定位，因为报错现场（后面那个处理函数）离真正的原因（前面那个处理函数做了什么）在代码里可能隔得很远——如果你的处理函数会产生"删除/使某个对象失效"这类副作用，值得留意它是不是也被同一个信号的其他订阅者依赖着。

---

## 14. 数据驱动：把"这只怪物"变成"一份配置"

现在假设你想在游戏里加第二种怪物——一只近战速度更快、血更薄的"小怪"，和一只移动慢、血厚攻击重的"重甲怪"。按现在的写法，你得复制一份 `Enemy.cs`，改几个数字，存成 `FastEnemy.cs`——这样做的问题是：**每次想调一个数值（比如把所有怪物的视野角度统一调宽一点），都要挨个脚本文件去改**，而且策划/关卡设计者（哪怕这个角色也是你自己）如果不懂 C#，完全没法参与数值调整。

### 14.1 `Resource`：Godot 里的"数据资产"

Godot 有一种专门用来装"纯数据、可以在编辑器里像填表格一样编辑、可以存成独立文件复用"的类型：`Resource`。把怪物身上核心的"数值"部分拆出来，做成一份 `EnemyStats`（下面只演示最常调的这几项，9.5 节的 `PainThreshold`/`PainDebounce`、10.2.1 节的 `RagdollSettleDuration`/`RagdollSettleDamping` 这类没在下面出现的数值字段，照同样的思路自己加进 `EnemyStats` 就行，`Enemy.cs` 里对应保留为 `[Export]` 也不影响功能，只是没享受到数据驱动的好处）：

> 注意这里**没有** `AttackRange`/`AttackDamage`/`AttackCooldown`——9.4.1 节已经把这几个扁平字段换成了 `Attacks: Array<AttackDefinition>` 这张表，而 `AttackDefinition` 本身已经是 `[GlobalClass] Resource`，天然就能在编辑器里存成独立的 `.tres` 文件、跨怪物复用，本来就是数据驱动的，不需要再套一层塞进 `EnemyStats` 里。如果你还没做 9.4.1、`Enemy` 身上还是单一的 `AttackDamage`/`AttackRange` 字段，那把它们加进下面这份 `EnemyStats` 也完全没问题，两种写法不冲突，只是做了 9.4.1 之后这几个字段的数据驱动已经改由 `AttackDefinition` 各自负责了。

```csharp
using Godot;

[GlobalClass]
public partial class EnemyStats : Resource
{
    [Export] public float MaxHealth = 50.0f;
    [Export] public float MoveSpeed = 3.0f;
    [Export] public float ChaseRange = 15.0f;
    [Export] public float FieldOfViewDegrees = 100.0f;
    [Export] public float GibThreshold = -20.0f;
}
```

`[GlobalClass]` 这个特性很关键——加了它之后，Godot 编辑器会把 `EnemyStats` 识别成一种可以直接右键"新建资源"创建出来的类型，你可以在编辑器里新建 `imp_stats.tres`、`fast_enemy_stats.tres`、`heavy_enemy_stats.tres` 三个资源文件，分别填不同的数值，全程不用碰代码。

### 14.2 `Enemy.cs` 从"写死数值"改成"读一份配置"

```csharp
using Godot;

public partial class Enemy : CharacterBody3D
{
    [Export] public EnemyStats Stats;   // 在编辑器里把上面做的某份 .tres 文件拖进这个槽
    [Export] public Godot.Collections.Array<AttackDefinition> Attacks = new();   // 9.4.1 节的招式表，自己已经是 Resource，不需要经过 Stats

    private float _health;
    // ...

    public override void _Ready()
    {
        _health = Stats.MaxHealth;
        // 原本写死的 MoveSpeed、ChaseRange 等等，现在全部通过 Stats.xxx 读取
    }

    private void ChaseAndAttack(float dt)
    {
        // ...
        Velocity = new Vector3(direction.X * Stats.MoveSpeed, Velocity.Y, direction.Z * Stats.MoveSpeed);
        // ...
    }

    // TryAttack()/PickMeleeAttack() 等照 9.4.1/9.4.2 节的版本不变，直接读 Attacks，
    // 不经过 Stats——那几个字段的"数据驱动"已经靠 AttackDefinition 自己是 Resource 这件事解决了
}
```

现在场景里可以放三个 `Enemy.tscn` 的实例，各自的 `Stats` 槽拖进不同的资源文件、`Attacks` 槽拖进不同的招式表，就是三种手感完全不同的怪物——**同一份代码，靠不同的数据跑出不同的行为**，这正是"数据驱动"这个词的字面意思。想做第四种怪物，不需要写一行代码，新建一份 `.tres`、填几个数字就行。

### 14.3 武器同样适用这一套思路

回到第 5 章的 `Weapon.cs`，把 `Damage`、`Range`、`ClipSize`、`ReloadTime`、`FireRate` 这些字段拆成一份 `WeaponStats : Resource`，跟 `EnemyStats` 是同一个套路，这里不重复写一遍代码——**这一章真正想教的不是"怎么给怪物做配置"这一件具体的事，是"任何一组从多个具体实例里能看出规律的数值，都值得拆成一份独立的、非程序员也能编辑的数据"这个一般性的判断标准**，一旦你在怪物身上用顺手了，武器、拾取物、关卡里的任何"一类东西、多个变体"的场景，都可以套用同一个模式。

### 14.4 什么时候不该数据驱动

不是所有东西都该拆成 `Resource`。一个判断标准：**如果这个数值/行为在整个游戏里只会出现一次，硬拆成独立配置文件反而是额外的间接层，没有实际收益**。比如"最终 boss 房间那扇门的开启条件"，这种独一无二、只在一个地方用一次的逻辑，直接写在具体的脚本里就好，不需要为了"看起来更规范"而强行抽象成数据——过度数据驱动和过度抽象是同一类问题：多绕了一层，却没有换来真正的复用收益。

### 14.5 一个真实的坑：共享的 `Resource` 不能当可变状态用

这一节要单独拿出来讲，因为它不是"理论上可能发生"的边缘情况——`.tres` 资源在 Godot 里默认是**引用共享**的：场景里 10 只小怪的 `Stats` 字段都拖进同一份 `imp_stats.tres`，这 10 个 `[Export] public EnemyStats Stats` 字段在内存里指向的是**同一个 `EnemyStats` C# 对象**，不是各自独立的副本。14.1/14.2 节讲的"三份 `.tres` 就是三种手感的怪物"能成立，前提正是这份共享——这本身是数据驱动的优点，不是缺陷；但它有一个必须知道的边界：**任何时候只要有代码尝试在运行时修改 `Stats` 上的字段，这次修改会同时影响到所有引用同一份 `.tres` 的怪物实例，不只是你以为的"这一只"**。

上面 14.2 节的 `Enemy.cs` 目前是安全的——`_health = Stats.MaxHealth` 只是把值**读出来、拷贝进** `Enemy` 自己的字段，之后所有扣血/回血都发生在 `_health` 这个每个实例各自独立的字段上，`Stats.MaxHealth` 本身从始至终没有被写过。真正会踩坑的是后续你自己扩展系统时很容易写出来的代码，比如给怪物加一个"精英词缀"系统，想让某一只怪物的移动速度临时翻倍：

```csharp
// 危险写法——如果 Stats 是从共享的 .tres 拖进来的，这一行会让所有用同一份 .tres 的怪物
// 全部变成两倍速度，包括场景里跟这只怪物毫不相干的其他实例，且这个修改会一直生效到游戏重启
Stats.MoveSpeed *= 2.0f;
```

这行代码本身编译、运行都完全正常，不会有任何报错或警告，Bug 只会在"为什么我加了一个精英词缀之后满屏怪物突然都变快了"这种排查起来毫无头绪的场景里暴露出来——这正是这一类问题最阴险的地方：**没有异常、没有崩溃，只有一个跟你的修改意图不符的、悄悄扩散出去的副作用**。这不是本教程编出来的假想问题，是这一类"共享 `Resource` + 运行时可变字段"的组合本身就自带的一个坑，用过 Godot `Resource` 系统的人几乎都遇到过至少一次。

**解法只有一条原则：需要在运行时被修改的状态，永远不要直接存在共享的 `Resource` 字段上**，有两种具体做法：

1. **不修改 `Stats` 本身，把"修改结果"存在 `Enemy` 自己的字段上**——跟 `_health` 现在的做法完全一样，这是首选。精英词缀的例子应该写成 `EffectiveMoveSpeed`（9.9 节 Boss 阶段系统已经示范过这个模式：`EffectiveMoveSpeed => MoveSpeed * (...)`，这里同理）而不是直接改 `Stats.MoveSpeed`。
2. **如果确实需要"这一份配置从此以后专属于这一个实例、可以被随便改"**（比较少见，比如一个"boss 房间里独一份、要在战斗过程中动态调整数值"的特殊怪），Godot 提供了 `Duplicate()`：`Stats = (EnemyStats)Stats.Duplicate();` 在 `_Ready()` 里执行一次，之后这只怪物拿到的就是一份专属副本，随便怎么改都不会影响到其他实例——但代价是从这一刻起，这份配置**不再享受"改一份 `.tres` 全场同步生效"的数据驱动收益**，如果你后来想统一调整这个怪物类型的基础速度，这只用了 `Duplicate()` 的实例不会跟着变。Godot 编辑器里 `Resource` 的 Inspector 面板也有一个等价的开关叫 `resource_local_to_scene`，效果是"这份资源在当前场景里自动变成独立副本"，跟代码手动 `Duplicate()` 是同一件事的另一种入口，适合"我知道这份资源就是要跟这个场景绑死"的场景。

`AttackDefinition`（9.4.1 节）、`BossPhase`（9.9 节）这两个已经在用的 `Resource` 子类，同样适用上面这条原则——事实上 9.4.2 节把"上一次使用时间"从直觉上应该属于 `AttackDefinition` 自己的字段，特意搬到 `Enemy._attackReadyTime`（一个以 `AttackDefinition` 为 key 的字典）上，理由跟这里完全一致：`AttackDefinition` 可能被多只怪物共享，把运行时状态存在共享资源自己身上，会导致多只怪物的攻击冷却互相污染。回头看那一节的原话——"`AttackDefinition` 是 `Resource`，同一份资源理论上可能被多只怪物实例共享，如果把'上次使用时间'这种运行时状态也塞进 `Resource` 里，多只怪物共用同一条 `AttackDefinition` 时就会互相污染对方的冷却计时"——说的正是这一节讲的这同一类问题，只是那里没有点破"这是一类通用问题，不是 `AttackDefinition` 这一个类型独有的"，这里把它归纳成一条通用规则：**判断一个字段该不该放在共享的 `Resource` 上，只需要问一句"这个字段的值，是不是应该在所有引用同一份 `.tres` 的实例之间保持一致"——是，放 `Resource` 上；不是（哪怕只是"运行时会变化"这一点不一致），就必须挪到具体实例自己的类（`Enemy`）上，或者用字典按实例/按资源引用分别记账**。

---

## 15. 存档系统

玩家现在能死、能捡东西、能开门——如果没有存档，每次重开游戏都要从头再来一遍，体验会很糟。这一章把"当前游戏状态"写到磁盘上，下次读回来。

### 15.1 先想清楚：存什么、不存什么

不是场景里的一切都需要存。血量、位置、拾取物是否已被捡走、门是否已经打开——这些"读档后如果没恢复，玩家会觉得游戏世界不连贯"的状态需要存。粒子特效播放到第几帧、动画播放位置——这些"哪怕重置成默认状态玩家也不会觉得奇怪"的东西不需要存。**判断标准就是这一句话**，具体存不存看你自己的判断。

### 15.2 给需要存档的节点一个统一的接口

```csharp
public interface ISaveable
{
    Godot.Collections.Dictionary GetSaveData();
    void LoadSaveData(Godot.Collections.Dictionary data);
}
```

`Enemy` 实现它：

```csharp
public partial class Enemy : CharacterBody3D, ISaveable
{
    // ...

    public Godot.Collections.Dictionary GetSaveData()
    {
        return new Godot.Collections.Dictionary
        {
            { "position", GlobalPosition },
            { "health", _health },
            { "is_dead", _state == State.Dead },
        };
    }

    public void LoadSaveData(Godot.Collections.Dictionary data)
    {
        GlobalPosition = (Vector3)data["position"];
        _health = (float)data["health"];
        if ((bool)data["is_dead"])
        {
            Die();
        }
    }
}
```

`AmmoPickup`、`Door` 之类需要存"是否已触发/已拾取"这类状态的节点，照同样的模式各自实现一份。

### 15.2.1 存档要跟上第 9 章的状态机——不能停留在"血量+位置"

上面这版 `Enemy.GetSaveData()` 是本教程刚引入存档概念时的最简版本，只存了 `position`/`health`/`is_dead` 三样。但第 9 章给 `Enemy` 加了一整套运行时状态——`_state`（`Idle/Alert/Combat/Reposition/Charging/Dead`）、`_lastKnownPlayerPos`（警觉状态要去查看的最后已知位置）、`_currentPatrolPoint`（巡逻打断后要从哪个路点继续）、`_attackReadyTime`（每条招式各自的冷却）、`_lastUsedAttack`（`WeightedPick` 用来避免连续两次出同一招）、`_activePhase`/`_enraged`（Boss 阶段与激怒）——这些字段现在**一个都没有被存进去**。如果你的项目支持"战斗过程中随时存档、读档后从原地继续打"（这是大多数商业 FPS 的标准玩法，而不是只能在安全区存档），上面这版存档在读档瞬间会让所有敌人集体"失忆"：正在追你的怪物读档后变回站桩警觉，Boss 明明血量只剩 20% 却读档后又能立刻用满血阶段的招式（因为 `_activePhase` 归零重算前不会有问题，但如果玩家是存在了无敌窗口或者巡逻点走到一半的状态，行为衔接会非常突兀），所有招式的冷却全部清零（读档瞬间敌人可能对着你连续打出好几条刚才明明还在冷却的招式）。

**这不是这份教程自己拍脑袋加的顾虑**：翻真实 DOOM 3 源码的 `idAI::Save()`（`AI.cpp:411-543`）就知道，商业级 FPS 对这件事的态度是"AI 身上几乎所有运行时状态都要存"，随手挑几行能跟这里直接对上号的：`lastAttackTime`（对应这里的攻击冷却）、`enemy`（当前锁定的目标引用，用 `idEntityPtr::Save()`）、`lastVisibleEnemyPos`（对应这里的 `_lastKnownPlayerPos`）——DOOM 3 允许玩家在任意时刻（包括激烈战斗中）按下快速存档键，读档后战场状态要能完整复原，这不是一个可以退让的边缘需求。下面是补全后的版本，**替换掉上面 15.2 节那份简化的 `GetSaveData()`/`LoadSaveData()`**：

```csharp
// Enemy.cs —— 替换 15.2 节的 GetSaveData()/LoadSaveData()
using System.Collections.Generic;

public Godot.Collections.Dictionary GetSaveData()
{
    var data = new Godot.Collections.Dictionary
    {
        { "position", GlobalPosition },
        { "health", _health },
        { "state", (int)_state },
    };

    if (_lastKnownPlayerPos.HasValue)
        data["last_known_player_pos"] = _lastKnownPlayerPos.Value;

    if (_currentPatrolPoint != null)
        data["patrol_point_path"] = _currentPatrolPoint.GetPath().ToString();

    // AttackDefinition/BossPhase 都是 Resource，不能直接塞进存档字典——StoreVar 没有办法把
    // "游戏里到底是哪一份 Resource 引用"这件事原样落盘再原样读回来。改存它在这只怪物自己的
    // 攻击/阶段表里的下标，读档时反查回具体引用。14.5 节讲过"共享 Resource 不能存运行时可变状态"，
    // 这里是同一条原则的另一个体现：_attackReadyTime 的 key 本身就是 Resource 引用，
    // 存档格式必须经过一层"下标"才能落地，不能指望直接序列化这个引用
    var allAttacks = AllPossibleAttacks();
    var cooldowns = new Godot.Collections.Dictionary();
    double now = Time.GetTicksMsec() / 1000.0;
    foreach (var kv in _attackReadyTime)
    {
        int index = allAttacks.IndexOf(kv.Key);
        if (index < 0) continue;   // 理论上不会发生，除非场景在存读档之间改过 Attacks/Phases 配置
        double remaining = kv.Value - now;
        // 冷却是用 Time.GetTicksMsec() 起算的绝对时刻，读档时游戏计时器已经从零重新开始，
        // 直接存这个绝对值没有意义——存"还剩多久才能再用"这个相对值，读档时以读档那一刻重新起算
        if (remaining > 0) cooldowns[index.ToString()] = remaining;
    }
    data["attack_cooldowns"] = cooldowns;

    if (_lastUsedAttack != null)
    {
        int lastIndex = allAttacks.IndexOf(_lastUsedAttack);
        if (lastIndex >= 0) data["last_used_attack"] = lastIndex;
    }

    if (Phases.Count > 0)
        data["active_phase"] = _activePhase != null ? Phases.IndexOf(_activePhase) : -1;

    data["enraged"] = _enraged;

    return data;
}

public void LoadSaveData(Godot.Collections.Dictionary data)
{
    GlobalPosition = (Vector3)data["position"];
    _health = (float)data["health"];

    var allAttacks = AllPossibleAttacks();
    double now = Time.GetTicksMsec() / 1000.0;
    _attackReadyTime.Clear();
    if (data.ContainsKey("attack_cooldowns"))
    {
        var cooldowns = (Godot.Collections.Dictionary)data["attack_cooldowns"];
        foreach (var key in cooldowns.Keys)
        {
            int index = int.Parse((string)key);
            if (index < 0 || index >= allAttacks.Count) continue;
            _attackReadyTime[allAttacks[index]] = now + (double)cooldowns[key];
        }
    }

    if (data.ContainsKey("last_used_attack"))
    {
        int lastIndex = (int)data["last_used_attack"];
        if (lastIndex >= 0 && lastIndex < allAttacks.Count) _lastUsedAttack = allAttacks[lastIndex];
    }

    if (Phases.Count > 0 && data.ContainsKey("active_phase"))
    {
        int phaseIndex = (int)data["active_phase"];
        _activePhase = phaseIndex >= 0 && phaseIndex < Phases.Count ? Phases[phaseIndex] : null;
    }

    _enraged = data.ContainsKey("enraged") && (bool)data["enraged"];

    if (data.ContainsKey("last_known_player_pos"))
        _lastKnownPlayerPos = (Vector3)data["last_known_player_pos"];

    if (data.ContainsKey("patrol_point_path"))
        _currentPatrolPoint = GetTree().Root.GetNodeOrNull<PatrolPoint>((string)data["patrol_point_path"]);

    var state = (State)(int)data["state"];
    if (state == State.Dead)
        Die();
    else
        _state = state;
}

// Attacks 和每一阶段的 PhaseAttacks 拼在一起、按固定顺序去重，给存档用的稳定下标——
// 这份顺序只要 Attacks/Phases 在编辑器里配置好之后不再运行时改动就是稳定的。普通怪物
// （Phases 为空）这个列表就等于 Attacks 本身；Boss 的 PhaseAttacks 可能包含不在 Attacks
// 里的招式（9.9 节提到过两者"可以完全独占"），所以两边都要收进来，不能只用 Attacks
private List<AttackDefinition> AllPossibleAttacks()
{
    var all = new List<AttackDefinition>();
    foreach (var atk in Attacks) if (!all.Contains(atk)) all.Add(atk);
    foreach (var phase in Phases)
        foreach (var atk in phase.PhaseAttacks)
            if (!all.Contains(atk)) all.Add(atk);
    return all;
}
```

**哪些字段特意没有存，也说明一下理由，避免看起来像是漏掉的**：9.5 节的 `_lastPainTime`/`_staggerUntil`（疼痛硬直的计时）、9.9 节的 `_combatStartTime`（激怒计时的起点）都没有存——这几个都是"绝对时间戳"，而不是"剩余时长"，读档瞬间游戏的时间基准（`Time.GetTicksMsec()`）已经重新从零开始，如果不加处理直接照抄绝对时间戳，读档后这些计时器的语义会整个错乱（比如 `_staggerUntil` 存的是一个"游戏启动以来第几毫秒"，读档后这个数字大概率已经"过期"，效果上等于读档就自动解除硬直——这个副作用不算严重，甚至可以说是无伤大雅的巧合，但如果反过来 `_combatStartTime` 恰好没过期，会出现"读档瞬间激怒计时器里比原来的进度更靠后"这种说不清楚的状态）。按 15.1 节的判断标准衡量：疼痛硬直丢一次玩家完全感觉不到（下一次挨打会重新触发），激怒计时器丢一次顶多是"这场 Boss 战的风筝耐心值被重置了一点"，都不是"读档后世界看起来不连贯"的那类问题，值得为了省事而不存——这跟上面 `_attackReadyTime`/`_activePhase` 这些"不存的话，读档瞬间会打出明显不该出现的行为"的字段，是两类不同性质的取舍，不是"图省事漏掉一部分、认真做完另一部分"这种不一致的处理。

### 15.3 存档管理器：Autoload 单例

```csharp
using Godot;
using System.Collections.Generic;

public partial class SaveManager : Node
{
    public static SaveManager Instance { get; private set; }

    public override void _Ready()
    {
        Instance = this;
    }

    public void SaveGame(string slotName)
    {
        var records = new Godot.Collections.Array();

        foreach (Node node in GetTree().GetNodesInGroup("saveable"))
        {
            if (node is ISaveable saveable)
            {
                records.Add(new Godot.Collections.Dictionary
                {
                    { "scene_path", node.SceneFilePath },
                    { "node_path", node.GetPath().ToString() },
                    { "data", saveable.GetSaveData() },
                });
            }
        }

        using var file = FileAccess.Open($"user://{slotName}.sav", FileAccess.ModeFlags.Write);
        file.StoreVar(records);
        GD.Print($"已存档：{slotName}");
    }

    public void LoadGame(string slotName)
    {
        string path = $"user://{slotName}.sav";
        if (!FileAccess.FileExists(path))
        {
            GD.Print("没有找到存档");
            return;
        }

        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        var records = (Godot.Collections.Array)file.GetVar();

        foreach (var recordVariant in records)
        {
            var record = (Godot.Collections.Dictionary)recordVariant;
            string nodePath = (string)record["node_path"];
            Node node = GetTree().Root.GetNodeOrNull(nodePath);

            if (node is ISaveable saveable)
            {
                saveable.LoadSaveData((Godot.Collections.Dictionary)record["data"]);
            }
        }
        GD.Print($"已读档：{slotName}");
    }
}
```

把这个脚本注册成 Autoload（跟第 13 章 `EventBus` 一样的操作），把所有需要存档的节点加进 `saveable` 组（`Enemy`、`AmmoPickup`、`Door`……）。存档时调用 `SaveManager.Instance.SaveGame("slot1")`，读档时 `SaveManager.Instance.LoadGame("slot1")`——可以先绑几个测试用的按键触发，方便你现在就试。

**这里有一个简化**：上面的读档假设"场景结构在存档和读档之间完全没变"（靠 `node_path` 直接按路径找回节点）。这对"暂停游戏存一下、马上读回来测试"够用；如果要支持"读档时重新加载整个关卡场景、再把状态灌回去"（更常见的真实用法），需要先加载对应的关卡场景、等它加载完、再执行上面这套按路径查找的逻辑——多一步"先确保场景存在"，核心的数据读写逻辑不变。

### 15.4 存档时机

不要求玩家手动存档是更友好的体验——在关键节点自动存档：

```csharp
// 在你觉得合适的地方调用，比如某个 TriggerZone 走过去自动存档，
// 或者每次开一扇新门、清空一个房间之后
SaveManager.Instance.SaveGame("autosave");
```

---

## 16. HUD 与反馈

游戏现在能玩，但玩家看不到自己的血量、弹药，也没有任何"我打中了"的反馈。这一章补上这些。

### 16.1 血量与弹药显示

新建一个 `CanvasLayer` 场景，加 `Label` 节点显示血量/弹药，`Hud.cs`：

```csharp
using Godot;

public partial class Hud : CanvasLayer
{
    private Label _healthLabel;
    private Label _ammoLabel;

    public override void _Ready()
    {
        _healthLabel = GetNode<Label>("HealthLabel");
        _ammoLabel = GetNode<Label>("AmmoLabel");

        var player = GetTree().GetFirstNodeInGroup("player");
        player.Connect(PlayerController.SignalName.HealthChanged, new Callable(this, MethodName.OnHealthChanged));
    }

    private void OnHealthChanged(float current, float max)
    {
        _healthLabel.Text = $"HP {Mathf.CeilToInt(current)}/{Mathf.CeilToInt(max)}";
        _healthLabel.Modulate = current < max * 0.25f ? Colors.Red : Colors.White;
    }
}
```

这正是第 13 章讲的信号系统第一次真正派上用场的地方——`Hud` 完全不需要每帧去读玩家血量，玩家血量变化时自己会通知它。回头对比一下，如果没有第 13 章的信号系统，你可能会写成"`Hud` 在 `_Process` 里每帧读一次 `player.Health`"——能跑，但每帧查询是浪费，而且血量以外的每一种状态都要重复这个模式，信号系统在这里的收益是实打实的。

**弹药这一栏不能照抄血量的写法，直接复制粘贴——上一版这里写了句"思路和血量一样"就跳过了，但实际上不一样**：`PlayerController.Health` 从第 8 章开始就是一个单一的数字，而弹药从 5.2.2 节开始就已经不是了——`WeaponManager` 管的是一个按 `AmmoType` 索引的共享储备字典，同一个"弹药数字"要看你问的是"当前手上这把枪的弹匣还剩几发"（`Weapon.CurrentAmmo`，每把枪各自独立）还是"这把枪用的这种弹药，储备还有多少"（`GetReserveAmmo(weapon.AmmoType)`，同类型武器共享）。HUD 通常只需要显示"当前手持这把枪"对应的这两个数字，不需要（也不该）把玩家身上所有弹药类型的库存都摆出来。要做到这一点，通知的时机也比血量复杂——不只是"挨打了才变"，开火、换弹、切枪、5.2.2 节以及 11.1 节的捡弹药，任何一个都可能改变这两个数字：

```csharp
// WeaponManager.cs 追加——弹药 HUD 要显示的是"当前手持武器的弹匣 + 它所用弹药类型的共享储备"，
// 两者任何一处变化都要通知一次，所以专门开一个信号，而不是让 Hud 自己每帧去读 CurrentWeapon 的字段
[Signal] public delegate void AmmoDisplayChangedEventHandler(int clipAmmo, int reserveAmmo);

public void NotifyAmmoDisplayChanged()
{
    if (CurrentWeapon == null) return;
    EmitSignal(SignalName.AmmoDisplayChanged, CurrentWeapon.CurrentAmmo, GetReserveAmmo(CurrentWeapon.AmmoType));
}
```

在 `Weapon.Fire()`、`Weapon.Reload()` 结尾，`WeaponManager` 切枪逻辑结尾，以及 11.1 节 `AddReserveAmmo`/`GiveAmmo` 结尾，各补一行调用 `NotifyAmmoDisplayChanged()`——凡是会改变"当前弹匣还有几发"或者"这种弹药还剩多少储备"这两个数字里任意一个的地方，都要触发一次这个通知，漏掉任何一处，HUD 在那个操作之后到下一次开火之前都会显示过期的数字（比如切枪之后弹药栏还停留在上一把枪的数字，直到你对着新枪开一枪才刷新）：

```csharp
// Hud.cs 追加
public override void _Ready()
{
    // ...原有初始化...
    var player = GetTree().GetFirstNodeInGroup("player");
    var weaponManager = player.GetNode<WeaponManager>("WeaponManager");   // 路径按你项目实际的节点树结构改
    weaponManager.Connect(WeaponManager.SignalName.AmmoDisplayChanged, new Callable(this, MethodName.OnAmmoDisplayChanged));
}

private void OnAmmoDisplayChanged(int clipAmmo, int reserveAmmo)
{
    _ammoLabel.Text = $"{clipAmmo} / {reserveAmmo}";
}
```

### 16.2 受伤反馈：屏幕红一下，以及伤害是从哪个方向来的

```csharp
// Hud.cs 追加
private ColorRect _damageFlash;

public override void _Ready()
{
    // ...
    _damageFlash = GetNode<ColorRect>("DamageFlash");
    _damageFlash.Color = new Color(1, 0, 0, 0);
}

private void OnHealthChanged(float current, float max)
{
    // ...原有逻辑...
    FlashDamage();
}

private async void FlashDamage()
{
    _damageFlash.Color = new Color(1, 0, 0, 0.4f);
    var tween = CreateTween();
    tween.TweenProperty(_damageFlash, "color:a", 0.0f, 0.3f);
    await ToSignal(tween, Tween.SignalName.Finished);
}
```

**这一屏幕闪红，只回答了"我挨打了"，回答不了商业 FPS 玩家真正在意的下一个问题——"从哪边打过来的"**。全屏一致地闪一下红色，玩家没有任何线索知道该往哪个方向转身应对，尤其是被身后或者侧面的敌人偷袭时，这个信息缺失会直接影响玩家能不能做出合理反应。**先说清楚这不是从 DOOM 3 抄来的**：真实源码 `idPlayer::Damage()` 确实会算一个 `lastDamageDir`（伤害来向，`Player.cpp:8564-8565`），但它拿这个方向去做的事跟现代 FPS 常见的"HUD 方向指示箭头"完全不同——`playerView.DamageImpulse(lastDamageDir * viewAxis.Transpose(), &def->dict)`（`Player.cpp:10131`）把这个方向转换到玩家自己的视角空间，喂给 `idPlayerView::DamageImpulse()`（`PlayerView.cpp:241`），效果是**按受击方向给镜头一次瞬间的甩动（head kick）+ 一段"双重视觉"（double vision，画面短暂重影/模糊）**——本质上是"让玩家的视野本身受到冲击"，不是在屏幕边缘画一个箭头。2004 年的 FPS 设计语言里，"受击方向指示箭头"这个 HUD 元素还不是行业标配，它更多是后来（大致从 2007 年前后的《使命召唤 4》一类游戏开始）才逐渐变成主流第一人称射击游戏的标准配置。所以这里要加的方向指示器，**是一个本教程基于"现代商业 FPS 通用手感"主动补充的功能，不是对 DOOM 3 源码的复原**——如果你想要更贴近 DOOM 3 原版那种"镜头甩动+重影"的手感，也完全可以做，思路是复用第 3 章视角系统已经搭好的效果叠加机制，给镜头再叠加一次基于受击方向的瞬时旋转冲量，这里不展开，跟方向指示箭头两者并不冲突，可以同时做。

要显示方向，`TakeDamage` 得知道伤害是从哪里来的——但 6 章以来所有调用点都是 `hitObject.Call("TakeDamage", damage)` 这种单参数写法，13.3 节已经明确说过"改 `TakeDamage` 签名会牵连所有调用点，这里不做"，同样的顾虑在这里依然成立。解法是**不碰 `TakeDamage` 本身，另开一个独立的、可选调用的方法**，两者各自负责各自的事——不知道这个方法存在的调用点完全不受影响，伤害结算和 `HealthChanged` 信号照常工作：

```csharp
// PlayerController.cs 追加——独立于 TakeDamage，谁想要方向反馈就顺手多调一次，不想要就不调，
// 完全不影响 6 章以来已经写好的所有 TakeDamage 调用点
[Signal] public delegate void DamageTakenEventHandler(Vector3 sourcePosition);

public void NotifyDamageDirection(Vector3 sourcePosition)
{
    EmitSignal(SignalName.DamageTaken, sourcePosition);
}
```

`Enemy.ExecuteMeleeAttack()`/`AttackRanged()`（9.4.2/9.8 节）里调用 `_player.Call("TakeDamage", finalDamage)` 的地方，紧挨着补一行 `_player.Call("NotifyDamageDirection", GlobalPosition);`——**这一行不是强制的**：不加这一行，游戏完全正常运行，只是这一次命中不会触发方向指示，不会报错、也不会有任何副作用，你可以按自己的节奏挑"值得做方向反馈"的伤害来源（比如只给怪物近战/远程攻击加，不给环境陷阱加）逐个补上。

```csharp
// Hud.cs 追加
[Export] public NodePath DamageDirectionIndicatorPath;
[Export] public NodePath CameraPath;   // 指向 3 章那台第一人称 Camera3D

private TextureRect _damageDirectionIndicator;   // 一张箭头贴图，Pivot 对齐图片中心，默认朝上（指向正前方）
private PlayerController _player;
private Camera3D _camera;

public override void _Ready()
{
    // ...原有初始化...
    _damageDirectionIndicator = GetNode<TextureRect>(DamageDirectionIndicatorPath);
    _damageDirectionIndicator.Visible = false;
    _player = GetTree().GetFirstNodeInGroup("player") as PlayerController;
    _camera = GetNode<Camera3D>(CameraPath);
    _player.Connect(PlayerController.SignalName.DamageTaken, new Callable(this, MethodName.OnDamageTaken));
}

private void OnDamageTaken(Vector3 sourcePosition)
{
    Vector3 toSource = sourcePosition - _player.GlobalPosition;
    toSource.Y = 0;   // 只处理水平方向上"从哪一侧打过来的"，不处理"从楼上/楼下"这种垂直分量——
                       // 这是商业 FPS 方向指示器的通行做法，不是这里省事简化的
    if (toSource.LengthSquared() < 0.01f) return;   // 贴身命中，方向没有意义

    Vector3 camForward = -_camera.GlobalTransform.Basis.Z; camForward.Y = 0; camForward = camForward.Normalized();
    Vector3 camRight = _camera.GlobalTransform.Basis.X; camRight.Y = 0; camRight = camRight.Normalized();
    toSource = toSource.Normalized();

    // atan2(右侧分量, 前方分量)：正前方为 0，右侧为正角度，左侧为负角度——
    // 直接拿这个角度旋转一张"默认朝上"的箭头贴图，角度和箭头朝向的对应关系正好一致
    float angle = Mathf.Atan2(camRight.Dot(toSource), camForward.Dot(toSource));

    _damageDirectionIndicator.PivotOffset = _damageDirectionIndicator.Size / 2f;
    _damageDirectionIndicator.Rotation = angle;
    _damageDirectionIndicator.Visible = true;

    var tween = CreateTween();
    tween.TweenInterval(0.6f);
    tween.TweenCallback(Callable.From(() => _damageDirectionIndicator.Visible = false));
}
```

`DamageDirectionIndicator` 建议做成一个摆在屏幕正中央、初始朝向"正上方"的箭头图标（`TextureRect`），上面这段代码只负责转它的角度、控制它的显隐——真实商业 FPS 通常会做成"箭头沿着一个以准星为圆心的圆周分布在受击方向那一侧"而不是原地转向，效果更精细，但原理是同一个角度计算，摆位方式属于美术呈现层面的选择，这里只搭最基础的骨架。**这个 tween 每次受击都会重新创建一个**——如果连续两次受击间隔很短，前一个 tween 还没跑完，后一个会覆盖它的显隐结果（`Visible = true` 会被立刻重新设置），不会报错也不会叠加出奇怪的效果，只是指示器会跟着最新一次命中重新计时，这正是你想要的行为（永远反映"最近一次受击的方向"），不需要额外处理。

### 16.3 交互提示文字

回到第 12 章的 `UpdateFocus()`，把那个占位的 `GD.Print` 换成真正的信号广播，让 `Hud` 订阅并显示/隐藏提示文字——具体写法跟血量显示是同一个模式（`PlayerController` 发信号，`Hud` 订阅），这里不再重复整段代码，作为练习留给你自己完成：需要一个 `FocusChanged(Node3D newFocus)` 信号，`Hud` 订阅后判断 `newFocus` 是否为空、是否有 `GetInteractPrompt` 方法（如果你想让不同的可交互物体显示不同的提示文字，就给它们各自实现一个返回字符串的 `GetInteractPrompt` 方法，`Hud` 调用它拿到文字显示出来，没实现这个方法的就不显示提示——这与第 4 章"鸭子类型式调用"是同一个模式的又一次复用）。

---

## 17.（可选）联机合作

这一章是可选的——如果你做的是纯单机游戏，可以完全跳过，直接看第 18 章。如果你想让好友能加入你的世界一起打（不是那种需要专门服务器运维的竞技对局，就是"我开一局，朋友直接连进来"的合作模式），这一节给一个最小可用的起点。

### 17.1 建立连接

```csharp
using Godot;

public partial class NetworkManager : Node
{
    public static NetworkManager Instance { get; private set; }
    private const int Port = 7777;

    public override void _Ready()
    {
        Instance = this;
    }

    public void HostGame()
    {
        var peer = new ENetMultiplayerPeer();
        peer.CreateServer(Port, 8);
        Multiplayer.MultiplayerPeer = peer;
        GD.Print("已开始host，等待玩家加入");
    }

    public void JoinGame(string address)
    {
        var peer = new ENetMultiplayerPeer();
        peer.CreateClient(address, Port);
        Multiplayer.MultiplayerPeer = peer;
        GD.Print($"正在连接到 {address}");
    }
}
```

### 17.2 伤害判定必须只在 host 一侧生效

这是联机代码里最容易出错、也最重要的一条规则：**任何会真正改变游戏状态的判断（谁死了、谁掉了多少血），只能由一个权威的一方（host）说了算**，客户端只能"请求"，不能自己直接判定。用 `[Rpc]` 特性实现这个约束：

```csharp
// Enemy.cs 追加
[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
public void RequestDamage(float amount)
{
    if (!Multiplayer.IsServer()) return;   // 只有 host 真正执行伤害逻辑，客户端调用这个方法会被这一行拦下
    TakeDamage(amount);
}
```

原本直接调用 `TakeDamage(amount)` 的地方（比如 `CombatUtil.RaycastAttack`），改成调用 `enemy.RpcId(1, MethodName.RequestDamage, amount)`（`RpcId(1, ...)` 表示"把这次调用发给编号为 1 的对等端"，host 默认就是编号 1）——任何一端都能发起这个请求，但只有 host 真正执行伤害。

### 17.2.1 只把伤害判定挡住，AI 这一侧还留着一个更大的口子

**上面这条规则只处理了"谁掉血、谁死了"，但第 9 章的 `Enemy` 现在远不止"扣血"这一件需要权威判定的事**——这里要老实指出一个如果不特别处理、这一章的方案根本无法真正联机运行的问题：`Enemy._PhysicsProcess()` 里跑的那一整套状态机（`TickIdle`/`TickAlert`/`TickCombat`/`TickReposition`，9.2 节的视觉/听觉感知，9.4.2 节 `WeightedPick` 的随机权重选招，9.9 节冲锋的路径验证，9.9 节 Boss 的阶段切换判定），从第 8 章写下第一行代码开始，**从来没有被限制成"只在某一端跑"**——按目前的写法，一旦联机，host 和每一个客户端会各自独立地对同一只 `Enemy` 跑一遍完整的 AI 决策，包括各自独立的 `RandomNumberGenerator` 抽样。这意味着：host 这边 `WeightedPick` 可能抽中了近战攻击，客户端那边因为随机数不同抽中了远程攻击，两边的怪物做出的实际动作会从这一刻开始彻底分叉——玩家在客户端屏幕上看到的怪物行为，和 host 屏幕上看到的、真正决定伤害判定的那一份行为，可能完全对不上。这不是"伤害判定不同步"这一类问题（那一类已经被上面的 `RequestDamage` RPC 挡住了），而是**表现层面就已经不一致**：客户端玩家会看到怪物在原地放一个火箭筒动画，但被 `RequestDamage` 真正判定命中的却是（host 视角里）它其实正在近战挥爪，两边对不上号，观感上会非常诡异。

**这不是从 DOOM 3 里来的、可以照抄的答案**——DOOM 3 BFG 的怪物系统设计年代早于它的多人对战模式，多人模式里出现的都是玩家角色之间的战斗，源码里找不到"多个客户端各自独立运行同一只 AI 决策逻辑"这种场景需要解决的问题，所以这里没有源码可以对照，纯粹是本教程基于"只要 AI 状态机在多台机器上各自独立运行、又依赖随机数或者其他非确定性输入，就必然会分叉"这条通用的网络同步原理，主动指出的一个必须处理的缺口。解法跟"伤害判定只在 host 生效"是同一个思路的延伸——**AI 的决策逻辑本身也只应该在 host 一侧真正跑**，客户端只负责显示 host 算出来的结果：

```csharp
// Enemy.cs —— 替换 8/9 章的 _PhysicsProcess()
public override void _PhysicsProcess(double delta)
{
    float dt = (float)delta;
    Vector3 velocity = Velocity;
    if (!IsOnFloor()) velocity.Y -= Gravity * dt;
    Velocity = velocity;

    // 只有 host（或者单机模式，这时 Multiplayer.HasMultiplayerPeer() 为 false）才真正跑状态机——
    // 客户端完全不参与决策，它们的 Position/Rotation/当前动画状态全部交给 MultiplayerSynchronizer
    // （17.3 节）从 host 同步过来，本地这份状态机代码在客户端上根本不会被执行，
    // 自然也就不存在"客户端自己抽了一个不一样的随机数"这类分叉的可能
    if (Multiplayer.HasMultiplayerPeer() && !Multiplayer.IsServer())
    {
        MoveAndSlide();   // 客户端仍然需要走一次物理更新，让本地的碰撞体跟上同步过来的 Position，
                           // 但不做任何"决定怪物接下来要干什么"的判断
        return;
    }

    switch (_state)
    {
        case State.Idle: TickIdle(); break;
        case State.Alert: TickAlert(); break;
        case State.Combat: TickCombat(dt); break;
        // ...其余分支不变...
        case State.Dead: MoveAndSlide(); return;
    }

    MoveAndSlide();
}
```

`MultiplayerSynchronizer`（17.3 节）除了 `Position`/`Rotation`，还需要把**决定了怪物"看起来在干什么"的那部分状态**也加进同步列表——至少是 `_state`（决定播放哪一组动画）和"当前正在执行的具体是哪一条 `AttackDefinition`"（决定播放哪一段攻击动画）。`_attackReadyTime`/`_lastUsedAttack` 这类只影响 host 自己"下一次该选哪一招"的内部决策状态，不需要同步给客户端——客户端反正不会用它们做任何判断，同步了也没有意义，只会浪费带宽。

### 17.3 位置同步

角色的位置/朝向这类"持续变化、需要让所有端看到基本一致"的状态，不需要为每一帧手写一个 RPC——给 `Enemy`/`PlayerController` 加一个 `MultiplayerSynchronizer` 子节点，在它的 Inspector 面板里把 `Position`、`Rotation` 这些属性加进"要同步的属性"列表，Godot 会自动处理好"host 的值周期性广播给其他端"这件事，你不需要手写任何同步代码。

**这一整章对本教程的定位是加分项，不是必需品**——如果你的目标就是做一个扎实的单人 FPS，完全可以永远不实现这一章，前面 16 章教的所有内容在纯单机模式下都是完整、独立、可以直接发布的。

---

## 18. 延伸阅读：如果你想知道 DOOM 3 原版是怎么做的

这份教程教你的是"用现代的 Godot + C#，做出一个 DOOM 3 风格的单人 FPS"。经过前面几轮修订，第 2-3、6、7、9、10 章里凡是涉及**具体手感公式/数值算法**的部分（移动加速度、视角 Bob、武器摇摆后坐、近战两阶段检测、疼痛打断、保底不死、布娃娃沉降），绝大部分都已经是照着 DOOM 3 BFG 源码的真实实现还原的，不是随手拍脑袋编的数字——如果你对照 [DOOM3-BFG-Gameplay架构精读.md](DOOM3-BFG-Gameplay架构精读.md) 里对应的章节，应该能一一找到对应关系。但"逐行还原"这个说法要打个折扣：正文里明确标注过好几处**有意为之、且写清楚了原因**的数值/机制简化（比如 3.2 节 view bob 幅度系数刻意调大了一倍、3.3 节落地冲击用峰值下落速度近似源码真正的子帧冲击量计算、10.2.1 节布娃娃摩擦力凹陷用单调阻尼近似源码的 V 形双时间线、9.9 节冲锋攻击的路径验证和保底豁免），这些都不是疏漏，是标注过的取舍，但也不该被笼统地称为"逐行还原"。真正还存在、且**应该存在**的差异，除了这些标注过的数值简化，还有两大类：**代码组织方式的不同**（DOOM 3 拆成很多个协作的 C++ 类，本教程为了教学连贯性把很多东西写在少数几个脚本里）、以及**平台能力的不同**（DOOM 3 手写的一些底层机制，Godot 已经作为引擎内置能力提供，不需要重新发明）。按章节的对应关系：

| 本教程 | 对应精读文档章节 | 差异属于哪一类 |
|---|---|---|
| 第 2-3 章：玩家移动/视角 Bob | 精读第 5 章 `idPlayer`/`idPhysics_Player`，5.8/5.10/5.11 节 | 代码组织：DOOM 3 把"游戏逻辑"和"纯物理仿真"拆成 `idPlayer`/`idPhysics_Player` 两个类，本教程写在一个 `PlayerController` 里；公式本身（加速度、摩擦力、爬梯磁吸/双通道输入、台阶步进的贴地判定、bob 四个分量、落地冲击触发条件）已逐项对齐，其中 view bob 的三个幅度系数（`BobUpAmount` 等）标注过是刻意调大到原版的两倍，为的是在 Godot 这套单位下手感可感知 |
| 第 4-6 章：武器系统 | 精读第 7 章 | 代码组织：DOOM 3 的武器状态机用它自己发明的 DOOM Script 写，本教程直接用 C#——因为 C# 本身已经是热重载友好的脚本语言，不需要再嵌一层脚本虚拟机（精读 9.8 节详细论证过这个取舍）；切枪时"举枪动画播完才允许收枪"、火箭筒范围伤害的遮挡判定、近战推力接入增益查询点这几处都已对齐，5.2.1 节"开火打断装弹"和 5.4 节追踪导弹的随机抖动/抵近脱锁两处仍是明确标注过的未实现项 |
| 第 7 章：物理与门/电梯 | 精读第 6、10 章 | 平台能力：DOOM 3 手写了完整的推挤/防穿模算法（`idPush`），Godot 用 `AnimatableBody3D` + `SyncToPhysics` 内置解决了同一个问题；团队门联动的重定向模式、以及"中途被挡住会整队反向"这个团队系统真正存在的理由，都已经对齐；7.3 节也澄清了"两站式升降台"（结构等同门）和真正的多楼层 `idElevator`（独立的大协调器）不是同一回事，本教程只实现前者 |
| 第 8-9 章：AI | 精读第 8 章 | 平台能力：DOOM 3 的寻路系统（AAS）是自己写的一整套区域图+缓存路由算法，Godot 用内置导航网格系统解决同一个问题；两阶段近战检测、疼痛三道门、保底不死、难度数值表已逐项对齐；9.4.1 节把原来"一只怪物一个 `AttackDamage`"的写法换成了 `AttackDefinition` 招式表，对应源码里近战/远程/冲锋分别接受名字或关节参数、能表达"一只怪物多种打法"这件事；9.9 节的冲锋攻击还留有一处没有完全数据驱动的不对称（借用表里一条近战条目的数值，但执行时不走 9.4.1 的保底判定，正文里已经说明原因和边界） |
| 第 10 章：布娃娃 | 精读 6.5 节 | 平台能力：DOOM 3 的布娃娃约束求解器是自己手写的（拉格朗日乘子法），Godot 用内置物理引擎的 `PhysicalBoneSimulator3D` 解决同一个问题；慢动作沉降效果因为 Godot 不支持单物体时间缩放，改用阻尼渐变实现，这一点在 10.2.1 节已经明确说明是不同机制、相近效果，不是偷懒；10.2.1 节的摩擦力凹陷曲线（真实源码是 V 形双时间线，关节和接触各走各的）目前也只用了单调阻尼近似，正文同样已经标注清楚不是逐段还原；10.3 节的肢解在结构上（整体切换掉 + 单独生成飞散碎块，而不是从活体骨架上拆一根骨头）其实是对齐的，`AFEntity.cpp` 里 `Gib()`/`SpawnGibs()`/`GIB_DELAY` 已在正文中逐条核对；10.4 节的尸体数量上限是本教程主动加的，源码里没有对应机制，正文已明确标注这一条不算差异（不是"该对齐却没对齐"，是源码本身也没有这个东西） |
| 第 11-12 章：关卡工具 | 精读第 10 章 | 代码组织：DOOM 3 原版有几十种专门的 `target_*` 类，本教程用"只要有 `Activate` 方法就能被调用"这一个更通用的模式覆盖了同样的需求；11.2/12.2 节的重复触发防抖（`Wait` 字段）现在是对齐过的——`idTrigger_Multi` 的 `wait`/`nextTriggerTime` 机制（`Trigger.cpp:257-336`）已经在正文中核对并还原，默认值 0.5 秒也是源码本身的默认值，不是本教程另挑的数字 |
| 第 13 章：事件系统 | 精读第 2 章 | 平台能力：DOOM 3 自己实现了一整套类型反射+事件派发系统（`idClass`/`idEvent`），因为 C++ 没有这些能力；C# 本身自带反射和信号，不需要自己造；13.5 节讲的信号生命周期管理（`_ExitTree` 里断开订阅、避免重复订阅）是 C#/Godot 这套实现自己特有的注意事项，DOOM 3 的 `idEvent` 系统不存在对应的坑，不需要类比 |
| 第 14 章：数据驱动 | 精读第 3 章 | 平台能力：DOOM 3 用纯字符串的键值字典（`spawnArgs`）做数据驱动，因为它面向的是文本编辑的 `.map`/`.def` 文件；`Resource` 是 Godot 编辑器原生支持的强类型资源，两者目的相同；14.5 节讲的共享 `Resource` 运行时可变状态陷阱，同样是 Godot 这套强类型资源系统自己特有的坑，`spawnArgs` 那种纯文本字典不会有这个问题（每个实体各自持有自己那份字典的拷贝），不需要类比 |
| 第 15 章：存档 | 精读第 4 章 | 代码组织+有意简化：DOOM 3 用的是两阶段索引式序列化（先枚举全部对象、按索引建立空壳、再统一回填数据），能支持"场景结构在存读档之间发生变化"这种更通用的情况；本教程 15.3 节为了教学连贯性，用了更简单的"按 `node_path` 直接找回已存在节点"的实现，15.3 节末尾已经明确写清楚这个简化的适用边界（假设场景结构没变）以及要支持真正的关卡重载该怎么补；15.2.1 节把第 9 章新增的战斗/感知/阶段状态也补进了存档，这一点的必要性是照着 `idAI::Save()`（`AI.cpp:411-543`）核实过的——真实源码确实把 `lastAttackTime`/`lastVisibleEnemyPos` 这类运行时 AI 状态当作存档的一部分，不是本教程过度设计 |

还剩下两处**明确标注过是有意选择、不是简化**的地方，值得在这里再强调一遍，避免被误认为是疏漏：第 4.4 节的命中部位缩放依赖角色有细分碰撞体积，本教程用的单胶囊体碰撞暂时不支持，给出了两条扩展路径；第 6.4 节的视图/世界模型分离是纯单机、纯第一人称项目可以直接跳过的可选内容。这两处都在正文里明确写清楚了原因和补全方式，不是含糊带过。

---

*本文与 [DOOM3-BFG-Gameplay架构精读.md](DOOM3-BFG-Gameplay架构精读.md)、[FPS引擎路线图.md](FPS引擎路线图.md)、[Phase0.1-Quake3架构精读.md](Phase0.1-Quake3架构精读.md) 共同构成 Re_Shirox 项目的阅读材料。本文示例代码为 Godot 4.x C#（.NET 版），运行前请确认引擎版本一致；随着 Godot 版本更新，个别 API（尤其 `PhysicalBoneSimulator3D`、`NavigationAgent3D`）的方法名可能变化，请对照当时的官方文档核实。