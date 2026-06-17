

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

        public void StartCombat(CombatState combat_state)
        {
            _CombatState = combat_state;
            TaskHelper.RunSafely(StartCombatInternal());
        }
        private async Task StartCombatInternal()
        {
            await ExecutePlayerTurn();
            await ExecuteEnemyTurn();
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