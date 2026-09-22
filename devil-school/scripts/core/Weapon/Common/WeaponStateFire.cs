
using Godot;

namespace EGame
{
    public class WeaponStateFire : WeaponState
    {
        public override string StateName => WeaponConfig.Fire;

        public override void OnEnter(NWeapon weapon)
        {
            base.OnEnter(weapon);

            if (weapon.RangedData.Type == WeaponModel.WeaponType.Shotgun)
                weapon.FireShotgunInternal();
            else
                weapon.FireInternal();
        }

        public override void OnProcess(NWeapon weapon, double dt)
        {
            base.OnProcess(weapon, dt);

            if (_RunningTime > GetFireGap(weapon))
            {
                weapon.ChangeState(WeaponConfig.Idle);
            }
        }

        protected virtual float GetFireGap(NWeapon weapon)
        {
            return weapon.RangedData.FireTime;
        }
    }
}