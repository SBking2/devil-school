using System;
using System.Collections.Generic;

namespace EGame
{
    public static class AbstractConsoleCmdSubtypes
    {
        public static IReadOnlyList<Type> All => _AllTypes;
        
        private static Type[] _AllTypes = new Type[]
        {
            typeof(LogConsoleCmd),
        };
    }
}