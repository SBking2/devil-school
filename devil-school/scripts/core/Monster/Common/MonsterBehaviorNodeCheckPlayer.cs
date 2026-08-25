
namespace EGame
{
    public class MonsterBehaviorNodeCheckPlayer : AbstractAgentBehaviorCondition
    {
        private const float ChaseEnterRange = 5f;
        private const float ChaseExitRange = 10f;

        private bool _IsChasing;

        protected override bool Check(NAgent agent, double dt)
        {
            var player = NGame.Instance?.PlayerNode;
            if (player == null)
            {
                _IsChasing = false;
                return false;
            }

            float distance = agent.GlobalPosition.DistanceTo(player.GlobalPosition);
            float threshold = _IsChasing ? ChaseExitRange : ChaseEnterRange;
            _IsChasing = distance <= threshold;
            return _IsChasing;
        }
    }
}
