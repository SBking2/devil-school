
using Godot;

namespace EGame
{
    public partial class NCameraController : Node3D
    {
        private Camera3D _Camera;
        public Camera3D CurrentCamera => _Camera;

        public void SetCamera(Camera3D camera)
        {
            if(_Camera != null)
                _Camera.QueueFree();
            
            AddChild(camera);
            _Camera = camera;
        }
    }
}