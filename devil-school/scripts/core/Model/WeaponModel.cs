
using System;
using System.Collections.Generic;

namespace EGame
{
    public abstract class WeaponModel : AbstractModel
    {
        public virtual string PrefabName => "weapon/" + ID.Entry.ToLowerInvariant();
        public virtual int Attack => 2;
        public virtual float SwitchTime => 0.3f;
        public virtual float ReloadTime => 2f;
        public virtual float FireTime => 1f;
        public virtual UInt16 HitMask => (UInt16)(CollisionMask.GrandMask | CollisionMask.MonsterMask);
        public virtual float MeleeRange => 0f;    // 0 = 远程(走射线检测)，大于 0 = 近战(走距离判断)，值就是攻击距离
        public virtual string SwitchAnimTrigger => "switch";
        public virtual string ReloadAnimTrigger => "reload";
        public virtual string FireAnimTrigger => "fire";

        public override void OnWeaponCreated(NWeapon weapon)
        {
            BuildStateMachine(weapon);
        }

        protected virtual void BuildStateMachine(NWeapon weapon)
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