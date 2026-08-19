
using System;

namespace EGame
{
    public static class CollisionMask
    {
        public static UInt16 PlayerMask => 1 << 1;
        public static UInt16 GrandMask => 1 << 2;
    }
}