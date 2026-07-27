
using System.Collections.Generic;
using System;

namespace EGame
{
    public class WorldMoveStateMachine
    {
        private AbstractWorldMoveState _CurrentState;
        public AbstractWorldMoveState CurrentState => _CurrentState;
        public Dictionary<string, AbstractWorldMoveState> States { get; } = new Dictionary<string, AbstractWorldMoveState>();
        public List<string> StateLog { get; } = new List<string>();

        public WorldMoveStateMachine(IEnumerable<AbstractWorldMoveState> states, AbstractWorldMoveState init_state)
        {
            foreach (var state in states)
                States.Add(state.ID, state);

            SetState(init_state);
        }

        public void Update(float delta)
        {
            if (_CurrentState == null)
                return;

            _CurrentState.OnUpdate(delta);
        }

        private void SetState(AbstractWorldMoveState state)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            _CurrentState?.OnExit();
            _CurrentState = state;
            _CurrentState.OnEnter();

            if (_CurrentState.IsMove)
                StateLog.Add(_CurrentState.ID);
        }

        private void NextState(Creature owenr, Rng rng)
        {
            var next_state = _CurrentState.GetNextState(owenr , rng);

            if (string.IsNullOrEmpty(next_state) || next_state.Equals(_CurrentState.ID))
                return;

            if (States.ContainsKey(next_state) == false)
                throw new InvalidOperationException("no valid state found: " + next_state);

            SetState(States[next_state]);
        }
    }
}
