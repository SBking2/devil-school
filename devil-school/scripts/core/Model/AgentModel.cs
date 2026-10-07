
namespace EGame
{
    public abstract class AgentModel : CharacterModel
    {
        public abstract string PrefabPath { get; }

        public override void OnAgentCreated(NAgent agent)
        {
            BehaviourNodeFactory factory = new BehaviourNodeFactory();
            RegisterNodeReplacements(factory);
            agent.SetBehaviourTree(BuildBehaviourTree(factory));
        }

        // 子类用 factory.Replace<旧, 新>() 声明节点替换，重写时先调 base 带上父类的替换
        protected virtual void RegisterNodeReplacements(BehaviourNodeFactory factory) { }

        protected virtual AbstractAgentBehaviourNode BuildBehaviourTree(BehaviourNodeFactory factory)
        {
            return null;
        }
    }
}