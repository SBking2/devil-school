
using Godot;

namespace EGame
{
    public partial class NCameraController : Node
    {
        private INCamera _Camera;
        public INCamera CurrentCamera => _Camera;

        public void SetCamera(INCamera camera)
        {
            if(_Camera != null)
                (_Camera as Node).QueueFree();
            
            AddChild(camera as Node);
            _Camera = camera;
            _Camera.MakeCurrent();
        }
    }
}