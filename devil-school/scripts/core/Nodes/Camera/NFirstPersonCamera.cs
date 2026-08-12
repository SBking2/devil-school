
using Godot;
using System;

namespace EGame
{
    public partial class NFirstPersonCamera : Node3D, INCamera
    {
        private NEnvCreature _FolloTarget;

        private Node3D _PitchPivot;
        private Node3D _YawPivot;
        private Node3D _CameraEffectPos;    //这个用来作Bob、Landing、Recoil等效果

        private Node3D _WeaponBobPos;     //武器的Bob、Sway等
        private Node3D _WeaponSwayPos;
        private Node3D _WeaponViewRoot;

        public Quaternion HorizontalQuaternion => _PitchPivot.Quaternion;
        public Quaternion VerticalQuaternion => _YawPivot.Quaternion;

        private Camera3D _RealCamera;

        public float FOV { get; private set; } = 70f;

        public static NFirstPersonCamera Create(NEnvCreature target)
        {
            NFirstPersonCamera camera = new NFirstPersonCamera();
            camera._FolloTarget = target;
            return camera;
        }

        public void MakeCurrent()
        {
            if (_RealCamera == null)
                throw new InvalidOperationException("FirstPersonCamera is null!");
            _RealCamera.MakeCurrent();
            Input.MouseMode = Input.MouseModeEnum.Captured;
        }

        /// <summary>
        /// 构建相机节点树
        /// </summary>
        public override void _Ready()
        {
            base._Ready();
            _PitchPivot = this;

            _YawPivot = new Node3D();
            _PitchPivot.AddChild(_YawPivot);

            _CameraEffectPos = new Node3D();
            _YawPivot.AddChild(_CameraEffectPos);

            _RealCamera = new Camera3D();
            _CameraEffectPos.AddChild(_RealCamera);

            _WeaponBobPos = new Node3D();
            _CameraEffectPos.AddChild(_WeaponBobPos);

            _WeaponSwayPos = new Node3D();
            _WeaponBobPos.AddChild(_WeaponSwayPos);

            _WeaponViewRoot = new Node3D();
            _WeaponSwayPos.AddChild(_WeaponViewRoot);

            if(_FolloTarget != null)
            {
                (_FolloTarget as NEnvCreature).SetVisualParent(_WeaponViewRoot);
            }

            _RealCamera.Fov = this.FOV;
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////
        ////////                           Camera的上下左右旋转
        /////////////////////////////////////////////////////////////////////////////////////////////////////
        private float _VerticalLimitMax { get; } = 89f;
        private float _VerticalLimitMin { get; } = -89f;

        private float _CurrentVerticalAngle;

        public float VerticalSence { get; set; } = 0.04f;
        public float HorizontalSence { get; set; } = 0.04f;
        public Vector3 CameraPosOffset { get; } = new Vector3(0.0f, 0.15f, 0.0f);

        public override void _Input(InputEvent @event)
        {
            base._Input(@event);

            if (@event is InputEventMouseMotion motion)
            {

                var delta = motion.Relative;

                //竖直方向旋转
                float yaw_delta = -delta.Y * VerticalSence;
                _CurrentVerticalAngle += yaw_delta;
                _CurrentVerticalAngle = Mathf.Clamp(_CurrentVerticalAngle, _VerticalLimitMin, _VerticalLimitMax);

                var euler = new Vector3(
                    Mathf.DegToRad(_CurrentVerticalAngle)
                    , 0f
                    , 0f
                    );

                _YawPivot.Quaternion = Quaternion.FromEuler(euler);

                //水平方向旋转
                float pitch_delta = -delta.X * HorizontalSence;
                _WeaponSwayHorizontalViewDelta += pitch_delta;
                RotateY(Mathf.DegToRad(pitch_delta));
            }
        }
        public override void _Process(double delta)
        {
            base._Process(delta);
            this.GlobalPosition = _FolloTarget.GlobalPosition + CameraPosOffset;
            _FolloTarget.Quaternion = _PitchPivot.Quaternion;
            ProcessCameraBob((float)delta);
            ProcessWeaponBob((float)delta);
            ProcessWeaponSway((float)delta);
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////
        ////////                           CameraBob
        /////////////////////////////////////////////////////////////////////////////////////////////////////
        private bool CameraBobEnabled { get; } = true;
        private float CameraBobFrequency { get; } = 2.0f;
        private float CameraBobVerticalAmplitude { get; } = 0.05f;
        private float CameraBobHorizontalAmplitude { get; } = 0.03f;
        private float CameraBobReferenceSpeed { get; } = 4.0f;
        private float CameraBobReturnSpeed { get; } = 10.0f;

        private float _CameraBobTime;
        private Vector3 _CameraBobOffset = Vector3.Zero;

        private void ProcessCameraBob(float delta)
        {
            if (_CameraEffectPos == null)
                return;

            if (CameraBobEnabled == false)
            {
                _CameraBobTime = 0.0f;
                _CameraBobOffset = _CameraBobOffset.Lerp(Vector3.Zero, GetLerpWeight(CameraBobReturnSpeed, delta));
                _CameraEffectPos.Position = _CameraBobOffset;
                return;
            }
            
            float speed = GetTargetHorizontalSpeed();
            float weight = Mathf.Clamp(speed / CameraBobReferenceSpeed, 0.0f, 1.0f);

            if (weight <= 0.01f)
            {
                _CameraBobTime = 0.0f;
                _CameraBobOffset = _CameraBobOffset.Lerp(Vector3.Zero, GetLerpWeight(CameraBobReturnSpeed, delta));
                _CameraEffectPos.Position = _CameraBobOffset;
                return;
            }

            _CameraBobTime += delta * CameraBobFrequency * weight;

            float vertical = Mathf.Sin(_CameraBobTime * Mathf.Tau) * CameraBobVerticalAmplitude * weight;
            float horizontal = Mathf.Cos(_CameraBobTime * Mathf.Tau * 0.5f) * CameraBobHorizontalAmplitude * weight;

            var target_offset = new Vector3(horizontal, vertical, 0.0f);
            _CameraBobOffset = _CameraBobOffset.Lerp(target_offset, GetLerpWeight(CameraBobReturnSpeed, delta));
            _CameraEffectPos.Position = _CameraBobOffset;

            _CameraEffectPos.LookAt(GetLookAtPos());
        }

        /// <summary>
        /// CameraBob在上下晃动的时候需要调节相机角度，让玩家焦点始终不变
        /// </summary>
        private Vector3 GetLookAtPos()
        {
            var forward_dir = -_YawPivot.GlobalTransform.Basis.Z.Normalized();
            return _YawPivot.GlobalPosition + forward_dir * 15f;
        }

        private float GetLerpWeight(float speed, float delta)
        {
            return 1.0f - Mathf.Exp(-speed * delta);
        }

        private float GetTargetHorizontalSpeed()
        {
            if (_FolloTarget is CharacterBody3D body)
            {
                var velocity = body.Velocity;
                velocity.Y = 0.0f;
                return velocity.Length();
            }

            return 0.0f;
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////
        ////////                           WeaponBob(Position)
        /////////////////////////////////////////////////////////////////////////////////////////////////////
        private bool WeaponBobEnabled { get; } = true;
        private float WeaponBobFrequency { get; } = 2.0f;
        private float WeaponBobVerticalAmplitude { get; } = 0.05f;
        private float WeaponBobHorizontalAmplitude { get; } = 0.03f;
        private float WeaponBobReferenceSpeed { get; } = 4.0f;
        private float WeaponBobReturnSpeed { get; } = 10.0f;

        private float _WeaponBobTime;

        private Vector3 _WeaponBobOffset = Vector3.Zero;
        private void ProcessWeaponBob(float delta)
        {
            if (_WeaponBobPos == null)
                return;

            if (WeaponBobEnabled == false)
            {
                _WeaponBobTime = 0.0f;
                _WeaponBobOffset = _WeaponBobOffset.Lerp(Vector3.Zero, GetLerpWeight(WeaponBobReturnSpeed, delta));
                _WeaponBobPos.Position = _WeaponBobOffset;
                return;
            }

            float speed = GetTargetHorizontalSpeed();
            float weight = Mathf.Clamp(speed / WeaponBobReferenceSpeed, 0.0f, 1.0f);

            if (weight <= 0.01f)
            {
                _WeaponBobTime = 0.0f;
                _WeaponBobOffset = _WeaponBobOffset.Lerp(Vector3.Zero, GetLerpWeight(WeaponBobReturnSpeed, delta));
                _WeaponBobPos.Position = _WeaponBobOffset;
                return;
            }

            _WeaponBobTime += delta * WeaponBobFrequency * weight;

            float vertical = Mathf.Sin(_WeaponBobTime * Mathf.Tau) * WeaponBobVerticalAmplitude * weight;
            float horizontal = Mathf.Cos(_WeaponBobTime * Mathf.Tau * 0.5f) * WeaponBobHorizontalAmplitude * weight;

            var target_offset = new Vector3(horizontal, vertical, 0.0f);
            _WeaponBobOffset = _WeaponBobOffset.Lerp(target_offset, GetLerpWeight(WeaponBobReturnSpeed, delta));
            _WeaponBobPos.Position = _WeaponBobOffset;
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////
        ////////                           WeaponSway(Rotation)
        /////////////////////////////////////////////////////////////////////////////////////////////////////

        private bool WeaponSwayEnabled { get; } = true;
        private float WeaponSwayZAnglePerViewAngle { get; } = 1.0f;
        private float WeaponSwayMouseMaxZAngle { get; } = 6.0f;
        private float WeaponSwayMoveZAnglePerSpeed { get; } = -1.5f;
        private float WeaponSwayMoveMaxZAngle { get; } = 4.0f;
        private float WeaponSwayFollowSpeed { get; } = 18.0f;
        private float WeaponSwayReturnSpeed { get; } = 10.0f;

        private float _WeaponSwayHorizontalViewDelta;
        private float _WeaponSwayMouseZAngle;
        private float _WeaponSwayMoveZAngle;

        private void ProcessWeaponSway(float delta)
        {
            if (_WeaponSwayPos == null)
                return;

            if (WeaponSwayEnabled == false)
            {
                _WeaponSwayHorizontalViewDelta = 0.0f;
                _WeaponSwayMouseZAngle = Mathf.Lerp(_WeaponSwayMouseZAngle, 0.0f, GetLerpWeight(WeaponSwayReturnSpeed, delta));
                _WeaponSwayMoveZAngle = Mathf.Lerp(_WeaponSwayMoveZAngle, 0.0f, GetLerpWeight(WeaponSwayReturnSpeed, delta));
                ApplyWeaponSwayRotation();
                return;
            }

            float target_mouse_z_angle = Mathf.Clamp(
                _WeaponSwayHorizontalViewDelta * WeaponSwayZAnglePerViewAngle,
                -WeaponSwayMouseMaxZAngle,
                WeaponSwayMouseMaxZAngle
            );

            _WeaponSwayHorizontalViewDelta = 0.0f;

            float target_move_z_angle = Mathf.Clamp(
                GetTargetLocalVelocityX() * WeaponSwayMoveZAnglePerSpeed,
                -WeaponSwayMoveMaxZAngle,
                WeaponSwayMoveMaxZAngle
            );

            float mouse_lerp_speed = Mathf.Abs(target_mouse_z_angle) > 0.01f ? WeaponSwayFollowSpeed : WeaponSwayReturnSpeed;
            float move_lerp_speed = Mathf.Abs(target_move_z_angle) > 0.01f ? WeaponSwayFollowSpeed : WeaponSwayReturnSpeed;

            _WeaponSwayMouseZAngle = Mathf.Lerp(_WeaponSwayMouseZAngle, target_mouse_z_angle, GetLerpWeight(mouse_lerp_speed, delta));
            _WeaponSwayMoveZAngle = Mathf.Lerp(_WeaponSwayMoveZAngle, target_move_z_angle, GetLerpWeight(move_lerp_speed, delta));

            ApplyWeaponSwayRotation();
        }

        private void ApplyWeaponSwayRotation()
        {
            float z_angle = _WeaponSwayMouseZAngle + _WeaponSwayMoveZAngle;
            _WeaponSwayPos.Rotation = new Vector3(0.0f, 0.0f, Mathf.DegToRad(z_angle));
        }

        private float GetTargetLocalVelocityX()
        {
            if (_FolloTarget is CharacterBody3D body)
            {
                var velocity = body.Velocity;
                velocity.Y = 0.0f;
                return velocity.Dot(_PitchPivot.GlobalTransform.Basis.X.Normalized());
            }

            return 0.0f;
        }
    }
}
