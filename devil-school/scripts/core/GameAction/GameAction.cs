
using System;
using System.Threading.Tasks;

namespace EGame
{
    public abstract class GameAction
    {
        public event Action<GameAction> OnTaskCompleted;
        public async Task Excute()
        {
            await GetActionTask();
            if (OnTaskCompleted != null)
                OnTaskCompleted(this);
        }

        protected abstract Task GetActionTask();
    }
}