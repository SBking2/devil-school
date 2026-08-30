
using Godot;

namespace EGame
{
    // 冷却没打完之前不重新判距离——不然玩家一后退，攻击到一半就被 Selector 交还给 chase，
    // 表现出来就是"攻击还没结束就又能动了"。只有冷却真正打完那一刻才重新问一次"玩家还在范围内吗"
    public class MonsterBehaviorNodeAttack : AbstractAgentBehaviorNode
    {
        private double _CooldownTimer;

        protected override BehaviorStatus OnTick(NAgent agent, double dt)
        {
            _CooldownTimer -= dt;
            if (_CooldownTimer > 0)
            {
                agent.Intent.WishDir = Vector3.Zero;
                agent.Velocity = new Vector3(0, agent.Velocity.Y, 0);
                return BehaviorStatus.Running;
            }

            var player = NGame.Instance?.PlayerNode;
            if (player == null)
                return BehaviorStatus.Failure;

            float distance = agent.GlobalPosition.DistanceTo(player.GlobalPosition);
            if (distance > GetRange())
                return BehaviorStatus.Failure;

            agent.Intent.WishDir = Vector3.Zero;
            agent.Velocity = new Vector3(0, agent.Velocity.Y, 0);

            agent.AnimTrigger(AnimationConfig.AttackTrigger);
            FireMelee(agent);

            _CooldownTimer = GetCooldown();
            return BehaviorStatus.Running;
        }

        private void FireMelee(NAgent agent)
        {
            Vector3 forward = -agent.GlobalTransform.Basis.Z;
            var target = MeleeDetection.FindTarget(agent.GetWorld3D(), agent.GlobalPosition, forward, GetRange(), CollisionMask.PlayerMask);
            if (target == null)
                return;

            var damageInfo = new DamageInfo(target, target.GlobalPosition, Vector3.Up, agent.Data, GetDamage());
            DamageSystem.Instance.ReportHit(damageInfo);
        }

        protected virtual float GetRange()
        {
            return 2f;
        }

        protected virtual int GetDamage()
        {
            return 2;
        }

        protected virtual float GetCooldown()
        {
            return 1.5f;
        }
    }
}
