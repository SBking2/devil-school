

namespace EGame
{
    public class WeaponStateSwitch : WeaponState
    {
        public override string StateName => WeaponConfig.Switch;

        public override void OnEnter(NWeapon weapon)
        {
            base.OnEnter(weapon);
        }

        public override void OnProcess(NWeapon weapon, double dt)
        {
            base.OnProcess(weapon, dt);

            if(_RunningTime > GetSwitchTime(weapon))
            {
                weapon.ChangeState(WeaponConfig.Idle);
            }
        }

        protected virtual float GetSwitchTime(NWeapon weapon)
        {
            return weapon.SwitchTime;
        }
    }
}