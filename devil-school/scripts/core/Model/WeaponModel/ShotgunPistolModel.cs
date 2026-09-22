
namespace EGame
{
    public class ShotgunPistolModel : RangedWeaponModel
    {
        public override string ParentName => "hand_r";

        public override WeaponType Type => WeaponType.Shotgun;

        public override float Range => 3f;
    }
}