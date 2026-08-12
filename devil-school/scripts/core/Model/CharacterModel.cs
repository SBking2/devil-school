
using Godot;
using System;

namespace EGame
{
    public abstract class CharacterModel : AbstractModel
    {
        public virtual int MaxHP => 10;
        public virtual float MoveSpeed => 3;

        protected virtual string _VisualsPath => $"creature_visuals/" + ID.Entry.ToLowerInvariant();

        public CharacterMovementStateMachine MovementStateMachine
        {
            get
            {
                return _MovementStateMachine;
            }
            
            set
            {
                AssertMutable();
                if(_MovementStateMachine != null)
                {
                    throw new InvalidOperationException("character model already has the movement state-machine");
                }
                _MovementStateMachine = value;
            }
        }

        private CharacterMovementStateMachine _MovementStateMachine;

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
            MovementStateMachine = CreateMovementStateMachine(ncreature);
        }
        public virtual void OnWorldProcess(double delta)
        {
            if (MovementStateMachine != null)
                MovementStateMachine.OnUpdate(delta);
        }
        public virtual void OnWorldPhysicalProcess(double delta)
        {
            if (MovementStateMachine != null)
                MovementStateMachine.OnPhysicalUpdate(delta);
        }
        protected virtual CharacterMovementStateMachine CreateMovementStateMachine(NEnvCreature creature)
        {
            return null;
        }
    }
}
