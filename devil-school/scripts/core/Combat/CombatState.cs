
using System.Collections.Generic;

namespace EGame
{
    public class CombatState
    {
        public CombatState()
        {

        }
        
        private uint _CurTurn;
        
        private CombatSide _CurCombatSide;

        private readonly List<Creature> _Players = new List<Creature>();

        private readonly List<Creature> _Enemies = new List<Creature>();

        public IReadOnlyList<Creature> Players => _Players;
        public IReadOnlyList<Creature> Enemies => _Enemies;
        public uint CurTurn => _CurTurn;
        public CombatSide CombatSide => _CurCombatSide;
    }
}