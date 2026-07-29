
using Godot;

namespace EGame
{
    public abstract class CharacterModel : AbstractModel
    {
        public virtual int MaxHP => 10;
        public virtual float MoveSpeed => 3;

        protected virtual string _VisualsPath => $"creature_visuals/" + ID.Entry.ToLowerInvariant();

        public NCreatureVisual CreateVisual()
        {
            return SceneHelper.LoadScene<NCreatureVisual>(_VisualsPath);
        }
            
        public virtual CreatureAnimator CreateAnimator(AnimationPlayer anim_player)
        {
            AnimState idle_state = new AnimState("idle_loop", 0.0f, true);
            CreatureAnimator animator = new CreatureAnimator(anim_player, idle_state);
            return animator;
        }

        public virtual void SetUpForWorld(NEnvCreature ncreature)
        {

        }
    }
}