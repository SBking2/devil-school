
namespace EGame
{
    public class SniperModel : RangedWeaponModel
    {
        public override WeaponType Type => WeaponType.Sniper;
        public override string ParentName => "hand_r";
    }
}