
using System;
using System.Collections.Generic;

namespace EGame
{
	[ModelCategory]
	public abstract class MonsterModel : AgentModel
	{
        public override string PrefabPath => "monster/" + ID.Entry.Slugify().ToLowerInvariant();
        
        protected override AbstractAgentBehaviourNode BuildBehaviourTree(BehaviourNodeFactory factory)
        {
            //玩家接近之后进入追逐状态
            MonsterBehaviourNodeChase chase = factory.Create<MonsterBehaviourNodeChase>();
            MonsterBehaviourNodeCheckPlayer check_player_chase = factory.Create<MonsterBehaviourNodeCheckPlayer>(chase);

            //idle-patrol队列：先把巡逻注释掉，只留 idle 站桩，测试视野锥用
            MonsterBehaviourNodeIdle idle = factory.Create<MonsterBehaviourNodeIdle>();
            //MonsterBehaviourNodePatrol patrol = factory.Create<MonsterBehaviourNodePatrol>();
            //AgentBehaviourSequence patrol_seq = new AgentBehaviourSequence(new List<AbstractAgentBehaviourNode>() { idle, patrol });

            //近战攻击：自己判距离，冷却没打完之前优先级压住 chase，不会打到一半又被交还出去
            MonsterBehaviourNodeAttack attack = factory.Create<MonsterBehaviourNodeAttack>();

            //挨打了优先播受伤、不能动
            MonsterBehaviourNodeHurt hurt = factory.Create<MonsterBehaviourNodeHurt>();

            //死了就永远待在这个分支，优先级比受伤还高
            MonsterBehaviourNodeDead dead = factory.Create<MonsterBehaviourNodeDead>();

            //选择 死亡 或者 受伤 或者 近战攻击 或者 追逐 或者 idle
            AgentBehaviourSelector root = new AgentBehaviourSelector(new List<AbstractAgentBehaviourNode>() { dead, hurt, attack, check_player_chase, idle });

            return root;
        }

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
