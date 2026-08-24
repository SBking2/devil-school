
using System;

namespace EGame
{
    public class Creature
    {
        public Creature(CharacterModel model)
        {
            CharacterModel = model;
            HP = model.MaxHP;
        }

        public CharacterModel CharacterModel { get; private set; }

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