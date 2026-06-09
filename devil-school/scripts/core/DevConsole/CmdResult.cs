
using System.Threading.Tasks;

namespace EGame
{
    public struct CmdResult
    {
        public readonly bool Success;
        public readonly string Message;
        public readonly Task Task;

        public CmdResult(bool success, string msg, Task task)
        {
            this.Success = success;
            this.Message = msg;
            this.Task = task;
        }

        public CmdResult(bool success, string msg)
        {
            this.Success = success;
            this.Message = msg;
            this.Task = null;
        }
    }
}