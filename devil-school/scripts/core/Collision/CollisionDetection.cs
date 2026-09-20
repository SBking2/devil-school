
using Godot;
using System.Collections.Generic;

namespace EGame
{
    public struct RayHitResult
    {
        public readonly Node3D HitObject;
        public readonly Vector3 HitPoint;
        public readonly Vector3 HitNormal;

        public RayHitResult(Node3D hitObject, Vector3 hitPoint, Vector3 hitNormal)
        {
            HitObject = hitObject;
            HitPoint = hitPoint;
            HitNormal = hitNormal;
        }
    }

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

        // 霰弹枪命中检测：以 forward 为中心，在圆锥范围内打 pellet_count 条射线，只返回真正打中的
        public static List<RayHitResult> FindShotgunTargets(World3D world, Vector3 origin, Vector3 forward, Vector3 right, Vector3 up, float range, uint mask, int pellet_count, float spread_degrees)
        {
            var results = new List<RayHitResult>();

            for (int i = 0; i < pellet_count; i++)
            {
                Vector3 dir = GetPelletDirection(forward, right, up, spread_degrees);
                Vector3 to = origin + dir * range;

                if (FindRayTarget(world, origin, to, mask, out Node3D hitObject, out Vector3 hitPoint, out Vector3 hitNormal))
                    results.Add(new RayHitResult(hitObject, hitPoint, hitNormal));
            }

            return results;
        }

        // 圆锥内均匀采样一个偏移方向：极坐标采样（sqrt(random)*半径 + 随机角度），
        // 不能直接对 yaw/pitch 各自独立取随机值——那样散布会偏向四个角，不是正圆
        private static Vector3 GetPelletDirection(Vector3 forward, Vector3 right, Vector3 up, float max_spread_degrees)
        {
            float radius = max_spread_degrees * Mathf.Sqrt((float)GD.RandRange(0.0, 1.0));
            float angle = (float)GD.RandRange(0.0, Mathf.Tau);

            float pitch_offset = radius * Mathf.Cos(angle);
            float yaw_offset = radius * Mathf.Sin(angle);

            Vector3 dir = forward.Rotated(up, Mathf.DegToRad(yaw_offset));
            dir = dir.Rotated(right, Mathf.DegToRad(pitch_offset));
            return dir;
        }
    }
}
