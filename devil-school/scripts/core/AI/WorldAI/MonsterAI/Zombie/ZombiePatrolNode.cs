
using Godot;

namespace EGame
{
    public class ZombiePatrolNode : WorldNodeAction
    {
        private Vector3 _PatrolTargetPoint = Vector3.Zero;

        public ZombiePatrolNode() : base("patrol")
        {

        }

        public override void OnEnter(WorldBehaviorContext context)
        {
            //选择巡逻目标
            var x = Rng.RealRandom.RangeFloat(-20f, 20f);
            var z = Rng.RealRandom.RangeFloat(-20f, 20f);
            _PatrolTargetPoint = new Vector3(x, 0.0f, z);
            
            context.Owner.TargetMoveDir = (_PatrolTargetPoint - context.Owner.GlobalPosition).Normalized();
        }
        
        protected override WorldBehaviorStatus OnTick(WorldBehaviorContext context)
        {
            //巡逻,抵达目标即完成
            if ((context.Owner.GlobalPosition -_PatrolTargetPoint).Length() < 0.1f)
                return WorldBehaviorStatus.Success;
            return WorldBehaviorStatus.Running;
        }

        public override void OnExit(WorldBehaviorContext context)
        {
            context.Owner.TargetMoveDir = Vector3.Zero;
        }
    }
}
