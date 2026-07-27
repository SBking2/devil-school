
using System;
using System.Collections.Generic;
using System.Linq;

namespace EGame
{
    public class WorldStateRandomBrach : AbstractWorldMoveState
    {
        private struct StateWeight
        {
            public string StateID;
            public MoveRepeatType RepeatType;
            public int MaxTimes;
            public int CoolTime;
            public Func<float> WeightFunc;
        }

        private readonly string _StateID;
        private readonly Rng _Rng;
        private readonly List<StateWeight> _StateWeights = new List<StateWeight>();

        public override string ID => _StateID;

        public WorldStateRandomBrach(string id, Rng rng = null)
        {
            _StateID = id;
            _Rng = rng ?? Rng.RealRandom;
        }

        public override string GetNextState(Creature owner, Rng rng)
        {
            if (_Rng == null)
                throw new InvalidOperationException($"Random State : {ID} doesn't have rng!");

            float sum = _StateWeights.Sum((weight) =>
            {
                return GetStateWeight(weight, owner.MonsterModel.WorldMoveStateMachine);
            });

            if (sum <= 0f)
                throw new InvalidOperationException($"Random State : {ID} doesn't have valid weight!");

            float random = _Rng.RangeFloat(0f, sum);

            for (int i = 0; i < _StateWeights.Count; i++)
            {
                random -= GetStateWeight(_StateWeights[i], owner.MonsterModel.WorldMoveStateMachine);
                if (random <= 0f)
                    return _StateWeights[i].StateID;
            }

            throw new InvalidOperationException($"Random State : {ID} could not find the next state!");
        }

        private float GetStateWeight(StateWeight weight, WorldMoveStateMachine state_machine)
        {
            if (state_machine == null)
                throw new InvalidOperationException($"Random State : {ID} doesn't have state machine!");

            if (weight.WeightFunc == null)
                throw new InvalidOperationException($"Random State : {ID} doesn't have the weight func!");

            float base_weight = weight.WeightFunc();
            float final_multi = 1f;

            int used_times = 0;
            int closest_time = -1;

            for (int i = 0; i < state_machine.StateLog.Count; i++)
            {
                if (state_machine.StateLog[i].Equals(weight.StateID))
                {
                    used_times++;
                    closest_time = state_machine.StateLog.Count - i - 1;
                }
            }

            if (weight.RepeatType == MoveRepeatType.UseOnlyOnce && used_times != 0)
                final_multi = 0f;

            if (weight.RepeatType == MoveRepeatType.CanRepeatXTimes && closest_time == 0)
            {
                int repeat_time = 0;
                for (int i = state_machine.StateLog.Count - 1; i >= 0; i--)
                {
                    if (state_machine.StateLog[i].Equals(weight.StateID) == false)
                        break;

                    repeat_time++;
                }

                if (repeat_time >= weight.MaxTimes)
                    final_multi = 0f;
            }

            if (weight.CoolTime > 0 && closest_time >= 0 && closest_time + 1 <= weight.CoolTime)
                final_multi = 0f;

            return final_multi * base_weight;
        }
    }
}
