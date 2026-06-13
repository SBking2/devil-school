

namespace EGame
{
    /// <summary>
    /// 管理战斗的逻辑，流程，数据全由CombatState管理
    /// </summary>
    public class CombatManager
    {
        public static CombatManager Instance { get; } = new CombatManager();

        private CombatState _CombatState;
    }
}