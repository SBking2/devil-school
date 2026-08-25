
namespace EGame
{
    public abstract class AgentModel : CharacterModel
    {
        public abstract string PrefabPath { get;}

        protected virtual void BuildStateMachine(NAgent agent)
        {
            
        }
    }
}