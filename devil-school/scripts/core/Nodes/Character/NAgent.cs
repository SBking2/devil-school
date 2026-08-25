
using System;
using System.Collections.Generic;
using Godot;

namespace EGame
{
    public partial class NAgent : CharacterBody3D, INCharacter
    {
        public static NAgent Create(Creature data)
        {
            var prefab = data.CreateAgent();
            prefab.Data = data;
            prefab.Data.OnAgentCreated(prefab);
            return prefab;
        }

        public Creature Data { get; private set; }

        //模拟角色的输入
        public AgentIntent Intent { get; private set; } = new AgentIntent();

        private AbstractAgentBehaviorNode _Root;

        private readonly HashSet<string> _PendingEvents = new HashSet<string>();

        private bool _Ticking;
        private bool _RestartRequested;

        // 兜底：防止事件互相触发形成死循环
        private const int MaxRestartsPerTick = 8;
        public double DecisionInterval { get; set; } = 0.2;

        private double _DecisionTimer;

        private const float Gravity = 14f;
        private const float WalkSpeed = 3.5f;
        private const float MinStopSpeed = 2.54f;
        private const float Friction = 6f;
        private const float AccelerationRate = 10f;

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

            Tick(0);
            _DecisionTimer = 0;
        }

        public bool HasEvent(string eventName) => _PendingEvents.Contains(eventName);

        public override void _PhysicsProcess(double delta)
        {
            base._PhysicsProcess(delta);

            Vector3 horizontal = new Vector3(Velocity.X, 0, Velocity.Z);
            horizontal = ApplyFriction(horizontal, Friction, delta);
            horizontal = ApplyAcceleration(horizontal, Intent.WishDir, AccelerationRate, WalkSpeed, delta);
            Velocity = new Vector3(horizontal.X, Velocity.Y, horizontal.Z);

            if (!IsOnFloor())
                Velocity += Vector3.Down * Gravity * (float)delta;
            MoveAndSlide();

            if (_Root == null || _Ticking)
                return;

            _DecisionTimer += delta;
            if (_DecisionTimer < DecisionInterval)
                return;

            double elapsed = _DecisionTimer;
            _DecisionTimer = 0;
            Tick(elapsed);
        }

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

            curSpeed = curSpeed > MinStopSpeed ? curSpeed : MinStopSpeed;
            float drop = (float)(curSpeed * friction * dt);
            float totalSpeed = Mathf.Max(0f, curSpeed - drop);

            return source * (totalSpeed / curSpeed);
        }

        private void Tick(double dt)
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
                while (_RestartRequested && ++restarts < MaxRestartsPerTick);

                if (_RestartRequested)
                    GD.PushWarning($"NAgent({Name}) 行为树事件互相触发导致连续重启超过 {MaxRestartsPerTick} 次，本帧强制中止，请检查是否有事件循环触发");
            }
            finally
            {
                _Ticking = false;
                _PendingEvents.Clear();
            }
        }
    }
}