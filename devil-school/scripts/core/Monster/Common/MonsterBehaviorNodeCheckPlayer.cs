
namespace EGame
{
    public class MonsterBehaviorNodeCheckPlayer : AbstractAgentBehaviorDecorator
    {
        private const float _ChaseEnterRange = 5f;
        private const float _ChaseExitRange = 10f;

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
            float threshold = _IsChasing ? _ChaseExitRange : _ChaseEnterRange;
            _IsChasing = distance <= threshold;
            return _IsChasing;
        }
    }
}
