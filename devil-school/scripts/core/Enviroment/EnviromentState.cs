
using System.Collections.Generic;

namespace EGame
{
    public class EnviromentState
    {
        private List<Player> _Players;
        public IReadOnlyList<Player> Players => _Players;

        public void AddPlayer(Player player)
        {
            _Players.Add(player);
        }
    }
}