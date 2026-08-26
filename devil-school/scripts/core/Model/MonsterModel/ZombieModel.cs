
namespace EGame
{
    public class ZombieModel : MonsterModel
    {
        public override float MoveSpeed => 1.5f;

        protected override CreatureAnimator BuildAnimator(INCharacter character)
        {
            AnimState idle_state = new AnimState("Zombie_Idle", 0.1f, true);
            AnimState walk_state = new AnimState("Zombie_Walk_Fwd", 0.1f, true);

            idle_state.AddBranch(AnimationConfig.WalkTrigger, walk_state);
            walk_state.AddBranch(AnimationConfig.IdleTrigger, idle_state);

            CreatureAnimator animator = new CreatureAnimator(idle_state);
            return animator;
        }
    }
}