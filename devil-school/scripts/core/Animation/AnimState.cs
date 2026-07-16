
using System;
using System.Collections.Generic;

namespace EGame
{
    public class AnimState
    {
        private struct Branch
        {
            public AnimState State;
            public Func<bool> Condition;
        }

        private Dictionary<string, List<Branch>> _Braches;
        public string ID { get;}
        public bool IsLoop { get;}
        public AnimState NextState { get; set; }
        public AnimState(string id, bool is_loop = false)
        {
            this.ID = id;
            this.IsLoop = is_loop;
            _Braches = new Dictionary<string, List<Branch>>();
        }

        public void AddBranch(string trigger, AnimState state, Func<bool> condition)
        {
            if(_Braches.ContainsKey(trigger) == false)
                _Braches.Add(trigger, new List<Branch>());

            _Braches[trigger].Add(new Branch() { State = state, Condition = condition });
        }

        public AnimState CallTrigger(string trigger)
        {
            if (_Braches.ContainsKey(trigger) == false)
                return null;

            foreach(var branch in _Braches[trigger])
            {
                if (branch.Condition == null || branch.Condition.Invoke() == true)
                    return branch.State;
            }

            return null;
        }
    }
}