
using Godot;
using System;

namespace EGame
{
    public partial class NPlayer : CharacterBody3D, INCharacter
    {
        private const string _PrefabPath = "player/player";
        public static NPlayer Create(Player player)
        {
            var instance = SceneHelper.LoadScene<NPlayer>(_PrefabPath);
            instance.PlayerData = player;
            instance.Data.CharacterModel.OnCharacterCreated(instance);
            instance.Data.CharacterModel.OnPlayerCreated(instance);
            return instance;
        }

        public Player PlayerData { get; private set; }
        
        public Creature Data
        {
            get
            {
                return PlayerData.CreatureData;
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////                                      人物移动
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private float WalkSpeed => PlayerData.PlayerModel.MoveSpeed;
        private readonly float _MinStopSpeed = 2.54f;
        private readonly float _Friction = 6f;
        private readonly float _AccelerationRate = 10f;
        
        private Vector3 GroundMove(Vector3 source, Vector3 wish_dir, double dt)
        {
            return ApplyAcceleration(source, wish_dir, _AccelerationRate, WalkSpeed, dt);
        }

        private Vector3 AirMove(Vector3 source, Vector3 wish_dir, double dt)
        {
            return ApplyAcceleration(source, wish_dir, _AccelerationRate * 0.1f, WalkSpeed, dt);
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

        private readonly float _UpGravity = -9.8f;
        private readonly float _DownGravity = -15.0f;

        private readonly float _JumpSpeed = 4.6f;

        private Vector3 ApplyGravity(Vector3 source, double dt)
        {
            if (IsOnFloor())
            {
                if (source.Y < 0f)
                    source.Y = -0.5f;
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
        
        private bool IsCrouch
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
            capsule.Height = target_crouch_height;
            _MoveCollisionShape.Position = new Vector3(0.0f, target_crouch_height * 0.5f, 0.0f);

            float target_eyes_offset = target_crouch_height - _EyeOffsetFromTop;
            float weight = 1f - Mathf.Exp(-_CrouchChangeSpeed * (float)dt);
            _EyesPos = Mathf.Lerp(_EyesPos, target_eyes_offset, weight);

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

        private readonly float _WalkBobRate = 0.8f;
        private readonly float _RunBobRate = 0.8f;
        private readonly float _CrouchBobRate = 1.0f;
        private readonly float _MinBobSpeed = 0.3f;      //低于这个速度直接清零，不产生 bob

        private readonly float _CameraBobRightScale = 0.01f;   //Bob水平幅度
        private readonly float _CameraBobUpScale = 0.0035f;      //Bob垂直幅度
        private readonly float _CameraLookAheadDistance = 15f;

        private float _BobCycle;
        private Vector3 _ViewBobPosition;

        private float _XySpeed;

        private void UpdateViewBob(double dt)
        {
            var horizontal_vel = new Vector3(Velocity.X, 0f, Velocity.Z);
            _XySpeed = horizontal_vel.Length();

            if (!IsOnFloor() || _XySpeed <= _MinBobSpeed)
            {
                _BobCycle = 0f;
                _ViewBobPosition = _ViewBobPosition.Lerp(Vector3.Zero, (float)dt * 10f);
                ApplyBobToCamera();
                return;
            }

            bool is_crouching = IsCrouch;

            float bob_rate = is_crouching ? _CrouchBobRate : (Input.IsActionPressed(EGInput.RUN) ? _RunBobRate : _WalkBobRate);
            _BobCycle += bob_rate * (float)dt * Mathf.Tau;

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

        private readonly bool _WeaponBobEnabled = true;

        private readonly float _WeaponBobRightScale = -0.002f;
        private readonly float _WeaponBobUpScale = 0.001f;

        private void UpdateWeaponBob(double dt)
        {
            if (_WeaponBobNode == null)
                return;

            _WeaponBobNode.Position = _WeaponBobEnabled
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

        private readonly bool _WeaponSwayEnabled = true;   // 转头滞后，跟脚步相位无关

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

            if (!_WeaponSwayEnabled)
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
        }
        
        private bool _JustJumped;

        public override void _PhysicsProcess(double delta)
        {
            _JustJumped = false;

            if (IsOnFloor() && Input.IsActionJustPressed(EGInput.JUMP))
            {
                Velocity = ApplyJump(Velocity);
                _JustJumped = true;
            }

            //如果已经起跳了就不要加摩擦力了，否则会吃掉兔子跳的速度
            if (IsOnFloor() && !_JustJumped)
            {
                Velocity = ApplyFriction(Velocity, _Friction, delta);
                Velocity = GroundMove(Velocity, _YawNode.Quaternion * GetMoveDir(), delta);
            }
            else
                Velocity = AirMove(Velocity, _YawNode.Quaternion * GetMoveDir(), delta);

            Velocity = ApplyGravity(Velocity, delta);
            UpdateCrouch(delta);
            MoveAndSlide();
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