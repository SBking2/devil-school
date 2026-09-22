
using System.Collections.Generic;

namespace EGame
{
    // 远程武器专用 Model：走射线检测
    public abstract class RangedWeaponModel : WeaponModel
    {
        public virtual int Attack => 2;
        public virtual float ReloadTime => 2f;
        public virtual float FireTime => 1f;
        public virtual float Range => 100f;

        protected override void BuildStateMachine(NWeapon weapon)
        {
            var idle = new WeaponStateIdle();
            var fire = new WeaponStateFire();
            var switch_state = new WeaponStateSwitch();
            var switch_reloading = new WeaponStateReloading();

            weapon.BuildStateMachine(new List<WeaponState>()
            {
                idle, fire, switch_state, switch_reloading
            }, idle);
        }
    }
}
