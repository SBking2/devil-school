
namespace EGame
{
    public class KnightBehaviourCheckPlayer : MonsterBehaviourNodeCheckPlayer
    {
        public KnightBehaviourCheckPlayer(AbstractAgentBehaviourNode child) : base(child)
        {
        }

        protected override float GetEnterRange()
        {
            return 15f;
        }

        protected override float GetExitRange()
        {
            return 18f;
        }
    }
}