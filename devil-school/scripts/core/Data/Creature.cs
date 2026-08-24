
using System;

namespace EGame
{
    public class Creature
    {
        private int _HP;
        private Action<int, int> _OnHPChanged;

        public int HP
        {
            get
            {
                return _HP;
            }

            set
            {
                int old = _HP;
                _HP = value;

                if (_OnHPChanged != null)
                    _OnHPChanged.Invoke(old, _HP);
            }
        }
    }
}