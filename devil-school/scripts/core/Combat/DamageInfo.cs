
using Godot;

namespace EGame
{
    public class DamageInfo
    {
        public DamageInfo(Node3D hitObject, Vector3 hitPoint, Vector3 hitNormal, INCharacter shooter, int amount)
        {
            HitObject = hitObject;
            HitPoint = hitPoint;
            HitNormal = hitNormal;
            Shooter = shooter;
            Amount = amount;
        }

        public Node3D HitObject { get; }
        public Vector3 HitPoint { get; }
        public Vector3 HitNormal { get; }
        public INCharacter Shooter { get; }
        public int Amount { get; }
    }
}
