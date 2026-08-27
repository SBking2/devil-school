
namespace EGame
{
    public abstract class AgentModel : CharacterModel
    {
        public abstract string PrefabPath { get; }

        public override void OnAgentCreated(NAgent agent)
        {
            agent.SetBehaviorTree(BuildBehaviorTree());
        }

        protected virtual AbstractAgentBehaviorNode BuildBehaviorTree()
        {
            return null;
        }
    }
}