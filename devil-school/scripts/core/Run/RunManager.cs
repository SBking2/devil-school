
using System.Collections.Generic;

namespace EGame
{
    /// <summary>
    /// 管理着游戏运行
    /// </summary>
    public class RunManager
    {
        public static RunManager Instance { get; } = new RunManager();

        private List<Player> _Players = new List<Player>();
        public IReadOnlyList<Player> Players => _Players;
    }
}