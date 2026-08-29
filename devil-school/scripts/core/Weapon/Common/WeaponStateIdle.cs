
namespace EGame
{
    public class WeaponStateIdle : WeaponState
    {
        public override string StateName => WeaponConfig.Idle;

        public override void OnProcess(NWeapon weapon, double dt)
        {
            base.OnProcess(weapon, dt);
            if (weapon.Intent.Pressing)
                weapon.ChangeState(WeaponConfig.Fire);
        }
    }
}