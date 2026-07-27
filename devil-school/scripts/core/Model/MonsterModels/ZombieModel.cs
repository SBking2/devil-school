
using System.Threading.Tasks;
using Godot;

namespace EGame
{
    public class ZombieModel : MonsterModel
    {
        public override TurnMoveStateMachine CreateTurnMoveStateMachine()
        {
            TurnStateMove chase_state = new TurnStateMove("chase", (creatures) =>
            {
                return Task.CompletedTask;
            });

            TurnStateMove patrol_state = new TurnStateMove("patrol", (creatures) =>
            {
                return Task.CompletedTask;
            });

            TurnMoveStateMachine state_machine = new TurnMoveStateMachine(null, null);
            return state_machine;
        }

        public override CreatureAnimator CreateAnimator(AnimationPlayer anim_player)
        {
            AnimState idle_state = new AnimState("Zombie_Idle", 0.1f, true);
            AnimState walk_state = new AnimState("Zombie_Walk_Fwd", 0.1f, true);

            idle_state.AddBranch("walk", walk_state);
            walk_state.AddBranch("idle", idle_state);

            CreatureAnimator animator = new CreatureAnimator(anim_player, idle_state);
            return animator;
        }
    }
}
