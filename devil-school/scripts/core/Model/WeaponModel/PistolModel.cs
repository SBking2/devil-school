
namespace EGame
{
    public class PistolModel : RangedWeaponModel
    {
        public override string ParentName => "hand_r";
        public override WeaponType Type => WeaponType.Pistol;
        public override float FireTime => 0.5f;
    }
}