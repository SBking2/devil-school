
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

        public string ID { get; private set; }

        public AnimState(string id)
        {
            this.ID = id;
            _Braches = new Dictionary<string, List<Branch>>();
        }

        public void AddBranch(string trigger, AnimState state, Func<bool> condition = null)
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