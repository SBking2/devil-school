
using Godot;

namespace EGame
{
    // 近战命中检测：球心往面朝方向前移、半径减半，避免打到身后的目标；只挑离 origin 最近的一个
    public static class MeleeDetection
    {
        public static Node3D FindTarget(World3D world, Vector3 origin, Vector3 forward, float range, uint mask)
        {
            var space_state = world.DirectSpaceState;
            Vector3 center = origin + forward * (range * 0.5f);

            var shape = new SphereShape3D();
            shape.Radius = range * 0.5f;

            var query = new PhysicsShapeQueryParameters3D();
            query.Shape = shape;
            query.Transform = new Transform3D(Basis.Identity, center);
            query.CollisionMask = mask;

            var results = space_state.IntersectShape(query);

            Node3D target = null;
            float closestDistance = range;

            foreach (var result in results)
            {
                Node3D node3D = (Node3D)result["collider"];
                float distance = origin.DistanceTo(node3D.GlobalPosition);
                if (distance <= closestDistance)
                {
                    closestDistance = distance;
                    target = node3D;
                }
            }

            return target;
        }
    }
}
