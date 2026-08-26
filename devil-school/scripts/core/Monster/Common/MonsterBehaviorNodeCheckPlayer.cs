
namespace EGame
{
    public class MonsterBehaviorNodeCheckPlayer : AbstractAgentBehaviorDecorator
    {
        private const float ChaseEnterRange = 5f;
        private const float ChaseExitRange = 10f;

        private bool _IsChasing;

        public MonsterBehaviorNodeCheckPlayer(AbstractAgentBehaviorNode child) : base(child) { }

        protected override bool ShouldRun(NAgent agent, double dt)
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
