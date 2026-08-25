
using System;
using Godot;

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

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        ///////                                             为了让数据与表现分离，尽量不要让表现端去访问内部结构
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        
        public NAgent CreateAgent()
        {
            AgentModel model = CharacterModel as AgentModel;
            if (model == null)
                throw new InvalidOperationException("Trying to create agent from un-agent model!");

            var prefab = SceneHelper.LoadScene<NAgent>(model.PrefabPath);
            return prefab;
        }

    }
}