
namespace EGame
{
    public abstract class CharacterModel : AbstractModel
    {
        public virtual int MaxHP => 10;
        public virtual int MoveSpeed => 300;

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