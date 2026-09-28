
using Godot;
using System;
using System.Collections.Generic;

namespace EGame
{
    public partial class NPlayer : CharacterBody3D, INCharacter
    {
        private const string _PrefabPath = "player/player";
        public static NPlayer Create(Player player)
        {
            var instance = SceneHelper.LoadScene<NPlayer>(_PrefabPath);
            instance.PlayerData = player;
            instance.Data.OnCharacterCreated(instance);
            instance.Data.OnPlayerCreated(instance);
            return instance;
        }

        public Player PlayerData { get; private set; }
        
        public CharacterModel Data
        {
            get
            {
                return PlayerData.PlayerModel;
            }
        }

        // 分层状态机：什么时候移动/开火/换枪都由状态决定，NPlayer 自己负责每帧驱动三个 layer，以及"怎么算"（移动公式、镜头效果）和存数据。
        // ModeLayer 互斥，Normal 模式里才会让 MovementLayer 和 ActionLayer 有当前状态（并行跑）
        public PlayerStateLayer ModeLayer { get; private set; }
        public PlayerStateLayer MovementLayer { get; private set; }
        public PlayerStateLayer ActionLayer { get; private set; }

        private void BuildStateLayers()
        {
            ModeLayer = new PlayerStateLayer(this);
            MovementLayer = new PlayerStateLayer(this);
            ActionLayer = new PlayerStateLayer(this);

            ModeLayer.Add(new PlayerModeStateNormal());
            ModeLayer.Add(new PlayerModeStateDash());
            ModeLayer.Add(new PlayerModeStateFly());

            MovementLayer.Add(new PlayerMoveStateIdle());
            MovementLayer.Add(new PlayerMoveStateWalk());
            MovementLayer.Add(new PlayerMoveStateRun());
            MovementLayer.Add(new PlayerMoveStateCrouch());
            MovementLayer.Add(new PlayerMoveStateAir());

            ActionLayer.Add(new PlayerActionStateIdle());
            ActionLayer.Add(new PlayerActionStateSwitch());
            ActionLayer.Add(new PlayerActionStateFire());
            ActionLayer.Add(new PlayerActionStateReload());
            ActionLayer.Add(new PlayerActionStateMelee());

            ModeLayer.ChangeState(PlayerConfig.ModeNormal);
        }

        // 开火输入意图，动作层状态从这里读
        public WeaponIntent Intent { get; } = new WeaponIntent();

        public void TakeDamage(DamageInfo info)
        {
            if (Data.HP <= 0)
                return;    // 已经死了，不再触发受伤反馈

            Data.HP -= info.Amount;
            if (Data.HP <= 0)
                Die();
        }

        public void Die()
        {
            OnDead();
        }

        private void OnDead()
        {
            AnimTrigger(AnimationConfig.DeadTrigger);
            UIManager.Instance.Show(UIPanelType.FailurePanel);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////
        ////////                                    Animator
        ///////////////////////////////////////////////////////////////////////////////////////////////////////

        private CreatureAnimator _Animator;

        public void BuildAnimator(CreatureAnimator animator)
        {
            if (animator == null)
                return;

            var player = FindChild("AnimationPlayer", recursive: true, owned: false) as AnimationPlayer;
            if (player == null)
                return;

            if (_Animator != null)
                throw new InvalidOperationException("Animator already had value!");

            _Animator = animator;
            _Animator.SetPlayer(player);
        }

        public void AnimTrigger(string trigger)
        {
            _Animator?.CallTrigger(trigger);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////                                      人物移动
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public float WalkSpeed => PlayerData.PlayerModel.MoveSpeed;
        public float RunSpeed => PlayerData.PlayerModel.RunSpeed;
        public float CrouchSpeed => PlayerData.PlayerModel.CrouchSpeed;

        private readonly float _MinStopSpeed = 2.54f;
        private readonly float _Friction = 6f;
        private readonly float _AirFriction = 1f;    // 空中水平摩擦，比地面小很多，只给一点点空气阻力感
        private readonly float _AccelerationRate = 10f;

        public bool RunInput
        {
            get
            {
                return false;
                return Input.IsActionPressed(EGInput.RUN) && IsCrouch == false;
            }
        }

        public bool JumpPressed => Input.IsActionJustPressed(EGInput.JUMP);
        public bool DashPressed => Input.IsActionJustPressed(EGInput.DASH);
        public bool ReloadPressed => Input.IsActionJustPressed(EGInput.RELOAD);

        // 冲刺冷却：从开始冲刺那一刻算起，CanDash 为 true 才允许再冲
        private readonly double _DashCooldown = 1.5f;
        private double _DashCooldownRemaining;

        public bool CanDash => _DashCooldownRemaining <= 0;

        // 给 HUD 显示冷却用
        public float DashCooldownTime => (float)_DashCooldown;
        public float DashCooldownRemaining => (float)Math.Max(_DashCooldownRemaining, 0.0);

        public void StartDashCooldown()
        {
            _DashCooldownRemaining = _DashCooldown;
        }

        // 身体正前方（水平），跟 GetMoveDir 里"按前进键"的方向一致
        public Vector3 ForwardDirection => _YawNode.Quaternion * Vector3.Back;

        // 玩家想往哪走：输入方向按当前朝向转到世界空间
        public Vector3 WishDirection => _YawNode.Quaternion * GetMoveDir();

        // 自由环游模式用：按摄像机完整朝向（含俯仰）转输入方向，抬头按前进就是往上飞。
        // 不能直接拿 _RealCamera.GlobalTransform.Basis 乘——Godot 真实摄像机是 -Z 朝前，
        // 跟这个项目自己"局部 +Z 才是前方"的约定是反的，得复用 _YawNode.Quaternion 这套已经处理对的
        public Vector3 FlyDirection => _YawNode.Quaternion * Quaternion.FromEuler(new Vector3(Mathf.DegToRad(_PitchAngle), 0f, 0f)) * GetMoveDir();

        // 下面三个是给状态机用的"怎么动"：状态决定什么时候用、用多大速度，具体的加速/摩擦公式留在这里
        public void MoveOnGround(double dt, float speed)
        {
            Velocity = ApplyFriction(Velocity, _Friction, dt);
            Velocity = ApplyAcceleration(Velocity, WishDirection, _AccelerationRate, speed, dt);
        }

        public void MoveInAir(double dt)
        {
            Velocity = ApplyHorizontalFriction(Velocity, _AirFriction, dt);
            Velocity = ApplyAcceleration(Velocity, WishDirection, _AccelerationRate * 0.1f, WalkSpeed, dt);
        }

        // 站在地面上时，按当前输入应该处在哪个地面状态（Idle/Walk/Run/Crouch）
        public string ResolveGroundMoveState()
        {
            if (IsCrouch)
                return PlayerConfig.MoveCrouch;

            if (WishDirection.LengthSquared() < 0.0001f)
                return PlayerConfig.MoveIdle;

            return RunInput ? PlayerConfig.MoveRun : PlayerConfig.MoveWalk;
        }

        public void Jump()
        {
            Velocity = ApplyJump(Velocity);
        }

        private Vector3 ApplyAcceleration(Vector3 source, Vector3 wish_dir, float acceleration_rate, float move_speed, double dt)
        {
            if (wish_dir.LengthSquared() < 0.0001f)
                return source;

            float vel_proj = source.Dot(wish_dir) / wish_dir.Length();
            
            float add_speed = move_speed - vel_proj;    //计算出当前速度距离目标速度还差多少
            float true_add_speed = Mathf.Min((float)(add_speed * dt * acceleration_rate), add_speed);  //钳制最大速度，防止速度超出最大速度
            
            return source += wish_dir * true_add_speed;
        }

        // 只对水平分量做摩擦，Y（下落/上升速度）原样保留——不然摩擦会把重力算出来的下落速度也一起吃掉
        private Vector3 ApplyHorizontalFriction(Vector3 source, float friction, double dt)
        {
            Vector3 horizontal = new Vector3(source.X, 0f, source.Z);
            Vector3 damped = ApplyFriction(horizontal, friction, dt);
            return new Vector3(damped.X, source.Y, damped.Z);
        }

        private Vector3 ApplyFriction(Vector3 source, float friction, double dt)
        {
            float cur_speed = source.Length();
            if (cur_speed < 0.001f)
                return Vector3.Zero;

            cur_speed = cur_speed > _MinStopSpeed ? cur_speed : _MinStopSpeed;    //此处是为了防止速度太小，导致速度一致减不下去
            float drop = (float)(cur_speed * friction * dt);
            float total_speed = Mathf.Max(0f, cur_speed - drop);

            //标量转为矢量,不用source.Length()，少了一次根号运算
            return source * (total_speed / cur_speed);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////                                      Y轴速度相关
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////

        // 状态控制的开关：冲刺这类需要平直移动的状态会关掉重力
        public bool GravityEnabled { get; set; } = true;

        // 自由环游模式关掉碰撞：不调 MoveAndSlide，直接改 GlobalPosition，能穿墙穿地板
        public bool CollisionEnabled { get; set; } = true;

        private readonly float _UpGravity = -9.8f;
        private readonly float _DownGravity = -15.0f;

        private readonly float _JumpSpeed = 4.6f;

        private Vector3 ApplyGravity(Vector3 source, double dt)
        {
            if (IsOnFloor())
            {
                if (source.Y < 0f)
                    source.Y = IsCrouch ? -3.0f : -0.5f;
                return source;
            }

            float gravity = source.Y > 0f ? _UpGravity : _DownGravity;
            source.Y += (float)(gravity * dt);
            return source;
        }

        private Vector3 ApplyJump(Vector3 source)
        {
            return source + new Vector3(0.0f, _JumpSpeed, 0.0f);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////                                      蹲伏相关
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private readonly float _StandHeight = 1.8f;
        private readonly float _CrouchHeight = 1.2f;
        private readonly float _CrouchChangeSpeed = 12.0f;

        private readonly float _EyeOffsetFromTop = 0.4f;
        private float _EyesPos = 0.0f;

        private CollisionShape3D _MoveCollisionShape;
        
        public bool IsCrouch
        {
            get
            {
                return Input.IsActionPressed(EGInput.CROUCH);
            }
        }

        private void UpdateCrouch(double dt)
        {
            float target_crouch_height = IsCrouch ? _CrouchHeight : _StandHeight;

            var capsule = (CapsuleShape3D)_MoveCollisionShape.Shape;

            float target_eyes_offset = _StandHeight - _EyeOffsetFromTop;
            float weight = 1f - Mathf.Exp(-_CrouchChangeSpeed * (float)dt);

            _EyesPos = Mathf.Lerp(_EyesPos, target_eyes_offset, weight);
            capsule.Height = Mathf.Lerp(capsule.Height, target_crouch_height, weight);
            _MoveCollisionShape.Position.Lerp(new Vector3(0f, _StandHeight - target_crouch_height * 0.5f, 0f), weight);

            _PitchNode.Position = new Vector3(0.0f, _EyesPos, 0.0f);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////                                      相机上下左右旋转相关
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private Node3D _YawNode;
        private Node3D _PitchNode;
        private Camera3D _RealCamera;

        //x为yaw, y为pitch
        private Vector2 _RotateSensity = new Vector2(0.05f, 0.05f);

        private readonly Vector2 _PitchLimit = new Vector2(-90f, 90f);
        private float _PitchAngle = 0f;

        private Vector2 ViewAngleDegrees => new Vector2(_PitchAngle, RotationDegrees.Y);

        private void HandleCameraRotation(Vector2 mouse_delta)
        {
            float x_delta = -mouse_delta.X * _RotateSensity.X;
            _YawNode.Rotate(Vector3.Up, Mathf.DegToRad(x_delta));

            float y_delta = mouse_delta.Y * _RotateSensity.Y;
            _PitchAngle += y_delta;
            _PitchAngle = Mathf.Clamp(_PitchAngle, _PitchLimit.X, _PitchLimit.Y);
            _PitchNode.Quaternion = Quaternion.FromEuler(new Vector3(Mathf.DegToRad(_PitchAngle), 0.0f, 0.0f));
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////                                       视角 Lean
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private Node3D _CameraLeanNode;

        private readonly float _RunPitchAmount = 0.0035f; //纯速度驱动的前后倾
        private readonly float _RunRollAmount = 0.0018f;   //纯速度驱动的左右倾

        private Vector3 _ViewRunLeanAngles;

        private void UpdateCameraLean()
        {
            var local_vel = Transform.Basis.Inverse() * Velocity;   // 角色本体现在自己就是 Yaw，直接用自己的 Transform 转到局部坐标
            _ViewRunLeanAngles = new Vector3(local_vel.Z * _RunPitchAmount, 0f, -local_vel.X * _RunRollAmount);

            // 直接赋值，不是叠加——每帧都是全新算出来的目标角度，不会有累积问题
            _CameraLeanNode.Rotation = _ViewRunLeanAngles;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////                                       相机Bob
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////
        
        private Node3D _CameraBobNode;

        public readonly float WalkBobRate = 0.8f;
        public readonly float RunBobRate = 1.2f;
        public readonly float CrouchBobRate = 0.6f;
        private readonly float _MinBobSpeed = 0.3f;      //低于这个速度直接清零，不产生 bob

        // 移动层状态控制的开关：Bob 开不开、当前用多快的节奏
        public bool ViewBobEnabled { get; set; } = true;
        public float ViewBobRate { get; set; } = 0.8f;
        
        private readonly float _CameraBobRightScale = 0.009f;   //Bob水平幅度
        private readonly float _CameraBobUpScale = 0.0025f;      //Bob垂直幅度
        private readonly float _CameraLookAheadDistance = 15f;

        private float _BobCycle;
        private Vector3 _ViewBobPosition;

        private float _XySpeed;

        private void UpdateViewBob(double dt)
        {
            var horizontal_vel = new Vector3(Velocity.X, 0f, Velocity.Z);
            _XySpeed = horizontal_vel.Length();

            if (!ViewBobEnabled || _XySpeed <= _MinBobSpeed)
            {
                _BobCycle = 0f;
                _ViewBobPosition = _ViewBobPosition.Lerp(Vector3.Zero, (float)dt * 10f);
                ApplyBobToCamera();
                return;
            }

            _BobCycle += ViewBobRate * (float)dt * Mathf.Tau;

            _ViewBobPosition = ComputeCameraBobOffset(_BobCycle, _XySpeed);

            ApplyBobToCamera();
        }

        //视角的水平和垂直位置偏移
        private Vector3 ComputeCameraBobOffset(float bob_cycle, float xy_speed)
        {
            float bob_right = xy_speed * _CameraBobRightScale * Mathf.Sin(bob_cycle);
            float bob_up = xy_speed * _CameraBobUpScale * Mathf.Cos(2f * bob_cycle);
            return new Vector3(bob_right, bob_up, 0f);
        }

        private void ApplyBobToCamera()
        {
            _CameraBobNode.Position = _ViewBobPosition;

            Vector3 look_target = _CameraLeanNode.ToGlobal(new Vector3(0f, 0f, -_CameraLookAheadDistance));
            Vector3 lean_up = _CameraLeanNode.GlobalTransform.Basis.Y;
            _CameraBobNode.LookAt(look_target, lean_up);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////                                       落地时的冲击力
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private Node3D _CameraLandNode;

        private float _LandOffset;
        private float _LandStregth = 0f;
        private double _LandStartTimer = 0.0f;
        private readonly float _LandDeflectTime = 0.03f;     //下沉时间
        private readonly float _LandReturnTime = 0.3f;       //回弹时间
        private float _LastFallSpeed;

        private Vector3 LandingDipOffset => new Vector3(0f, _LandOffset, 0f);

        private void TrachFallSpeed()
        {
            if (!IsOnFloor())
            {
                _LastFallSpeed = Mathf.Min(_LastFallSpeed, Velocity.Y);
                return;
            }
            if (_LastFallSpeed < -3.0f)   // 有意义的下落速度才触发，轻微的台阶步进不该有反馈
            {
                // 按冲击力度分四档，越重摔得越明显
                float severity = Mathf.Abs(_LastFallSpeed);
                _LandStregth = severity switch
                {
                    > 16f => 0.28f,
                    > 12f => 0.22f,
                    > 9f => 0.17f,
                    _ => 0.13f,
                };
                _LandStartTimer = Time.GetTicksMsec() / 1000.0;
            }
            _LastFallSpeed = 0;
        }

        private void UpdateLandingOffset()
        {
            if (_LandStregth < 0.01f)
                return;

            double process_time = (Time.GetTicksMsec() / 1000.0) - _LandStartTimer;
            if(process_time < _LandDeflectTime)
            {
                _LandOffset = (float)Mathf.Lerp(0f, -_LandStregth, process_time / _LandDeflectTime);
            }
            else if(process_time < _LandDeflectTime + _LandReturnTime)
            {
                _LandOffset = (float)Mathf.Lerp(-_LandStregth, 0f, (process_time - _LandDeflectTime) / _LandReturnTime);
            }
            else
            {
                _LandStregth = 0f;
                _LandOffset = 0f;
            }

            _CameraLandNode.Position = new Vector3(0.0f, _LandOffset, 0.0f);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////                                       武器 Bob
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private Node3D _WeaponBobNode;

        public bool WeaponBobEnabled { get; set; } = true;

        private readonly float _WeaponBobRightScale = -0.002f;
        private readonly float _WeaponBobUpScale = 0.001f;

        private void UpdateWeaponBob(double dt)
        {
            if (_WeaponBobNode == null)
                return;

            _WeaponBobNode.Position = WeaponBobEnabled
                ? ComputeWeaponBobOffset(_BobCycle, _XySpeed)
                : Vector3.Zero;
        }
        
        private Vector3 ComputeWeaponBobOffset(float bob_cycle, float xy_speed)
        {
            float bob_right = xy_speed * _WeaponBobRightScale * Mathf.Sin(bob_cycle);
            float bob_up = xy_speed * _WeaponBobUpScale * Mathf.Cos(2f * bob_cycle);
            return new Vector3(bob_right, bob_up, 0f);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////                                       武器 Sway
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private Node3D _WeaponSwayNode;

        public bool WeaponSwayEnabled { get; set; } = true;   // 转头滞后，跟脚步相位无关

        private readonly float _WeaponTurnSwayScale = 0.15f;
        private readonly float _WeaponTurnSwayMaxDegrees = 6.0f;
        private readonly int _WeaponTurnSwayAverageFrames = 10;

        // 视角历史用数组+游标模拟环形缓冲，而不是 Queue——这样可以按"帧号"直接索引取值，
        // 不用每帧都做一次出队/入队
        private readonly Vector2[] _ViewAngleHistory = new Vector2[64];
        private int _ViewAngleWriteIndex;
        private int _ViewAngleFrameCount;

        private void UpdateWeaponSway(double dt)
        {
            if (_WeaponSwayNode == null)
                return;

            if (!WeaponSwayEnabled)
            {
                _WeaponSwayNode.RotationDegrees = Vector3.Zero;
                return;
            }

            Vector2 view_angle_degrees = ViewAngleDegrees;
            LogViewAngle(view_angle_degrees);

            _WeaponSwayNode.RotationDegrees = ComputeWeaponTurnOffset(view_angle_degrees);
        }

        private void LogViewAngle(Vector2 view_angle_degrees)
        {
            _ViewAngleHistory[_ViewAngleWriteIndex % _ViewAngleHistory.Length] = view_angle_degrees;
            _ViewAngleWriteIndex++;
            _ViewAngleFrameCount = Mathf.Min(_ViewAngleFrameCount + 1, _ViewAngleHistory.Length);
        }

        private Vector3 ComputeWeaponTurnOffset(Vector2 current_view_angle)
        {
            if (_ViewAngleFrameCount == 0) return Vector3.Zero;

            //取最近n帧
            int n = Mathf.Min(_WeaponTurnSwayAverageFrames, _ViewAngleFrameCount);

            //计算最近n帧内，视角的评价偏移
            Vector2 avg = current_view_angle;
            for (int j = 1; j < n; j++)
            {
                int idx = (_ViewAngleWriteIndex - 1 - j + _ViewAngleHistory.Length) % _ViewAngleHistory.Length;
                Vector2 sample = _ViewAngleHistory[idx];
                float yaw_delta = sample.Y - current_view_angle.Y;
                if (yaw_delta > 180f) yaw_delta -= 360f;
                else if (yaw_delta < -180f) yaw_delta += 360f;
                avg += new Vector2(sample.X - current_view_angle.X, yaw_delta) / n;
            }

            //移动的平均偏移越大，武器越偏
            Vector2 diff = (avg - current_view_angle) * _WeaponTurnSwayScale;
            diff.X = Mathf.Clamp(diff.X, -_WeaponTurnSwayMaxDegrees, _WeaponTurnSwayMaxDegrees);
            diff.Y = Mathf.Clamp(diff.Y, -_WeaponTurnSwayMaxDegrees, _WeaponTurnSwayMaxDegrees);
            return new Vector3(diff.X, diff.Y, 0);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////                                       武器 速度后拉
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private Node3D _WeaponSpeedPullNode;

        private readonly float _WeaponSpeedPullReferenceSpeed = 6f;   // 用来把 xy_speed 归一化到 0~1，再套缓动曲线
        private readonly float _WeaponSpeedPullMax = 0.06f;           // 曲线顶点对应的最大后拉幅度
        
        private void UpdateWeaponSpeedPull(double dt)
        {
            if (_WeaponSpeedPullNode == null)
                return;

            _WeaponSpeedPullNode.Position = ComputeWeaponSpeedPullOffset(_XySpeed);
        }

        // 先把速度归一化到 0~1，再套 InCubic 缓动(t^3)：跟 OutSine 相反，低速时后拉起步很慢、
        // 几乎感觉不到，快到参考速度时才陡然冲起来，触顶前反而是最快的一段
        private Vector3 ComputeWeaponSpeedPullOffset(float xy_speed)
        {
            float t = Mathf.Clamp(xy_speed / _WeaponSpeedPullReferenceSpeed, 0f, 1f);
            float eased = t * t * t;
            float pull = eased * _WeaponSpeedPullMax;
            return new Vector3(0f, 0f, pull);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////                                       武器 Landing
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private Node3D _WeaponLandingNode;

        private void UpdateWeaponLanding(double dt)
        {
            if (_WeaponLandingNode == null)
                return;

            _WeaponLandingNode.Position = LandingDipOffset * 0.25f;   // 武器自己的落地冲击，强度是摄像机那份的 0.25 倍
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////                                      辅助函数
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private Vector3 GetMoveDir()
        {
            var dir = Vector3.Zero;

            if (Input.IsActionPressed(EGInput.UP))
                dir.Z += 1f;

            if (Input.IsActionPressed(EGInput.DOWN))
                dir.Z -= 1f;

            if (Input.IsActionPressed(EGInput.RIGHT))
                dir.X -= 1f;

            if (Input.IsActionPressed(EGInput.LEFT))
                dir.X += 1f;

            dir = dir.Normalized();
            return dir;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////                                          武器管理
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private Dictionary<string, BoneAttachment3D> _BoneAttachmentDic = new Dictionary<string, BoneAttachment3D>();
        private List<NWeapon> _Weapons = new List<NWeapon>();
        private int _CurrentWeaponIndex = -1;
        private bool WeaponIndexValid => _CurrentWeaponIndex >= 0 && _CurrentWeaponIndex < _Weapons.Count;

        // 当前手上的武器，没有武器时是 null；动作层状态都从这里拿武器
        public NWeapon CurrentWeapon => WeaponIndexValid ? _Weapons[_CurrentWeaponIndex] : null;

        // 当前武器换了（拿到新武器/切枪）时触发
        public event Action<NWeapon> OnWeaponChanged;

        // 当前武器的弹药变了（当前弹匣或者备弹任意一个）就转发这个事件，外部（HUD）只用订阅这一个
        public event Action OnAmmoChanged;

        private void RaiseAmmoChanged()
        {
            OnAmmoChanged?.Invoke();
        }

        private void RegisterWeaponBoneAttachment()
        {
            var model = GetNodeOrNull<Node3D>("%Model");
            if(model != null)
            {
                var bone_attach_ments = FindChildren("*", nameof(BoneAttachment3D), true);
                foreach(var bone in bone_attach_ments)
                {
                    if (_BoneAttachmentDic.ContainsKey(bone.Name) == false)
                        _BoneAttachmentDic.Add(bone.Name, bone as BoneAttachment3D);
                }
            }
        }

        private void PickWeapon(NWeapon weapon)
        {
            var parent_name = weapon.Data.ParentName;
            if (_BoneAttachmentDic.ContainsKey(parent_name))
            {
                _BoneAttachmentDic[parent_name].AddChild(weapon);
                weapon.Position = Vector3.Zero;
                weapon.Quaternion = Quaternion.Identity;
                weapon.Scale = Vector3.One * 0.01f;
            }
            else
                throw new InvalidOperationException($"Unknow weapon parent : {parent_name}");

            _Weapons.Add(weapon);
            SetWeapon(_Weapons.Count - 1);
        }

        private void RemoveWeapon(int index)
        {
            _Weapons.RemoveAt(index);
            if (_CurrentWeaponIndex == index)
                SetWeapon(0);
        }

        private void SetWeapon(int index)
        {
            AssertWeaponIndex(index);
            if (index == _CurrentWeaponIndex)
                return;

            var old_weapon = CurrentWeapon;
            old_weapon?.UnEquip();

            _CurrentWeaponIndex = index;
            CurrentWeapon.Equip();
            Intent.Reset();
            OnWeaponChanged?.Invoke(CurrentWeapon);

            if (old_weapon?.RangedData != null)
                old_weapon.RangedData.OnAmmoChanged -= RaiseAmmoChanged;

            if (CurrentWeapon?.RangedData != null)
                CurrentWeapon.RangedData.OnAmmoChanged += RaiseAmmoChanged;

            // 切枪播放切枪动画+计时都在 Switch 状态里，这里只负责切过去（同时会让上一个动作状态正常退出，比如关掉近战碰撞体）
            ActionLayer.ChangeState(PlayerConfig.ActionSwitch);
        }

        // 读取武器相关的输入：切枪直接执行，开火只是记下意图，什么时候真正开火由动作层状态决定
        private void HandleWeaponInput()
        {
            // 动作层只在 Normal 模式里运行，其他模式（以后的冲刺、硬直）下不接收武器输入
            if (_Weapons.Count == 0 || ModeLayer.CurrentName != PlayerConfig.ModeNormal)
            {
                Intent.Reset();
                return;
            }

            if (Input.IsActionJustPressed(EGInput.SWITCHLEFT))
                SetWeapon((_CurrentWeaponIndex - 1 + _Weapons.Count) % _Weapons.Count);
            else if (Input.IsActionJustPressed(EGInput.SWITCHRIGHT))
                SetWeapon((_CurrentWeaponIndex + 1) % _Weapons.Count);

            Intent.Pressing = Input.IsActionPressed(EGInput.FIRE);
            Intent.JustPressed = Input.IsActionJustPressed(EGInput.FIRE);
        }

        private void AssertWeaponIndex(int index)
        {
            if (index < 0 && index >= _Weapons.Count)
                throw new ArgumentException("Weapon Index is overflow!");
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////

        public override void _Ready()
        {
            base._Ready();

            Input.MouseMode = Input.MouseModeEnum.Captured;

            _MoveCollisionShape = GetNode<CollisionShape3D>("%MoveCollider");
            _YawNode = this;
            _PitchNode = GetNode<Node3D>("%Pitch");
            _RealCamera = GetNode<Camera3D>("%RealCamera");
            _CameraLeanNode = GetNode<Node3D>("%CameraLean");
            _CameraBobNode = GetNode<Node3D>("%CameraBob");
            _CameraLandNode = GetNode<Node3D>("%CameraLand");
            _WeaponBobNode = GetNodeOrNull<Node3D>("%WeaponBob");
            _WeaponSwayNode = GetNodeOrNull<Node3D>("%WeaponSway");
            _WeaponSpeedPullNode = GetNodeOrNull<Node3D>("%WeaponSpeedPull");
            _WeaponLandingNode = GetNodeOrNull<Node3D>("%WeaponLanding");

            _EyesPos = _StandHeight - _EyeOffsetFromTop;
            _PitchNode.Position = new Vector3(0.0f, _EyesPos, 0.0f);

            RegisterWeaponBoneAttachment();

            // 状态机要先启动，再拿武器：拿武器时会让动作层切到 Switch，得有一个已经在跑的动作层
            BuildStateLayers();

            /*var hand = ModelDB.MeleeWeapon<SwordModel>() as MeleeWeaponModel;
            var hand_weapon = NWeapon.Create(this, hand);
            PickWeapon(hand_weapon); // AddChild 之后 NWeapon._Ready() 才跑完，AttackCollision 才有值
            hand_weapon.AttackCollision.BodyEntered += (body) => OnMeleeHit(hand_weapon, body);*/

            var pistol = ModelDB.RangedWeapon<ShotgunPistolModel>().MutableClone() as RangedWeaponModel;
            PickWeapon(NWeapon.Create(this, pistol));

            var greate_sword = ModelDB.MeleeWeapon<GreateSwordModel>() as MeleeWeaponModel;
            var greate_sword_n = NWeapon.Create(this, greate_sword);
            PickWeapon(greate_sword_n);
            greate_sword_n.AttackCollision.BodyEntered += (body) => OnMeleeHit(greate_sword_n, body);
        }

        // 近战武器的 AttackCollision 扫到目标时调用，伤害按这一下命中时的连击段数取
        private void OnMeleeHit(NWeapon weapon, Node3D body)
        {
            Log.VeryDebug($"[MeleeHit] 打到了: {body.Name}");
            int damage = weapon.MeleeData.GetDamage(weapon.CurrentComboIndex);
            var damageInfo = new DamageInfo(body, body.GlobalPosition, Vector3.Up, Data, damage);
            DamageSystem.Instance.ReportHit(damageInfo);
        }

        public override void _Input(InputEvent @event)
        {
            base._Input(@event);
            
            if (@event is InputEventMouseMotion motion)
                HandleCameraRotation(motion.Relative);

        }
        
        public override void _Process(double delta)
        {
            base._Process(delta);
            if(Input.IsActionJustPressed(EGInput.EXIT))
            {
                var is_locked = Input.MouseMode == Input.MouseModeEnum.Captured;
                Input.MouseMode = is_locked ? Input.MouseModeEnum.Visible : Input.MouseModeEnum.Captured;
            }

            HandleWeaponInput();
            ModeLayer.Process(delta);
            MovementLayer.Process(delta);
            ActionLayer.Process(delta);
        }

        public override void _PhysicsProcess(double delta)
        {
            if (_DashCooldownRemaining > 0)
                _DashCooldownRemaining -= delta;

            // 状态机负责水平速度和起跳（移动层），重力、蹲伏碰撞体、MoveAndSlide 和镜头效果每帧都要算，留在这里
            ModeLayer.PhysicalProcess(delta);
            MovementLayer.PhysicalProcess(delta);
            ActionLayer.PhysicalProcess(delta);

            if (GravityEnabled)
                Velocity = ApplyGravity(Velocity, delta);

            if (CollisionEnabled)
            {
                UpdateCrouch(delta);
                MoveAndSlide();
            }

            UpdateCameraLean();
            UpdateViewBob(delta);

            TrachFallSpeed();
            UpdateLandingOffset();
            UpdateWeaponBob(delta);
            UpdateWeaponSway(delta);
            UpdateWeaponSpeedPull(delta);
            UpdateWeaponLanding(delta);
        }
    }
}