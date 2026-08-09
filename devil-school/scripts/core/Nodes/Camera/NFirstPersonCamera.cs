
using Godot;
using System;

namespace EGame
{
    public partial class NFirstPersonCamera : Node3D, INCamera
    {
        private Node3D _FolloTarget;

        private Node3D _PitchPivot;
        private Node3D _YawPivot;
        private Node3D _CameraEffectPos;    //这个用来作Bob、Landing、Recoil等效果

        private Node3D _WeaponViewRoot;
        private Node3D _WeaponEffectPos;     //武器的Bob、Sway等

        public Quaternion HorizontalQuaternion => _PitchPivot.Quaternion;
        public Quaternion VerticalQuaternion => _YawPivot.Quaternion;

        private Camera3D _RealCamera;

        public static NFirstPersonCamera Create(Node3D target)
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
        }
    }
}