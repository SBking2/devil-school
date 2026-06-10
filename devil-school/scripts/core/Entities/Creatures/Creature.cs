
using System;

namespace EGame
{
    /// <summary>
    /// 基本战斗单位, 怪物从配置表中来, 玩家从Player数据来
    /// </summary>
    public class Creature
    {
        private Player _Player;
        private MonsterModel _MonsterModel;
        private PartnerModel _PartnerModel;

        public bool IsPlayer => _Player != null;
        public bool IsMonster => _MonsterModel != null;
        public bool IsPartner => _PartnerModel != null;

        public event Action<int, int> OnHPChanged;
        public event Action<int, int> OnMaxHPChanged;

        private int _HP;
        private int _MaxHP;

        public int HP
        {
            get
            {
                return _HP;
            }

            set
            {
                if(_HP != value)
                {
                    int old_value = _HP;
                    _HP = value;
                    OnHPChanged?.Invoke(old_value, _HP);
                }
            }
        }

        public int MaxHP
        {
            get
            {
                return _MaxHP;
            }

            set
            {
                if(_MaxHP != value)
                {
                    int old_value = _MaxHP;
                    _MaxHP = value;
                    OnMaxHPChanged?.Invoke(old_value, _MaxHP);
                }
            }
        }
    }
}