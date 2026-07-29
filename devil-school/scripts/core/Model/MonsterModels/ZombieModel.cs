
using Godot;

namespace EGame
{
    public class ZombieModel : MonsterModel
    {
        public override CreatureAnimator CreateAnimator(AnimationPlayer anim_player)
        {
            AnimState idle_state = new AnimState("Zombie_Idle", 0.1f, true);
            AnimState walk_state = new AnimState("Zombie_Walk_Fwd", 0.1f, true);
            
            idle_state.AddBranch("walk", walk_state);
            walk_state.AddBranch("idle", idle_state);

            CreatureAnimator animator = new CreatureAnimator(anim_player, idle_state);
            return animator;
        }

        protected override WorldBehaviorTree CreateWorldBehaviorTree(NEnvCreature ncreature)
        {
            WorldNodeSequence chase_sequence = new WorldNodeSequence("chase_sequence");
            chase_sequence.AddBranch(new ZombieHasTargetNode());
            chase_sequence.AddBranch(new ZombieChaseNode());

            WorldNodeSequence idle_patrol_sequence = new WorldNodeSequence("idle_patrol_sequence");
            idle_patrol_sequence.AddBranch(new ZombieIdleNode());
            idle_patrol_sequence.AddBranch(new ZombiePatrolNode());

            WorldNodeSelector root = new WorldNodeSelector("zombie_world_ai");
            root.AddBranch(chase_sequence);
            root.AddBranch(idle_patrol_sequence);

            return new WorldBehaviorTree(root);
        }

        protected override void LoadSensor(NEnvCreature ncreature)
        {
            base.LoadSensor(ncreature);
            ncreature.AddSensor(NVisualSensor.Create(ncreature));
        }
    }
}
