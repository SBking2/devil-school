
using Godot;

namespace EGame
{
    public class MovementIntent
    {
        public Vector3 MoveDir = Vector3.Zero;
        public bool WantsCrouch;
        public bool WantsRun;
        public bool WantsJump;
    }
}
