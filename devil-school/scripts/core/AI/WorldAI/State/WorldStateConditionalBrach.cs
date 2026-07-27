
using System;
using System.Collections.Generic;

namespace EGame
{
    public class WorldStateConditionalBrach : AbstractWorldMoveState
    {
        private readonly struct ConditionalBrach
        {
            public readonly string StateID;
            private readonly Func<bool> ConditionalFunc;

            public ConditionalBrach(string state_id, Func<bool> condition)
            {
                StateID = state_id;
                ConditionalFunc = condition;
            }

            public bool Evaluate()
            {
                if (ConditionalFunc == null)
                    throw new InvalidOperationException("Brach doesn't have condition!");

                return ConditionalFunc.Invoke();
            }
        }

        private readonly string _StateID;
        private readonly List<ConditionalBrach> _Brachs = new List<ConditionalBrach>();

        public override string ID => _StateID;

        public WorldStateConditionalBrach(string id)
        {
            _StateID = id;
        }

        public override string GetNextState(Creature owner, Rng rng)
        {
            for (int i = 0; i < _Brachs.Count; i++)
            {
                if (_Brachs[i].Evaluate())
                    return _Brachs[i].StateID;
            }

            throw new InvalidOperationException($"Condition State : {ID} could not find a next state!");
        }

        public void AddBrach(AbstractWorldMoveState state, Func<bool> condition)
        {
            _Brachs.Add(new ConditionalBrach(state.ID, condition));
        }
    }
}
