
namespace EGame
{
    public class WeaponStateReloading : WeaponState
    {
        public override string StateName => WeaponConfig.Reload;

        public override void OnProcess(NWeapon weapon, double dt)
        {
            base.OnProcess(weapon, dt);

            if (_RunningTime > GetReloadTime(weapon))
            {
                weapon.ChangeState(WeaponConfig.Idle);
            }
        }

        protected virtual float GetReloadTime(NWeapon weapon)
        {
            return weapon.ReloadTime;
        }
    }
}