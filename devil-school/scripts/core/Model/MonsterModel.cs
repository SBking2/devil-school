
namespace EGame
{
	[ModelCategory]
	public abstract class MonsterModel : CharacterModel
	{
		public MonsterMoveStateMachine MoveStateMachine { get; }

		/// <summary>
		/// 创建怪物的AI决策状态机
		/// </summary>
		public virtual MonsterMoveStateMachine CreateMoveStateMachine()
		{
			MonsterMoveStateMachine state_machine = new MonsterMoveStateMachine(null, null);
			return state_machine;
		}
    }
}
