
using System;
using System.Collections.Generic;

namespace EGame
{
    public class AnimState
    {
        public string ID { get;}
        public bool IsLoop { get;}
        public float MixDuration { get; }
        public AnimState NextState { get; set; }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////
        ////////                                    触发分支
        ///////////////////////////////////////////////////////////////////////////////////////////////////////

        private struct Branch
        {
            public AnimState State;
            public Func<bool> Condition;
        }

        private Dictionary<string, List<Branch>> _Branches;

        public AnimState(string id, float mix_duration = 0f, bool is_loop = false)
        {
            this.ID = id;
            this.IsLoop = is_loop;
            this.MixDuration = mix_duration;
            _Branches = new Dictionary<string, List<Branch>>();
        }

        public void AddBranch(string trigger, AnimState state, Func<bool> condition = null)
        {
            if(_Branches.ContainsKey(trigger) == false)
                _Branches.Add(trigger, new List<Branch>());

            _Branches[trigger].Add(new Branch() { State = state, Condition = condition });
        }

        public AnimState CallTrigger(string trigger)
        {
            if (_Branches.ContainsKey(trigger) == false)
                return null;

            foreach(var branch in _Branches[trigger])
            {
                if (branch.Condition == null || branch.Condition.Invoke() == true)
                    return branch.State;
            }

            return null;
        }
    }
}
