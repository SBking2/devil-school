
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
            //追逐到接近之后停止
            MonsterBehaviorNodeChase chase = new MonsterBehaviorNodeChase();
            MonsterBehaviorNodeCheckClosePlayer check_close_node = new MonsterBehaviorNodeCheckClosePlayer(chase);
            MonsterBehaviorNodeIdle idle_node = new MonsterBehaviorNodeIdle();
            AgentBehaviorSelector close_selector = new AgentBehaviorSelector(new List<AbstractAgentBehaviorNode>() { check_close_node, idle_node });

            //玩家接近之后进入追逐状态
            MonsterBehaviorNodeCheckPlayer check_player_chase = new MonsterBehaviorNodeCheckPlayer(close_selector);

            //idle-patrol队列
            MonsterBehaviorNodeIdle idle = new MonsterBehaviorNodeIdle();
            MonsterBehaviorNodePatrol patrol = new MonsterBehaviorNodePatrol();
            AgentBehaviorSequence patrol_seq = new AgentBehaviorSequence(new List<AbstractAgentBehaviorNode>() { idle, patrol });

            //选择 (chase-close) 或者 (idle-patrol)
            AgentBehaviorSelector root = new AgentBehaviorSelector(new List<AbstractAgentBehaviorNode>() { check_player_chase, patrol_seq });

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
