
using Godot;

namespace EGame
{
    public class WeaponStateFire : WeaponState
    {
        public override string StateName => WeaponConfig.Fire;

        public override void OnEnter(NWeapon weapon)
        {
            base.OnEnter(weapon);
            weapon.FireInternal();
        }

        public override void OnProcess(NWeapon weapon, double dt)
        {
            base.OnProcess(weapon, dt);

            if (dt > GetFireGap(weapon))
            {
                weapon.ChangeState(WeaponConfig.Idle);
            }
        }

        protected virtual float GetFireGap(NWeapon weapon)
        {
            return weapon.FireTime;
        }
    }
}