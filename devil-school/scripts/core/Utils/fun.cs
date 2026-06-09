
using System;

namespace EGame
{
    public static class fun
    {
        public static bool TryGetEnum<T>(string str, out T value) where T : struct, Enum
        {
            var types = Enum.GetValues<T>();
            for(int i = 0; i < types.Length; i++)
            {
                if(str.Equals(types[i].ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    value = types[i];
                    return true;
                }
            }

            value = default(T);
            return false;
        }
    }
}