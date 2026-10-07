
using System;
using System.Collections.Generic;

namespace EGame
{
    public class Timer
    {
        public class TimerTask
        {
            public long ID;
            public double Time;
            public Action Task;
        }

        public static Timer Instance { get; } = new Timer();

        private List<TimerTask> _Tasks = new List<TimerTask>();
        private Queue<TimerTask> _AddQueue = new Queue<TimerTask>();

        public void Process(double delta)
        {
            //Process之前，先把队列里的东西加进去
            while(_AddQueue.Count > 0)
            {
                var task = _AddQueue.Dequeue();
                _Tasks.Add(task);
            }

            int remove_index = -1;
            for(int i = 0; i < _Tasks.Count; i++)
            {
                _Tasks[i].Time -= delta;
                if (_Tasks[i].Time <= 0)
                {
                    _Tasks[i].Task?.Invoke();
                    if (remove_index == -1)
                        remove_index = i;
                }
                else
                {
                    if(remove_index != -1)
                    {
                        _Tasks[remove_index] = _Tasks[i];
                        remove_index++;
                    }
                }
            }

            if (remove_index != -1)
                _Tasks.RemoveRange(remove_index, _Tasks.Count - remove_index);
        }

        public long SetTimerTask(double delay, Action action)
        {
            var random_id = Rng.RealRandom.RandomInt64();
            TimerTask task = new TimerTask()
            {
                ID = random_id,
                Time = delay,
                Task = action
            };

            _AddQueue.Enqueue(task);
            return random_id;
        }
    }
}