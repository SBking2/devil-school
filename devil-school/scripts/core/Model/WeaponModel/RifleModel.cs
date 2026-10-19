
namespace EGame
{
    public class RifleModel : RangedWeaponModel
    {
        public override WeaponType Type => WeaponType.Rifle;
        public override string ParentName => "hand_r";
        public override float FireTime => 0.01f;
        public override int MaxTotalAmmo => 1000;
        public override int MagazineSize => 100;
    }
}