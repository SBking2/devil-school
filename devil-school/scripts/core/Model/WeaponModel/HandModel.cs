
namespace EGame
{
    public class HandModel : WeaponModel
    {
        public override string SwitchAnimTrigger => "hand_switch";
        public override string FireAnimTrigger => "hand_fire";
        public override float MeleeRange => 2f;
    }
}