
namespace EGame
{
	public abstract class MonsterModel : AbstractModel
	{
		public virtual int MaxHP => 10;
		protected virtual string _VisualsPath => $"creature_visuals/" + ID.Entry.ToLowerInvariant();
		public MonsterMoveStateMachine MoveStateMachine { get; }

		public NCreatureVisual CreateVisual()
		{
			return SceneHelper.LoadScene<NCreatureVisual>(_VisualsPath);
		}

        public virtual CreatureAnimator CreateAnimator(EGSpineSprite sprite)
        {
            AnimState idle_state = new AnimState("idle_loop", true);
            CreatureAnimator animator = new CreatureAnimator(sprite, idle_state);
            return animator;
        }

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
