
using Godot;

namespace EGame
{
    public partial class NSphereSensor : NVisualSensor
    {
        protected override CollisionShape3D CreateShpae()
        {
            float radius = EnvCreatureOwner.Data.MonsterModel.VisualLength;
            var sphere_shape = new CollisionShape3D();
            sphere_shape.Shape = new SphereShape3D { Radius = radius };

            return sphere_shape;
        }
    }
}