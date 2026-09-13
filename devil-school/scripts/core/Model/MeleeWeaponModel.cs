
using System.Collections.Generic;

namespace EGame
{
    // 近战武器专用 Model：走连击 + 碰撞体判定
    public abstract class MeleeWeaponModel : WeaponModel
    {
        // 每一段连击(招式)自己一个完整配置：阶段时间点 lock -> release_input -> combo -> duration(结束)，
        // 加另一套独立的碰撞体开关时间点，再加这一下的伤害——都写在同一个 MeleeComboStep 里，全是从这一招开始算起的绝对时间
        protected virtual MeleeComboStep[] ComboSteps => new MeleeComboStep[]
        {
            new MeleeComboStep()
            {
                LockEndTime = 0.25f,
                ReleaseInputEndTime = 0.35f,
                ComboEndTime = 0.55f,
                Duration = 0.85f,
                HitboxOpenTime = 0.2f,
                HitboxCloseTime = 0.35f,
                Damage = 2,
            },
        };

        public int ComboCount => ComboSteps.Length;

        public float GetLockEndTime(int comboIndex) => ComboSteps[comboIndex].LockEndTime;
        public float GetReleaseInputEndTime(int comboIndex) => ComboSteps[comboIndex].ReleaseInputEndTime;
        public float GetComboEndTime(int comboIndex) => ComboSteps[comboIndex].ComboEndTime;
        public float GetDuration(int comboIndex) => ComboSteps[comboIndex].Duration;
        public float GetHitboxOpenTime(int comboIndex) => ComboSteps[comboIndex].HitboxOpenTime;
        public float GetHitboxCloseTime(int comboIndex) => ComboSteps[comboIndex].HitboxCloseTime;
        public int GetDamage(int comboIndex) => ComboSteps[comboIndex].Damage;

        protected override void BuildStateMachine(NWeapon weapon)
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
