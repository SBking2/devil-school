
using System.Collections.Generic;
using System;
namespace EGame
{
    public class MonsterMoveStateMachine
    {
        private BaseMonsterMoveState _CurrentState;
        public Dictionary<string, BaseMonsterMoveState> States { get; } = new Dictionary<string, BaseMonsterMoveState>();
        public List<MoveState> StateLog { get; } = new List<MoveState>();
        public MonsterMoveStateMachine(IEnumerable<BaseMonsterMoveState> states, BaseMonsterMoveState init_state)
        {
            foreach(var state in states)
                States.Add(state.ID, state);

            SetState(init_state);
        }

        private void SetState(BaseMonsterMoveState state)
        {
            //不允许_CurrentState为空，因此不判空
            _CurrentState.OnExit();
            _CurrentState = state;
            _CurrentState.OnEnter();
        }

        public MoveState RollMove(Rng rng)
        {
            NextState(rng);

            if (_CurrentState.IsMove == false)
                throw new InvalidOperationException($"{_CurrentState.ID} is not a move state!");

            return _CurrentState as MoveState;
        }

        private void NextState(Rng rng)
        {
            //至少取一次下个节点,跳转到一个Move节点
            do
            {
                var next_state = _CurrentState.GetNextState(rng);
                if (string.IsNullOrEmpty(next_state) || States.ContainsKey(next_state) == false)
                    throw new InvalidOperationException("no valid state found: " + next_state);

                SetState(States[next_state]);
            }
            while (_CurrentState.IsMove == false);

            //把执行过的行为记录下来
            StateLog.Add(_CurrentState as MoveState);
        }
    }
}