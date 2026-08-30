
namespace EGame
{
    public class PistolModel : WeaponModel
    {
        public override float FireTime => 0.5f;
        public override string SwitchAnimTrigger => "pistol_switch";
        public override string FireAnimTrigger => "pistol_fire";
    }
}