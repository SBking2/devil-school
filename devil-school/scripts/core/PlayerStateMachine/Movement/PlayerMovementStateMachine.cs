
using System;
using System.Collections.Generic;

namespace EGame
{
    public class PlayerMovementStateMachine
    {
        private PlayerMovementContext _Context;
        private AbstractPlayerMovementState _CurState;
        private Dictionary<string, AbstractPlayerMovementState> _StateMap = new Dictionary<string, AbstractPlayerMovementState>();
        public PlayerMovementStateMachine(NEnvCreature owner, IEnumerable<AbstractPlayerMovementState> states, AbstractPlayerMovementState init_state)
        {
            _Context = new PlayerMovementContext(owner);

            foreach (var state in states)
                _StateMap.Add(state.StateName, state);

            _CurState = init_state;
            _CurState?.OnEnter(_Context);
        }

        public void OnUpdate(double delta)
        {
            _CurState?.OnUpdate(_Context, delta);
        }

        public void ChangeState(string state_name)
        {
            AbstractPlayerMovementState state = null;
            if (_StateMap.TryGetValue(state_name, out state))
            {
                _CurState?.OnExit(_Context);
                _CurState = state;
                _CurState.OnEnter(_Context);
            }
            else
                throw new InvalidOperationException($"Unkown State {state_name}");
        }
    }
}