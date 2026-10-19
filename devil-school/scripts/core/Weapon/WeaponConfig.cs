
using System.Collections.Generic;

namespace EGame
{
    public static class WeaponConfig
    {
        // 每种武器类型对应的动画 trigger 名字，集中定义在这里，武器 Model 自己不用再声明一遍
        private static readonly Dictionary<WeaponModel.WeaponType, string> _SwitchAnimTriggers = new Dictionary<WeaponModel.WeaponType, string>()
        {
            { WeaponModel.WeaponType.Hand, "hand_switch" },
            { WeaponModel.WeaponType.Pistol, "pistol_switch" },
            { WeaponModel.WeaponType.Shotgun, "pistol_switch" },   // 先跟手枪共用一份，之后有霰弹枪专属动画再换W
            { WeaponModel.WeaponType.Sword, "sword_switch" },
            { WeaponModel.WeaponType.Rifle, "rifle_switch" },
            { WeaponModel.WeaponType.Sniper, "sniper_switch" },
        };

        // 按连击段数排列，远程武器只有一段，下标就是 0
        private static readonly Dictionary<WeaponModel.WeaponType, string[]> _AttackAnimTriggers = new Dictionary<WeaponModel.WeaponType, string[]>()
        {
            { WeaponModel.WeaponType.Pistol, new string[] { "pistol_fire" } },
            { WeaponModel.WeaponType.Shotgun, new string[] { "pistol_fire" } },   // 先跟手枪共用一份，之后有霰弹枪专属动画再换
            { WeaponModel.WeaponType.Rifle, new string[] { "rifle_fire" } },
            { WeaponModel.WeaponType.Sword, new string[] { "sword_attack1", "sword_attack2", "sword_attack3" } },
        };

        // 只有远程武器用得到
        private static readonly Dictionary<WeaponModel.WeaponType, string> _ReloadAnimTriggers = new Dictionary<WeaponModel.WeaponType, string>()
        {
            { WeaponModel.WeaponType.Pistol, "pistol_reload" },
            { WeaponModel.WeaponType.Shotgun, "pistol_reload" },   // 先跟手枪共用一份，之后有霰弹枪专属动画再换
        };

        public static string GetSwitchAnimTrigger(WeaponModel.WeaponType type)
        {
            return _SwitchAnimTriggers[type];
        }

        public static string GetReloadAnimTrigger(WeaponModel.WeaponType type)
        {
            return _ReloadAnimTriggers[type];
        }

        public static string GetAttackAnimTrigger(WeaponModel.WeaponType type, int comboIndex)
        {
            return _AttackAnimTriggers[type][comboIndex];
        }

        public static MeleeComboStep[] GetDefaultComboStep(WeaponModel.WeaponType type)
        {
            return type switch
            {
                WeaponModel.WeaponType.Sword => SwordComboStep,
                WeaponModel.WeaponType.Hand => null,
                _ => null
            };
        }

        private static MeleeComboStep[] SwordComboStep = new MeleeComboStep[]
        {
            new MeleeComboStep()
            {
                LockEndTime = 0.133f,
                ReleaseInputEndTime = 0.433f,
                ComboEndTime = 0.666f,
                Duration = 1.332f,
                HitboxOpenTime = 0.099f,
                HitboxCloseTime = 0.198f,
                Damage = 2,
            },
            new MeleeComboStep()
            {
                LockEndTime = 0.149f,
                ReleaseInputEndTime = 0.3f,
                ComboEndTime = 0.599f,
                Duration = 1.332f,
                HitboxOpenTime = 0.033f,
                HitboxCloseTime = 0.133f,
                Damage = 2,
            },
            new MeleeComboStep()
            {
                LockEndTime = 0.699f,
                ReleaseInputEndTime = 0.699f,
                ComboEndTime = 0.699f,
                Duration = 1.332f,
                HitboxOpenTime = 0.266f,
                HitboxCloseTime = 0.323f,
                Damage = 2,
            },
        };
    }
}
