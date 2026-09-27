using Godot;

namespace EGame
{
    // 发现玩家：距离 + 视野锥角度都满足才算"看见"；已经在追的就只看退出距离，不再管角度——
    // 不然怪物转身/玩家绕到侧后方，追到一半会被判定成"看不见了"，手感很怪
    public class MonsterBehaviorNodeCheckPlayer : AbstractAgentBehaviorDecorator
    {
        private const float _ChaseEnterRange = 5f;
        private const float _ChaseExitRange = 6f;
        private const float _ViewAngleDegrees = 120f;    // 视野锥总张角

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

            if (_IsChasing)
            {
                _IsChasing = distance <= _ChaseExitRange;
                return _IsChasing;
            }

            if (distance > _ChaseEnterRange)
                return false;

            Vector3 toPlayer = player.GlobalPosition - agent.GlobalPosition;
            toPlayer.Y = 0;

            if (toPlayer.LengthSquared() < 0.0001f)
            {
                _IsChasing = true;    // 贴脸了，不用算角度
                return true;
            }

            Vector3 forward = -agent.GlobalTransform.Basis.Z;
            float angle_degrees = Mathf.RadToDeg(forward.AngleTo(toPlayer));
            _IsChasing = angle_degrees <= _ViewAngleDegrees * 0.5f;
            return _IsChasing;
        }
    }
}
