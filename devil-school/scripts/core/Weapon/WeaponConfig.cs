
using System.Collections.Generic;

namespace EGame
{
    public static class WeaponConfig
    {
        public static string Idle = "idle";
        public static string Fire = "fire";
        public static string Reload = "reload";
        public static string Switch = "switch";
        public static string MeleeAttack = "melee_attack";

        // 每种武器类型对应的动画 trigger 名字，集中定义在这里，武器 Model 自己不用再声明一遍
        private static readonly Dictionary<WeaponModel.WeaponType, string> _SwitchAnimTriggers = new Dictionary<WeaponModel.WeaponType, string>()
        {
            { WeaponModel.WeaponType.Hand, "hand_switch" },
            { WeaponModel.WeaponType.Pistol, "pistol_switch" },
            { WeaponModel.WeaponType.Sword, "sword_switch" },
        };

        // 按连击段数排列，远程武器只有一段，下标就是 0
        private static readonly Dictionary<WeaponModel.WeaponType, string[]> _AttackAnimTriggers = new Dictionary<WeaponModel.WeaponType, string[]>()
        {
            { WeaponModel.WeaponType.Hand, new string[] { "hand_fire" } },
            { WeaponModel.WeaponType.Pistol, new string[] { "pistol_fire" } },
            { WeaponModel.WeaponType.Sword, new string[] { "sword_attack1", "sword_attack2", "sword_attack3" } },
        };

        // 只有远程武器用得到，目前就手枪一种
        private static readonly Dictionary<WeaponModel.WeaponType, string> _ReloadAnimTriggers = new Dictionary<WeaponModel.WeaponType, string>()
        {
            { WeaponModel.WeaponType.Pistol, "pistol_reload" },
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
    }
}
