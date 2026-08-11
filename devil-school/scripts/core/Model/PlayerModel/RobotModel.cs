
using Godot;

namespace EGame
{
    public class RobotModel : PlayerModel
    {
        public override float MoveSpeed => 3f;

        public override CreatureAnimator CreateAnimator(AnimationPlayer anim_player)
        {
            AnimState idle_state = new AnimState("ArmsRig|finger_gun_idle", 0.1f, true);
            //AnimState walk_state = new AnimState("Zombie_Walk_Fwd", 0.1f, true);

            //idle_state.AddBranch("walk", walk_state);
            //walk_state.AddBranch("idle", idle_state);

            CreatureAnimator animator = new CreatureAnimator(anim_player, idle_state);
            return animator;
        }
    }
}