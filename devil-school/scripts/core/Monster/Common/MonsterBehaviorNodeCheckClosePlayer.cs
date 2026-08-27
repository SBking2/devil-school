
namespace EGame
{
    public class MonsterBehaviorNodeCheckClosePlayer : AbstractAgentBehaviorDecorator
    {
        private const float _IdleEnterRange = 1.5f;
        private const float _IdleExitRange = 2f;

        private bool _IsIdling;

        public MonsterBehaviorNodeCheckClosePlayer(AbstractAgentBehaviorNode child) : base(child)
        {

        }

        protected override bool ShouldRun(NAgent agent, double dt)
        {
            var player = NGame.Instance?.PlayerNode;
            if (player == null)
            {
                _IsIdling = false;
                return false;
            }

            float distance = agent.GlobalPosition.DistanceTo(player.GlobalPosition);
            float threshold = _IsIdling ? _IdleExitRange : _IdleEnterRange;
            _IsIdling = distance <= threshold;
            return _IsIdling;
        }
    }
}