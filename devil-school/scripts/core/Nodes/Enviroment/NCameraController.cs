
using Godot;

namespace EGame
{
    public partial class NCameraController : Node2D
    {
        private Camera2D _Camera;
        public Camera2D CurrentCamera => _Camera;

        public void SetCamera(Camera2D camera)
        {
            if(_Camera != null)
                _Camera.QueueFree();
            
            AddChild(camera);
            _Camera = camera;
        }
    }
}