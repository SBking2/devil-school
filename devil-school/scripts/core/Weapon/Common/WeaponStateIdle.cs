
namespace EGame
{
    public class WeaponStateIdle : WeaponState
    {
        public override string StateName => WeaponConfig.Idle;

        public override void OnProcess(NWeapon weapon, double dt)
        {
            base.OnProcess(weapon, dt);

            // 近战必须是新按下的一次才能开打，不然打完最后一段回到 Idle 时按键还没松手，会被 Pressing 立刻拉回去重新连击
            bool wants_attack = weapon.MeleeData != null ? weapon.Intent.JustPressed : weapon.Intent.Pressing;
            if (wants_attack)
                weapon.ChangeState(weapon.MeleeData != null ? WeaponConfig.MeleeAttack : WeaponConfig.Fire);
        }
    }
}