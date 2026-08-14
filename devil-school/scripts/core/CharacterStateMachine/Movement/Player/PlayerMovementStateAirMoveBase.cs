
using Godot;

namespace EGame
{
    public abstract class PlayerMovementStateAirMoveBase : PlayerMovementStateMoveBase
    {
        // 空中加速度，越小转向越拖泥带水，越大越接近地面手感
        private const float AIR_ACCELERATE = 2.0f;

        public override void OnPhysicalUpdate(CharacterMovementContext context, double delta)
        {
            ApplyAirAccelerate(context, delta);
            context.Owner.MoveAndSlide();
        }

        // Jump/Fall共用：随时可能落地，不一定非要经过Fall再落地
        protected bool TryLandOnGround(CharacterMovementContext context)
        {
            if (context.Owner.IsGround == false)
                return false;

            bool wants_run = context.Owner.Intent.WantsRun && context.Owner.IsCrouching == false;
            string next = context.Owner.Intent.MoveDir.Length() > 0.1f ? (wants_run ? "run" : "walk") : "idle";
            context.StateMachine.ChangeState(next);
            return true;
        }

        // Quake式加速度：只限制速度在wishdir方向上的投影，垂直分量不受影响，靠这个连续变向才能越跑越快
        private void ApplyAirAccelerate(CharacterMovementContext context, double delta)
        {
            var owner = context.Owner;
            var wishdir = owner.Intent.MoveDir;
            float wishspeed = ApplyCrouchMultiplier(context, owner.Data.CharacterModel.MoveSpeed);

            var horizontal = new Vector3(owner.Velocity.X, 0f, owner.Velocity.Z);
            float current_speed = horizontal.Dot(wishdir);
            float add_speed = wishspeed - current_speed;

            if (add_speed > 0f)
            {
                float accel_speed = Mathf.Min(AIR_ACCELERATE * wishspeed * (float)delta, add_speed);
                horizontal += wishdir * accel_speed;
            }

            var velocity = owner.Velocity;
            velocity.X = horizontal.X;
            velocity.Z = horizontal.Z;
            owner.Velocity = velocity;
        }
    }
}
