
using System.Collections.Generic;

namespace EGame
{
    // 近战武器专用 Model：走连击 + 碰撞体判定，跟远程的 WeaponModel 是完全独立的两套
    public abstract class MeleeModel : AbstractModel
    {
        public virtual string PrefabName => "weapon/" + ID.Entry.ToLowerInvariant();
        public virtual float SwitchTime => 0.3f;
        public virtual string SwitchAnimTrigger => "switch";

        // 连击段数，每一段自己一套时间轴，下标按 comboIndex 取
        public virtual int ComboCount => 1;

        // 攻击阶段时间轴，顺序不重叠：lock -> 前摇(windup) -> release_input -> combo -> exit
        // lock：完全锁定，不读输入；windup：播前摇动画；release_input：期间的输入会被记录，留到 combo 阶段一进入就触发下一段；
        // combo：期间只要有输入（含 release_input 阶段缓冲下来的）就立刻进下一段；exit：连击已经断了，纯等这段攻击收尾
        public virtual float GetLockTime(int comboIndex) => 0.05f;
        public virtual float GetWindupTime(int comboIndex) => 0.2f;
        public virtual float GetReleaseInputTime(int comboIndex) => 0.1f;
        public virtual float GetComboTime(int comboIndex) => 0.2f;
        public virtual float GetExitTime(int comboIndex) => 0.3f;

        // 碰撞体开关是另一套独立时间轴，从这一段攻击开始（lock 进入那一刻）算起，不随上面的阶段切换重置
        public virtual float GetHitboxOpenTime(int comboIndex) => 0.2f;
        public virtual float GetHitboxCloseTime(int comboIndex) => 0.35f;

        public virtual int GetDamage(int comboIndex) => 2;
        public virtual string GetAnimTrigger(int comboIndex) => "attack";

        public override void OnWeaponCreated(NWeapon weapon)
        {
            BuildStateMachine(weapon);
        }

        protected virtual void BuildStateMachine(NWeapon weapon)
        {
            var idle = new WeaponStateIdle();
            var melee_attack = new WeaponStateMeleeAttack();
            var switch_state = new WeaponStateSwitch();

            weapon.BuildStateMachine(new List<WeaponState>()
            {
                idle, melee_attack, switch_state
            }, idle);
        }
    }
}
