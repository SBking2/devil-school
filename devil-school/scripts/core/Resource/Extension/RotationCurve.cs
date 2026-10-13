
using Godot;

namespace EGame
{
    [GlobalClass]
    public partial class RotationCurve : Resource
    {
        [Export] public Curve X;
        [Export] public Curve Y;
        [Export] public Curve Z;

        // 三条曲线的横轴就是秒数，持续时间取三条里最长的那个
        public float Duration
        {
            get
            {
                float duration = 0f;
                if (X != null) duration = Mathf.Max(duration, X.MaxDomain);
                if (Y != null) duration = Mathf.Max(duration, Y.MaxDomain);
                if (Z != null) duration = Mathf.Max(duration, Z.MaxDomain);
                return duration;
            }
        }

        public Vector3 SampleEuler(float t)
        {
            return new Vector3(
                X != null ? X.SampleBaked(t) : 0f,
                Y != null ? Y.SampleBaked(t) : 0f,
                Z != null ? Z.SampleBaked(t) : 0f) * Mathf.DegToRad(1f);
        }

        public Quaternion Sample(float t)
        {
            return Quaternion.FromEuler(SampleEuler(t));
        }
    }
}