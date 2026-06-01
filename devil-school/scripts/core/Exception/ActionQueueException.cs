
using System;

namespace EGame
{
    public class ActionQueueException : Exception
    {
        public ActionQueueException(string message) : base(message)
        {

        }

        public ActionQueueException(string message, Exception inner_exception) : base(message, inner_exception)
        {
            
        }
    }
}