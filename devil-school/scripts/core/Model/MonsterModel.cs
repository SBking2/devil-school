
using System;
using System.Collections.Generic;

namespace EGame
{
	[ModelCategory]
	public abstract class MonsterModel : AgentModel
	{
        public override string PrefabPath => "monster/" + ID.Entry.ToLowerInvariant();
        
        protected override AbstractAgentBehaviorNode BuildBehaviorTree()
        {
            MonsterBehaviorNodeCheckPlayer check_player = new MonsterBehaviorNodeCheckPlayer();
            MonsterBehaviorNodeChase chase = new MonsterBehaviorNodeChase();

            AgentBehaviorSequence chase_seq = new AgentBehaviorSequence(new List<AbstractAgentBehaviorNode>() { check_player, chase});

            MonsterBehaviorNodeIdle idle = new MonsterBehaviorNodeIdle();
            MonsterBehaviorNodePatrol patrol = new MonsterBehaviorNodePatrol();

            AgentBehaviorSequence patrol_seq = new AgentBehaviorSequence(new List<AbstractAgentBehaviorNode>() { idle, patrol });

            AgentBehaviorSelector root = new AgentBehaviorSelector(new List<AbstractAgentBehaviorNode>() { chase_seq, patrol_seq });

            return root;
        }

        protected override CreatureAnimator BuildAnimator(INCharacter character)
        {
            AnimState idle_state = new AnimState("idle", 0.1f, true);
            AnimState walk_state = new AnimState("walk", 0.1f, true);

            idle_state.AddBranch(AnimationConfig.WalkTrigger, walk_state);
            walk_state.AddBranch(AnimationConfig.IdleTrigger, idle_state);

            CreatureAnimator animator = new CreatureAnimator(idle_state);
            return animator;
        }
    }
}
