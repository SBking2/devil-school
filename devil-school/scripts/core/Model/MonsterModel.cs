
using System;

namespace EGame
{
	[ModelCategory]
	public abstract class MonsterModel : CharacterModel
	{
		public virtual float VisualLength => 10f;
		public virtual float VisualAngle => 90f;

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

		protected virtual WorldBehaviorTree CreateWorldBehaviorTree(NEnvCreature ncreature)
		{
			return null;
		}

        public override void SetUpForWorld(NEnvCreature ncreature)
        {
            base.SetUpForWorld(ncreature);
			WorldBehaviorTree = CreateWorldBehaviorTree(ncreature);
        }
    }
}
