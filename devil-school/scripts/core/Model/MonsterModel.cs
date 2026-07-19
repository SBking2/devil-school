
namespace EGame
{
	public abstract class MonsterModel : AbstractModel
	{
		public virtual int MaxHP => 10;

		protected virtual string _VisualsPath => $"creature_visuals/" + ID.Entry.ToLowerInvariant();

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
    }
}
