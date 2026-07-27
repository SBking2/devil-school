
using System.Threading.Tasks;
using Godot;

namespace EGame
{
    public class ZombieModel : MonsterModel
    {
        public override MonsterMoveStateMachine CreateMoveStateMachine()
        {
            MoveState chase_state = new MoveState("chase", (creatures) =>
            {
                return Task.CompletedTask;
            });

            MoveState patrol_state = new MoveState("patrol", (creatures) =>
            {
                return Task.CompletedTask;
            });

            MonsterMoveStateMachine state_machine = new MonsterMoveStateMachine(null, null);
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