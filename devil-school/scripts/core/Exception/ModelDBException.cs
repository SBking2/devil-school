
using System;

namespace EGame
{
    public class ModelDBException : Exception
    {
        public ModelDBException(string message) : base(message)
        {

        }

        public ModelDBException(string message, Exception inner_exception) : base(message, inner_exception)
        {

        }
    }
}