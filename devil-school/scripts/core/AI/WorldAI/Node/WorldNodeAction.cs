
namespace EGame
{
    public abstract class WorldNodeAction : AbstractWorldBehaviorNode
    {
        private readonly string _NodeID;

        public override string ID => _NodeID;

        protected WorldNodeAction(string id)
        {
            _NodeID = id;
        }
    }
}
