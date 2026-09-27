
namespace EGame
{
    public class PlayerActionStateFire : PlayerState
    {
        public override string StateName => PlayerConfig.ActionFire;

        public override void OnEnter(NPlayer player)
        {
            base.OnEnter(player);

            var weapon = player.CurrentWeapon;
            weapon.RangedData.CurrentAmmo--;
            if (weapon.RangedData.Type == WeaponModel.WeaponType.Shotgun)
                weapon.FireShotgunInternal();
            else
                weapon.FireInternal();
        }

        public override void OnProcess(NPlayer player, double dt)
        {
            base.OnProcess(player, dt);

            if (_RunningTime > player.CurrentWeapon.RangedData.FireTime)
                player.ActionLayer.ChangeState(PlayerConfig.ActionIdle);
        }
    }
}
