
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

        // a、b 是胶囊两个半球的球心
        public static void Capsule(Vector3 a, Vector3 b, float radius)
        {
            if (!Settings.DrawCollisionQueries)
                return;

            NCollisionDebugDraw.GetInstance().AddCapsule(a, b, radius, Duration);
        }

        // 四个点围成的半透明四边形，a-b-c-d 要按顺序围成一圈
        public static void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            if (!Settings.DrawCollisionQueries)
                return;

            NCollisionDebugDraw.GetInstance().AddQuad(a, b, c, d, Duration);
        }

        // 一条普通的线，比如扫掠检测每一帧走过的路径
        public static void Line(Vector3 from, Vector3 to)
        {
            if (!Settings.DrawCollisionQueries)
                return;

            NCollisionDebugDraw.GetInstance().AddLine(from, to, Duration);
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
