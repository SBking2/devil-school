

using Godot;

namespace EGame
{
    public interface INCamera
    {
        public void MakeCurrent();
        public Quaternion HorizontalQuaternion { get; }
        public Quaternion VerticalQuaternion { get; }
    }
}