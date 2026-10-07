
using Godot;

namespace EGame
{
    public class DamageInfo
    {
        public DamageInfo(Node3D attacker, Node3D target, Vector3 hitPoint, Vector3 hitNormal, CharacterModel shooter, int amount)
        {
            Attacker = attacker;
            Target = target;
            HitPoint = hitPoint;
            HitNormal = hitNormal;
            Shooter = shooter;
            Amount = amount;
        }

        public Node3D Attacker { get; }
        public Node3D Target { get; }
        public Vector3 HitPoint { get; }
        public Vector3 HitNormal { get; }
        public CharacterModel Shooter { get; }
        public int Amount { get; }
    }
}
