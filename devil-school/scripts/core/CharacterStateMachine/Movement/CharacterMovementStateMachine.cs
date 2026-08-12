
using System;
using System.Collections.Generic;

namespace EGame
{
    public class CharacterMovementStateMachine
    {
        private CharacterMovementContext _Context;
        private AbstractCharacterMovementState _CurState;
        private Dictionary<string, AbstractCharacterMovementState> _StateMap = new Dictionary<string, AbstractCharacterMovementState>();
        public CharacterMovementStateMachine(NEnvCreature owner, IEnumerable<AbstractCharacterMovementState> states, AbstractCharacterMovementState init_state)
        {
            _Context = new CharacterMovementContext(owner, this);

            if(states != null)
            {
                foreach (var state in states)
                    _StateMap.Add(state.StateName, state);
            }

            _CurState = init_state;
            _CurState?.OnEnter(_Context);
        }

        public void OnUpdate(double delta)
        {
            _CurState?.OnUpdate(_Context, delta);
        }

        public void OnPhysicalUpdate(double delta)
        {
            _CurState?.OnPhysicalUpdate(_Context, delta);
        }

        public void ChangeState(string state_name)
        {
            AbstractCharacterMovementState state = null;
            if (_StateMap.TryGetValue(state_name, out state))
            {
                if (_CurState == state)
                    return;

                _CurState?.OnExit(_Context);
                _CurState = state;
                _CurState.OnEnter(_Context);
            }
            else
                throw new InvalidOperationException($"Unkown State {state_name}");
        }
    }
}
