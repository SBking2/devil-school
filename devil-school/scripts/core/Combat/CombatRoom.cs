
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
			var players = RunManager.Instance.RunState.Players;
			foreach(var player in players)
				CombatState.AddPlayer(player);
			
			//生成具体MonsterModel
			this.Encounter.GenerateMonsterWithSlost();
			foreach (var slot in Encounter.MonsterWithSlot)
			{
				var creature = new Creature(slot.Item1, CombatSide.Enemy, slot.Item2);
				CombatState.AddCreature(creature);
			}
		}

		public void EnterRoom()
		{
			//场景加载房间
            var ncombat_room = NCombatRoom.Create(this);
            NRun.Instance.SetCurrentRoom(ncombat_room);

			//加载完毕后开始真正战斗
			StartCombat();
        }

        private void StartCombat()
		{
            
            CombatManager.Instance.SetUpCombat(CombatState);
            CombatManager.Instance.AfterRoomLoaded();
        }
	}
}
