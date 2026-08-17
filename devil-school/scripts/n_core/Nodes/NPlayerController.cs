
using Godot;

namespace EGame
{
    public partial class NPlayerController : CharacterBody3D
    {
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////                                      人物移动
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private readonly float _WalkSpeed = 8f;
        private readonly float _MinStopSpeed = 2.54f;
        private readonly float _Friction = 6f;
        private readonly float _AccelerationRate = 10f;
        
        private Vector3 GroundMove(Vector3 source, Vector3 wish_dir, double dt)
        {
            return ApplyAcceleration(source, wish_dir, _AccelerationRate, _WalkSpeed, dt);
        }

        private Vector3 AirMove(Vector3 source, Vector3 wish_dir, double dt)
        {
            return ApplyAcceleration(source, wish_dir, _AccelerationRate * 0.1f, _WalkSpeed, dt);
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
        private Vector2 _RotateSensity = new Vector2(0.1f, 0.1f);

        private readonly Vector2 _PitchLimit = new Vector2(-90f, 90f);
        private float _PitchAngle = 0f;

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
        //////                                       相机Bob（完整移植 DOOM3 Player.cpp::BobCycle()）
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private Node3D _CameraBobNode;

        private readonly float _WalkBobRate = 0.6f;      // 对应 pm_walkbob
        private readonly float _RunBobRate = 0.8f;       // 对应 pm_runbob
        private readonly float _CrouchBobRate = 1.0f;    // 对应 pm_crouchbob
        private readonly float _BobUpAmount = 0.01f;     // 对应 pm_bobup，垂直位置起伏幅度
        private readonly float _BobPitchAmount = 0.004f; // 对应 pm_bobpitch，点头角度幅度
        private readonly float _BobRollAmount = 0.004f;  // 对应 pm_bobroll，左右摇摆角度幅度
        private readonly float _RunPitchAmount = 0.004f; // 对应 pm_runpitch，纯速度驱动的前后倾（非周期性）
        private readonly float _RunRollAmount = 0.01f;   // 对应 pm_runroll，纯速度驱动的左右倾（非周期性）
        private readonly float _MinBobSpeed = 0.3f;      // 对应 MIN_BOB_SPEED：低于这个速度直接清零，不产生 bob

        private float _BobCycle;
        private Vector3 _ViewBobOffset;
        private Vector3 _ViewBobAngles;

        private void UpdateViewBob(double dt)
        {
            var horizontal_vel = new Vector3(Velocity.X, 0f, Velocity.Z);
            float xy_speed = horizontal_vel.Length();

            if (!IsOnFloor() || xy_speed <= _MinBobSpeed)
            {
                // 腾空或几乎静止时直接清零、不是渐隐——DOOM3 原版就是这样处理的，保证玩家站定瞄准时摄像机绝对静止
                _BobCycle = 0f;
                _ViewBobOffset = _ViewBobOffset.Lerp(Vector3.Zero, (float)dt * 10f);
                _ViewBobAngles = _ViewBobAngles.Lerp(Vector3.Zero, (float)dt * 10f);
                ApplyBobToCamera();
                return;
            }

            bool is_crouching = IsCrouch;

            // 按当前状态（蹲/走/跑）选择不同的周期速率——DOOM3 原版蹲/走/跑三档 bob 速率不同，不是同一个数字缩放
            float bob_rate = is_crouching ? _CrouchBobRate : (Input.IsActionPressed(EGInput.RUN) ? _RunBobRate : _WalkBobRate);
            _BobCycle += bob_rate * (float)dt * Mathf.Tau;   // 走完一个完整周期 = 2π，和三角函数的周期对齐

            float bob_frac_sin = Mathf.Abs(Mathf.Sin(_BobCycle));   // 恒非负的折叠正弦波，对应 bobfracsin
            bool second_half = Mathf.Sin(_BobCycle) < 0f;            // 对应 bobFoot 的奇偶——决定这一步是"左脚"还是"右脚"

            float crouch_multiplier = is_crouching ? 3.0f : 1.0f;    // DOOM3 原版：蹲下时点头/摇摆幅度 ×3

            // 位置：垂直起伏，钳制上限
            float vertical = Mathf.Min(bob_frac_sin * xy_speed * _BobUpAmount, 0.08f);

            // 角度：点头分量（恒正）+ 左右摇摆分量（按脚的奇偶翻转符号——这是把恒正的正弦波
            // 变成真正"左右左右"交替摇摆的关键，省略这一步的话画面只会朝一个方向倾斜再回正）
            float speed_for_angles = Mathf.Max(xy_speed, 2.0f);
            float pitch_bob = bob_frac_sin * _BobPitchAmount * speed_for_angles * crouch_multiplier;
            float roll_bob = bob_frac_sin * _BobRollAmount * speed_for_angles * crouch_multiplier;
            if (second_half)
                roll_bob = -roll_bob;

            var local_vel = Velocity;
            float run_pitch = local_vel.Z * _RunPitchAmount;
            float run_roll = -local_vel.X * _RunRollAmount;

            _ViewBobOffset = new Vector3(0f, vertical, 0f);
            _ViewBobAngles = new Vector3(pitch_bob + run_pitch, 0f, roll_bob + run_roll);

            ApplyBobToCamera();
        }

        private void ApplyBobToCamera()
        {
            _CameraBobNode.Position = _ViewBobOffset;
            _CameraBobNode.Rotation = new Vector3(_ViewBobAngles.X, 0f, _ViewBobAngles.Z);
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
            _CameraBobNode = GetNode<Node3D>("%CameraBob");

            _EyesPos = _StandHeight - _EyeOffsetFromTop;
            _PitchNode.Position = new Vector3(0.0f, _EyesPos, 0.0f);
        }

        public override void _Input(InputEvent @event)
        {
            base._Input(@event);
            
            if (@event is InputEventMouseMotion motion)
                HandleCameraRotation(motion.Relative);
        }

        public override void _PhysicsProcess(double delta)
        {
            bool justJumped = false;

            if (IsOnFloor() && Input.IsActionJustPressed(EGInput.JUMP))
            {
                Velocity = ApplyJump(Velocity);
                justJumped = true;
            }

            //如果已经起跳了就不要加摩擦力了，否则会吃掉兔子跳的速度
            if (IsOnFloor() && !justJumped)
            {
                Velocity = ApplyFriction(Velocity, _Friction, delta);
                Velocity = GroundMove(Velocity, _YawNode.Quaternion * GetMoveDir(), delta);
            }
            else
                Velocity = AirMove(Velocity, _YawNode.Quaternion * GetMoveDir(), delta);

            Velocity = ApplyGravity(Velocity, delta);
            UpdateCrouch(delta);
            MoveAndSlide();
            UpdateViewBob(delta);
        }
    }
}