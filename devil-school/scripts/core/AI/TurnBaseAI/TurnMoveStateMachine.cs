
using System.Collections.Generic;
using System;

namespace EGame
{
    public class TurnMoveStateMachine
    {
        private AbstractTurnMoveState _CurrentState;
        public Dictionary<string, AbstractTurnMoveState> States { get; } = new Dictionary<string, AbstractTurnMoveState>();
        public List<TurnStateMove> StateLog { get; } = new List<TurnStateMove>();

        //因为要存储使用过的招式，所以使用 字符串+字典 更好
        public TurnMoveStateMachine(IEnumerable<AbstractTurnMoveState> states, AbstractTurnMoveState init_state)
        {
            foreach (var state in states)
                States.Add(state.ID, state);

            SetState(init_state);
        }

        private void SetState(AbstractTurnMoveState state)
        {
            //不允许_CurrentState为空，因此不判空
            _CurrentState?.OnExit();
            _CurrentState = state;
            _CurrentState.OnEnter();
        }

        public TurnStateMove RollMove(Creature owner, Rng rng)
        {
            if (_CurrentState == null)
                throw new InvalidOperationException("Turn base state machine doesn't have current state!");

            NextState(owner, rng);

            if (_CurrentState.IsMove == false)
                throw new InvalidOperationException($"{_CurrentState.ID} is not a move state!");

            return _CurrentState as TurnStateMove;
        }

        private void NextState(Creature owner, Rng rng)
        {
            int left_step = States.Count + 1;

            //至少取一次下个节点,跳转到一个Move节点
            do
            {
                if (left_step-- <= 0)
                    throw new InvalidOperationException("too many turn base state branch jumps.");

                var next_state = _CurrentState.GetNextState(owner, rng);
                if (string.IsNullOrEmpty(next_state) || States.ContainsKey(next_state) == false)
                    throw new InvalidOperationException("no valid state found: " + next_state);

                SetState(States[next_state]);
            }
            while (_CurrentState.IsMove == false);

            //把执行过的行为记录下来
            StateLog.Add(_CurrentState as TurnStateMove);
        }
    }
}
