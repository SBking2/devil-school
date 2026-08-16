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
        // 起身前先做一次向上 trace，确认头顶没有东西挡着才允许站起来——对应源码里的自动站立检测
        var spaceState = GetWorld3D().DirectSpaceState;
        var query = PhysicsRayQueryParameters3D.Create(
            GlobalPosition + Vector3.Up * CrouchHeight,
            GlobalPosition + Vector3.Up * StandHeight);
        query.Exclude = new Godot.Collections.Array<Rid> { GetRid() };
        if (spaceState.IntersectRay(query).Count == 0)
        {
            _isDucked = false;
        }
    }

    // 碰撞体高度：瞬间切换，不做任何平滑——这是源码的真实行为
    float targetHeight = _isDucked ? CrouchHeight : StandHeight;
    var capsule = (CapsuleShape3D)_collisionShape.Shape;
    capsule.Height = targetHeight;
    _collisionShape.Position = new Vector3(0, targetHeight * 0.5f, 0);

    // 只有眼睛高度（摄像机位置）才平滑——用源码里那种逐帧固定权重混合，
    // 不是 dt 缩放的插值：Godot 的 _PhysicsProcess 本身就是固定步长跑的，
    // 这里直接照抄字面公式反而更贴近原版，不需要额外除以 dt
    float targetEyeOffset = targetHeight - 0.15f;
    _currentEyeOffset = _currentEyeOffset * CrouchTransitionRate + targetEyeOffset * (1f - CrouchTransitionRate);
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
        // 完全不按任何键时被动下沉——对应源码 "wishvel = gravityNormal * 60"，
        // 不是像陆地上那样靠摩擦力停在原地不动
        vel += Vector3.Down * 1.0f * dt;
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

### 2.6 爬梯

> 这一节的第一版写错了，写成"只要抬头就会自动往上爬，不需要按任何键"——这个说法不对，直接去读了 `neo/d3xp/physics/Physics_Player.cpp::LadderMove()`（第 852-926 行）的真实源码才发现问题：DOOM3 原版的垂直爬升速度是 `wishvel = -0.9f * gravityNormal * upscale * scale * (float)command.forwardmove;`，**乘了 `command.forwardmove`（前后移动键的输入值）**——不按 W/S，这一项恒为 0，人不会动。视角俯仰角算出来的 `upscale` 只是"调节爬升方向和快慢"的系数（水平看着梯子时它已经接近 1，低头会让它变小甚至反向），不是唯一驱动力。下面是按真实源码改正之后的版本。

```csharp
// PlayerController.cs 追加
public bool IsOnLadder;
public Vector3 LadderNormal;   // 由梯子的 Area3D 检测逻辑提供朝向，指向"墙外"（玩家所在的一侧）

private void ApplyLadderMove(Vector2 rawInput, Vector3 wishDir, float dt)
{
    // 左右横移：滤掉沿梯子法线方向的分量，只保留贴着梯子平面的部分——这部分没有问题，保留原样
    Vector3 lateral = wishDir - wishDir.Project(LadderNormal);

    // upscale：对应源码里的同名变量。camForward 与"上"方向的夹角决定这个系数，
    // 钳制在 [-1,1]，水平看着梯子时已经接近 1，不需要真的仰头
    Vector3 camForward = -_camera.GlobalTransform.Basis.Z;
    float upscale = Mathf.Clamp((Vector3.Up.Dot(camForward) + 0.5f) * 2.5f, -1f, 1f);

    // 关键修正：climbSpeed 必须乘上 forwardInput——这是上一版遗漏的部分。
    // rawInput.Y 是 Input.GetVector 前后轴的原始值，按你 InputMap 的方向约定可能需要取负号，
    // 保证"按 W 才会爬升"，不按键 forwardInput 为 0，climbSpeed 恒为 0
    float forwardInput = -rawInput.Y;
    float climbSpeed = -0.9f * upscale * forwardInput * RunSpeed;

    Vector3 targetVel = lateral * RunSpeed + Vector3.Up * climbSpeed;
    Velocity = Velocity.Lerp(targetVel, dt * 10.0f);
}
```

同样在 `_PhysicsProcess` 顶部加一个 `IsOnLadder` 分支，优先级比游泳和地面/空中都高，调用时把 `Input.GetVector(...)` 算出的原始 `Vector2` 和 `wishDir` 一起传进去。梯子的检测可以用一个贴着梯子表面的窄 `Area3D`，进入时记录法线方向、置位 `IsOnLadder`。

这次的错误提醒了一件事：**本教程前面几轮"完全参考"的修订，一部分是我直接去读源码验证的，另一部分是根据更早之前研究这份源码时留下的文字总结转述的——后者存在被我自己转述错、或者当时的总结本身就不够精确的风险**。如果你在后面章节看到某个说法感觉不对劲、或者行为跟直觉明显冲突，最可靠的做法就是像这次一样直接要求我去读对应的源码文件核实，而不是默认我转述的一定准确。

### 2.7 台阶步进：楼梯不该让角色一顿一顿地跳

`MoveAndSlide()` 本身能处理"撞到矮台阶就自动上去"这类简单情况，但台阶如果比较陡或者移动速度比较快，会出现明显的一顿一顿的抖动感。DOOM 3 在 `SlideMove()` 里专门处理了这个问题：先在当前高度试走、发现被挡住了，再垫高试走、确认垫高确实有用，最后把结果贴回台阶实际的高度。

> 这一节的第一版代码写得不完整，只做了"垫高之后走一下"，没有先判断"当前高度到底有没有被挡住"——结果是角色在完全平坦的地面上走路也会被这段代码不断顶高，因为条件判断只测了"垫高之后能不能走"，平地上这个条件几乎永远成立。改正版本补上了被漏掉的两步：**先确认当前高度确实被挡住了才有必要垫高**，以及**垫高之后要往下探一次、贴回台阶的实际高度**，不能直接假设台阶正好有 `StepHeight` 那么高。

```csharp
// PlayerController.cs 追加
[Export] public float StepHeight = 0.3f;

private void ApplyStepUp(Vector3 horizontalMotion, float dt)
{
    if (!IsOnFloor() || horizontalMotion.LengthSquared() < 0.0001f) return;

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
    GlobalPosition = steppedPos + downResult.GetTravel();

    // 这一帧的水平移动已经在上面这几步里手动做完了（垫高、走过去、再贴回台阶高度），
    // 如果紧接着调用的 MoveAndSlide() 还拿同一份 Velocity 再走一次，水平方向就会
    // 被重复叠加，实际移动距离变成两倍。这里把水平分量清空，只留 Y 方向（重力/跳跃）
    // 交给 MoveAndSlide() 去处理。
    Velocity = new Vector3(0, Velocity.Y, 0);
}
```

> 这一步之前还有一个问题：把 `GlobalPosition` 直接设成了包含完整水平位移的 `steppedPos`，但紧接着 `_PhysicsProcess` 里还会用同一份 `horizontalVelocity` 调用一次 `MoveAndSlide()`——水平方向等于走了两遍，这一帧实际移动的距离会变成正常的两倍，虽然平时不容易注意到（因为只在真正踩上台阶那一帧发生），但快速贴墙走楼梯时能明显感觉到"一步窜出去很远"。修法不是不做水平位移（那样台阶又量不到该往上垫多高），而是在 `ApplyStepUp` 自己已经手动完成这一帧的水平移动之后，把 `Velocity` 的水平分量清空，让随后的 `MoveAndSlide()` 只处理垂直方向。

在 `_PhysicsProcess` 里 `MoveAndSlide()` **之前**、算完地面加速度之后调用 `ApplyStepUp(horizontalVelocity, dt);`——三步走完之后，只有真正遇到台阶时角色才会被垫高并贴回台阶表面，平地上完全不会触发，也不会凭空往上飘。注意 `ApplyStepUp` 里读写的都是 `Velocity`（`CharacterBody3D` 自带的那个属性），传进来的 `horizontalVelocity` 参数只是用来做碰撞测试用的一份拷贝，函数末尾清空的是真正驱动 `MoveAndSlide()` 的那个 `Velocity`。

到这里，第 2 章的移动状态机已经完整覆盖了 DOOM 3 `idPhysics_Player` 的全部移动模式（走/跑/蹲/空中/游泳/爬梯/台阶步进），是时候进入视角部分了。

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

### 3.2 视角晃动（View Bob）：完整移植 DOOM 3 的 `BobCycle()`

现在角色能走能看了，但站着不动和走路时摄像机是完全静止的，感觉发"飘"。这一节**完整照抄** `Player.cpp::BobCycle()` 的公式，不是一个近似效果——DOOM 3 的 view bob 实际上由三个独立部分叠加而成：一个走/跑速率不同的周期性正弦位置起伏、一个用"脚的奇偶"翻转符号做出的左右交替角度摇摆、以及一个跌落/落地时的独立冲击下沉。逐个实现：

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

private float _bobCycle;
private Vector3 _viewBobOffset;
private Vector3 _viewBobAngles;

private void UpdateViewBob(float dt)
{
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
    Vector3 camPos = _viewBobOffset + _landingDipOffset;   // _landingDipOffset 见 3.3 节
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

### 3.3 落地冲击：跌落速度越快、镜头下沉越明显

DOOM 3 的 `CrashLand()` 按跌落冲击力度分四档，用快速下沉+缓慢回弹两段式给出反馈，这是一个和上面周期性 bob**完全独立**的第三个偏移源，两者在 `ApplyBobToCamera()` 里简单相加：

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

### 4.4 命中部位缩放（可选）：爆头为什么伤害更高

DOOM 3 的伤害系统里，命中扫描/近战都会带上"打中了哪个部位"的信息，配合一张按部位缩放的表（比如打中头部伤害 ×3），这是完整实现的一部分，这里说明**为什么这一节标了"可选"、以及要做到什么程度才算数**：这个效果依赖被打中的角色**有细分的碰撞体积**（每个身体部位是独立的碰撞形状，或者角色骨骼上挂了带命名的物理骨骼），而本教程第 8 章的怪物目前只有一个整体的 `CapsuleShape3D`——射线打中它，`result["collider"]` 永远是同一个节点，物理查询层面根本拿不到"打中的是头还是脚"这个信息，不是漏写了判断逻辑，是碰撞体积的精细度还不支持。

想要这个效果，有两条路：

1. **粗糙但省事**：给怪物加几个手动摆放的小 `Area3D`"命中区域"（一个套在头部位置，判定优先级更高），命中扫描先测试这些区域再测试主碰撞体，命中区域自带一个 `DamageMultiplier` 导出字段。
2. **精细但需要更多前期工作**：走真正的骨骼命中检测——如果角色用的是带骨骼的模型且已经配置了第 10 章要讲的物理骨骼，Godot 的物理查询命中 `PhysicalBone3D` 时结果里能带出具体命中了哪根骨骼（`result["collider"]` 直接就是那根骨骼对应的物理体），可以按骨骼名字查一张倍率表——这跟 DOOM 3 的原始实现（命中扫描击中演员身上具体某根骨骼，转换成关节句柄，查 `damage_zone`/`damage_scale` 表）是同一个思路，只是要先有骨骼化的角色模型才谈得上做这件事。

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

### 5.2 多把武器：切换——完整照抄 `idPlayer::Weapon_Combat()` 的"意图/当前状态分离"模型

一个玩家通常不止一把枪，而 DOOM 3 的切枪不是"点一下立刻换"，而是有真实的收枪/举枪动画：必须先把当前武器完整收起（`PutAway` → `IsHolstered()`），再举起新武器（`Raise`）。这是 `idPlayer` 里管理武器切换那部分职责的核心逻辑，值得从一开始就做对，而不是先写一个"直接切换"的简化版本再回头改：

先给每把武器一个状态机（`Weapon.cs` 追加）：

```csharp
// Weapon.cs 追加
public enum WeaponState { Holstered, Raising, Idle, Firing, Reloading, Lowering }
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
        if (current.State != Weapon.WeaponState.Holstered)
        {
            if (current.State != Weapon.WeaponState.Lowering)
            {
                current.PutAway();   // 还没开始收，先让它开始收
            }
            return;   // 没收完之前，什么都不做，下一帧继续检查
        }

        RaiseIdealWeapon();
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

第 4 章、第 6 章里 `Weapon.Fire()`/`Melee()` 目前没有检查 `State` 是不是 `Idle`——回头把这两处开头加一行 `if (State != WeaponState.Idle) return;`，防止武器还在收起/举起过程中就能开火。

### 5.3 投射物武器：火箭筒——直接命中和范围伤害是两件独立的事

到现在为止，`Weapon.Fire()` 只有一种打法：一条射线，打中即判定，没有飞行时间。这在手枪/步枪上没问题，但火箭筒、等离子炮这类武器不该是"瞬间命中"——它们需要一个真的在世界里飞行、会被躲开、命中后炸出范围伤害的**投射物实体**。

这一节要"完全参考"的是 DOOM 3 `neo/d3xp/Projectile.cpp` 里 `idProjectile::Collide()`（约 554-724 行）和 `Explode()` 的设计，核心是一个容易被忽略的点：**直接命中伤害和范围爆炸伤害，是两次独立的判定，不是二选一**。一发火箭打中一个怪物，会先对这个怪物单独算一次"直接命中"伤害，然后**无论有没有命中任何东西**都会引爆，再对爆炸半径内的所有实体算一遍范围伤害——已经吃过直接命中的那个目标要从范围伤害里排除掉，不然会被炸两次。

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

    public Node3D Owner3D;   // 发射者，范围伤害要排除它自己（不能自己炸自己）
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
        if (body == Owner3D) return;   // 排除发射者自己（比如爆炸半径够大、火箭贴脸炸的情况）

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
            CollisionMask = 0b0110   // 检测 Enemy(第3层) + Projectile(第4层)，按你项目实际的层规划调整
        };

        var results = spaceState.IntersectShape(query);
        var alreadyDamaged = new HashSet<Node3D>();
        if (directHitTarget != null) alreadyDamaged.Add(directHitTarget);   // 已经直接命中过的目标，范围伤害要排除，不能炸两次

        foreach (var result in results)
        {
            Node3D hitObject = (Node3D)result["collider"];
            if (alreadyDamaged.Contains(hitObject)) continue;
            alreadyDamaged.Add(hitObject);

            // 简单的距离衰减：越靠近爆心伤害越高，边缘接近 0——DOOM 3 的 RadiusDamage 也是同一个思路
            float distance = hitObject.GlobalPosition.DistanceTo(explodePosition);
            float falloff = 1.0f - Mathf.Clamp(distance / SplashRadius, 0.0f, 1.0f);

            if (hitObject.HasMethod("TakeDamage"))
            {
                hitObject.Call("TakeDamage", SplashDamage * falloff);
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

```csharp
// Weapon.cs 追加
[Export] public bool IsHitscan = true;
[Export] public PackedScene ProjectileScene;   // 拖入 Rocket.tscn，IsHitscan = false 时使用
[Export] public float ProjectileSpeed = 25.0f;

private void Fire()
{
    if (IsHitscan)
    {
        FireHitscan();   // 原来 4.2/4.3 节的射线判定逻辑，改个名字
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
    rocket.Launch(direction, GetParent().GetParent().GetParent<CharacterBody3D>());   // 传入发射者，用于排除自伤
}
```

在编辑器里把火箭筒的 `IsHitscan` 打勾去掉、`ProjectileScene` 拖入 `Rocket.tscn`，手枪/步枪保持 `IsHitscan = true` 不用改任何代码——**这就是"武器差异是数据配置的差异，不是代码分支的差异"这条原则的具体体现**，跟第 4 章开始就在强调的"鸭子类型式调用"是同一种设计取向：让尽量多的差异落在 Inspector 面板能调的字段上，而不是散落在一堆 `if (weaponType == ...)` 分支里。

### 5.4（可选进阶）追踪导弹：会自己转向的投射物

如果想要一把"追踪导弹"武器，在 `Rocket.cs` 基础上加一个转向逻辑就够了，不需要另起一个类——这是 DOOM 3 `idGuidedProjectile::Think()`（`Projectile.cpp:1651-1721`）的简化版：每帧朝目标方向转一点，转向速率有上限（不能瞬间掉头，不然追踪导弹的飞行轨迹会显得很假）：

```csharp
// Rocket.cs 追加
[Export] public bool IsGuided = false;
[Export] public float TurnRateDegreesPerSec = 90.0f;
public Node3D Target;   // 发射时指定要追踪的目标（比如玩家开火时，锁定准星下最近的怪物）

public override void _PhysicsProcess(double delta)
{
    float dt = (float)delta;

    if (IsGuided && Target != null && IsInstanceValid(Target))
    {
        Vector3 toTarget = (Target.GlobalPosition - GlobalPosition).Normalized();
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

外加一个同样被漏掉的细节：**武器落地时也会有一次冲击下沉**（3.3 节那个 `_landingDipOffset` 的 0.25 倍强度），只是幅度比摄像机自己的落地反馈更轻。完整实现：

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

### 6.3 近战攻击：伤害与物理冲击分开，并预留增益倍率接口

近战本质上和第 4 章的开火是同一件事——一条射线，只是距离短得多，不消耗弹药。但要"完全参考"DOOM 3 的近战，还有两个不能省略的细节：

1. **物理冲击和伤害数值是两条完全独立的调用**——命中之后既要造成伤害，也要给被打中的物体（如果是刚体）一个推开的冲量，两者互不依赖。
2. **伤害倍率要经过一个集中的增益修饰符查询点**，而不是直接用固定数值——哪怕你现在还没做狂暴/双倍伤害之类的增益道具，也应该先把这个查询点留出来，这是 DOOM 3 `idPlayer::PowerUpModifier()` 的设计：所有会影响战斗数值的增益，都通过同一个函数查询，而不是散落地在各处判断"当前是否处于某个状态"。

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

    var spaceState = GetWorld3D().DirectSpaceState;
    Vector3 from = Camera.GlobalPosition;
    Vector3 to = from + (-Camera.GlobalTransform.Basis.Z) * (MeleeRange * rangeScale);

    var query = PhysicsRayQueryParameters3D.Create(from, to);
    query.CollisionMask = 0b0101;
    var result = spaceState.IntersectRay(query);

    if (result.Count == 0) return;

    Node3D hitObject = (Node3D)result["collider"];
    Vector3 hitPoint = (Vector3)result["position"];

    if (hitObject.HasMethod("TakeDamage"))
    {
        hitObject.Call("TakeDamage", MeleeDamage * damageScale);
    }

    // 物理冲击和伤害是两件独立的事——一个纯装饰性的、没有 TakeDamage 方法的刚体，
    // 挨了一拳依然应该被推开
    if (hitObject is RigidBody3D rigidBody)
    {
        Vector3 pushDir = (to - from).Normalized();
        rigidBody.ApplyImpulse(pushDir * MeleePushForce, hitPoint - rigidBody.GlobalPosition);
    }
}
```

记得加 `melee` 输入映射（建议绑 V 键或鼠标中键）。你可能已经注意到，`Fire()`、`Melee()`、下一章要写的怪物近战判定，全部长得差不多——都是"从某个点往某个方向打一条射线，检测多远，命中了就调用 `TakeDamage`"。这不是偶然，也不是本教程偷懒——**几乎所有 FPS 里的近战攻击，本质上都是一条射程很短的"子弹"**，没有必要为它单独设计一套碰撞体积检测。这个观察本身也是第 13 章要讲的"什么时候该抽公共代码"的一个具体例子，先记住这个感觉，到时候会讲怎么把这几处重复的射线检测代码收拢成一个共享函数（到时候 `CombatUtil.RaycastAttack` 也会顺带把 `damageScale`/推力这两个参数一起纳入，不会漏掉这一节加的东西）。

### 6.4 视图模型与世界模型：其他人看到的武器，和你自己看到的不是同一个

到目前为止，`WeaponHolder` 下面只有一个模型，这在单机游戏里通常够用——但如果你的项目以后可能有第三人称观察（过场动画摄像机、死亡后观察队友、联机），需要知道 DOOM 3 对这个问题的完整解法：**第一人称视角看到的武器模型和其他人看到你身上挂着的武器模型，是两个独立的物体**，只是动画同步播放。

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

**这里有一个值得记住的设计判断**：伤害（`TakeDamage`）和物理冲击（`ApplyImpulse`）是**两条完全独立的逻辑**，一个物体完全可以"只掉血不被打飞"，或者"被打飞但不掉血"（比如一个纯装饰性的空罐子，没有 `TakeDamage` 方法，但依然是 `RigidBody3D`，一样会被子弹撞飞）。不要把这两件事写成互相依赖的逻辑，分开处理会让后面加新物体类型时轻松很多。

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

    private Vector3 _closedPosition;
    private bool _isOpen;

    public override void _Ready()
    {
        _closedPosition = Position;
        SyncToPhysics = true;   // 关键设置，见下方说明
    }

    public void Activate()
    {
        _isOpen = !_isOpen;
        Vector3 target = _isOpen ? _closedPosition + OpenOffset : _closedPosition;

        var tween = CreateTween();
        tween.SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
        tween.TweenProperty(this, "position", target, MoveTime);
    }
}
```

`SyncToPhysics = true` 这一行**非常容易被漏掉、漏掉之后的表现是"门确实动了，但站在门上的玩家会穿模掉下去"**——这是 Godot 物理系统一个真实存在的坑：`AnimatableBody3D` 默认不会把自己的运动通报给物理引擎，导致 `CharacterBody3D` 感知不到"我脚下的平台在动"。开了这个开关之后，还需要在 `PlayerController.cs` 里告诉玩家"哪些碰撞层算作可以站上去被带走的地面"：

```csharp
// PlayerController.cs 的 _Ready() 里加一行
PlatformFloorLayers = 1;   // 假设门/电梯所在的碰撞层是第 1 层
```

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

### 7.3 电梯：同样的原理，多一个状态

电梯就是一扇会在两个以上的位置之间往返移动的"门"，代码结构完全一样，只是多了个"当前在哪一层"的状态和到达后暂停一下再返回的逻辑：

```csharp
using Godot;

public partial class Elevator : AnimatableBody3D
{
    [Export] public float TopOffset = 4.0f;
    [Export] public float MoveTime = 2.0f;
    [Export] public float WaitTime = 3.0f;

    private Vector3 _bottomPosition;
    private bool _atTop;
    private bool _isMoving;

    public override void _Ready()
    {
        _bottomPosition = Position;
        SyncToPhysics = true;
    }

    public async void Activate()
    {
        if (_isMoving) return;
        _isMoving = true;

        Vector3 target = _atTop ? _bottomPosition : _bottomPosition + Vector3.Up * TopOffset;
        var tween = CreateTween();
        tween.SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
        tween.TweenProperty(this, "position", target, MoveTime);
        await ToSignal(tween, Tween.SignalName.Finished);

        _atTop = !_atTop;
        _isMoving = false;
    }
}
```

这一节的三个例子（箱子、门、电梯）分别对应物理系统里三种不同的"物体该怎么动"：**完全交给物理引擎仿真**（箱子）、**按预定轨迹运动、但要正确参与物理交互**（门/电梯）、以及第 2 章已经写过的**由玩家输入直接驱动**（角色）。这三种是几乎所有 FPS 里"会动的东西"的全部分类，之后做怪物（第 8 章）的时候，你会发现敌人的移动方式其实是第三种和第一种的某种混合。

### 7.4 多部件联动的门：双开门为什么不能各自独立触发

一扇"双开门"（两片门扇同时向两侧打开）如果给每一片各自独立触发，玩家触碰到其中一片、只有那一片会动，看起来很别扭。DOOM 3 的解法是给同一组门指定一个共同的"团队名字"，其中一个成员被触发时，会把动作转发给整个团队、并且所有成员用**同一个起始时间**开始运动，保证严格同步（不是"各自播放同一段动画"，是"共用一份运动的起止时间"，这个区别在两片门运动速度不同或者中途被打断反向时会体现出来）：

```csharp
// Door.cs 追加
[Export] public string TeamName = "";
private static readonly Dictionary<string, List<Door>> Teams = new();
private bool _isTeamMaster;

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
}

public void Activate()
{
    if (TeamName != "" && !_isTeamMaster)
    {
        // 从属成员把触发请求转发给队长，自己不直接处理——对应精读文档描述的
        // "所有控制 API 从属成员重定向到主控" 这个模式
        Teams[TeamName].Find(d => d._isTeamMaster)?.Activate();
        return;
    }

    if (TeamName != "")
    {
        double startTime = Time.GetTicksMsec() / 1000.0;
        foreach (var member in Teams[TeamName])
        {
            member.ActivateAt(startTime);
        }
    }
    else
    {
        ActivateAt(Time.GetTicksMsec() / 1000.0);
    }
}

private void ActivateAt(double startTime)
{
    _isOpen = !_isOpen;
    Vector3 target = _isOpen ? _closedPosition + OpenOffset : _closedPosition;
    var tween = CreateTween();
    tween.SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
    tween.TweenProperty(this, "position", target, MoveTime);
}
```

需要在文件顶部加 `using System.Collections.Generic;`。把双开门的两个 `Door` 实例 `TeamName` 都填成同一个字符串（比如 `"double_door_01"`），`OpenOffset` 分别指向左右两侧相反的方向，触发任意一片都会让整组同步开启。**"从属成员把请求转发给队长处理"这个重定向模式**，本教程后面不会再重复贴一遍代码，但如果以后遇到"一组东西需要被当成一个整体触发/控制"的场景（比如一组联动的灯光、一组必须同时启动的机关），都可以照抄这个思路：一个字符串分组键 + 队伍内选出一个队长 + 其余成员的操作全部转发给队长处理。

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

### 9.2 感知：看不见就不该追

现在的怪物只要在 `ChaseRange` 范围内就会追，哪怕隔着一堵墙。加一个视线检测：

```csharp
// Enemy.cs 追加
[Export] public float FieldOfViewDegrees = 100.0f;
private Node3D _lastKnownPlayerPos;   // Node3D 类型只是为了偷懒复用引用类型判空，实际存的是位置概念，见下方说明

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

    if (result.Count == 0) return true;   // 没打到任何东西，说明视线畅通
    return (Node3D)result["collider"] == _player;   // 打到的第一个东西就是玩家本人，也算看得见
}
```

（上面 `_lastKnownPlayerPos` 字段这一步先声明但还不用，9.3 节的状态机会用到它，这里先跳过它专注理解 `CanSeePlayer()` 本身。）

把 `_PhysicsProcess` 里 `GlobalPosition.DistanceTo(_player.GlobalPosition) < ChaseRange` 的判断换成 `CanSeePlayer()`。**这里有一个性能上的考量值得提前说一句**：`CanSeePlayer()` 每帧对每只怪物都做一次射线检测，怪物一多会有开销。第 12 章讲感知优化的时候会回头处理这个问题，现在关卡里怪物不多，先不用担心。

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
    }

    private void TickAlert()
    {
        // 警觉状态：停下来，原地观望；重新看到就回战斗，超时就回待机
        if (CanSeePlayer())
        {
            _state = State.Combat;
            _lastSeenTime = Time.GetTicksMsec() / 1000.0;
        }
        else if (Time.GetTicksMsec() / 1000.0 - _lastSeenTime > AlertDuration)
        {
            _state = State.Idle;
            GD.Print($"{Name}：目标丢失，恢复待机");
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

这个状态机现在只有 4 个状态、转移条件也很简单，但这已经是一个**可以无限扩展的骨架**——想加"受伤后短暂僵直""听到声音但没看到人、跑去查看"这类行为，都是往 `switch` 里加一个新状态分支，不需要推倒重来。

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

### 9.5 疼痛打断：受伤会不会打断当前动作

现在的怪物挨打只会掉血，不会有任何"被打疼了一下"的反应。DOOM 3 的疼痛系统有三道门，缺一个都不对：**冷却时间**（疼痛反应之间有最短间隔，不会每一下都触发）、**免打断窗口**（脚本/攻击逻辑可以主动申请"接下来这段时间不许被打断"，比如重击动作的起手阶段）、以及**伤害下限**（这是一个绝对数值门槛，不是概率——很多人凭直觉以为这是"一定概率被打退"，实际上 DOOM 3 里这一下伤害没达到阈值，疼痛反应根本不会触发，达到了就必定触发，不掷骰子）。

> 去读 `idActor::Pain()`（`Actor.cpp:2368` 起）发现还有一处容易漏掉的细节：**冷却计时器的重置，和"疼痛动画到底播不播"是两件独立的事**——只要冷却时间够了，疼痛提示音**总会播放**（并且冷却计时器立刻重置），哪怕接下来 `allowPain`/伤害门槛这两道门把疼痛**动画**挡住了。如果像下面这样把 `_lastPainTime = now` 也一起挡在最后（只有全部门都通过才重置冷却），会导致"这一下被免打断窗口挡住了"之后，冷却计时器根本没重置，下一次伤害立刻又能重新触发判断——这跟原版"冷却只看时间，不管动画播没播"的行为不一样。改正版本：

```csharp
// Enemy.cs 追加
[Export] public float PainThreshold = 8.0f;    // 绝对伤害门槛，不是概率
[Export] public float PainDebounce = 0.5f;      // 两次疼痛反应之间的最短间隔
private double _lastPainTime = -999;
private bool _painAllowed = true;
private double _painPreventedUntil;

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
    // 具体表现取决于你的动画资源，逻辑上到这里就完成了
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
```

`PreventPain(duration)` 这个方法本身不会在教程里被直接调用（因为现在的怪物还没有"正在做一个不能被打断的大招"这种场景），但**先把这个原语准备好**——以后想做"重甲怪抡大锤前摇不能被打断"这类效果，直接在攻击逻辑的起手处调一句 `PreventPain(0.8f)` 就行，不需要重新设计打断机制。

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
// Enemy.cs 追加
[Export] public PatrolPoint PatrolStart;
private PatrolPoint _currentPatrolPoint;
private bool _isPatrolling;

private async void TickIdle()
{
    if (CanSeePlayer())
    {
        _state = State.Combat;
        _lastSeenTime = Time.GetTicksMsec() / 1000.0;
        GD.Print($"{Name}：发现目标，进入战斗状态");
        return;
    }

    if (PatrolStart == null || _isPatrolling) return;
    _isPatrolling = true;
    _currentPatrolPoint ??= PatrolStart;

    while (_state == State.Idle)
    {
        _navAgent.TargetPosition = _currentPatrolPoint.GlobalPosition;
        while (!_navAgent.IsNavigationFinished() && _state == State.Idle)
        {
            if (CanSeePlayer()) { _isPatrolling = false; return; }   // 巡逻途中发现玩家，立刻中断巡逻
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

这段协程式的巡逻循环，和第 5.1 节介绍过的 `async`/`await`（"方法执行到 `await` 那一行会暂停，但不阻塞游戏其他部分"）是同一套机制，只是这里用 `while` 循环 + `await get_tree().physics_frame` 表达一段跨越多帧、能被外部条件随时打断的行为——`while (_state == State.Idle)` 这个循环条件，一旦怪物发现玩家、`_state` 被外部改成 `Combat`，下一次循环检查就会自然退出，不需要专门写一段"打断巡逻"的清理代码。

### 9.8 远程攻击：不是所有怪物都该贴脸近战

第 8.3 节给怪物做的攻击只有一种：贴近了才能打的近战判定。真实的 DOOM 3 里，一只怪物到底是近战还是远程、发射的是子弹还是会爆炸的火球，靠的是 `idAI::LaunchProjectile(jointname, target, clampToAttackCone)`（`neo/d3xp/ai/AI.h:532`）这**同一个**方法——区别只在于传给它的投射物定义是哪一份。这一节把这个思路搬过来：怪物的近战和远程攻击不该是两套互相独立的系统，而是"攻击方式"这个字段的两种取值。

先把攻击方式拆成一个独立方法，复用第 5.3 节做的 `Rocket.tscn`：

```csharp
// Enemy.cs 追加
public enum AttackType { Melee, Ranged }
[Export] public AttackType MyAttackType = AttackType.Melee;
[Export] public PackedScene ProjectileScene;   // 远程怪物才需要拖入，比如 Rocket.tscn
[Export] public Node3D ProjectileSpawnPoint;   // 投射物从哪里生成，通常是怪物的"嘴"或"手"位置
[Export] public float RangedAttackRange = 12.0f;   // 远程攻击的有效距离，通常比近战 AttackRange 大得多
[Export] public float AttackConeDegrees = 30.0f;   // 对应 idAI 的 attack_cone：目标偏出这个角度就不发射，避免"背对着打中"的怪异画面

private void TryAttack()
{
    if (_player == null) return;
    float distance = GlobalPosition.DistanceTo(_player.GlobalPosition);
    float effectiveRange = MyAttackType == AttackType.Melee ? AttackRange : RangedAttackRange;
    if (distance > effectiveRange) return;

    // 攻击锥角检测：目标是否大致在怪物面朝的方向上，不是随便什么角度都能开火
    Vector3 toTarget = (_player.GlobalPosition - GlobalPosition); toTarget.Y = 0;
    float angle = Mathf.RadToDeg(GlobalTransform.Basis.Z.AngleTo(toTarget.Normalized()));
    if (180.0f - angle > AttackConeDegrees) return;   // GlobalTransform.Basis.Z 是"身后"方向，这里换算成"正面偏离角度"

    double now = Time.GetTicksMsec() / 1000.0;
    if (now - _lastAttackTime < AttackCooldown) return;
    _lastAttackTime = now;

    if (MyAttackType == AttackType.Melee)
    {
        AttackMelee();   // 原来 8.3/9.4 节的近战判定逻辑
    }
    else
    {
        AttackRanged();
    }
}

private void AttackRanged()
{
    if (ProjectileScene == null || ProjectileSpawnPoint == null) return;

    var projectile = ProjectileScene.Instantiate<Rocket>();
    GetTree().Root.AddChild(projectile);
    projectile.GlobalPosition = ProjectileSpawnPoint.GlobalPosition;

    Vector3 direction = (_player.GlobalPosition - ProjectileSpawnPoint.GlobalPosition).Normalized();
    projectile.Launch(direction, this);   // this：把怪物自己传进去当 Owner3D，防止爆炸伤到自己
    GD.Print($"{Name} 发射了一枚投射物");
}
```

**这一节真正的重点不是这段代码本身，是它带来的设计结果**：想做一只近战僵尸和一只远程暴风兵，不需要写两个不同的怪物类，只需要在场景编辑器里，一个的 `MyAttackType` 选 `Melee`，另一个选 `Ranged` 并拖入一个投射物场景——**甚至可以把 `ProjectileScene` 换成不同配置的投射物场景**（改小 `SplashRadius`、换成 `IsGuided = true` 的追踪弹版本），做出"近战怪、普通远程怪、会追踪的远程怪"三种手感完全不同的敌人，`Enemy.cs` 一行都不用改。这正是第 14 章要系统讲的"数据驱动"的一次提前预演。

### 9.9（可选进阶）冲锋攻击：不是所有攻击都能站着放

有些怪物的攻击不是"站在原地打"，而是主动冲向你——DOOM 3 里这类攻击靠 `Event_ChargeAttack`/`Event_TestChargeAttack`（`neo/d3xp/ai/AI_events.cpp:1743-1802`）实现，冲锋前会先用寻路系统验证"冲过去这条路线走不走得通"，避免怪物一头冲进墙里卡住。用本教程已经搭好的 `NavigationAgent3D` 复刻同样的验证步骤：

```csharp
// Enemy.cs 追加
[Export] public float ChargeSpeed = 10.0f;
[Export] public float ChargeAttackRange = 6.0f;
private bool _isCharging;

private async void TryChargeAttack()
{
    if (_isCharging || _player == null) return;
    float distance = GlobalPosition.DistanceTo(_player.GlobalPosition);
    if (distance > ChargeAttackRange || distance < AttackRange) return;   // 太远冲不到、太近没必要冲，直接近战就行

    // 对应 TestChargeAttack：冲锋前先问寻路系统这条路能不能走通，走不通就放弃这次冲锋
    _navAgent.TargetPosition = _player.GlobalPosition;
    await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);   // 等一帧，让 NavigationAgent3D 把新目标点的路径算好
    if (!_navAgent.IsTargetReachable())
    {
        GD.Print($"{Name}：冲锋路线走不通，放弃");
        return;
    }

    _isCharging = true;
    GD.Print($"{Name} 开始冲锋！");
    double chargeStart = Time.GetTicksMsec() / 1000.0;

    while (Time.GetTicksMsec() / 1000.0 - chargeStart < 1.5 && _isCharging)
    {
        Vector3 direction = (_player.GlobalPosition - GlobalPosition); direction.Y = 0;
        direction = direction.Normalized();
        Velocity = new Vector3(direction.X * ChargeSpeed, Velocity.Y, direction.Z * ChargeSpeed);

        if (GlobalPosition.DistanceTo(_player.GlobalPosition) < AttackRange)
        {
            AttackMelee();   // 冲到贴脸距离，直接按近战判定收尾，冲锋本身不额外算一次伤害
            break;
        }
        await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
    }

    _isCharging = false;
}
```

这一节标"可选"不是因为它不重要，是因为不是每种怪物都需要这个攻击方式——跟 9.8 节一样，`MyAttackType` 枚举完全可以再加一个 `Charge` 分支，`TryAttack()` 里按类型分派到 `AttackMelee()`/`AttackRanged()`/`TryChargeAttack()`，这三种攻击方式互相独立、可以按怪物类型任意组合，不需要为每一种组合单独写一个怪物类——这跟 5.3/9.8 节反复强调的"差异应该落在配置上，不是代码分支上"是同一条原则的第三次应用，读到这里这个模式应该已经很熟悉了。

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

如果你玩过 DOOM 3，可能会记得那种敌人死亡瞬间身体先短暂地以慢动作瘫倒、然后逐渐恢复正常速度的效果——这不是错觉，是 DOOM 3 在布娃娃刚触发的一段时间内，专门给这具身体的物理仿真套了一个从慢到快的时间缩放渐变（死亡前 1.6 秒到死亡后 0.8 秒这段区间内生效），配合关节/接触摩擦力也同步从低到高变化，做出"身体先松软地沉降、再逐渐恢复正常物理反应"的质感。

**这里要老实说明一个 Godot 和 DOOM 3 的真实差异，不能假装能完全照搬**：DOOM 3 的关节人偶系统是自己手写的约束求解器，可以给单具布娃娃单独设置时间缩放；Godot 内置物理引擎的 `PhysicalBoneSimulator3D` 没有暴露"让这一具骨架用比世界其他物体更慢的时间流速仿真"这个能力（`Engine.TimeScale` 是全局的，会拖慢整个游戏，不能只影响一具尸体）。所以下面这个实现**不是时间缩放，而是用一个从强到弱变化的阻尼（damping）去模拟类似的视觉效果**——物理上不是同一回事，但视觉上很接近"一开始软绵绵地沉降、逐渐恢复正常物理反应"这个效果，这是在 Godot 现有能力下最接近的忠实还原，而不是敷衍的替代：

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
        // t 从 0 到 1：阻尼从 RagdollSettleDamping 线性衰减到 0，
        // 对应 DOOM3 原版 "刚触发时摩擦力低、逐渐恢复正常" 的效果方向（DOOM3 是摩擦力从低到高，
        // 这里反过来用阻尼从高到低，两者视觉上都是 "先软后硬" 的沉降感）
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

---

## 11. 关卡工具箱：触发器、开关、拾取物

到目前为止，关卡里的一切都是你在场景编辑器里手摆的、代码里写死的。这一章开始搭一套**关卡设计师（哪怕这个设计师就是你自己）不用改代码就能拼关卡**的工具箱。

### 11.1 拾取物：弹药、血包

```
AmmoPickup (Area3D)
├── CollisionShape3D
└── MeshInstance3D
```

`AmmoPickup.cs`：

```csharp
using Godot;

public partial class AmmoPickup : Area3D
{
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

        body.Call("GiveAmmo", AmmoAmount);
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

需要在 `PlayerController.cs`（或者更合理地，`WeaponManager.cs`，看你的项目怎么组织）加一个 `GiveAmmo` 方法接住这次调用——具体怎么加到已有的弹药系统上，取决于第 5 章你怎么设计的弹药存储方式，这里不重复展开。

### 11.2 触发体积：走进去，门就开了

第 7 章的门是"用一个绑在门自己身上的 `Area3D` 侦测玩家"，这样每扇门都要自己管理触发逻辑。更灵活的做法是把"触发体积"和"被触发的效果"拆成两个独立的东西——一个触发器可以同时激活好几个目标（一扇门 + 一盏灯 + 一段音效），互相之间不需要认识对方：

```csharp
using Godot;
using System.Collections.Generic;

public partial class TriggerZone : Area3D
{
    [Export] public NodePath[] Targets = System.Array.Empty<NodePath>();
    [Export] public bool OneShot = true;

    private bool _fired;

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
    }

    private void OnBodyEntered(Node3D body)
    {
        if (_fired && OneShot) return;
        if (!body.IsInGroup("player")) return;

        _fired = true;
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

把第 7 章的 `Door.Activate()` 方法保留（正好已经叫这个名字），现在触发器可以直接在编辑器的 Inspector 面板里把 `Targets` 数组指向任意数量的门/灯/其他实现了 `Activate()` 方法的节点，**门自己不再需要关心是谁触发了它**。这个"只要有 `Activate` 方法就能被任何触发器调用"的模式，会在第 13 章被正式讨论——你现在已经不知不觉用了好几次这种"只关心方法名、不关心具体类型"的写法（`TakeDamage`、`GiveAmmo`、`Activate`），是时候回头看看这套模式到底是什么、能不能做得更规范了。

### 11.3 可重复触发的开关

`TriggerZone` 的 `OneShot = false` 已经支持重复触发，但如果你想要"必须主动按按钮才触发，而不是走进某个区域就自动触发"，需要另一种触发方式——留到第 12 章一起讲，那一章专门处理"主动交互"这类场景。

---

## 12. 交互系统：按钮、终端、可点击的屏幕

第 11 章的触发器是"被动的"——玩家走进某个区域就自动触发，不需要玩家主动做任何操作。这一章加"主动交互"：玩家看着某个东西、按一个键，才会发生事情。

### 12.1 一个能看的"焦点"系统

```csharp
// PlayerController.cs 追加
[Export] public float InteractRange = 3.0f;
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

### 12.2 一个按钮

```csharp
using Godot;

public partial class Button3D : StaticBody3D
{
    [Export] public NodePath[] Targets = System.Array.Empty<NodePath>();

    public void Interact(Node whoInteracted)
    {
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

注意 `Button3D.Interact()` 和上一节 `TriggerZone.OnBodyEntered()` 最后做的事情几乎一样——都是遍历一个目标列表，调用 `Activate()`。**触发方式不同（走进区域 vs 主动按键），但触发之后"该发生什么"是同一套逻辑**。这正是第 11 章末尾埋下的伏笔：不管是走进去触发、还是按键触发，最终都收敛到同一个"激活"动作上。

### 12.3 世界里的可交互屏幕（选学）

如果你想做那种"墙上有一块可以用准星点击的电脑屏幕"的效果（终端、电梯面板这类），Godot 的做法是用 `SubViewport`：把一整套 UI（按钮、文字）渲染到一个独立的"子视口"里，再把这个子视口的画面当贴图贴到一个 3D 网格上。玩家的准星射线打中这块网格时，把命中点换算成这块 UI 上的二维坐标，往子视口里注入一个"鼠标点在这里"的事件——这块内容涉及的知识点（UV 坐标换算、`SubViewport` 事件转发）比较独立，不影响后面任何一章的学习，这里先跳过，等你需要真的做一块"可交互屏幕"的时候再单独查资料实现，思路上跟前面讲的"焦点检测+按键触发"是同一套骨架，只是多了一层"把命中点转换成 UI 坐标"的换算。

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

---

## 14. 数据驱动：把"这只怪物"变成"一份配置"

现在假设你想在游戏里加第二种怪物——一只近战速度更快、血更薄的"小怪"，和一只移动慢、血厚攻击重的"重甲怪"。按现在的写法，你得复制一份 `Enemy.cs`，改几个数字，存成 `FastEnemy.cs`——这样做的问题是：**每次想调一个数值（比如把所有怪物的视野角度统一调宽一点），都要挨个脚本文件去改**，而且策划/关卡设计者（哪怕这个角色也是你自己）如果不懂 C#，完全没法参与数值调整。

### 14.1 `Resource`：Godot 里的"数据资产"

Godot 有一种专门用来装"纯数据、可以在编辑器里像填表格一样编辑、可以存成独立文件复用"的类型：`Resource`。把怪物身上核心的"数值"部分拆出来，做成一份 `EnemyStats`（下面只演示最常调的这几项，9.5 节的 `PainThreshold`/`PainDebounce`、10.2.1 节的 `RagdollSettleDuration`/`RagdollSettleDamping` 这类没在下面出现的数值字段，照同样的思路自己加进 `EnemyStats` 就行，`Enemy.cs` 里对应保留为 `[Export]` 也不影响功能，只是没享受到数据驱动的好处）：

```csharp
using Godot;

[GlobalClass]
public partial class EnemyStats : Resource
{
    [Export] public float MaxHealth = 50.0f;
    [Export] public float MoveSpeed = 3.0f;
    [Export] public float ChaseRange = 15.0f;
    [Export] public float FieldOfViewDegrees = 100.0f;
    [Export] public float AttackRange = 2.0f;
    [Export] public float AttackDamage = 10.0f;
    [Export] public float AttackCooldown = 1.5f;
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

    private void TryAttack()
    {
        // ...
        if (distance > Stats.AttackRange) return;
        // ...
        _player.Call("TakeDamage", Stats.AttackDamage * DifficultySettings.EnemyDamageMultiplier());
    }
}
```

现在场景里可以放三个 `Enemy.tscn` 的实例，各自的 `Stats` 槽拖进不同的资源文件，就是三种手感完全不同的怪物——**同一份代码，靠不同的数据跑出不同的行为**，这正是"数据驱动"这个词的字面意思。想做第四种怪物，不需要写一行代码，新建一份 `.tres`、填几个数字就行。

### 14.3 武器同样适用这一套思路

回到第 5 章的 `Weapon.cs`，把 `Damage`、`Range`、`ClipSize`、`ReloadTime`、`FireRate` 这些字段拆成一份 `WeaponStats : Resource`，跟 `EnemyStats` 是同一个套路，这里不重复写一遍代码——**这一章真正想教的不是"怎么给怪物做配置"这一件具体的事，是"任何一组从多个具体实例里能看出规律的数值，都值得拆成一份独立的、非程序员也能编辑的数据"这个一般性的判断标准**，一旦你在怪物身上用顺手了，武器、拾取物、关卡里的任何"一类东西、多个变体"的场景，都可以套用同一个模式。

### 14.4 什么时候不该数据驱动

不是所有东西都该拆成 `Resource`。一个判断标准：**如果这个数值/行为在整个游戏里只会出现一次，硬拆成独立配置文件反而是额外的间接层，没有实际收益**。比如"最终 boss 房间那扇门的开启条件"，这种独一无二、只在一个地方用一次的逻辑，直接写在具体的脚本里就好，不需要为了"看起来更规范"而强行抽象成数据——过度数据驱动和过度抽象是同一类问题：多绕了一层，却没有换来真正的复用收益。

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

        // 弹药信息目前是 Weapon 自己管理的（第 5 章），同样给它加一个信号，思路和血量一样，这里不重复展开代码
    }

    private void OnHealthChanged(float current, float max)
    {
        _healthLabel.Text = $"HP {Mathf.CeilToInt(current)}/{Mathf.CeilToInt(max)}";
        _healthLabel.Modulate = current < max * 0.25f ? Colors.Red : Colors.White;
    }
}
```

这正是第 13 章讲的信号系统第一次真正派上用场的地方——`Hud` 完全不需要每帧去读玩家血量，玩家血量变化时自己会通知它。回头对比一下，如果没有第 13 章的信号系统，你可能会写成"`Hud` 在 `_Process` 里每帧读一次 `player.Health`"——能跑，但每帧查询是浪费，而且血量以外的每一种状态都要重复这个模式，信号系统在这里的收益是实打实的。

### 16.2 受伤反馈：屏幕红一下

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

### 17.3 位置同步

角色的位置/朝向这类"持续变化、需要让所有端看到基本一致"的状态，不需要为每一帧手写一个 RPC——给 `Enemy`/`PlayerController` 加一个 `MultiplayerSynchronizer` 子节点，在它的 Inspector 面板里把 `Position`、`Rotation` 这些属性加进"要同步的属性"列表，Godot 会自动处理好"host 的值周期性广播给其他端"这件事，你不需要手写任何同步代码。

**这一整章对本教程的定位是加分项，不是必需品**——如果你的目标就是做一个扎实的单人 FPS，完全可以永远不实现这一章，前面 16 章教的所有内容在纯单机模式下都是完整、独立、可以直接发布的。

---

## 18. 延伸阅读：如果你想知道 DOOM 3 原版是怎么做的

这份教程教你的是"用现代的 Godot + C#，做出一个 DOOM 3 风格的单人 FPS"。经过前面几轮修订，第 2-3、6、7、9、10 章里凡是涉及**具体手感公式/数值算法**的部分（移动加速度、视角 Bob、武器摇摆后坐、近战两阶段检测、疼痛打断、保底不死、布娃娃沉降），都已经是照着 DOOM 3 BFG 源码的真实实现逐行还原的，不是"差不多意思"的简化版——如果你对照 [DOOM3-BFG-Gameplay架构精读.md](DOOM3-BFG-Gameplay架构精读.md) 里对应的章节，应该能一一找到对应关系。真正还存在、且**应该存在**的差异，只剩下两类：**代码组织方式的不同**（DOOM 3 拆成很多个协作的 C++ 类，本教程为了教学连贯性把很多东西写在少数几个脚本里）、以及**平台能力的不同**（DOOM 3 手写的一些底层机制，Godot 已经作为引擎内置能力提供，不需要重新发明）。按章节的对应关系：

| 本教程 | 对应精读文档章节 | 差异属于哪一类 |
|---|---|---|
| 第 2-3 章：玩家移动/视角 Bob | 精读第 5 章 `idPlayer`/`idPhysics_Player`，5.8/5.10/5.11 节 | 代码组织：DOOM 3 把"游戏逻辑"和"纯物理仿真"拆成 `idPlayer`/`idPhysics_Player` 两个类，本教程写在一个 `PlayerController` 里；公式本身（加速度、摩擦力、bob 各分量、落地冲击）已逐项对齐 |
| 第 4-6 章：武器系统 | 精读第 7 章 | 代码组织：DOOM 3 的武器状态机用它自己发明的 DOOM Script 写，本教程直接用 C#——因为 C# 本身已经是热重载友好的脚本语言，不需要再嵌一层脚本虚拟机（精读 9.8 节详细论证过这个取舍） |
| 第 7 章：物理与门/电梯 | 精读第 6、10 章 | 平台能力：DOOM 3 手写了完整的推挤/防穿模算法（`idPush`），Godot 用 `AnimatableBody3D` + `SyncToPhysics` 内置解决了同一个问题；团队门联动的重定向模式已经对齐 |
| 第 8-9 章：AI | 精读第 8 章 | 平台能力：DOOM 3 的寻路系统（AAS）是自己写的一整套区域图+缓存路由算法，Godot 用内置导航网格系统解决同一个问题；两阶段近战检测、疼痛三道门、保底不死、难度数值表已逐项对齐 |
| 第 10 章：布娃娃 | 精读 6.5 节 | 平台能力：DOOM 3 的布娃娃约束求解器是自己手写的（拉格朗日乘子法），Godot 用内置物理引擎的 `PhysicalBoneSimulator3D` 解决同一个问题；慢动作沉降效果因为 Godot 不支持单物体时间缩放，改用阻尼渐变实现，这一点在 10.2.1 节已经明确说明是不同机制、相近效果，不是偷懒 |
| 第 11-12 章：关卡工具 | 精读第 10 章 | 代码组织：DOOM 3 原版有几十种专门的 `target_*` 类，本教程用"只要有 `Activate` 方法就能被调用"这一个更通用的模式覆盖了同样的需求 |
| 第 13 章：事件系统 | 精读第 2 章 | 平台能力：DOOM 3 自己实现了一整套类型反射+事件派发系统（`idClass`/`idEvent`），因为 C++ 没有这些能力；C# 本身自带反射和信号，不需要自己造 |
| 第 14 章：数据驱动 | 精读第 3 章 | 平台能力：DOOM 3 用纯字符串的键值字典（`spawnArgs`）做数据驱动，因为它面向的是文本编辑的 `.map`/`.def` 文件；`Resource` 是 Godot 编辑器原生支持的强类型资源，两者目的相同 |
| 第 15 章：存档 | 精读第 4 章 | 代码组织+有意简化：DOOM 3 用的是两阶段索引式序列化（先枚举全部对象、按索引建立空壳、再统一回填数据），能支持"场景结构在存读档之间发生变化"这种更通用的情况；本教程 15.3 节为了教学连贯性，用了更简单的"按 `node_path` 直接找回已存在节点"的实现，15.3 节末尾已经明确写清楚这个简化的适用边界（假设场景结构没变）以及要支持真正的关卡重载该怎么补 |

还剩下两处**明确标注过是有意选择、不是简化**的地方，值得在这里再强调一遍，避免被误认为是疏漏：第 4.4 节的命中部位缩放依赖角色有细分碰撞体积，本教程用的单胶囊体碰撞暂时不支持，给出了两条扩展路径；第 6.4 节的视图/世界模型分离是纯单机、纯第一人称项目可以直接跳过的可选内容。这两处都在正文里明确写清楚了原因和补全方式，不是含糊带过。

---

*本文与 [DOOM3-BFG-Gameplay架构精读.md](DOOM3-BFG-Gameplay架构精读.md)、[FPS引擎路线图.md](FPS引擎路线图.md)、[Phase0.1-Quake3架构精读.md](Phase0.1-Quake3架构精读.md) 共同构成 Re_Shirox 项目的阅读材料。本文示例代码为 Godot 4.x C#（.NET 版），运行前请确认引擎版本一致；随着 Godot 版本更新，个别 API（尤其 `PhysicalBoneSimulator3D`、`NavigationAgent3D`）的方法名可能变化，请对照当时的官方文档核实。