
namespace EGame
{
    public class PlayerActionStateSwitch : PlayerState
    {
        public override string StateName => PlayerConfig.ActionSwitch;

        public override void OnEnter(NPlayer player)
        {
            base.OnEnter(player);
            player.AnimTrigger(player.CurrentWeapon.Data.SwitchAnimTrigger);
        }

        public override void OnProcess(NPlayer player, double dt)
        {
            base.OnProcess(player, dt);

            if (_RunningTime > player.CurrentWeapon.Data.SwitchTime)
                player.ActionLayer.ChangeState(PlayerConfig.ActionIdle);
        }
    }
}
