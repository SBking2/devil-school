
using Godot;

namespace EGame
{
    public interface INSensor
    {
        NEnvCreature Owner { get; }
        void Bind(Node3D parent);
    }
}
