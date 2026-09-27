
using System;
using System.Collections.Generic;

namespace EGame
{
    // 一层状态机：同一时刻只有一个当前状态，按名字切换。顶层/移动层/动作层都是这个类的实例
    public class PlayerStateLayer
    {
        private readonly NPlayer _Player;
        private readonly Dictionary<string, PlayerState> _States = new Dictionary<string, PlayerState>();
        private PlayerState _Current;

        public PlayerStateLayer(NPlayer player)
        {
            _Player = player;
        }

        public string CurrentName => _Current?.StateName;

        public void Add(PlayerState state)
        {
            _States.Add(state.StateName, state);
        }

        public void ChangeState(string name)
        {
            if (!_States.TryGetValue(name, out var new_state))
                throw new InvalidOperationException($"PlayerStateLayer has no state: {name}");

            _Current?.OnExit(_Player);
            _Current = new_state;
            _Current.OnEnter(_Player);
        }

        // 停掉这一层：当前状态退出，之后不再有当前状态
        public void Stop()
        {
            _Current?.OnExit(_Player);
            _Current = null;
        }

        public void Process(double dt)
        {
            _Current?.OnProcess(_Player, dt);
        }

        public void PhysicalProcess(double dt)
        {
            _Current?.OnPhysicalProcess(_Player, dt);
        }
    }
}
