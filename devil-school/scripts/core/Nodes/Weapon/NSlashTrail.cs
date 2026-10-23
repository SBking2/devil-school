
using Godot;

namespace EGame
{
    public partial class NSlashTrail : MeshInstance3D
    {
        [Export] public Node3D TipNode;
        [Export] public Node3D BaseNode;
        [Export] public float Lifetime = 0.25f;
        [Export] public float MinSectionDistance = 0.03f;
        [Export] public int MaxSections = 48;
        [Export] public int Subdivisions = 3;

        public bool Emitting { get; set; }

        public override void _Ready()
        {
            // 顶点直接存世界坐标，节点自己不能跟着父节点动
            TopLevel = true;
            GlobalTransform = Transform3D.Identity;
            CastShadow = ShadowCastingSetting.Off;

            _ImmediateMesh = new ImmediateMesh();
            Mesh = _ImmediateMesh;

            _Sections = new Section[MaxSections];
            _Dense = new Section[MaxSections * (Subdivisions + 1) + 1];
        }

        public override void _Process(double delta)
        {
            _Clock += delta;

            RemoveExpired();
            if (Emitting)
                Sample();

            Rebuild();
        }

        public void Clear()
        {
            _Start = 0;
            _Count = 0;
            _ImmediateMesh.ClearSurfaces();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////
        ////////                                    采样
        ///////////////////////////////////////////////////////////////////////////////////////////////////////

        private struct Section
        {
            public Vector3 Tip;
            public Vector3 Base;
            public double Time;
        }

        private Section[] _Sections;
        private int _Start;
        private int _Count;
        private double _Clock;

        private Section GetSection(int index)
        {
            return _Sections[(_Start + index) % _Sections.Length];
        }

        private void SetSection(int index, Section section)
        {
            _Sections[(_Start + index) % _Sections.Length] = section;
        }

        private void PushSection(Section section)
        {
            if (_Count == _Sections.Length)
            {
                _Start = (_Start + 1) % _Sections.Length;
                _Count--;
            }

            SetSection(_Count, section);
            _Count++;
        }

        private void RemoveExpired()
        {
            while (_Count > 0 && _Clock - GetSection(0).Time > Lifetime)
            {
                _Start = (_Start + 1) % _Sections.Length;
                _Count--;
            }
        }

        // 最新的一个截面始终贴着刀尖走，只有刀尖离开倒数第二个截面够远才真正新增，避免帧率高时点挤成一团
        private void Sample()
        {
            if (TipNode == null || BaseNode == null)
                return;

            var section = new Section()
            {
                Tip = TipNode.GlobalPosition,
                Base = BaseNode.GlobalPosition,
                Time = _Clock,
            };

            bool merge = false;
            if (_Count >= 2)
                merge = section.Tip.DistanceTo(GetSection(_Count - 2).Tip) < MinSectionDistance;
            else if (_Count == 1)
                merge = section.Tip.DistanceTo(GetSection(0).Tip) < MinSectionDistance;

            if (merge)
                SetSection(_Count - 1, section);
            else
                PushSection(section);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////
        ////////                                    平滑
        ///////////////////////////////////////////////////////////////////////////////////////////////////////

        private Section[] _Dense;

        private int BuildDense()
        {
            int dense_count = 0;
            for (int i = 0; i < _Count - 1; i++)
            {
                var p0 = GetSection(Mathf.Max(i - 1, 0));
                var p1 = GetSection(i);
                var p2 = GetSection(i + 1);
                var p3 = GetSection(Mathf.Min(i + 2, _Count - 1));

                for (int j = 0; j <= Subdivisions; j++)
                {
                    float t = (float)j / (Subdivisions + 1);
                    _Dense[dense_count++] = new Section()
                    {
                        Tip = p1.Tip.CubicInterpolate(p2.Tip, p0.Tip, p3.Tip, t),
                        Base = p1.Base.CubicInterpolate(p2.Base, p0.Base, p3.Base, t),
                        Time = p1.Time + (p2.Time - p1.Time) * t,
                    };
                }
            }

            _Dense[dense_count++] = GetSection(_Count - 1);
            return dense_count;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////
        ////////                                    网格
        ///////////////////////////////////////////////////////////////////////////////////////////////////////

        private ImmediateMesh _ImmediateMesh;

        private void Rebuild()
        {
            _ImmediateMesh.ClearSurfaces();
            if (_Count < 2)
                return;

            int count = BuildDense();

            _ImmediateMesh.SurfaceBegin(Mesh.PrimitiveType.TriangleStrip);
            for (int i = 0; i < count; i++)
            {
                var section = _Dense[i];
                var prev = _Dense[Mathf.Max(i - 1, 0)];
                var next = _Dense[Mathf.Min(i + 1, count - 1)];

                var tangent = ((next.Tip + next.Base) - (prev.Tip + prev.Base)).Normalized();
                var width_dir = (section.Tip - section.Base).Normalized();
                var normal = tangent.Cross(width_dir).Normalized();
                float u = Mathf.Clamp((float)((_Clock - section.Time) / Lifetime), 0f, 1f);

                _ImmediateMesh.SurfaceSetNormal(normal);
                _ImmediateMesh.SurfaceSetTangent(new Plane(tangent, 1f));
                _ImmediateMesh.SurfaceSetUV(new Vector2(u, 0f));
                _ImmediateMesh.SurfaceAddVertex(section.Base);

                _ImmediateMesh.SurfaceSetNormal(normal);
                _ImmediateMesh.SurfaceSetTangent(new Plane(tangent, 1f));
                _ImmediateMesh.SurfaceSetUV(new Vector2(u, 1f));
                _ImmediateMesh.SurfaceAddVertex(section.Tip);
            }
            _ImmediateMesh.SurfaceEnd();
        }
    }
}
