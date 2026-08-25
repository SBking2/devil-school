
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
            return prefab;
        }

        public Creature Data { get; private set; }

        //模拟角色的输入
        public AgentIntent Intent { get; private set; }

        private Dictionary<string, AbstractAgentState> _StateDic;

        public void BuildStateMachine(IEnumerable<AbstractAgentState> states, AbstractAgentState init_state)
        {
            if(_StateDic != null)
                throw new InvalidOperationException("Agent already has state-machine!");

            _StateDic = new Dictionary<string, AbstractAgentState>();

            foreach(var state in states)
                _StateDic.Add(state.StateName, state);

            if (_StateDic.ContainsKey(init_state.StateName) == false)
                throw new InvalidOperationException("init-state does't exist in state-dic!");

            _CurState = init_state;
            _CurState.OnEnter(this);
        }

        protected AbstractAgentState _CurState;

        public void ChangeState(string name)
        {
            AbstractAgentState state = null;
            _StateDic.TryGetValue(name, out state);

            if(state != null)
            {
                _CurState.OnExit(this);
                _CurState = state;
                _CurState.OnEnter(this);
            }
        }

        public override void _Process(double delta)
        {
            base._Process(delta);
            _CurState.OnProcess(this, delta);
        }

        public override void _PhysicsProcess(double delta)
        {
            base._PhysicsProcess(delta);
            _CurState.OnPhysicalProcess(this, delta);
        }
    }
}