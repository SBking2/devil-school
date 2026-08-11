
using System;

namespace EGame
{
    [ModelCategory]
    public class PlayerModel : CharacterModel
    {
        public PlayerMovementStateMachine MovementStateMachine
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
                    throw new InvalidOperationException("player model already has the movement state-machine");
                }
                _MovementStateMachine = value;
            }
        }

        private PlayerMovementStateMachine _MovementStateMachine;
        
        public override void SetUpForWorld(NEnvCreature ncreature)
        {
            base.SetUpForWorld(ncreature);
            MovementStateMachine = CreateMovementStateMachine(ncreature);
        }

        public override void OnWorldProcess(double delta)
        {
            base.OnWorldProcess(delta);
            if(_MovementStateMachine != null)
                _MovementStateMachine.OnUpdate(delta);
        }

        protected virtual PlayerMovementStateMachine CreateMovementStateMachine(NEnvCreature creature)
        {
            return null;
        }
    }
}