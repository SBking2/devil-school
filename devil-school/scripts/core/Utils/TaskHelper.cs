
using System;
using System.Threading.Tasks;

namespace EGame
{
    public static class TaskHelper
    {
        public static Task RunSafely(Task task)
        {
            return ExecuteTask(task);
        }

        private static async Task ExecuteTask(Task task)
        {
            try
            {
                await task;
            }
            catch(Exception ex)
            {
                bool is_canceled_exception = ex is OperationCanceledException;
                if(is_canceled_exception == false)
                {
                    Log.Error(ex.ToString(), type: Log.LogType.GameSync);
                    //捕获到服务器上
                }
            }
        }
    }
}