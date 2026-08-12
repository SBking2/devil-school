
namespace EGame
{
    public class CharacterMovementContext
    {
        public CharacterMovementContext(NEnvCreature creature, CharacterMovementStateMachine state_machine)
        {
            Owner = creature;
            StateMachine = state_machine;
        }

        public NEnvCreature Owner { get; }
        public CharacterMovementStateMachine StateMachine { get; }
    }
}
