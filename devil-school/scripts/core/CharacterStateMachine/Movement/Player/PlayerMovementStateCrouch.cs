
using Godot;

namespace EGame
{
    public class PlayerMovementStateCrouch : PlayerMovementStateMoveBase
    {
        private const float CROUCH_SPEED_MULTIPLIER = 0.5f;

        public const float STAND_HEIGHT = 2.0f;
        public const float CROUCH_HEIGHT = 1.0f;

        // 胶囊体每秒能变化的高度，数值越大蹲下/起立的过渡越快
        private const float HEIGHT_CHANGE_SPEED = 4.0f;

        public override string StateName => "crouch";

        public override void OnEnter(CharacterMovementContext context)
        {
            context.Owner.SetAnimTrigger("crouch");
        }

        public override void OnUpdate(CharacterMovementContext context, double delta)
        {
            base.OnUpdate(context, delta);

            // 胶囊体已经完全恢复站立高度，才真正离开蹲伏状态；
            // 期间即便玩家已经松开蹲键，也要等身体真的"站起来"再切走
            if (Mathf.IsEqualApprox(context.Owner.ColliderHeight, STAND_HEIGHT))
                context.StateMachine.ChangeState(context.Owner.Intent.MoveDir.Length() > 0.1f ? "walk" : "idle");
        }

        public override void OnPhysicalUpdate(CharacterMovementContext context, double delta)
        {
            UpdateHeight(context, delta);

            var move = context.Owner.Intent.MoveDir.Normalized() * context.Owner.Data.CharacterModel.MoveSpeed * CROUCH_SPEED_MULTIPLIER;

            var velocity = context.Owner.Velocity;
            velocity.X = move.X;
            velocity.Z = move.Z;
            context.Owner.Velocity = velocity;

            context.Owner.MoveAndSlide();
        }

        private void UpdateHeight(CharacterMovementContext context, double delta)
        {
            var owner = context.Owner;

            // 想蹲、或者头顶没空间站起来，目标高度都保持蹲姿；只有既松开蹲键又确实有空间站起来，才允许朝站立高度变化
            float target_height = (owner.Intent.WantsCrouch || !CanStandUp(context)) ? CROUCH_HEIGHT : STAND_HEIGHT;

            float cur_height = owner.ColliderHeight;
            if (Mathf.IsEqualApprox(cur_height, target_height))
                return;

            float max_delta = HEIGHT_CHANGE_SPEED * (float)delta;
            float new_height = Mathf.MoveToward(cur_height, target_height, max_delta);
            float height_delta = new_height - cur_height;

            owner.ColliderHeight = new_height;

            // 胶囊体是围绕自身中心对称收缩的，蹲下时收缩量有一半会体现为"底部往上抬"。
            // 重力是逐帧累加的，加速度追不上这个收缩速度，如果不补偿，脚底会先离地悬空一小段时间，
            // 等重力慢慢把身体拽回地面，看起来就像凭空往下掉了一截。这里直接按收缩量的一半给一个向下的速度，
            // 让身体和胶囊体底部同步下降，脚底全程贴着地面。
            if (height_delta < 0f)
            {
                var velocity = owner.Velocity;
                velocity.Y = Mathf.Min(velocity.Y, height_delta / 2f / (float)delta);
                owner.Velocity = velocity;
            }
        }

        private bool CanStandUp(CharacterMovementContext context)
        {
            var owner = context.Owner;
            var spaceState = owner.GetWorld3D().DirectSpaceState;

            var from = owner.GlobalPosition + Vector3.Up * CROUCH_HEIGHT;
            var to = owner.GlobalPosition + Vector3.Up * STAND_HEIGHT;

            var query = PhysicsRayQueryParameters3D.Create(from, to);
            query.Exclude = new Godot.Collections.Array<Rid> { owner.GetRid() };

            var result = spaceState.IntersectRay(query);
            return result.Count == 0;
        }
    }
}
