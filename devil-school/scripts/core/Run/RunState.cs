
namespace EGame
{
    /// <summary>
    /// 管理一次Run的数据
    /// </summary>
    public class RunState
    {
        public Player Player { get; private set;}
        public static RunState CreateForNewRun(Player player)
        {
            var state = new RunState();
            state.Player = player;
            return state;
        }
    }
}