
using Godot;

namespace EGame
{
    public partial class NEnviromentController : Node
    {
        public static NEnviromentController Create(NEnvCreature controled)
        {
            var instance = new NEnviromentController();
            instance._Creature = controled;
            return instance;
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////
        ///////                                 控制Player部分
        /////////////////////////////////////////////////////////////////////////////////////////////////////////

        private NEnvCreature _Creature;

        private Vector2 GetMoveDir()
        {
            var dir = Vector2.Zero;

            if (Input.IsActionPressed(EGInput.UP))
                dir.Y -= 1f;

            if (Input.IsActionPressed(EGInput.DOWN))
                dir.Y += 1f;
            
            if (Input.IsActionPressed(EGInput.RIGHT))
                dir.X += 1f;

            if (Input.IsActionPressed(EGInput.LEFT))
                dir.X -= 1f;

            return dir;
        }

        public override void _Process(double delta)
        {
            base._Process(delta);

            if (_Creature != null)
            {
                var dir = GetMoveDir();
                _Creature.SetMoveDir(dir);
            }
        }
    }
}