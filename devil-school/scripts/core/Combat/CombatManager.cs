

using System.Threading.Tasks;

namespace EGame
{
    /// <summary>
    /// CombatManager仅仅管理战斗流程，数据由CombatRoom管理
    /// </summary>
    public class CombatManager
    {
        public static CombatManager Instance { get; } = new CombatManager();

        private CombatState _CombatState;

        public void SetUpCombat(CombatState combat_state)
        {
            _CombatState = combat_state;
        }

        public void AfterRoomLoaded()
        {
            TaskHelper.RunSafely(StartCombatInternal());
        }

        private async Task StartCombatInternal()
        {
            await StartTurn();
        }

        /// <summary>
        /// 一个完整的回合,包括敌人和玩家
        /// </summary>
        private async Task StartTurn()
        {
            
        }

        private async Task ExecutePlayerTurn()
        {
            await Task.CompletedTask;
        }

        private async Task ExecuteEnemyTurn()
        {
            await Task.CompletedTask;
        }
    }
}