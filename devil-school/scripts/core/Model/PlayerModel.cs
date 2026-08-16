
namespace EGame
{
    [ModelCategory]
    public class PlayerModel : CharacterModel
    {
        public virtual float JumpSpeed => 6;

        protected override CharacterMovementStateMachine CreateMovementStateMachine(NEnvCreature creature)
        {
            PlayerMovementStateIdle idle_state = new PlayerMovementStateIdle();
            PlayerMovementStateWalk walk_state = new PlayerMovementStateWalk();
            PlayerMovementStateRun run_state = new PlayerMovementStateRun();
            PlayerMovementStateJump jump_state = new PlayerMovementStateJump();
            PlayerMovementStateFall fall_state = new PlayerMovementStateFall();

            CharacterMovementStateMachine state_machine = new CharacterMovementStateMachine(
                creature
                , new AbstractCharacterMovementState[]
                {
                    idle_state,
                    walk_state,
                    run_state,
                    jump_state,
                    fall_state
                }
                , idle_state);

            return state_machine;
        }
    }
}
