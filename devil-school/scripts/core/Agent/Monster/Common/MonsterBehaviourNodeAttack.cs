
using Godot;

namespace EGame
{
    // 一次完整的攻击 = 前摇 → 出手 → 冷却，期间决策一直返回 Running，不重新判距离，
    // 不然玩家一后退，攻击到一半就被 Selector 交还给 chase。打完返回 Success，下一次决策才重新问"玩家还在范围内吗"
    public class MonsterBehaviourNodeAttack : AbstractAgentBehaviourNode
    {
        private double _Timer;
        private bool _IsWindingUp;
        private bool _IsFinished;

        protected override BehaviourStatus OnDecide(NAgent agent)
        {
            if (IsActive)
                return _IsFinished ? BehaviourStatus.Success : BehaviourStatus.Running;

            var player = NGame.Instance?.PlayerNode;
            if (player == null)
                return BehaviourStatus.Failure;

            float distance = agent.GlobalPosition.DistanceTo(player.GlobalPosition);
            return distance > GetExitRange() ? BehaviourStatus.Failure : BehaviourStatus.Running;
        }

        protected override void OnEnter(NAgent agent)
        {
            agent.AnimTrigger(AnimationConfig.AttackTrigger);
            _IsWindingUp = true;
            _IsFinished = false;
            _Timer = GetWindupTime();
        }

        protected override void OnProcess(NAgent agent, double dt)
        {
            agent.Intent.WishDir = Vector3.Zero;
            agent.Velocity = new Vector3(0, agent.Velocity.Y, 0);

            if (_IsFinished)
                return;

            TurnToPlayer(agent, dt);

            _Timer -= dt;
            if (_Timer > 0)
                return;

            if (_IsWindingUp)
            {
                _IsWindingUp = false;
                FireMelee(agent);
                _Timer = GetCooldown();
                return;
            }

            _IsFinished = true;
            agent.RequestDecision();
        }

        // 窗口期按进入攻击后的时间算，窗口外朝向保持不变
        private void TurnToPlayer(NAgent agent, double dt)
        {
            if (RunningTime < GetTurnStartTime() || RunningTime >= GetTurnEndTime())
                return;

            var player = NGame.Instance?.PlayerNode;
            if (player == null)
                return;

            agent.TurnToward(player.GlobalPosition - agent.GlobalPosition, GetTurnRate(), dt);
        }

        private void FireMelee(NAgent agent)
        {
            Vector3 forward = -agent.GlobalTransform.Basis.Z;
            var target = CollisionDetection.FindMeleeTarget(agent.GetWorld3D(), GetAttackPos(agent), forward, GetRange(), CollisionMask.PlayerMask);
            if (target == null)
                return;

            var damageInfo = new DamageInfo(agent, target, target.GlobalPosition, Vector3.Up, agent.Data, GetDamage());
            DamageSystem.Instance.ReportHit(damageInfo);
        }

        protected virtual float GetRange()
        {
            return 1f;
        }

        protected virtual float GetExitRange()
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

        protected virtual float GetWindupTime()
        {
            return 0.5f;
        }

        // 转向窗口：进入攻击后 [起点, 终点) 秒内朝向玩家，默认是整个前摇
        protected virtual float GetTurnStartTime()
        {
            return 0f;
        }

        protected virtual float GetTurnEndTime()
        {
            return GetWindupTime();
        }

        protected virtual float GetTurnRate()
        {
            return 8f;
        }

        protected virtual Vector3 GetAttackPos(NAgent agent)
        {
            return agent.GlobalPosition;
        }
    }
}
