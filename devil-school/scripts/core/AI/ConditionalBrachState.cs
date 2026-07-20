
using System;
using System.Collections.Generic;

namespace EGame
{
    public class ConditionalBrachState : BaseMonsterMoveState
    {
        /// <summary>
        /// 一条condition的分支
        /// </summary>
        private readonly struct ConditionalBrach(string state_id, Func<bool> condition)
        {
            public readonly string StateID = state_id;

            private readonly Func<bool> ConditionalFunc = condition;

            public bool Evaluate()
            {
                if (ConditionalFunc == null)
                    throw new InvalidOperationException($"Brach doesn't have condition!");

                return ConditionalFunc.Invoke();
            }
        }

        public override string ID => _StateID;

        private readonly string _StateID;

        private List<ConditionalBrach> _Brachs = new List<ConditionalBrach>();

        public ConditionalBrachState(string id)
        { 
            _StateID = id;
        }

        public override string GetNextState(Creature owner, Rng rng)
        {
            for(int i = 0; i < _Brachs.Count; i++)
            {
                if (_Brachs[i].Evaluate())
                    return _Brachs[i].StateID;
            }

            throw new InvalidOperationException($"Condition State : {ID} could not find a next state!");
        }

        /// <summary>
        /// 添加一个条件分支,越晚添加的优先级越低
        /// </summary>
        public void AddBrach(BaseMonsterMoveState state, Func<bool> condition)
        {
            _Brachs.Add(new ConditionalBrach(state.ID, condition));
        }
    }
}