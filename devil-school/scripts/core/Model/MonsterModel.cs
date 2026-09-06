
using System;
using System.Collections.Generic;

namespace EGame
{
	[ModelCategory]
	public abstract class MonsterModel : AgentModel
	{
        public override string PrefabPath => "monster/" + ID.Entry.Slugify().ToLowerInvariant();
        
        protected override AbstractAgentBehaviorNode BuildBehaviorTree()
        {
            //玩家接近之后进入追逐状态
            MonsterBehaviorNodeChase chase = new MonsterBehaviorNodeChase();
            MonsterBehaviorNodeCheckPlayer check_player_chase = new MonsterBehaviorNodeCheckPlayer(chase);

            //idle-patrol队列
            MonsterBehaviorNodeIdle idle = new MonsterBehaviorNodeIdle();
            MonsterBehaviorNodePatrol patrol = new MonsterBehaviorNodePatrol();
            AgentBehaviorSequence patrol_seq = new AgentBehaviorSequence(new List<AbstractAgentBehaviorNode>() { idle, patrol });

            //近战攻击：自己判距离，冷却没打完之前优先级压住 chase，不会打到一半又被交还出去
            MonsterBehaviorNodeAttack attack = new MonsterBehaviorNodeAttack();

            //挨打了优先播受伤、不能动
            MonsterBehaviorNodeHurt hurt = new MonsterBehaviorNodeHurt();

            //死了就永远待在这个分支，优先级比受伤还高
            MonsterBehaviorNodeDead dead = new MonsterBehaviorNodeDead();

            //选择 死亡 或者 受伤 或者 近战攻击 或者 追逐 或者 (idle-patrol)
            AgentBehaviorSelector root = new AgentBehaviorSelector(new List<AbstractAgentBehaviorNode>() { dead, hurt, attack, check_player_chase, patrol_seq });

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
