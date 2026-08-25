
using System;
using System.Collections.Generic;

namespace EGame
{
	[ModelCategory]
	public abstract class MonsterModel : AgentModel
	{
        public override string PrefabPath => "monster/" + ID.Entry.ToLowerInvariant();

        protected override AbstractAgentBehaviorNode BuildBehaviorTree(NAgent agent)
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
    }
}
