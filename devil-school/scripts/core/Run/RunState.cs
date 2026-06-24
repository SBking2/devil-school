
using System.Collections.Generic;

namespace EGame
{
    /// <summary>
    /// 管理一次Run的数据
    /// </summary>
    public class RunState
    {
        public IReadOnlyList<Player> Players { get; private set;}
        public static RunState CreateForNewRun(IReadOnlyList<Player> players)
        {
            var state = new RunState();
            state.Players = players;
            return state;
        }
    }
}