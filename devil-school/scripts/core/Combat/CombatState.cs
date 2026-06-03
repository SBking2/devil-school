
using System.Collections.Generic;

namespace EGame
{
    public class CombatState
    {
        public CombatState()
        {

        }
        
        private uint _CurTurn;

        private List<Creature> _Players = new List<Creature>();

        private List<Creature> _Enemies = new List<Creature>();

        public IReadOnlyList<Creature> Players => _Players;
        public IReadOnlyList<Creature> Enemies => _Enemies;
        public uint CurTurn => _CurTurn;
    }
}