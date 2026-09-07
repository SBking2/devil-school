
namespace EGame
{
    public class HandModel : MeleeModel
    {
        public override string SwitchAnimTrigger => "hand_switch";
        public override string GetAnimTrigger(int comboIndex) => "hand_fire";
    }
}