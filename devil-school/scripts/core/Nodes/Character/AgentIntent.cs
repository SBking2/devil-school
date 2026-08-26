
using System;
using Godot;

namespace EGame
{
    public class AgentIntent
    {
        private Vector3 _WishDir;

        // 参数：true = 从静止变为想移动，false = 从想移动变为静止——只在跳变的那一次触发
        public event Action<Vector3, Vector3> OnWishDirChanged;

        public Vector3 WishDir
        {
            get => _WishDir;
            set
            {
                var old = _WishDir;
                _WishDir = value;
                
                OnWishDirChanged?.Invoke(old, value);
            }
        }
    }
}