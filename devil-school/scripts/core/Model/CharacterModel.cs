
using Godot;
using System;

namespace EGame
{
    public abstract class CharacterModel : AbstractModel
    {
        public CharacterModel()
        {
            _HP = MaxHP;
        }

        public virtual int MaxHP => 10;
        public virtual float MoveSpeed => 8f;
        
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
                AssertMutable();
                var old = _HP;
                _HP = value;
                OnHPChanged?.Invoke(old, _HP);
            }
        }

        public bool IsDead => _HP <= 0;

        protected virtual CreatureAnimator BuildAnimator(INCharacter character)
        {
            return null;
        }

        public override void OnCharacterCreated(INCharacter character)
        {
            character.BuildAnimator(BuildAnimator(character));
        }
    }
}
