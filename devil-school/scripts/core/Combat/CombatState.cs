
using System.Collections.Generic;
using System.Linq;

namespace EGame
{
	public class CombatState
	{
		public CombatState()
		{

		}
		
		private uint _CurTurn;
		private CombatSide _CurCombatSide;
		public uint CurTurn => _CurTurn;
		public CombatSide CombatSide => _CurCombatSide;

		/////////////////////////////////////////////// Creature //////////////////////////////////////////////////
		
		private readonly List<Creature> _Allies = new List<Creature>();
		private readonly List<Creature> _Enemies = new List<Creature>();
		public IReadOnlyList<Creature> Allies => _Allies;
		public IReadOnlyList<Creature> Enemies => _Enemies;
		public IReadOnlyList<Creature> Creatures => _Allies.Concat(_Enemies).ToList();
		
		public void AddCreature(Creature creature)
		{
			var list = creature.IsPlayer ? _Allies : _Enemies;
			list.Add(creature);
		}
	}
}
