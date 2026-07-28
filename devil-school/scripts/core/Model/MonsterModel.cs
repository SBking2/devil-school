
using System;

namespace EGame
{
	[ModelCategory]
	public abstract class MonsterModel : CharacterModel
	{
		private TurnMoveStateMachine _TurnMoveStateMachine;
		public TurnMoveStateMachine TurnMoveStateMachine
		{
			get
			{
				return _TurnMoveStateMachine;
			}

			private set
			{
				AssertMutable();
				if (_TurnMoveStateMachine != null)
					throw new InvalidOperationException($"{ID} already has a turn-based state-machine");
				_TurnMoveStateMachine = value;
			}
		}

		private WorldMoveStateMachine _WorldMoveStateMachine;
		public WorldMoveStateMachine WorldMoveStateMachine
		{
			get
			{
				return _WorldMoveStateMachine;
			}

			private set
			{
				AssertMutable();
				if (_WorldMoveStateMachine != null)
					throw new InvalidOperationException($"{ID} already has a world state-machine");
				_WorldMoveStateMachine = value;
			}
		}

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

        public override void SetUpForWorld()
        {
            base.SetUpForWorld();
			_WorldMoveStateMachine = CreateWorldMoveStateMachine();
        }
    }
}
