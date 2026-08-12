
namespace EGame
{
    [ModelCategory]
    public class PlayerModel : CharacterModel
    {
        protected override CharacterMovementStateMachine CreateMovementStateMachine(NEnvCreature creature)
        {
            PlayerMovementStateIdle idle_state = new PlayerMovementStateIdle();
            PlayerMovementStateWalk walk_state = new PlayerMovementStateWalk();

            CharacterMovementStateMachine state_machine = new CharacterMovementStateMachine(
                creature
                , new AbstractCharacterMovementState[]
                {
                    idle_state,
                    walk_state
                }
                , idle_state);

            return state_machine;
        }
    }
}
