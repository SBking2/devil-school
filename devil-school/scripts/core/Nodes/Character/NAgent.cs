
using System;
using System.Collections.Generic;
using Godot;

namespace EGame
{
    public partial class NAgent : CharacterBody3D, INCharacter
    {
        public static NAgent Create(AgentModel data)
        {
            var prefab = SceneHelper.LoadScene<NAgent>(data.PrefabPath);
            prefab._AgentModel = data;
            prefab.Data.OnCharacterCreated(prefab);
            prefab.Data.OnAgentCreated(prefab);
            return prefab;
        }

        public CharacterModel Data => _AgentModel as CharacterModel;
        private AgentModel _AgentModel;

        public void TakeDamage(DamageInfo info)
        {
            Data.HP -= info.Amount;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////
        ////////                                    Intent 意图机制
        ///////////////////////////////////////////////////////////////////////////////////////////////////////

        //模拟角色的输入
        public AgentIntent Intent { get; private set; } = new AgentIntent();
        public Blackboard Blackboard { get; private set; } = new Blackboard();

        private void OnIntentMovingChanged(Vector3 old_vel, Vector3 new_vel)
        {
            if (old_vel != Vector3.Zero && new_vel == Vector3.Zero)
                AnimTrigger(AnimationConfig.IdleTrigger);

            else if (old_vel == Vector3.Zero && new_vel != Vector3.Zero)
                AnimTrigger(AnimationConfig.WalkTrigger);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////
        ////////                                    Movement
        ///////////////////////////////////////////////////////////////////////////////////////////////////////

        private const float _Gravity = 14f;
        private float WalkSpeed => Data.MoveSpeed;
        private const float _MinStopSpeed = 2.54f;
        private const float _Friction = 6f;
        private const float _AccelerationRate = 10f;
        private const float _RotationRate = 10f;

        private Vector3 ApplyAcceleration(Vector3 source, Vector3 wishDir, float accelerationRate, float moveSpeed, double dt)
        {
            if (wishDir.LengthSquared() < 0.0001f)
                return source;

            float velProj = source.Dot(wishDir) / wishDir.Length();
            float addSpeed = moveSpeed - velProj;
            float trueAddSpeed = Mathf.Min((float)(addSpeed * dt * accelerationRate), addSpeed);

            return source + wishDir * trueAddSpeed;
        }

        private Vector3 ApplyFriction(Vector3 source, float friction, double dt)
        {
            float curSpeed = source.Length();
            if (curSpeed < 0.001f)
                return Vector3.Zero;

            curSpeed = curSpeed > _MinStopSpeed ? curSpeed : _MinStopSpeed;
            float drop = (float)(curSpeed * friction * dt);
            float totalSpeed = Mathf.Max(0f, curSpeed - drop);

            return source * (totalSpeed / curSpeed);    
        }

        /// <summary>
        /// 面朝向朝向移动方向
        /// </summary>
        private Quaternion ApplyFaceRota(Quaternion cur, Vector3 velocity, double dt)
        {
            var horizontal_vel = velocity;
            horizontal_vel.Y = 0f;

            if (horizontal_vel.LengthSquared() < 0.0001f)
                return cur;   // 没有水平移动，保持当前朝向，避免除零/朝向乱转

            var target = Basis.LookingAt(horizontal_vel, Vector3.Up).GetRotationQuaternion();
            float weight = 1f - Mathf.Exp(-_RotationRate * (float)dt);   // 跟你项目里蹲伏/相机那套指数衰减插值是同一个写法
            return cur.Slerp(target, weight);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////
        ////////                                    Animator
        ///////////////////////////////////////////////////////////////////////////////////////////////////////

        private CreatureAnimator _Animator;

        public void BuildAnimator(CreatureAnimator animator)
        {
            if (animator == null)
                return;

            var player = FindChild("AnimationPlayer", recursive: true, owned: false) as AnimationPlayer;
            if (player == null)
                return;

            if (_Animator != null)
                throw new InvalidOperationException("Animator already had value!");

            _Animator = animator;
            _Animator.SetPlayer(player);
        }

        public void AnimTrigger(string trigger)
        {
            _Animator?.CallTrigger(trigger);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////
        ////////                                    行为树
        ///////////////////////////////////////////////////////////////////////////////////////////////////////

        private AbstractAgentBehaviorNode _Root;

        private readonly HashSet<string> _PendingEvents = new HashSet<string>();

        private bool _Ticking;
        private bool _RestartRequested;

        // 兜底：防止事件互相触发形成死循环
        private const int _MaxRestartsPerTick = 8;
        public double DecisionInterval { get; set; } = 0.2;

        private double _DecisionTimer;
        public bool HasEvent(string eventName) => _PendingEvents.Contains(eventName);
        public void SetBehaviorTree(AbstractAgentBehaviorNode root)
        {
            if (_Root != null)
                throw new InvalidOperationException("Agent already has a behavior tree!");

            _Root = root;
        }

        public void NotifyEvent(string eventName)
        {
            _PendingEvents.Add(eventName);
            if (_Root == null)
                return;

            if (_Ticking)
            {
                _RestartRequested = true;
                return;
            }

            TreeTick(0);
            _DecisionTimer = 0;
        }

        private void TreeTick(double dt)
        {
            _Ticking = true;
            try
            {
                int restarts = 0;
                do
                {
                    _RestartRequested = false;
                    _Root.Tick(this, dt);
                    dt = 0; // 重启的这几遍不重复消耗时间
                }
                while (_RestartRequested && ++restarts < _MaxRestartsPerTick);

                if (_RestartRequested)
                    GD.PushWarning($"NAgent({Name}) 行为树事件互相触发导致连续重启超过 {_MaxRestartsPerTick} 次，本帧强制中止，请检查是否有事件循环触发");
            }
            finally
            {
                _Ticking = false;
                _PendingEvents.Clear();
            }
        }

        private void OnTreeProcess(double dt)
        {
            if (_Root == null || _Ticking)
                return;

            _DecisionTimer += dt;
            if (_DecisionTimer < DecisionInterval)
                return;

            double elapsed = _DecisionTimer;
            _DecisionTimer = 0;
            TreeTick(elapsed);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////
        public override void _Ready()
        {
            base._Ready();
            Intent.OnWishDirChanged += OnIntentMovingChanged;
        }

        public override void _PhysicsProcess(double delta)
        {
            base._PhysicsProcess(delta);

            OnTreeProcess(delta);

            Vector3 horizontal = new Vector3(Velocity.X, 0, Velocity.Z);
            horizontal = ApplyFriction(horizontal, _Friction, delta);
            horizontal = ApplyAcceleration(horizontal, Intent.WishDir, _AccelerationRate, WalkSpeed, delta);
            Velocity = new Vector3(horizontal.X, Velocity.Y, horizontal.Z);

            if (!IsOnFloor())
                Velocity += Vector3.Down * _Gravity * (float)delta;
            else
                this.Quaternion = ApplyFaceRota(this.Quaternion, Velocity, delta);

            MoveAndSlide();
        }
    }
}