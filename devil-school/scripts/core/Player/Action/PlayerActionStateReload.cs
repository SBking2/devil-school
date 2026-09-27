
using System;

namespace EGame
{
    // 换弹：计时结束才补满弹匣；换弹中途切枪会被 Switch 打断，弹药保持原样
    public class PlayerActionStateReload : PlayerState
    {
        public override string StateName => PlayerConfig.ActionReload;

        public override void OnEnter(NPlayer player)
        {
            base.OnEnter(player);
            player.AnimTrigger(WeaponConfig.GetReloadAnimTrigger(player.CurrentWeapon.RangedData.Type));
        }

        public override void OnProcess(NPlayer player, double dt)
        {
            base.OnProcess(player, dt);

            var ranged = player.CurrentWeapon.RangedData;
            if (_RunningTime > ranged.ReloadTime)
            {
                // 从备弹里补，补不满弹匣就有多少补多少
                int amount = Math.Min(ranged.MagazineSize - ranged.CurrentAmmo, ranged.TotalAmmo);
                ranged.TotalAmmo -= amount;
                ranged.CurrentAmmo += amount;
                player.ActionLayer.ChangeState(PlayerConfig.ActionIdle);
            }
        }
    }
}
