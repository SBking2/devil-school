
using Godot;

namespace EGame
{
    public partial class NPlayerController : CharacterBody3D
    {
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////                                      人物移动
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private readonly float _WalkSpeed = 3.0f;
        private readonly float _MinStopSpeed = 1.0f;
        private readonly float _Friction = 1.0f;

        private Vector3 GroundMove(Vector3 source, Vector3 wish_dir, double dt)
        {
            return ApplyAcceleration(source, wish_dir, 0.3f, _WalkSpeed, dt);
        }

        private Vector3 AirMove(Vector3 source, Vector3 wish_dir, double dt)
        {
            return ApplyAcceleration(source, wish_dir, 0.03f, _WalkSpeed, dt);
        }

        private Vector3 ApplyAcceleration(Vector3 source, Vector3 wish_dir, float acceleration_rate, float move_speed, double dt)
        {
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

        private readonly float _JumpSpeed = 5.0f;

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

        private readonly float _StandHeight = 2.0f;
        private readonly float _CrouchHeight = 1.2f;
        private readonly float _CrouchChangeSpeed = 3.0f;

        private float _EyesPos = 0.0f;

        private readonly CollisionShape3D _MoveCollisionShape;
        
        private bool _IsWantCrouch
        {
            get
            {
                return false;
            }
        }

        private void UpdateCrouch(double dt)
        {
            float target_crouch_height = _IsWantCrouch ? _CrouchHeight : _StandHeight;

            var capsule = (CapsuleShape3D)_MoveCollisionShape.Shape;
            capsule.Height = target_crouch_height;
            _MoveCollisionShape.Position = new Vector3(0.0f, target_crouch_height * 0.5f, 0.0f);

            float target_eyes_offset = target_crouch_height - 0.15f;
            _EyesPos = (_EyesPos * _CrouchChangeSpeed + (1 - _CrouchChangeSpeed) * target_crouch_height) * (float)dt;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////                                      辅助函数
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private Vector3 GetMoveDir()
        {
            var dir = Vector3.Zero;

            if (Input.IsActionPressed(EGInput.UP))
                dir.Z -= 1f;

            if (Input.IsActionPressed(EGInput.DOWN))
                dir.Z += 1f;

            if (Input.IsActionPressed(EGInput.RIGHT))
                dir.X += 1f;

            if (Input.IsActionPressed(EGInput.LEFT))
                dir.X -= 1f;

            dir = dir.Normalized();
            return dir;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////                                      相机上下左右旋转相关
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private readonly Node3D _YawNode;
        private readonly Node3D _PitchNode;
        private readonly Camera3D _RealCamera;

        //x为yaw, y为pitch
        private Vector2 _RotateSensity = new Vector2(0.03f, 0.03f);

        private readonly Vector2 _PitchLimit = new Vector2(-90f, 90f);
        private float _PitchAngle = 0f;

        private void HandleCameraRotation(Vector2 mouse_delta)
        {
            float x_delta = mouse_delta.X * _RotateSensity.X;
            _YawNode.Rotate(Vector3.Up, Mathf.DegToRad(x_delta));

            float y_delta = mouse_delta.Y * _RotateSensity.Y;
            _PitchAngle += y_delta;
            _PitchAngle = Mathf.Clamp(_PitchAngle, _PitchLimit.X, _PitchLimit.Y);
            _PitchNode.Quaternion = Quaternion.FromEuler(new Vector3(_PitchAngle, 0.0f, 0.0f));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////

        public override void _Ready()
        {
            base._Ready();
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
                Velocity = GroundMove(Velocity, GetMoveDir(), delta);
            }
            else
                Velocity = AirMove(Velocity, GetMoveDir(), delta);

            Velocity = ApplyGravity(Velocity, delta);
            UpdateCrouch(delta);
            MoveAndSlide();
        }
    }
}