
namespace EGame
{
    public class PlayerActionStateIdle : PlayerState
    {
        public override string StateName => PlayerConfig.ActionIdle;

        public override void OnProcess(NPlayer player, double dt)
        {
            base.OnProcess(player, dt);

            var weapon = player.CurrentWeapon;
            if (weapon == null)
                return;

            // 近战必须是新按下的一次才能开打，不然打完最后一段回到 Idle 时按键还没松手，会被 Pressing 立刻拉回去重新连击
            if (weapon.MeleeData != null)
            {
                if (player.Intent.JustPressed)
                    player.ActionLayer.ChangeState(PlayerConfig.ActionMelee);
                return;
            }

            // 远程：弹匣没满、备弹还有才能换弹；按住开火时弹匣空了自动换弹（备弹也没了就干瞪眼）
            var ranged = weapon.RangedData;
            bool can_reload = ranged.CurrentAmmo < ranged.MagazineSize && ranged.TotalAmmo > 0;

            if (player.ReloadPressed && can_reload)
            {
                player.ActionLayer.ChangeState(PlayerConfig.ActionReload);
                return;
            }

            if (player.Intent.Pressing)
            {
                if (ranged.CurrentAmmo > 0)
                    player.ActionLayer.ChangeState(PlayerConfig.ActionFire);
                else if (can_reload)
                    player.ActionLayer.ChangeState(PlayerConfig.ActionReload);
            }
        }
    }
}
