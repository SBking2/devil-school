# 成熟 FPS 镜头与武器表现系统制作教程

## 0. 前言

很多人在刚开始做第一人称游戏时，会以为 FPS 镜头的核心就是两个东西：`CameraBob` 和 `WeaponBob`。这两个确实重要，但它们只解决了一个很小的问题：玩家移动时画面不要像摄像机焊在角色头顶一样僵硬。成熟 FPS 的镜头系统真正难的地方在于，它不是单个效果，而是一套“输入、身体、镜头、武器、状态、反馈、网络同步”共同协作的系统。

成熟 FPS 的镜头需要同时满足几种矛盾的需求。第一，它要足够直接，玩家移动鼠标时不能有明显延迟，否则射击手感会变差。第二，它要有身体感，移动、落地、开火、换弹、受击时都应该让玩家感觉自己在控制一个有重量的角色。第三，它不能影响核心瞄准的可信度，视觉上的抖动和后坐力可以增强表现，但不能让玩家感觉准星和子弹逻辑是随机的。第四，它必须能和武器动画、角色状态、移动系统、网络同步兼容。

所以制作成熟 FPS 镜头时，最重要的不是先写一堆 `Camera.Position += xxx`，而是先把系统分层。一个健康的结构大概是：

```text
玩家输入层
    鼠标和手柄输入
    灵敏度
    死区和曲线

玩法瞄准层
    yaw / pitch
    射线方向
    命中判定
    后坐力的真实影响

视觉镜头层
    bob
    landing impact
    shake
    visual recoil
    FOV kick

武器表现层
    weapon bob
    weapon sway
    ADS
    weapon recoil
    obstruction

状态控制层
    idle
    walk
    sprint
    crouch
    air
    ADS
    reload
    fire
```

这份教程会围绕这个结构展开。代码示例以 Godot 4 C# 为主，但原理适用于 Unity、Unreal 或自研引擎。

## 1. 最核心的概念：Gameplay Aim 和 Visual Camera 分离

如果只记住一个原则，那就是：**真正用于射击的瞄准方向，不应该直接等于最终渲染出来的相机 Transform。**

很多新手会把所有效果都叠到 `Camera3D` 上：

```csharp
camera.Rotation += bobRotation;
camera.Rotation += recoilRotation;
camera.Rotation += shakeRotation;
camera.Position += landingOffset;
camera.Position += damageKick;
```

短期看起来有效，但很快会出问题。比如开火时你加了 visual recoil，镜头向上跳，如果射线也从这个跳动后的镜头发出，那么玩家的真实命中点会受到视觉动画影响。再比如脚步 bob 导致相机上下左右晃，如果射击射线跟着晃，玩家走路时准星就会产生额外随机误差。这种误差有时并不是设计想要的，而是实现混乱导致的。

更成熟的做法是把它拆成两套：

```text
Gameplay Aim:
    只受玩家输入、真实后坐力、武器散布、角色状态影响
    用于射线、弹道、交互检测、角色朝向

Visual Camera:
    在 Gameplay Aim 基础上叠加视觉效果
    用于画面表现，不一定改变真正射击方向
```

可以用一个简单的结构表示：

```csharp
public struct AimPose
{
    public float Yaw;
    public float Pitch;
}

public struct CameraVisualOffset
{
    public Vector3 LocalPosition;
    public Vector3 LocalRotation;
    public float FovOffset;
}
```

每一帧先更新 `AimPose`，再用 `AimPose` 驱动角色和基础镜头，然后把所有视觉偏移叠到 `Camera3D` 上：

```csharp
private AimPose _Aim;
private CameraVisualOffset _VisualOffset;

private void ApplyCamera(Camera3D camera)
{
    Basis aimBasis = Basis.Identity
        .Rotated(Vector3.Up, _Aim.Yaw)
        .Rotated(Vector3.Right, _Aim.Pitch);

    Basis visualBasis = Basis.Identity
        .Rotated(Vector3.Right, _VisualOffset.LocalRotation.X)
        .Rotated(Vector3.Up, _VisualOffset.LocalRotation.Y)
        .Rotated(Vector3.Forward, _VisualOffset.LocalRotation.Z);

    camera.Basis = aimBasis * visualBasis;
    camera.Position = _VisualOffset.LocalPosition;
}
```

注意这只是演示。实际项目里通常会有 `YawPivot` 和 `PitchPivot` 两层节点：

```text
PlayerBody
    YawPivot
        PitchPivot
            CameraEffectsPivot
                Camera3D
            WeaponViewModel
```

`YawPivot` 负责水平旋转，`PitchPivot` 负责上下看，`CameraEffectsPivot` 只负责视觉偏移。这样 Gameplay Aim 和视觉效果天然分开。

## 2. 输入旋转：鼠标和手柄不是同一种输入

FPS 镜头的第一层是 Look Input。这里最常见的错误是：鼠标输入也乘以 `deltaTime`。鼠标输入本质上是“这一帧鼠标移动了多少像素”，它已经是离散位移量。手柄摇杆输入本质上是“当前推杆程度”，表示一个持续的角速度，所以它需要乘以 `deltaTime`。

可以这样理解：

```text
鼠标：
    本帧移动量 = delta pixels
    yaw += mouseDelta.x * sensitivity

手柄：
    当前输入强度 = [-1, 1]
    yaw += stick.x * angularSpeed * delta
```

Godot 鼠标输入示例：

```csharp
private Vector2 _MouseLookDelta;

public override void _Input(InputEvent e)
{
    if (e is InputEventMouseMotion motion)
    {
        _MouseLookDelta += motion.Relative;
    }
}

public override void _PhysicsProcess(double delta)
{
    float sensitivity = 0.0025f;

    _Aim.Yaw -= _MouseLookDelta.X * sensitivity;
    _Aim.Pitch -= _MouseLookDelta.Y * sensitivity;
    _Aim.Pitch = Mathf.Clamp(_Aim.Pitch, Mathf.DegToRad(-89f), Mathf.DegToRad(89f));

    _MouseLookDelta = Vector2.Zero;
}
```

手柄输入示例：

```csharp
private void UpdateGamepadLook(float delta)
{
    Vector2 stick = new Vector2(
        Input.GetActionStrength("look_right") - Input.GetActionStrength("look_left"),
        Input.GetActionStrength("look_down") - Input.GetActionStrength("look_up")
    );

    stick = ApplyDeadZone(stick, 0.15f);
    stick = ApplyResponseCurve(stick, 1.8f);

    float yawSpeed = Mathf.DegToRad(220f);
    float pitchSpeed = Mathf.DegToRad(160f);

    _Aim.Yaw -= stick.X * yawSpeed * delta;
    _Aim.Pitch -= stick.Y * pitchSpeed * delta;
    _Aim.Pitch = Mathf.Clamp(_Aim.Pitch, Mathf.DegToRad(-89f), Mathf.DegToRad(89f));
}

private Vector2 ApplyDeadZone(Vector2 value, float deadZone)
{
    float length = value.Length();
    if (length <= deadZone)
        return Vector2.Zero;

    float remapped = (length - deadZone) / (1f - deadZone);
    return value.Normalized() * remapped;
}

private Vector2 ApplyResponseCurve(Vector2 value, float power)
{
    float length = value.Length();
    if (length <= 0.0001f)
        return Vector2.Zero;

    float curved = Mathf.Pow(length, power);
    return value.Normalized() * curved;
}
```

数学上，手柄曲线是在做一个非线性映射：

```text
output = input^power
```

当 `power > 1` 时，小输入会变得更细腻，大输入仍然可以达到最大速度。这样玩家轻推摇杆时能精确瞄准，推到底时又能快速转身。

## 3. 插值和平滑：为什么要用指数衰减

很多镜头效果都需要“平滑回正”。最直接的写法是：

```csharp
value = Mathf.Lerp(value, target, delta * speed);
```

这在小项目里常用，但严格来说它有一个问题：当 `delta * speed > 1` 时会过冲，而且不同帧率下手感不完全一致。更稳定的写法是指数衰减：

```text
t = 1 - exp(-speed * delta)
value = lerp(value, target, t)
```

这个公式来自一阶系统的解析解。可以把它理解为：

```text
当前值每秒按固定比例靠近目标值
```

帧率越高，每帧移动少一点；帧率越低，每帧移动多一点，但总体每秒变化一致。

Godot C# 示例：

```csharp
private static float ExpDecay(float current, float target, float speed, float delta)
{
    float t = 1f - Mathf.Exp(-speed * delta);
    return Mathf.Lerp(current, target, t);
}

private static Vector3 ExpDecay(Vector3 current, Vector3 target, float speed, float delta)
{
    float t = 1f - Mathf.Exp(-speed * delta);
    return current.Lerp(target, t);
}
```

在 FPS 镜头里，指数衰减适合用于：

```text
FOV 平滑
ADS 过渡
Weapon sway 回正
Landing offset 回弹
Visual recoil 恢复
Camera shake 淡出
```

注意，鼠标 Look 本身通常不要做明显平滑。鼠标瞄准追求低延迟，过重的平滑会让玩家感觉鼠标像拖着东西。你可以平滑视觉层，但不要明显平滑 Gameplay Aim。

## 4. Camera Bob：不是上下晃，而是步态函数

Camera Bob 的本质是一个周期函数。玩家走路时，每一步会产生身体上下起伏和左右摆动。最简单的数学形式是：

```text
y = sin(phase) * amplitudeY
x = cos(phase * 0.5) * amplitudeX
```

其中：

```text
phase = phase + speed * frequency * delta
```

`phase` 是步态相位。玩家速度越快，相位增长越快，bob 频率越高。`amplitude` 是幅度，走路小，跑步大，蹲走更小。

示例：

```csharp
public class CameraBob
{
    private float _Phase;
    private Vector3 _Offset;

    public Vector3 Offset => _Offset;

    public void Update(float delta, float moveSpeed01, bool isGrounded, float stateWeight)
    {
        if (!isGrounded || moveSpeed01 < 0.05f)
        {
            _Offset = ExpDecay(_Offset, Vector3.Zero, 12f, delta);
            return;
        }

        float frequency = Mathf.Lerp(6f, 11f, moveSpeed01);
        float amplitudeY = Mathf.Lerp(0.015f, 0.045f, moveSpeed01) * stateWeight;
        float amplitudeX = Mathf.Lerp(0.006f, 0.018f, moveSpeed01) * stateWeight;

        _Phase += frequency * delta;

        float x = Mathf.Sin(_Phase) * amplitudeX;
        float y = Mathf.Abs(Mathf.Cos(_Phase)) * amplitudeY;

        Vector3 target = new Vector3(x, y, 0f);
        _Offset = ExpDecay(_Offset, target, 20f, delta);
    }

    private static Vector3 ExpDecay(Vector3 current, Vector3 target, float speed, float delta)
    {
        float t = 1f - Mathf.Exp(-speed * delta);
        return current.Lerp(target, t);
    }
}
```

为什么 `y` 用 `Abs(Cos)`？因为人在走路时左右脚各落地一次，一个完整左右周期里会有两次竖直冲击。如果只用普通 `sin`，上下起伏会像波浪一样连续，缺少脚步感。用 `Abs(Cos)` 可以让竖直起伏更接近“每一步一次冲击”。

但成熟 FPS 的 Camera Bob 必须克制。镜头上下晃太多会让人晕，而且会影响瞄准。实际项目里，很多动感来自武器 bob、FOV、动画和音效，镜头本身反而很稳。

推荐数值：

```text
慢走 Camera Bob:
    Y: 0.01 到 0.025
    X: 0.003 到 0.01

奔跑 Camera Bob:
    Y: 0.035 到 0.06
    X: 0.012 到 0.025

ADS Camera Bob:
    乘以 0.1 到 0.25
```

## 5. Weapon Bob：比 Camera Bob 更能提供运动感

武器 Bob 可以比 Camera Bob 更夸张，因为它不直接影响整个画面，也不一定影响真实瞄准方向。它通常包含：

```text
位置 bob
旋转 bob
速度响应
状态权重
ADS 缩放
Sprint 下压
```

位置 bob 可以用类似 Camera Bob 的相位，但幅度更大：

```csharp
public class WeaponBob
{
    private float _Phase;
    private Vector3 _PositionOffset;
    private Vector3 _RotationOffset;

    public Vector3 PositionOffset => _PositionOffset;
    public Vector3 RotationOffset => _RotationOffset;

    public void Update(float delta, float moveSpeed01, bool grounded, float adsWeight, float sprintWeight)
    {
        float bobWeight = grounded ? moveSpeed01 : 0f;
        bobWeight *= Mathf.Lerp(1f, 0.15f, adsWeight);
        bobWeight *= Mathf.Lerp(1f, 1.5f, sprintWeight);

        if (bobWeight < 0.01f)
        {
            _PositionOffset = ExpDecay(_PositionOffset, Vector3.Zero, 10f, delta);
            _RotationOffset = ExpDecay(_RotationOffset, Vector3.Zero, 10f, delta);
            return;
        }

        float frequency = Mathf.Lerp(5.5f, 10f, moveSpeed01);
        _Phase += frequency * delta;

        Vector3 pos = new Vector3(
            Mathf.Sin(_Phase) * 0.035f,
            Mathf.Abs(Mathf.Cos(_Phase)) * 0.045f,
            Mathf.Cos(_Phase) * 0.015f
        ) * bobWeight;

        Vector3 rot = new Vector3(
            Mathf.Sin(_Phase) * Mathf.DegToRad(1.2f),
            Mathf.Sin(_Phase * 0.5f) * Mathf.DegToRad(1.0f),
            Mathf.Sin(_Phase) * Mathf.DegToRad(1.8f)
        ) * bobWeight;

        _PositionOffset = ExpDecay(_PositionOffset, pos, 18f, delta);
        _RotationOffset = ExpDecay(_RotationOffset, rot, 18f, delta);
    }

    private static Vector3 ExpDecay(Vector3 current, Vector3 target, float speed, float delta)
    {
        float t = 1f - Mathf.Exp(-speed * delta);
        return current.Lerp(target, t);
    }
}
```

武器 Bob 和 Camera Bob 不一定同相。成熟手感里，武器经常有一点滞后。玩家加速时，武器 bob 慢慢增强；停下时，武器有一点回摆。这种“延迟”让武器看起来有重量。

但也不要让武器移动太大。FPS 武器是屏幕中心的重要信息，过度晃动会影响读枪、读准星、读换弹动画。一个实用原则是：

```text
镜头 bob 负责身体感
武器 bob 负责运动感
音效和动画负责冲击感
```

## 6. Weapon Sway：让武器不要焊死在屏幕上

Weapon Sway 指玩家转动视角或移动时，武器产生轻微滞后。它的直觉是：相机已经转过去了，但手臂和武器有惯性，所以视觉上会稍微落后，然后回正。

输入来自两类：

```text
Look Input:
    鼠标水平/垂直移动导致武器旋转偏移

Move Input:
    左右移动、前后移动导致武器位置偏移
```

数学上，这也是一个“目标值 + 平滑跟随”的问题：

```text
targetSway = -lookDelta * scale
currentSway = lerp(currentSway, targetSway, smoothing)
targetSway 逐渐回到 0
```

示例：

```csharp
public class WeaponSway
{
    private Vector3 _RotationOffset;
    private Vector3 _PositionOffset;

    public Vector3 RotationOffset => _RotationOffset;
    public Vector3 PositionOffset => _PositionOffset;

    public void Update(float delta, Vector2 lookDelta, Vector2 moveInput, float adsWeight)
    {
        float lookScale = Mathf.Lerp(1f, 0.25f, adsWeight);
        float moveScale = Mathf.Lerp(1f, 0.35f, adsWeight);

        Vector3 targetRot = new Vector3(
            -lookDelta.Y * 0.002f,
            -lookDelta.X * 0.002f,
            lookDelta.X * 0.0015f
        ) * lookScale;

        Vector3 targetPos = new Vector3(
            -moveInput.X * 0.035f,
            0f,
            -Mathf.Abs(moveInput.Y) * 0.015f
        ) * moveScale;

        _RotationOffset = ExpDecay(_RotationOffset, targetRot, 14f, delta);
        _PositionOffset = ExpDecay(_PositionOffset, targetPos, 10f, delta);
    }

    private static Vector3 ExpDecay(Vector3 current, Vector3 target, float speed, float delta)
    {
        float t = 1f - Mathf.Exp(-speed * delta);
        return current.Lerp(target, t);
    }
}
```

这里要注意，`lookDelta` 最好使用未经平滑的原始输入，或者使用本帧 aim 角度变化量。否则 sway 会显得迟钝。ADS 时 sway 应该明显减弱，因为玩家进入瞄准状态后，武器应该更稳定。

高级一点可以使用弹簧模型。弹簧系统的形式是：

```text
velocity += (target - position) * stiffness * delta
velocity *= damping
position += velocity * delta
```

它比普通 lerp 更有“重量感”，因为有速度和回弹。

```csharp
public class SpringVector3
{
    public Vector3 Value;
    public Vector3 Velocity;

    public void Update(Vector3 target, float stiffness, float damping, float delta)
    {
        Vector3 force = (target - Value) * stiffness;
        Velocity += force * delta;
        Velocity *= Mathf.Exp(-damping * delta);
        Value += Velocity * delta;
    }
}
```

弹簧适合 weapon sway、recoil recovery、landing impact。缺点是参数调不好容易抖动或过冲，所以初期可以先用指数衰减。

## 7. Recoil：真实后坐力和视觉后坐力要拆开

后坐力是 FPS 手感的核心之一。它至少可以拆成两层：

```text
Gameplay Recoil:
    真正改变 AimPitch / AimYaw
    影响射击方向
    决定武器可控性

Visual Recoil:
    镜头抖动
    武器后退、上跳、旋转
    增强冲击感
```

如果只有 Visual Recoil，玩家会感觉枪在跳，但子弹不跳，缺少真实感。如果只有 Gameplay Recoil，枪口实际偏了，但画面反馈弱，也会觉得廉价。

一个简单的 Gameplay Recoil：

```csharp
public class GameplayRecoil
{
    private Vector2 _RecoilOffset;
    private Vector2 _RecoilVelocity;

    public Vector2 Offset => _RecoilOffset;

    public void AddShot(float pitchKick, float yawKick)
    {
        _RecoilVelocity += new Vector2(yawKick, pitchKick);
    }

    public void Update(float delta)
    {
        _RecoilOffset += _RecoilVelocity * delta;
        _RecoilVelocity = _RecoilVelocity.Lerp(Vector2.Zero, 1f - Mathf.Exp(-18f * delta));
        _RecoilOffset = _RecoilOffset.Lerp(Vector2.Zero, 1f - Mathf.Exp(-6f * delta));
    }
}
```

开火时：

```csharp
private GameplayRecoil _GameplayRecoil = new GameplayRecoil();

private void Fire()
{
    float pitch = Mathf.DegToRad(2.0f);
    float yaw = Mathf.DegToRad(Rng.RealRandom.RangeFloat(-0.4f, 0.4f));
    _GameplayRecoil.AddShot(pitch, yaw);
}

private void ApplyAim()
{
    float finalYaw = _Aim.Yaw + _GameplayRecoil.Offset.X;
    float finalPitch = _Aim.Pitch + _GameplayRecoil.Offset.Y;
}
```

更成熟的后坐力不是完全随机，而是 pattern：

```text
第 1 发：上跳 1.2，左右 0
第 2 发：上跳 1.5，左 0.2
第 3 发：上跳 1.7，右 0.3
第 4 发：上跳 1.6，右 0.5
```

随机只作为小扰动。这样玩家可以学习压枪，武器也有个性。

Visual Recoil 可以作用在武器上：

```csharp
public class WeaponRecoil
{
    private SpringVector3 _PosSpring = new SpringVector3();
    private SpringVector3 _RotSpring = new SpringVector3();

    public Vector3 PositionOffset => _PosSpring.Value;
    public Vector3 RotationOffset => _RotSpring.Value;

    public void AddShot()
    {
        _PosSpring.Velocity += new Vector3(0f, 0.02f, 0.18f);
        _RotSpring.Velocity += new Vector3(Mathf.DegToRad(-18f), Mathf.DegToRad(2f), Mathf.DegToRad(3f));
    }

    public void Update(float delta)
    {
        _PosSpring.Update(Vector3.Zero, 120f, 18f, delta);
        _RotSpring.Update(Vector3.Zero, 140f, 20f, delta);
    }
}
```

后坐力设计的关键不是公式，而是权重：

```text
站立腰射：recoil 1.0
移动腰射：recoil 1.2
奔跑中：通常不能开火或极不稳定
蹲下：recoil 0.8
ADS：recoil 0.65
连发越久：recoil recovery 越慢
```

## 8. ADS：瞄准不是简单改 FOV

ADS 是 Aim Down Sight，也就是开镜/机瞄。新手常做：

```csharp
camera.Fov = 45;
```

这远远不够。成熟 ADS 至少包括：

```text
武器移动到瞄准姿态
FOV 缩小
鼠标灵敏度降低
Weapon sway 降低
Weapon bob 降低
Camera bob 降低
Recoil 参数改变
准星 UI 改变
开火散布改变
```

ADS 最重要的是一个权重：

```text
adsWeight: 0 到 1
```

所有相关效果都用这个权重混合。

```csharp
private float _AdsWeight;

private void UpdateAds(float delta)
{
    bool aiming = Input.IsActionPressed("aim");
    float target = aiming ? 1f : 0f;
    _AdsWeight = ExpDecay(_AdsWeight, target, 14f, delta);

    camera.Fov = Mathf.Lerp(75f, 50f, _AdsWeight);
    mouseSensitivity = Mathf.Lerp(1.0f, 0.55f, _AdsWeight);
}
```

武器位置混合：

```csharp
Vector3 hipPos = new Vector3(0.18f, -0.18f, -0.45f);
Vector3 adsPos = new Vector3(0.0f, -0.095f, -0.32f);
weapon.Position = hipPos.Lerp(adsPos, _AdsWeight);
```

ADS 的数学本质是 Transform 插值。位置用线性插值即可：

```text
P = lerp(P_hip, P_ads, t)
```

旋转最好用四元数球面插值：

```text
Q = slerp(Q_hip, Q_ads, t)
```

Godot C#：

```csharp
Quaternion hipRot = Basis.Identity.GetRotationQuaternion();
Quaternion adsRot = Basis.Identity.Rotated(Vector3.Right, Mathf.DegToRad(-1f)).GetRotationQuaternion();
Quaternion finalRot = hipRot.Slerp(adsRot, _AdsWeight);
weapon.Quaternion = finalRot;
```

ADS 的调参重点是响应时间。太快像瞬移，太慢影响操作。常见范围：

```text
普通步枪 ADS: 0.16 到 0.25 秒
手枪 ADS: 0.10 到 0.18 秒
重武器 ADS: 0.28 到 0.45 秒
```

如果用指数衰减，`speed = 14` 大概是比较快的过渡，`speed = 8` 比较慢。

## 9. FOV Kick：速度感和冲击感的低成本来源

FOV 是成熟 FPS 手感里非常重要但容易被滥用的工具。它可以用于：

```text
奔跑时略微增大 FOV
滑铲时快速增大 FOV
开火时轻微 FOV kick
受击时短暂压缩
ADS 时缩小 FOV
```

FOV 改变的心理效果很明显。FOV 变大时，画面边缘运动更强，玩家会感觉速度变快。FOV 变小时，目标看起来更近，适合瞄准。

但不要把 FOV 动得太夸张。过强的 FOV 变化会让人晕，也会影响距离判断。

简单 FOV 管理：

```csharp
public class FovController
{
    private float _CurrentFov;
    private float _Kick;

    public float Fov => _CurrentFov + _Kick;

    public FovController(float baseFov)
    {
        _CurrentFov = baseFov;
    }

    public void AddKick(float amount)
    {
        _Kick += amount;
    }

    public void Update(float delta, float baseFov, float sprintWeight, float adsWeight)
    {
        float target = baseFov;
        target += Mathf.Lerp(0f, 8f, sprintWeight);
        target = Mathf.Lerp(target, 50f, adsWeight);

        _CurrentFov = Mathf.Lerp(_CurrentFov, target, 1f - Mathf.Exp(-10f * delta));
        _Kick = Mathf.Lerp(_Kick, 0f, 1f - Mathf.Exp(-16f * delta));
    }
}
```

开火时：

```csharp
_FovController.AddKick(0.8f);
```

奔跑时：

```text
baseFov 75
sprintFov 82
ADS FOV 50
```

FOV 有一个细节：如果你的游戏支持玩家自定义 FOV，不要把 ADS FOV 写死。可以用倍率：

```text
ADS_FOV = playerBaseFov * 0.68
```

或者按武器配置：

```text
red dot: base * 0.82
scope 2x: base * 0.55
scope 4x: base * 0.35
```

## 10. Landing Impact：落地冲击让角色有重量

跳跃和落地是 FPS 身体感的重要来源。落地时镜头可以轻微下沉，然后回弹。这个效果不能太大，但没有它会显得角色很轻。

数学上可以根据落地前的竖直速度计算冲击强度：

```text
impact = clamp(abs(velocityY) / maxLandingSpeed, 0, 1)
```

然后用弹簧或者衰减回正：

```csharp
public class LandingImpact
{
    private SpringVector3 _Spring = new SpringVector3();

    public Vector3 Offset => _Spring.Value;

    public void AddLanding(float fallSpeed)
    {
        float impact = Mathf.Clamp(fallSpeed / 18f, 0f, 1f);
        _Spring.Velocity += new Vector3(0f, -0.35f * impact, 0f);
    }

    public void Update(float delta)
    {
        _Spring.Update(Vector3.Zero, 90f, 16f, delta);
    }
}
```

在角色控制器里检测从空中到地面：

```csharp
private bool _WasGrounded;
private float _LastVerticalVelocity;

public override void _PhysicsProcess(double delta)
{
    bool grounded = IsOnFloor();

    if (!_WasGrounded && grounded)
    {
        _LandingImpact.AddLanding(Mathf.Abs(_LastVerticalVelocity));
    }

    _WasGrounded = grounded;
    _LastVerticalVelocity = Velocity.Y;
}
```

落地冲击通常还会配合：

```text
脚步声
尘土粒子
手臂下沉
武器轻微晃动
镜头 Pitch 轻微变化
```

成熟 FPS 里很多反馈都不是单个系统完成的，而是多个小系统同时给玩家暗示。

## 11. Camera Shake：不要直接 Random，每个 Shake 应该有生命周期

Camera Shake 用于爆炸、受击、开火、环境震动等。最差的写法是：

```csharp
camera.Rotation += new Vector3(Random(), Random(), Random());
```

这种抖动没有频率，没有衰减，没有方向，也不能叠加管理。成熟做法是定义 Shake Instance：

```csharp
public class CameraShake
{
    public float Time;
    public float Duration;
    public float Amplitude;
    public float Frequency;
    public Vector3 DirectionWeight = Vector3.One;

    public bool IsFinished => Time >= Duration;

    public Vector3 Update(float delta)
    {
        Time += delta;
        float life01 = Mathf.Clamp(Time / Duration, 0f, 1f);
        float envelope = 1f - life01;
        envelope *= envelope;

        float t = Time * Frequency;
        float x = Mathf.Sin(t * 17.1f) * DirectionWeight.X;
        float y = Mathf.Sin(t * 23.7f + 1.2f) * DirectionWeight.Y;
        float z = Mathf.Sin(t * 31.3f + 2.4f) * DirectionWeight.Z;

        return new Vector3(x, y, z) * Amplitude * envelope;
    }
}
```

管理多个 shake：

```csharp
private readonly List<CameraShake> _Shakes = new List<CameraShake>();

public Vector3 UpdateShakes(float delta)
{
    Vector3 result = Vector3.Zero;

    for (int i = _Shakes.Count - 1; i >= 0; i--)
    {
        result += _Shakes[i].Update(delta);
        if (_Shakes[i].IsFinished)
            _Shakes.RemoveAt(i);
    }

    return result;
}
```

这种 Shake 的优点是可控：

```text
开枪：短、弱、高频
受击：短、中等、方向性强
爆炸：长、强、低频
大型机关：循环、低频、可淡入淡出
```

更高级可以用 Perlin Noise 或 Simplex Noise。噪声比正弦叠加更自然，但正弦已经足够做原型。

## 12. Lean 和 Peek：探头不是只旋转镜头

战术 FPS 常见 Lean 左右探头。Lean 通常包括：

```text
相机横向移动
相机 Roll
角色碰撞检测
武器姿态变化
暴露身体判定
```

只做 Roll 会显得像屏幕倾斜，不像角色探出身体。更好的做法是同时移动相机：

```csharp
private float _LeanWeight;

private void UpdateLean(float delta)
{
    float target = 0f;
    if (Input.IsActionPressed("lean_left"))
        target = -1f;
    else if (Input.IsActionPressed("lean_right"))
        target = 1f;

    _LeanWeight = Mathf.Lerp(_LeanWeight, target, 1f - Mathf.Exp(-12f * delta));

    Vector3 leanPos = new Vector3(_LeanWeight * 0.28f, 0f, 0f);
    float leanRoll = Mathf.DegToRad(-8f) * _LeanWeight;
}
```

但是实际项目必须做碰撞检测，避免相机穿墙。可以用 RayCast 或 ShapeCast 检测从原始头部位置到目标 lean 位置是否被挡住：

```csharp
float allowedLean = CheckLeanCollision(_LeanWeight);
Vector3 finalLeanPos = new Vector3(allowedLean * 0.28f, 0f, 0f);
```

Lean 的复杂点在于网络和命中判定。如果只是视觉相机移动，但服务器仍认为角色身体在原地，就会产生“我看见别人但别人打不到我”的问题。因此商业游戏会把 Lean 作为角色状态同步，并影响暴露碰撞体或命中盒。

## 13. Weapon Obstruction：防止武器插墙

第一人称武器经常会穿进墙里，尤其是长枪靠近门、墙、箱子时。成熟 FPS 通常会做 Weapon Obstruction：

```text
检测武器前方是否有障碍
如果太近，把武器收回、压低或偏转
禁止开火或改变开火姿态
```

简单检测方式：从相机向前发射射线。

```csharp
private float _ObstructionWeight;

private void UpdateObstruction(float delta)
{
    Vector3 origin = camera.GlobalPosition;
    Vector3 end = origin + -camera.GlobalBasis.Z * 1.2f;

    PhysicsRayQueryParameters3D query = PhysicsRayQueryParameters3D.Create(origin, end);
    var result = GetWorld3D().DirectSpaceState.IntersectRay(query);

    float target = result.Count > 0 ? 1f : 0f;
    _ObstructionWeight = Mathf.Lerp(_ObstructionWeight, target, 1f - Mathf.Exp(-16f * delta));

    Vector3 blockedPos = new Vector3(0.0f, -0.12f, 0.22f);
    Vector3 blockedRot = new Vector3(Mathf.DegToRad(-12f), Mathf.DegToRad(18f), Mathf.DegToRad(8f));
}
```

然后把这个权重混到武器最终姿态：

```text
weaponPose = normalPose + obstructionPose * obstructionWeight
```

注意 Weapon Obstruction 不一定要影响真实射线。如果墙太近，可以直接禁止开火，或者让枪口射线从武器 muzzle 检测是否被挡住。否则玩家可能把枪插进墙里从另一边射击。

## 14. 状态权重系统：成熟感来自混合，而不是硬切

FPS 镜头效果最大的工程问题是状态很多：

```text
idle
walk
sprint
crouch
air
ADS
reload
fire
hit
dead
```

如果每个状态都直接写：

```csharp
if (isSprinting) camera.Fov = 82;
if (isAds) camera.Fov = 50;
if (isCrouching) bob *= 0.4;
```

很快会乱。成熟做法是把状态变成权重：

```csharp
public class FpsStateWeights
{
    public float Move;
    public float Sprint;
    public float Crouch;
    public float Air;
    public float Ads;
    public float Reload;
}
```

每帧根据状态平滑更新：

```csharp
private void UpdateWeights(float delta)
{
    Weights.Move = Smooth01(Weights.Move, currentSpeed > 0.1f ? 1f : 0f, 12f, delta);
    Weights.Sprint = Smooth01(Weights.Sprint, isSprinting ? 1f : 0f, 10f, delta);
    Weights.Crouch = Smooth01(Weights.Crouch, isCrouching ? 1f : 0f, 14f, delta);
    Weights.Air = Smooth01(Weights.Air, isGrounded ? 0f : 1f, 8f, delta);
    Weights.Ads = Smooth01(Weights.Ads, isAiming ? 1f : 0f, 14f, delta);
}

private float Smooth01(float current, float target, float speed, float delta)
{
    return Mathf.Lerp(current, target, 1f - Mathf.Exp(-speed * delta));
}
```

然后各个效果读取权重：

```text
CameraBob *= Move * Grounded * (1 - Ads * 0.85)
WeaponBob *= Move * (1 + Sprint * 0.5) * (1 - Ads * 0.8)
Sway *= 1 - Ads * 0.7
FOV = Base + SprintFov * Sprint - AdsFovReduce * Ads
```

这就是成熟 FPS 表现系统的关键：每一层都是小效果，但通过权重统一混合。

## 15. 最终合成：所有 Offset 应该集中应用

不要让每个模块自己直接写 Camera。更好的做法是每个模块输出 Offset：

```csharp
public struct FpsOffset
{
    public Vector3 Position;
    public Vector3 Rotation;
    public float Fov;

    public static FpsOffset operator +(FpsOffset a, FpsOffset b)
    {
        return new FpsOffset
        {
            Position = a.Position + b.Position,
            Rotation = a.Rotation + b.Rotation,
            Fov = a.Fov + b.Fov
        };
    }
}
```

每帧统一合成：

```csharp
private void ApplyFinalCamera(float delta)
{
    FpsOffset offset = new FpsOffset();
    offset.Position += _CameraBob.Offset;
    offset.Position += _LandingImpact.Offset;
    offset.Rotation += _ShakeRotation;
    offset.Rotation += _VisualRecoil.RotationOffset;
    offset.Fov += _FovController.Fov - BaseFov;

    _CameraEffectsPivot.Position = offset.Position;
    _CameraEffectsPivot.Rotation = offset.Rotation;
    _Camera.Fov = BaseFov + offset.Fov;
}
```

武器同理：

```csharp
private void ApplyFinalWeapon()
{
    Vector3 pos = _BaseWeaponPosition;
    Vector3 rot = _BaseWeaponRotation;

    pos += _WeaponBob.PositionOffset;
    rot += _WeaponBob.RotationOffset;

    pos += _WeaponSway.PositionOffset;
    rot += _WeaponSway.RotationOffset;

    pos += _WeaponRecoil.PositionOffset;
    rot += _WeaponRecoil.RotationOffset;

    pos = pos.Lerp(_AdsPosition, _AdsWeight);

    _WeaponRoot.Position = pos;
    _WeaponRoot.Rotation = rot;
}
```

真正项目里，旋转合成最好用四元数或 Basis，而不是简单 Euler 相加。Euler 相加简单直观，但多轴大角度时容易出现顺序问题。FPS 武器偏移一般角度较小，用 Euler 足够做原型。

## 16. Godot 节点结构建议

一个实用的 Godot 第一人称角色节点结构：

```text
NFpsCharacter : CharacterBody3D
    CollisionShape3D
    YawPivot : Node3D
        PitchPivot : Node3D
            CameraEffectsPivot : Node3D
                Camera3D
            WeaponViewRoot : Node3D
                WeaponSwayPivot : Node3D
                    WeaponModel
```

职责：

```text
NFpsCharacter:
    移动、碰撞、服务器同步、角色状态

YawPivot:
    水平旋转

PitchPivot:
    上下看

CameraEffectsPivot:
    bob、shake、landing、visual recoil

Camera3D:
    真正渲染

WeaponViewRoot:
    武器基础位置、ADS、obstruction

WeaponSwayPivot:
    sway、bob、weapon recoil
```

Godot 里注意方向：

```text
Camera3D 默认看向 -Z
Node3D.GlobalBasis.Z 是后方
-GlobalBasis.Z 才是 forward
```

所以发射射线时：

```csharp
Vector3 origin = camera.GlobalPosition;
Vector3 dir = -camera.GlobalBasis.Z;
Vector3 end = origin + dir * 1000f;
```

## 17. 网络游戏中的 FPS 镜头

如果你以后做联机，FPS 镜头还要分客户端和服务器。

客户端本地需要：

```text
即时响应输入
本地预测移动
本地播放武器动画
本地显示 recoil 和 shake
```

服务器需要：

```text
验证输入
计算角色真实位置
计算射击方向
处理命中
广播结果
```

关键点是：本地视觉可以很丰富，但服务器只关心必要的玩法数据：

```text
input sequence
yaw
pitch
move input
fire command
ads state
```

不要把 Camera Bob、Weapon Bob、Visual Shake 同步给服务器。这些是纯表现。服务器应该只知道玩家真实 aim 和角色状态。

开火时，本地可以立即播放：

```text
枪声
枪口火焰
weapon recoil
camera kick
```

服务器确认命中后再同步：

```text
伤害
命中特效
敌人受击
血量变化
```

这样既有响应速度，又保持服务器权威。

## 18. 常见问题和调试方法

### 问题 1：镜头太晕

通常是 Camera Bob 太强，FOV 变化太大，Shake 频率太低或幅度太高。解决：

```text
降低 Camera Bob
把更多动感转移到 Weapon Bob
ADS 时大幅降低所有 bob
Shake 使用短时间高频，而不是长时间大幅度
```

### 问题 2：射击感觉不准

检查 Gameplay Aim 和 Visual Camera 是否混在一起。射线应该从稳定的 aim 发出，而不是从加了 bob/shake 的最终 camera 发出。

### 问题 3：武器像贴在屏幕上

加 Weapon Sway、Move Lag、Recoil Spring。让武器有轻微滞后和回正。

### 问题 4：ADS 很廉价

ADS 不要只改 FOV。要同时改：

```text
weapon position
weapon rotation
sensitivity
bob
sway
recoil
FOV
```

### 问题 5：后坐力很随机

减少纯随机，使用 recoil pattern。随机只作为小扰动。

### 问题 6：代码很乱

每个效果输出 offset，最后集中合成。不要让每个系统直接操作 Camera。

## 19. 推荐实现顺序

不要一开始就做完整系统。推荐顺序：

```text
1. LookController
2. Gameplay Aim 和 Visual Camera 分离
3. CameraBob
4. WeaponBob
5. WeaponSway
6. ADS
7. Gameplay Recoil
8. Visual Recoil
9. LandingImpact
10. Sprint FOV
11. CameraShake
12. WeaponObstruction
13. Lean
14. 状态权重系统
15. 网络同步分离
```

每一步都应该单独调试。不要在一个文件里一次性加十个效果，否则你不知道手感问题来自哪里。

## 20. 一个最小整合示例

下面是一个非常简化的整合骨架：

```csharp
using Godot;

namespace EGame
{
    public partial class NFpsCameraController : Node3D
    {
        [Export] public Node3D YawPivot;
        [Export] public Node3D PitchPivot;
        [Export] public Node3D CameraEffectsPivot;
        [Export] public Camera3D Camera;
        [Export] public Node3D WeaponRoot;

        [Export] public float MouseSensitivity = 0.0025f;
        [Export] public float BaseFov = 75f;

        private Vector2 _MouseDelta;
        private float _Yaw;
        private float _Pitch;
        private float _AdsWeight;

        private CameraBob _CameraBob = new CameraBob();
        private WeaponBob _WeaponBob = new WeaponBob();
        private WeaponSway _WeaponSway = new WeaponSway();

        public override void _Input(InputEvent e)
        {
            if (e is InputEventMouseMotion motion)
                _MouseDelta += motion.Relative;
        }

        public override void _PhysicsProcess(double delta)
        {
            float dt = (float)delta;

            UpdateLook();
            UpdateStateWeights(dt);
            UpdateEffects(dt);
            ApplyTransforms();

            _MouseDelta = Vector2.Zero;
        }

        private void UpdateLook()
        {
            _Yaw -= _MouseDelta.X * MouseSensitivity;
            _Pitch -= _MouseDelta.Y * MouseSensitivity;
            _Pitch = Mathf.Clamp(_Pitch, Mathf.DegToRad(-89f), Mathf.DegToRad(89f));
        }

        private void UpdateStateWeights(float delta)
        {
            bool ads = Input.IsActionPressed("aim");
            float targetAds = ads ? 1f : 0f;
            _AdsWeight = Mathf.Lerp(_AdsWeight, targetAds, 1f - Mathf.Exp(-14f * delta));
        }

        private void UpdateEffects(float delta)
        {
            Vector2 moveInput = new Vector2(
                Input.GetActionStrength("right") - Input.GetActionStrength("left"),
                Input.GetActionStrength("up") - Input.GetActionStrength("down")
            );

            float moveSpeed01 = Mathf.Clamp(moveInput.Length(), 0f, 1f);

            _CameraBob.Update(delta, moveSpeed01, true, 1f - _AdsWeight * 0.85f);
            _WeaponBob.Update(delta, moveSpeed01, true, _AdsWeight, 0f);
            _WeaponSway.Update(delta, _MouseDelta, moveInput, _AdsWeight);
        }

        private void ApplyTransforms()
        {
            YawPivot.Rotation = new Vector3(0f, _Yaw, 0f);
            PitchPivot.Rotation = new Vector3(_Pitch, 0f, 0f);

            CameraEffectsPivot.Position = _CameraBob.Offset;
            Camera.Fov = Mathf.Lerp(BaseFov, BaseFov * 0.68f, _AdsWeight);

            Vector3 weaponPos = new Vector3(0.18f, -0.18f, -0.45f);
            weaponPos += _WeaponBob.PositionOffset;
            weaponPos += _WeaponSway.PositionOffset;

            Vector3 weaponRot = Vector3.Zero;
            weaponRot += _WeaponBob.RotationOffset;
            weaponRot += _WeaponSway.RotationOffset;

            WeaponRoot.Position = weaponPos;
            WeaponRoot.Rotation = weaponRot;
        }
    }
}
```

这段不是最终商业代码，而是告诉你系统应该怎么拆。实际项目里每个模块最好独立文件，武器参数也应该走数据配置。

## 21. 配置化：成熟项目一定要能调参数

FPS 手感高度依赖调参。不要把所有数值写死在代码里。推荐做一个资源：

```csharp
using Godot;

namespace EGame
{
    [GlobalClass]
    public partial class FpsCameraConfig : Resource
    {
        [Export] public float BaseFov = 75f;
        [Export] public float AdsFovMultiplier = 0.68f;
        [Export] public float MouseSensitivity = 0.0025f;

        [Export] public float WalkBobFrequency = 6f;
        [Export] public float SprintBobFrequency = 11f;
        [Export] public float CameraBobY = 0.03f;
        [Export] public float CameraBobX = 0.012f;

        [Export] public float WeaponBobPosition = 0.05f;
        [Export] public float WeaponSwayAmount = 0.002f;
        [Export] public float AdsSpeed = 14f;
    }
}
```

这样你可以在 Godot Inspector 里调参数，不用每次重新编译。商业项目里常见做法是每把武器有自己的 camera profile：

```text
PistolCameraProfile
RifleCameraProfile
ShotgunCameraProfile
SniperCameraProfile
```

每个 profile 控制：

```text
ADS FOV
ADS speed
recoil pattern
weapon sway
weapon bob
camera kick
```

## 22. 总结

成熟 FPS 镜头不是某一个技巧，而是一套分层系统。你可以把它记成下面这个公式：

```text
最终画面 = 稳定的玩法瞄准 + 可控的视觉偏移 + 状态权重混合 + 武器表现反馈
```

真正的工程原则是：

```text
Gameplay Aim 不要被视觉抖动污染
Camera Bob 要克制
Weapon Bob 和 Weapon Sway 提供运动感
Recoil 拆成真实和视觉两层
ADS 是整套状态，不只是 FOV
所有效果输出 Offset，最后统一合成
所有参数都应该配置化
```

如果你现在已经知道 `CameraBob` 和 `WeaponBob`，下一步最值得做的是：

```text
1. WeaponSway
2. ADS 权重系统
3. Gameplay Recoil + Visual Recoil
4. LandingImpact
5. 最终 Offset 合成器
```

这几项加上之后，FPS 手感会立刻从“相机在走路”变成“玩家在控制一个有重量、有反馈、有武器存在感的角色”。

## 23. 更深入的数学：为什么很多镜头效果都可以看成信号叠加

成熟 FPS 镜头的底层思想其实很像信号处理。玩家输入是一条信号，角色速度是一条信号，开火事件是一条脉冲信号，落地也是一条脉冲信号，受击和爆炸则是有持续时间和衰减的扰动信号。最终镜头不是由某一个信号决定，而是多个信号叠加后的结果。

可以把镜头位置写成：

```text
CameraPosition(t)
= BasePosition(t)
+ Bob(t)
+ Landing(t)
+ Recoil(t)
+ Shake(t)
+ Lean(t)
```

也可以把镜头旋转写成：

```text
CameraRotation(t)
= AimRotation(t)
+ VisualRecoil(t)
+ ShakeRotation(t)
+ BobRotation(t)
+ LeanRotation(t)
```

这里的 `t` 表示时间。每个效果都是时间函数。比如 Bob 是周期函数，Landing 是衰减函数，Shake 是噪声函数，Recoil 是脉冲加恢复函数。

Bob 的基础是三角函数：

```text
x(t) = A * sin(wt + p)
```

其中：

```text
A = amplitude，幅度
w = angular frequency，角频率
p = phase，相位
```

在代码中通常不用直接写 `w = 2πf`，而是让 `_Phase += frequency * delta`。严格一点，如果你希望 `frequency` 表示每秒几次循环，应该写：

```csharp
_Phase += Mathf.Tau * frequency * delta;
```

其中 `Tau = 2π`。如果只是调手感，直接把 `frequency` 当成相位速度也没问题，反正最终都靠调参。

Landing 和 Recoil 更像冲激响应。开火那一刻给系统一个速度或位移，然后系统逐渐回到零。用数学写就是：

```text
offset'(t) = velocity(t)
velocity'(t) = -k * offset(t) - c * velocity(t)
```

这是经典阻尼弹簧。`k` 是弹簧强度，`c` 是阻尼。强度越高，回正越快；阻尼越高，越不容易振荡。低阻尼会有弹性，高阻尼会稳但缺少冲击。FPS 武器一般适合“略微欠阻尼”，也就是有一点回弹，但不能来回摆太多。

如果你不想碰微分方程，可以用前面写过的简化弹簧：

```csharp
Velocity += (Target - Value) * stiffness * delta;
Velocity *= Mathf.Exp(-damping * delta);
Value += Velocity * delta;
```

这个公式虽然不是最严谨的物理积分，但足够做游戏手感。注意 `delta` 很大时仍然可能不稳定，所以真实项目中最好把移动和镜头更新放在稳定的 tick 中，或者限制最大 delta：

```csharp
float dt = Mathf.Min((float)delta, 1f / 30f);
```

Shake 可以理解成带包络的噪声：

```text
shake(t) = noise(t * frequency) * amplitude * envelope(t)
```

`envelope` 是包络，也就是随时间变小的权重。最简单包络：

```text
envelope = 1 - life01
```

更自然一点：

```text
envelope = (1 - life01)^2
```

平方会让开头更强，后面快速变弱。爆炸可以用慢衰减，开枪可以用快衰减。成熟项目里，每一种事件都应该有自己的 shake 配置，而不是所有东西共用一个随机抖动。

## 24. 瞄准射线、相机射线和枪口射线的关系

FPS 射击里有一个很容易绕晕的问题：子弹到底从哪里发射？从相机？从枪口？从屏幕中心？

常见方案有三种：

```text
1. 从相机中心发射射线
2. 从枪口发射射线
3. 相机先选目标点，枪口再朝目标点发射
```

第一种最符合玩家准星。玩家屏幕中心对着哪里，就打哪里。缺点是枪口可能被墙挡住，但相机还看得到敌人，导致“枪穿墙射击”的感觉。

第二种最符合物理。子弹真的从枪口出去。缺点是第一人称枪口和屏幕中心可能不完全一致，近距离射击会出现准星对着目标但子弹偏掉。

第三种是很多游戏会用的折中：

```text
先从相机中心射一条很远的射线，得到目标点 AimPoint
再从枪口朝 AimPoint 发射真实弹道
如果枪口到 AimPoint 中间被近处墙挡住，就打墙
```

代码示意：

```csharp
private Vector3 GetAimPoint(Camera3D camera)
{
    Vector3 origin = camera.GlobalPosition;
    Vector3 dir = -camera.GlobalBasis.Z;
    Vector3 end = origin + dir * 1000f;

    PhysicsRayQueryParameters3D query = PhysicsRayQueryParameters3D.Create(origin, end);
    var hit = GetWorld3D().DirectSpaceState.IntersectRay(query);

    if (hit.Count > 0)
        return (Vector3)hit["position"];

    return end;
}

private void FireFromMuzzle(Node3D muzzle, Camera3D camera)
{
    Vector3 aimPoint = GetAimPoint(camera);
    Vector3 origin = muzzle.GlobalPosition;
    Vector3 dir = (aimPoint - origin).Normalized();
    Vector3 end = origin + dir * 1000f;

    PhysicsRayQueryParameters3D query = PhysicsRayQueryParameters3D.Create(origin, end);
    var hit = GetWorld3D().DirectSpaceState.IntersectRay(query);
}
```

这里又回到前面的原则：用来取 `camera.GlobalBasis` 的最好是 Gameplay Aim 对应的稳定相机方向，而不是已经叠加大幅 shake 和 bob 的最终视觉相机方向。否则爆炸抖动或脚步 bob 会让射线方向微妙变化，手感会不稳定。

当然，有些游戏设计上就是希望开火时视觉后坐力影响真实瞄准，那也可以，但要明确区分“设计决定”和“代码混乱导致”。成熟系统最重要的是可控。

## 25. 参数调校方法：不要凭感觉乱调

FPS 手感调参非常主观，但仍然可以有方法。建议你每次只调一个维度，并且写下当前数值。比如调 Camera Bob 时，不要同时改 Weapon Bob、FOV、移动速度，否则你不知道到底是谁带来的变化。

一个实用调参流程：

```text
第一步：关闭所有视觉效果，只保留基础 Look 和移动
第二步：调鼠标灵敏度和 Pitch 限制
第三步：打开 Weapon Sway，调到武器不僵但不干扰
第四步：打开 Weapon Bob，调走路和跑步
第五步：少量加入 Camera Bob
第六步：加入 ADS，确保 ADS 稳定
第七步：加入 Recoil，先调单发，再调连发
第八步：加入 LandingImpact 和 FOV
第九步：加入 Shake
```

推荐调参时做一个 Debug UI，实时显示：

```text
speed
moveWeight
sprintWeight
adsWeight
bobPhase
cameraBobOffset
weaponSwayOffset
recoilOffset
currentFov
```

Godot 简单调试输出可以先用 `Label`，或者用 `Logger`：

```csharp
private void DebugCameraState()
{
    GD.Print(
        $"ads={_AdsWeight:F2}, " +
        $"bob={_CameraBob.Offset.Length():F3}, " +
        $"fov={Camera.Fov:F1}"
    );
}
```

但不要每帧都疯狂打印，否则输出会刷爆。可以每 0.2 秒打印一次：

```csharp
private float _DebugTimer;

private void UpdateDebug(float delta)
{
    _DebugTimer -= delta;
    if (_DebugTimer > 0f)
        return;

    _DebugTimer = 0.2f;
    DebugCameraState();
}
```

成熟项目里通常会做运行时调参面板。你可以在 Godot 里用 `Resource` 保存配置，编辑器里改数值，运行时热加载或者直接引用。这样调手感会快很多。

## 26. 动画和程序化镜头的边界

FPS 武器表现通常由两部分组成：

```text
动画师做的关键帧动画
程序生成的实时偏移
```

比如换弹、检视、拉栓、切枪，这些适合动画。移动时轻微摆动、转向滞后、后坐回弹、ADS 对齐，这些适合程序化。不要试图用程序解决所有动画问题，也不要把所有细微反馈都塞进动画里。

一个常见分层：

```text
WeaponAnimationRoot:
    播放换弹、开火、切枪等动画

WeaponProceduralRoot:
    叠加 bob、sway、recoil、ADS

WeaponModel:
    真正模型
```

也可以反过来，让程序根节点在上，动画节点在下。关键是你要明确顺序。顺序不同，效果不同：

```text
先 ADS 后 Recoil:
    后坐力在瞄准姿态上跳，常见

先 Recoil 后 ADS:
    ADS 会把一部分后坐力压回去，可能显得怪
```

一般建议：

```text
BasePose
-> ADS Pose
-> Weapon Bob
-> Weapon Sway
-> Weapon Recoil
-> Obstruction
-> Animation Additive
```

但具体取决于你的动画系统。如果使用 Godot 的 `AnimationPlayer`，你要小心动画直接写 Node3D 的 Position/Rotation 时，会覆盖程序化代码。解决方法是让动画控制一个子节点，程序控制父节点，避免抢同一个 Transform。

例如：

```text
WeaponViewRoot        程序控制 ADS 和 obstruction
    WeaponMotionRoot  程序控制 bob/sway/recoil
        WeaponAnimRoot 动画控制开火/换弹
            WeaponMesh
```

这样不同系统不会互相覆盖。

## 27. 设计风格：不同 FPS 的镜头不是同一种

成熟 FPS 镜头没有唯一答案，因为游戏风格不同。你要先决定你的游戏想要什么感觉。

竞技 FPS 通常要求：

```text
镜头非常稳
输入延迟极低
Camera Bob 很弱
视觉后坐力克制
命中反馈清晰
FOV 变化少
```

战术 FPS 通常要求：

```text
武器存在感强
ADS 很重要
Lean 和 Obstruction 重要
移动更有重量
呼吸和轻微 sway 可以更明显
```

生存恐怖 FPS 通常要求：

```text
镜头更有身体感
武器不一定稳定
受击和疲劳反馈更强
黑暗环境下手电筒和相机运动结合
```

高速移动 FPS 通常要求：

```text
FOV kick 明显
落地和滑铲反馈强
相机仍要稳定，不能因为速度快就乱晃
武器动画可以更夸张
```

所以调参数之前，要先写一句设计目标。比如：

```text
这个项目的 FPS 镜头目标：
镜头本体稳定，武器有轻微重量感，奔跑有速度感，ADS 稳定，后坐力可学习。
```

有了目标以后，你就能判断某个效果该不该加。如果一个效果看起来很酷，但破坏了目标，就应该删掉或减弱。成熟不是效果多，而是每个效果都服务于玩法。

## 28. 一个更完整的模块清单

最后给你一个可以直接当开发 checklist 的清单：

```text
基础输入：
    鼠标输入
    手柄输入
    灵敏度
    ADS 灵敏度倍率
    反转 Y 轴
    Pitch clamp

镜头基础：
    YawPivot
    PitchPivot
    CameraEffectsPivot
    Gameplay Aim
    Visual Camera

移动反馈：
    Camera Bob
    Weapon Bob
    Sprint FOV
    Crouch height transition
    Landing impact
    Jump lift

武器反馈：
    Weapon Sway
    Move Lag
    Fire Recoil
    Recoil Recovery
    Reload animation separation
    Weapon Obstruction

瞄准：
    ADS position
    ADS rotation
    ADS FOV
    ADS sensitivity
    ADS bob reduction
    ADS sway reduction

战斗反馈：
    Camera Shake
    Hit Direction Kick
    Damage Vignette
    Low HP breathing
    Explosion impulse

工程化：
    Config Resource
    Offset composition
    Debug UI
    Per weapon profile
    Network separation
```

每完成一个模块，都要问三个问题：

```text
它是否影响 Gameplay Aim？
它是否能被状态权重控制？
它是否能单独关闭调试？
```

如果答案不清楚，说明架构还不够成熟。

## 29. 最后的一点经验

FPS 镜头的好坏，往往不是某个公式决定的，而是大量很小的反馈共同决定的。一个 0.02 米的武器偏移，一个 0.8 度的 Roll，一个 0.15 秒的 ADS 过渡，一个 6 度的 sprint FOV 增量，单独看都不显眼，但叠在一起就会让玩家感觉“这个角色是活的”。

但也正因为这些效果很小，所以最怕无节制叠加。每个效果都应该有原因，有范围，有权重，有关闭开关。你要能随时回答：

```text
这个 offset 从哪里来？
它影响视觉还是玩法？
它在哪些状态下变强？
它在哪些状态下变弱？
它什么时候归零？
```

当你能回答这些问题，你的 FPS 镜头系统就已经从“几个效果脚本”变成了“可维护的表现系统”。
