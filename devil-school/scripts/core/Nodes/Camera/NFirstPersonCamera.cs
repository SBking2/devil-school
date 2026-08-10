
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

        private Node3D _WeaponViewRoot;
        private Node3D _WeaponEffectPos;     //武器的Bob、Sway等

        public Quaternion HorizontalQuaternion => _PitchPivot.Quaternion;
        public Quaternion VerticalQuaternion => _YawPivot.Quaternion;

        private Camera3D _RealCamera;

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

            _WeaponViewRoot = new Node3D();
            _CameraEffectPos.AddChild(_WeaponViewRoot);
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////
        ////////                           Camera的上下左右旋转
        /////////////////////////////////////////////////////////////////////////////////////////////////////
        private float _VerticalLimitMax { get; } = 89f;
        private float _VerticalLimitMin { get; } = -89f;

        private float _CurrentVerticalAngle;

        public float VerticalSence { get; set; } = 1f;
        public float HorizontalSence { get; set; } = 1f;

        private float YOffset = 0.3f;

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
                RotateY(Mathf.DegToRad(pitch_delta));
            }
        }
        public override void _Process(double delta)
        {
            base._Process(delta);
            this.GlobalPosition = _FolloTarget.GlobalPosition + new Vector3(0.0f, YOffset, 0.0f);
            _FolloTarget.Quaternion = _PitchPivot.Quaternion;
            ProcessCameraBob((float)delta);
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////
        ////////                           CameraBob
        /////////////////////////////////////////////////////////////////////////////////////////////////////
        private bool CameraBobEnabled { get; } = true;
        private float CameraBobFrequency { get; } = 8.0f;
        private float CameraBobVerticalAmplitude { get; } = 0.03f;
        private float CameraBobHorizontalAmplitude { get; } = 0.012f;
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
    }
}
