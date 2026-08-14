
using Godot;

namespace EGame
{
    public partial class NEnviromentInput : Node
    {
        public static NEnviromentInput Create(NEnvCreature controled)
        {
            var instance = new NEnviromentInput();
            instance._Creature = controled;
            return instance;
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////
        ///////                                 控制Player部分
        /////////////////////////////////////////////////////////////////////////////////////////////////////////

        private NEnvCreature _Creature;

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

            dir = NRun.Instance.CameraController.CurrentCamera.HorizontalQuaternion * dir;
            dir = dir.Normalized();
            return dir;
        }

        public override void _Process(double delta)
        {
            base._Process(delta);

            if (_Creature != null)
            {
                _Creature.Intent.MoveDir = GetMoveDir();
                _Creature.Intent.WantsCrouch = Input.IsActionPressed(EGInput.CROUCH);
                _Creature.Intent.WantsRun = Input.IsActionPressed(EGInput.RUN);
                _Creature.Intent.WantsJump = Input.IsActionPressed(EGInput.JUMP);
            }
        }
    }
}