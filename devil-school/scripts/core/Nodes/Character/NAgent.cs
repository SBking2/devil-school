
using System;
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
        public AgentIntent Intent { get; private set; }

        private AbstractAgentBehaviorNode _Root;

        public void SetBehaviorTree(AbstractAgentBehaviorNode root)
        {
            if (_Root != null)
                throw new InvalidOperationException("Agent already has a behavior tree!");

            _Root = root;
        }

        public override void _PhysicsProcess(double delta)
        {
            base._PhysicsProcess(delta);
            _Root?.Tick(this, delta);
        }
    }
}