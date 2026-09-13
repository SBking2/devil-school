
using Godot;

namespace EGame
{
    // 近战命中检测：球心往面朝方向前移、半径减半，避免打到身后的目标；只挑离 origin 最近的一个
    public static class CollisionDetection
    {
        public static Node3D FindMeleeTarget(World3D world, Vector3 origin, Vector3 forward, float range, uint mask)
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

        // 远程命中检测：从 from 往 to 打一条射线，打不中返回 false
        public static bool FindRayTarget(World3D world, Vector3 from, Vector3 to, uint mask, out Node3D hitObject, out Vector3 hitPoint, out Vector3 hitNormal)
        {
            var space_state = world.DirectSpaceState;
            var query = PhysicsRayQueryParameters3D.Create(from, to);
            query.CollisionMask = mask;

            var result = space_state.IntersectRay(query);
            if (result.Count == 0)
            {
                hitObject = null;
                hitPoint = Vector3.Zero;
                hitNormal = Vector3.Zero;
                return false;
            }

            hitObject = (Node3D)result["collider"];
            hitPoint = (Vector3)result["position"];
            hitNormal = (Vector3)result["normal"];
            return true;
        }
    }
}
