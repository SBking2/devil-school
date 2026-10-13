
using System.Collections.Generic;

namespace EGame
{
    // 近战武器专用 Model：走连击 + 碰撞体判定
    public abstract class MeleeWeaponModel : WeaponModel
    {
        public virtual MeleeComboStep[] ComboSteps => WeaponConfig.GetDefaultComboStep(Type);

        // 没有配置连击就不能攻击
        public bool CanAttack => ComboSteps != null && ComboSteps.Length > 0;
        public int ComboCount => CanAttack ? ComboSteps.Length : 0;

        public float GetLockEndTime(int comboIndex) => ComboSteps[comboIndex].LockEndTime;
        public float GetReleaseInputEndTime(int comboIndex) => ComboSteps[comboIndex].ReleaseInputEndTime;
        public float GetComboEndTime(int comboIndex) => ComboSteps[comboIndex].ComboEndTime;
        public float GetDuration(int comboIndex) => ComboSteps[comboIndex].Duration;
        public float GetHitboxOpenTime(int comboIndex) => ComboSteps[comboIndex].HitboxOpenTime;
        public float GetHitboxCloseTime(int comboIndex) => ComboSteps[comboIndex].HitboxCloseTime;
        public int GetDamage(int comboIndex) => ComboSteps[comboIndex].Damage;
    }
}
