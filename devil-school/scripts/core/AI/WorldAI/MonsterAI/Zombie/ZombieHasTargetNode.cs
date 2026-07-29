
namespace EGame
{
    public class ZombieHasTargetNode : WorldNodeCondition
    {
        public ZombieHasTargetNode() : base("has_chase_target")
        {

        }

        protected override bool CheckCondition(WorldBehaviorContext context)
        {
            return false;
        }
    }
}
