
using System;
using System.Collections.Generic;

namespace EGame
{
    public class TurnStateConditionalBranch : AbstractTurnMoveState
    {
        /// <summary>
        /// 一条condition的分支
        /// </summary>
        private readonly struct ConditionalBranch(string state_id, Func<bool> condition)
        {
            public readonly string StateID = state_id;

            private readonly Func<bool> ConditionalFunc = condition;

            public bool Evaluate()
            {
                if (ConditionalFunc == null)
                    throw new InvalidOperationException($"Branch doesn't have condition!");

                return ConditionalFunc.Invoke();
            }
        }

        public override string ID => _StateID;

        private readonly string _StateID;

        private List<ConditionalBranch> _Branchs = new List<ConditionalBranch>();

        public TurnStateConditionalBranch(string id)
        { 
            _StateID = id;
        }

        public override string GetNextState(Creature owner, Rng rng)
        {
            for(int i = 0; i < _Branchs.Count; i++)
            {
                if (_Branchs[i].Evaluate())
                    return _Branchs[i].StateID;
            }

            throw new InvalidOperationException($"Condition State : {ID} could not find a next state!");
        }

        /// <summary>
        /// 添加一个条件分支,越晚添加的优先级越低
        /// </summary>
        public void AddBranch(AbstractTurnMoveState state, Func<bool> condition)
        {
            _Branchs.Add(new ConditionalBranch(state.ID, condition));
        }

        public void AddBranch(string state_id, Func<bool> condition)
        {
            _Branchs.Add(new ConditionalBranch(state_id, condition));
        }
    }
}
