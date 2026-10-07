
using Godot;

namespace EGame
{
    // 碰撞查询可视化的入口，统一画成黄色，开关是 Settings.DrawCollisionQueries
    public static class CollisionDebugDraw
    {
        private const double Duration = 1.0;
        private const float HitCrossSize = 0.15f;
        private const float HitMarkerRadius = 0.08f;

        public static void Sphere(Vector3 center, float radius)
        {
            if (!Settings.DrawCollisionQueries)
                return;

            NCollisionDebugDraw.GetInstance().AddSphere(center, radius, Duration);
        }

        // 命中的话射线只画到命中点，并在命中点标一个十字和小球；没命中画整条
        public static void Ray(Vector3 from, Vector3 to, bool hit, Vector3 hit_point)
        {
            if (!Settings.DrawCollisionQueries)
                return;

            NCollisionDebugDraw draw = NCollisionDebugDraw.GetInstance();
            if (hit)
            {
                draw.AddLine(from, hit_point, Duration);
                draw.AddCross(hit_point, HitCrossSize, Duration);
                draw.AddSphere(hit_point, HitMarkerRadius, Duration);
            }
            else
            {
                draw.AddLine(from, to, Duration);
            }
        }
    }
}
