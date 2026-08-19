
using Godot;
using System;

namespace EGame
{
    public abstract class CharacterModel : AbstractModel
    {
        public virtual int MaxHP => 10;
        public virtual float MoveSpeed => 3;
        public virtual float RunSpeed => 5;

        protected virtual string _VisualsPath => $"creature_visuals/" + ID.Entry.ToLowerInvariant();
        public virtual CreatureAnimator CreateAnimator(AnimationPlayer anim_player)
        {
            AnimState idle_state = new AnimState("idle_loop", 0.0f, true);
            CreatureAnimator animator = new CreatureAnimator(anim_player, idle_state);
            return animator;
        }
    }
}
