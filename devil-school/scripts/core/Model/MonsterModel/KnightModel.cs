
namespace EGame
{
    public class KnightModel : MonsterModel
    {
        public override int MaxHP => 1000;

        protected override void RegisterNodeReplacements(BehaviourNodeFactory factory)
        {
            base.RegisterNodeReplacements(factory);
            factory.Replace<MonsterBehaviourNodeAttack, KnightBehaviourAttack>();
            factory.Replace<MonsterBehaviourNodeCheckPlayer, KnightBehaviourCheckPlayer>();
        }

        protected override CreatureAnimator BuildAnimator(INCharacter character)
        {
            AnimState idle_state = new AnimState("Armature|idle", 0.1f, true);
            AnimState walk_state = new AnimState("Armature|run", 0.1f, true);

            idle_state.AddBranch(AnimationConfig.WalkTrigger, walk_state);
            walk_state.AddBranch(AnimationConfig.IdleTrigger, idle_state);

            CreatureAnimator animator = new CreatureAnimator(idle_state);
            var hurt_state = new AnimState("Armature|impact_2");
            hurt_state.NextState = idle_state;
            animator.AddAnyBranch(AnimationConfig.HurtTrigger, hurt_state);

            //死亡不设置 NextState，播完就停在最后一帧，不会自动回到 idle
            var dead_state = new AnimState("Armature|death");
            animator.AddAnyBranch(AnimationConfig.DeadTrigger, dead_state);

            var attack_state = new AnimState("Armature|kick");
            attack_state.NextState = idle_state;
            animator.AddAnyBranch(AnimationConfig.AttackTrigger, attack_state);

            return animator;
        }
    }
}