
using System.Collections.Generic;

namespace EGame
{
	/// <summary>
	/// 管理一次Combat的数据
	/// </summary>
	public class CombatRoom
	{
		public CombatState CombatState { get; private set; }
		public IReadOnlyList<Creature> Allies => CombatState.Allies;
		public IReadOnlyList<Creature> Enemies => CombatState.Enemies;
		public EncounterModel Encounter { get; private set; }

		public CombatRoom(EncounterModel model)
		{
			this.Encounter = model;

			CombatState = new CombatState();

			//进入战斗之前先添加Creature
			CombatState.AddPlayer(RunManager.Instance.RunState.Player);
			
			//生成具体MonsterModel
			this.Encounter.GenerateMonsterWithSlost();
			foreach (var slot in Encounter.MonsterWithSlot)
			{
				var creature = new Creature(slot.Item1, CombatSide.Enemy, slot.Item2);
				CombatState.AddCreature(creature);
			}
		}
	}
}
