
namespace EGame
{
    // 玩家状态机里所有状态的名字，按层分组
    public static class PlayerConfig
    {
        // 顶层：模式（互斥，独占型状态放这一层，以后的冲刺/硬直/死亡都加在这里）
        public const string ModeNormal = "mode_normal";
        public const string ModeDash = "mode_dash";

        // 移动层（只在 Normal 模式里运行）
        public const string MoveIdle = "move_idle";
        public const string MoveWalk = "move_walk";
        public const string MoveRun = "move_run";
        public const string MoveCrouch = "move_crouch";
        public const string MoveAir = "move_air";

        // 动作层（只在 Normal 模式里运行，跟移动层同时跑）
        public const string ActionIdle = "action_idle";
        public const string ActionSwitch = "action_switch";
        public const string ActionFire = "action_fire";
        public const string ActionReload = "action_reload";
        public const string ActionMelee = "action_melee";
    }
}
