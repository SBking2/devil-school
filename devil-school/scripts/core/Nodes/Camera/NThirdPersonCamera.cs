
using Godot;

namespace EGame
{
    public partial class NThirdPersonCamera : Node3D, INCamera
    {
        public Quaternion HorizontalQuaternion => this.Quaternion;
        public Quaternion VerticalQuaternion => _YawPoint.Quaternion;

        public float VerticalSence { get; set; } = 1f;
        public float HorizontalSence { get; set; } = 1f;

        private float CameraFollowLerpSpeed { get; } = 10f;

        //Zoom
        private float ArmLength { get; set; } = 5f;
        private float ArmMinLength { get; } = 2f;
        private float ArmMaxLength { get; } = 10f;
        private float ArmZoomSpeed { get; } = 0.1f;

        //俯仰角角度限制
        private float _VerticalLimitMax { get; } = 80f;
        private float _VerticalLimitMin { get; } = -80f;

        private float _CurrentVerticalAngle;

        private Node3D _Target;

        private Camera3D _RealCamera;

        private Node3D _YawPoint;

        public static NThirdPersonCamera Create(Node3D target)
        {
            NThirdPersonCamera camera = new NThirdPersonCamera();
            camera._Target = target;
            return camera;
        }

        public override void _Ready()
        {
            base._Ready();
            _YawPoint = new Node3D();
            AddChild(_YawPoint);

            _RealCamera = new Camera3D();
            _YawPoint.AddChild(_RealCamera);
            _RealCamera.Position = new Vector3(0.0f, 0.0f, ArmLength);
        }

        public override void _Input(InputEvent @event)
        {
            base._Input(@event);

            if(@event is InputEventMouseMotion motion)
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

                _YawPoint.Quaternion = Quaternion.FromEuler(euler);

                //水平方向旋转
                float pitch_delta = -delta.X * HorizontalSence;
                RotateY(Mathf.DegToRad(pitch_delta));
            }

            if(@event is InputEventMouseButton button_event)
            {
                float delta = 0;
                if (button_event.ButtonIndex == MouseButton.WheelUp)
                    delta = -1f;
                if (button_event.ButtonIndex == MouseButton.WheelDown)
                    delta = +1f;

                this.ArmLength += delta * this.ArmZoomSpeed;
                this.ArmLength = Mathf.Clamp(this.ArmLength, ArmMinLength, ArmMaxLength);
                UpdateArmLength(ArmLength);
            }
        }

        public override void _PhysicsProcess(double delta)
        {
            base._PhysicsProcess(delta);
            ProcessMove(delta);
        }

        public void MakeCurrent()
        {
            if (_RealCamera != null)
                _RealCamera.MakeCurrent();
        }

        private void UpdateArmLength(float length)
        {
            this.ArmLength = length;
            this._RealCamera.Position = new Vector3(0f, 0, this.ArmLength);
        }

        private void ProcessMove(double delta)
        {
            if (_Target != null)
            {
                GlobalPosition = GlobalPosition.Lerp(_Target.GlobalPosition, (float)delta * CameraFollowLerpSpeed);
            }
        }
    }
}