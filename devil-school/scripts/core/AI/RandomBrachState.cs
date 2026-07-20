
using System;
using System.Collections.Generic;
using System.Linq;

namespace EGame
{
    public enum MoveRepeatType
    {
        CanRepeatForever,
        CanRepeatXTimes,
        UseOnlyOnce
    }

    public class RandomBrachState : BaseMonsterMoveState
    {
        private struct StateWeight
        {
            public string StateID;
            public MoveRepeatType RepeatType;
            public int MaxTimes;    //最大重复次数
            public int CoolTime;
            public Func<float> WeightFunc;
        }

        private readonly string _StateID;
        public override string ID => _StateID;

        private List<StateWeight> _StateWeights = new List<StateWeight>();
        
        public RandomBrachState(string id)
        {
            _StateID = id;
        }

        public override string GetNextState(Creature owner, Rng rng)
        {
            float sum = _StateWeights.Sum((StateWeight weight) => GetStateWeight(weight, owner.MonsterModel.MoveStateMachine));
            float random = rng.RangeFloat(0f, sum);

            for(int i = 0; i < _StateWeights.Count; i++)
            {
                random -= GetStateWeight(_StateWeights[i], owner.MonsterModel.MoveStateMachine);
                if (random <= 0)
                    return _StateWeights[i].StateID;
            }

            throw new InvalidOperationException($"Random State : {ID} could not find the next state!");
        }

        private float GetStateWeight(StateWeight weight, MonsterMoveStateMachine machine)
        {
            if (weight.WeightFunc == null)
                throw new InvalidOperationException($"Random State : {ID} doesn't have the weight func!");

            float base_weight = weight.WeightFunc();
            float final_multi = 1f;

            int used_times = 0;
            int closest_time = -1;

            for(int i = 0; i < machine.StateLog.Count; i++)
            {
                if (machine.StateLog[i].ID.Equals(weight.StateID))
                {
                    used_times++;
                    closest_time = machine.StateLog.Count - i - 1;
                }    
            }

            //只能使用一次
            if (weight.RepeatType == MoveRepeatType.UseOnlyOnce && used_times != 0)
                final_multi = 0f;

            //最多连续释放maxtimes次
            if(weight.RepeatType == MoveRepeatType.CanRepeatXTimes && closest_time == 0)
            {
                int repeat_time = 0;
                for(int i = machine.StateLog.Count - 1; i >= 0; i--)
                {
                    if (machine.StateLog[i].ID.Equals(weight.StateID) == false)
                        break;
                    else
                        repeat_time++;
                }

                if (repeat_time >= weight.MaxTimes)
                    final_multi = 0f;
            }

            //额外规则，限制一个节点必须间隔几个回合才能使用
            if(weight.CoolTime > 0 && closest_time + 1 <= weight.CoolTime)
            {
                final_multi = 0f;
            }

            return final_multi * base_weight;
        }
    }
}