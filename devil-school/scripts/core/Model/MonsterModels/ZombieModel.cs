
using Godot;

namespace EGame
{
    public class ZombieModel : MonsterModel
    {
        public override float MoveSpeed => 1f;

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
            chase_sequence.AddBranch(new ZombieIdleNode());

            WorldNodeSequence idle_patrol_sequence = new WorldNodeSequence("idle_patrol_sequence");
            idle_patrol_sequence.AddBranch(new ZombieIdleNode());
            idle_patrol_sequence.AddBranch(new ZombiePatrolNode());

            WorldNodeSelector selector = new WorldNodeSelector("zombie_world_ai");
            selector.AddBranch(chase_sequence);
            selector.AddBranch(idle_patrol_sequence);

            WorldNodeRepeat root = new WorldNodeRepeat("repeat", selector);

            var tree = new WorldBehaviorTree(root, ncreature);

            ncreature.AddSensor(NVisualSensor.Create(ncreature
                , NVisualSensor.SensorShape.Sphere
                , (int)LayerManager.Layer.Creature
                , (int)LayerManager.Layer.Creature
                ,(creatures)=>
                {
                    NEnvCreature closet_creature = null;
                    float min_dis = 0f;
                    foreach (NEnvCreature creature in creatures)
                    {
                        if(creature.Data.Side == CombatSide.Player)
                        {
                            if (closet_creature != null)
                            {
                                float distance = (creature.GlobalPosition - ncreature.GlobalPosition).Length();
                                if (distance < min_dis)
                                {
                                    closet_creature = creature;
                                    min_dis = distance;
                                }
                            }
                            else
                                closet_creature = creature;
                        }
                    }

                    if(closet_creature != null)
                        tree.NotifyEvent(WorldAIEvent.FindTarget, closet_creature as object);
                    else
                        tree.NotifyEvent(WorldAIEvent.MissingTarget);
                }));

            return tree;
        }
    }
}
