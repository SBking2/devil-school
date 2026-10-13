
using Godot;

namespace EGame
{
    [GlobalClass]
    public partial class RotationCurve : Resource
    {
        [Export] public Curve X;
        [Export] public Curve Y;
        [Export] public Curve Z;

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