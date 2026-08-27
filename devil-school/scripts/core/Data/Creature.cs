
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
        public event Action<int, int> OnHPChanged;

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

                OnHPChanged?.Invoke(old, _HP);
            }
        }

        public void ApplyDamage(int amount)
        {
            HP = Mathf.Clamp(HP - amount, 0, CharacterModel.MaxHP);
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        ///////                                             简化表现端职责
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