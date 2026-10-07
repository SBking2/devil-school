
using Godot;
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

        public static void PlayAnimation(Node node, string anim, bool is_from_start = false)
        {
            var animator = node.GetAnimationPlayer();
            if (animator != null)
            {
                animator.Play(anim);
                if(is_from_start)
                    animator.Seek(0, true);
            }
        }
    }
}