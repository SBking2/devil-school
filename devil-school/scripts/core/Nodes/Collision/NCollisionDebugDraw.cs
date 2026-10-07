
using Godot;
using System.Collections.Generic;

namespace EGame
{
    // 瞬时碰撞查询没有场景节点，Godot 自带的碰撞体可视化看不到，所以把查询形状画成黄色线框+半透明表面、停留一会儿再消失。外部走 CollisionDebugDraw
    public partial class NCollisionDebugDraw : Node3D
    {
        public static NCollisionDebugDraw Instance { get; private set; }

        private const int _CircleSegments = 24;
        private const int _SphereRings = 12;
        private const int _SphereSegments = 24;

        private static readonly Color _LineColor = new Color(1f, 1f, 0f, 1f);
        private static readonly Color _FillColor = new Color(1f, 1f, 0f, 0.25f);

        private readonly struct DebugLine
        {
            public readonly Vector3 From;
            public readonly Vector3 To;
            public readonly ulong ExpireMsec;

            public DebugLine(Vector3 from, Vector3 to, ulong expireMsec)
            {
                From = from;
                To = to;
                ExpireMsec = expireMsec;
            }
        }

        private readonly struct DebugSphere
        {
            public readonly Vector3 Center;
            public readonly float Radius;
            public readonly ulong ExpireMsec;

            public DebugSphere(Vector3 center, float radius, ulong expireMsec)
            {
                Center = center;
                Radius = radius;
                ExpireMsec = expireMsec;
            }
        }

        private readonly List<DebugLine> _Lines = new List<DebugLine>();
        private readonly List<DebugSphere> _Spheres = new List<DebugSphere>();
        private ImmediateMesh _Mesh;
        private StandardMaterial3D _LineMaterial;
        private StandardMaterial3D _FillMaterial;
        private bool _Dirty;

        public static NCollisionDebugDraw GetInstance()
        {
            if (!IsInstanceValid(Instance))
            {
                NCollisionDebugDraw node = new NCollisionDebugDraw();
                node.Name = "CollisionDebugDraw";
                ((SceneTree)Engine.GetMainLoop()).Root.AddChild(node);
            }
            return Instance;
        }

        public override void _EnterTree()
        {
            base._EnterTree();
            Instance = this;
        }

        public override void _ExitTree()
        {
            base._ExitTree();
            if (Instance == this)
                Instance = null;
        }

        public override void _Ready()
        {
            base._Ready();

            _LineMaterial = new StandardMaterial3D();
            _LineMaterial.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
            _LineMaterial.VertexColorUseAsAlbedo = true;
            _LineMaterial.NoDepthTest = true;    // 隔着墙也能看到

            _FillMaterial = new StandardMaterial3D();
            _FillMaterial.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
            _FillMaterial.VertexColorUseAsAlbedo = true;
            _FillMaterial.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
            _FillMaterial.CullMode = BaseMaterial3D.CullModeEnum.Disabled;    // 球面前后两层都画，从里面看也有表面
            _FillMaterial.NoDepthTest = true;

            _Mesh = new ImmediateMesh();

            MeshInstance3D mesh_instance = new MeshInstance3D();
            mesh_instance.Mesh = _Mesh;
            mesh_instance.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
            mesh_instance.TopLevel = true;    // 顶点用的是世界坐标
            AddChild(mesh_instance);
        }

        public override void _Process(double delta)
        {
            base._Process(delta);

            ulong now = Time.GetTicksMsec();
            int removed = _Lines.RemoveAll(line => line.ExpireMsec <= now) + _Spheres.RemoveAll(sphere => sphere.ExpireMsec <= now);
            if (removed > 0)
                _Dirty = true;

            if (!_Dirty)
                return;

            _Dirty = false;
            Rebuild();
        }

        public void AddLine(Vector3 from, Vector3 to, double duration)
        {
            _Lines.Add(new DebugLine(from, to, Time.GetTicksMsec() + (ulong)(duration * 1000.0)));
            _Dirty = true;
        }

        public void AddSphere(Vector3 center, float radius, double duration)
        {
            _Spheres.Add(new DebugSphere(center, radius, Time.GetTicksMsec() + (ulong)(duration * 1000.0)));
            _Dirty = true;
        }

        public void AddCross(Vector3 point, float size, double duration)
        {
            AddLine(point - Vector3.Right * size, point + Vector3.Right * size, duration);
            AddLine(point - Vector3.Up * size, point + Vector3.Up * size, duration);
            AddLine(point - Vector3.Back * size, point + Vector3.Back * size, duration);
        }

        private void Rebuild()
        {
            _Mesh.ClearSurfaces();

            if (_Lines.Count > 0 || _Spheres.Count > 0)
            {
                _Mesh.SurfaceBegin(Mesh.PrimitiveType.Lines, _LineMaterial);
                foreach (DebugLine line in _Lines)
                    AddLineVertices(line.From, line.To);
                foreach (DebugSphere sphere in _Spheres)
                {
                    AddCircleVertices(sphere.Center, Vector3.Right, Vector3.Up, sphere.Radius);
                    AddCircleVertices(sphere.Center, Vector3.Right, Vector3.Back, sphere.Radius);
                    AddCircleVertices(sphere.Center, Vector3.Up, Vector3.Back, sphere.Radius);
                }
                _Mesh.SurfaceEnd();
            }

            if (_Spheres.Count > 0)
            {
                _Mesh.SurfaceBegin(Mesh.PrimitiveType.Triangles, _FillMaterial);
                foreach (DebugSphere sphere in _Spheres)
                    AddSphereTriangleVertices(sphere.Center, sphere.Radius);
                _Mesh.SurfaceEnd();
            }
        }

        private void AddLineVertices(Vector3 from, Vector3 to)
        {
            _Mesh.SurfaceSetColor(_LineColor);
            _Mesh.SurfaceAddVertex(from);
            _Mesh.SurfaceSetColor(_LineColor);
            _Mesh.SurfaceAddVertex(to);
        }

        private void AddCircleVertices(Vector3 center, Vector3 axis_a, Vector3 axis_b, float radius)
        {
            Vector3 previous = center + axis_a * radius;
            for (int i = 1; i <= _CircleSegments; i++)
            {
                float angle = Mathf.Tau * i / _CircleSegments;
                Vector3 current = center + (axis_a * Mathf.Cos(angle) + axis_b * Mathf.Sin(angle)) * radius;
                AddLineVertices(previous, current);
                previous = current;
            }
        }

        private void AddSphereTriangleVertices(Vector3 center, float radius)
        {
            for (int ring = 0; ring < _SphereRings; ring++)
            {
                float theta_0 = Mathf.Pi * ring / _SphereRings;
                float theta_1 = Mathf.Pi * (ring + 1) / _SphereRings;

                for (int segment = 0; segment < _SphereSegments; segment++)
                {
                    float phi_0 = Mathf.Tau * segment / _SphereSegments;
                    float phi_1 = Mathf.Tau * (segment + 1) / _SphereSegments;

                    Vector3 a = SpherePoint(center, radius, theta_0, phi_0);
                    Vector3 b = SpherePoint(center, radius, theta_0, phi_1);
                    Vector3 c = SpherePoint(center, radius, theta_1, phi_1);
                    Vector3 d = SpherePoint(center, radius, theta_1, phi_0);

                    AddTriangleVertices(a, b, c);
                    AddTriangleVertices(a, c, d);
                }
            }
        }

        private void AddTriangleVertices(Vector3 a, Vector3 b, Vector3 c)
        {
            _Mesh.SurfaceSetColor(_FillColor);
            _Mesh.SurfaceAddVertex(a);
            _Mesh.SurfaceSetColor(_FillColor);
            _Mesh.SurfaceAddVertex(b);
            _Mesh.SurfaceSetColor(_FillColor);
            _Mesh.SurfaceAddVertex(c);
        }

        private static Vector3 SpherePoint(Vector3 center, float radius, float theta, float phi)
        {
            return center + new Vector3(Mathf.Sin(theta) * Mathf.Cos(phi), Mathf.Cos(theta), Mathf.Sin(theta) * Mathf.Sin(phi)) * radius;
        }
    }
}
