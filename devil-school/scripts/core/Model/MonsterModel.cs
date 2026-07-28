
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

		private WorldBehaviorTree _WorldBehaviorTree;
		public WorldBehaviorTree WorldBehaviorTree
		{
			get
			{
				return _WorldBehaviorTree;
			}

			private set
			{
				AssertMutable();
				if (_WorldBehaviorTree != null)
					throw new InvalidOperationException($"{ID} already has a world behavior tree");
				_WorldBehaviorTree = value;
			}
		}

		/// <summary>
		/// 创建怪物的AI决策状态机
		/// </summary>
		public virtual TurnMoveStateMachine CreateTurnMoveStateMachine()
		{
			return null;
		}

		public virtual WorldBehaviorTree CreateWorldBehaviorTree()
		{
			return null;
		}

        public override void SetUpForWorld()
        {
            base.SetUpForWorld();
			WorldBehaviorTree = CreateWorldBehaviorTree();
        }
    }
}
