
using Godot;
using System.Collections.Generic;

namespace EGame
{
    // 通用 Node3D 对象池：按场景路径分组，每组内部再分激活/失活两堆。
    // Get 只会从失活堆里拿（保证拿到的一定是当前没人用的），Push 把用完的还回去、SetActive(false)、重新挂回 root 下
    public class PoolManager
    {
        public static PoolManager Instance { get; } = new PoolManager();

        private Node _Root;
        private Dictionary<string, HashSet<Node3D>> _ActivePools = new Dictionary<string, HashSet<Node3D>>();
        private Dictionary<string, Stack<Node3D>> _InactivePools = new Dictionary<string, Stack<Node3D>>();

        public void Init(Node root)
        {
            _Root = root;
        }

        public Node3D Get(string scenePath)
        {
            var inactive_pool = GetInactivePool(scenePath);
            var instance = inactive_pool.Count > 0 ? inactive_pool.Pop() : CreateInstance(scenePath);

            instance.SetActive(true);
            GetActivePool(scenePath).Add(instance);
            return instance;
        }

        // 用完调这个还回池子：从激活堆移除、失活、重新挂回 root 下，压进失活堆等下次复用
        public void Push(string scenePath, Node3D instance)
        {
            GetActivePool(scenePath).Remove(instance);

            instance.SetActive(false);
            instance.Reparent(_Root);

            GetInactivePool(scenePath).Push(instance);
        }

        private Node3D CreateInstance(string scenePath)
        {
            var instance = SceneHelper.LoadScene<Node3D>(scenePath);
            _Root.AddChild(instance);
            return instance;
        }

        private HashSet<Node3D> GetActivePool(string scenePath)
        {
            if (!_ActivePools.TryGetValue(scenePath, out var pool))
            {
                pool = new HashSet<Node3D>();
                _ActivePools[scenePath] = pool;
            }
            return pool;
        }

        private Stack<Node3D> GetInactivePool(string scenePath)
        {
            if (!_InactivePools.TryGetValue(scenePath, out var pool))
            {
                pool = new Stack<Node3D>();
                _InactivePools[scenePath] = pool;
            }
            return pool;
        }
    }
}
