
namespace EGame
{
	[ModelCategory]
	public abstract class MonsterModel : CharacterModel
	{
		private TurnMoveStateMachine _TurnMoveStateMachine;
		public TurnMoveStateMachine TurnMoveStateMachine => _TurnMoveStateMachine;

		private WorldMoveStateMachine _WorldMoveStateMachine;
		public WorldMoveStateMachine WorldMoveStateMachine => _WorldMoveStateMachine;

		/// <summary>
		/// 创建怪物的AI决策状态机
		/// </summary>
		public virtual TurnMoveStateMachine CreateTurnMoveStateMachine()
		{
			TurnMoveStateMachine state_machine = new TurnMoveStateMachine(null, null);
			return state_machine;
		}

		public virtual WorldMoveStateMachine CreateWorldMoveStateMachine()
		{
			WorldMoveStateMachine state_machine = new WorldMoveStateMachine(null, null);
			return state_machine;
		}
    }
}
