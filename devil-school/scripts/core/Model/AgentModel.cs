
namespace EGame
{
    public abstract class AgentModel : CharacterModel
    {
        public abstract string PrefabPath { get; }

        // 具体 Model 子类覆盖这个方法，返回自己的行为树根节点——
        // 不覆盖就保持 null，agent 会是一个"挂着但什么都不做"的空壳，不会报错
        protected virtual AbstractAgentBehaviorNode BuildBehaviorTree(NAgent agent)
        {
            return null;
        }

        public virtual void OnAgentCreated(NAgent agent)
        {
            agent.SetBehaviorTree(BuildBehaviorTree(agent));
        }
    }
}