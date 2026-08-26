
namespace EGame
{
    public abstract class AgentModel : CharacterModel
    {
        public abstract string PrefabPath { get; }

        public override void OnAgentCreated(NAgent agent)
        {
            agent.SetBehaviorTree(BuildBehaviorTree());
        }

        public override void OnCharacterCreated(INCharacter character)
        {
            var agent = character as NAgent;
            agent.BuildAnimator(BuildAnimator(character));
        }

        protected virtual AbstractAgentBehaviorNode BuildBehaviorTree()
        {
            return null;
        }
    }
}