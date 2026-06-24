
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
		public bool IsPlayer => _Player != null;
		public bool IsEnemy => Side == CombatSide.Enemy;

		public event Action<int, int> OnHPChanged;
		public event Action<int, int> OnMaxHPChanged;

		private int _HP;
		private int _MaxHP;

		private CombatSide Side { get; }

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

		/// <summary>
		/// 在房间里的位置
		/// </summary>
		public string SlotName { get; }

		private Player _PetOwener = null;
		public Player PetOwner
		{
			get
			{
				return _PetOwener;
			}

			set
			{
				if (_PetOwener != value)
					throw new InvalidOperationException($"creature : {this} already has a owner!");
				_PetOwener = value;
			}
		}

		public Creature(Player player, int max_hp)
		{
			_Player = player;
			Side = CombatSide.Player;

			_MaxHP = max_hp;
		}

		public Creature(MonsterModel monster_model, CombatSide side, string slot_name)
		{
			_MonsterModel = monster_model;
			Side = side;
			SlotName = slot_name;

			_MaxHP = monster_model.MaxHP;
		}

		public NCreatureVisual CreateVisuals()
		{
			if (_MonsterModel != null)
				return _MonsterModel.CreateVisual();
			else if(_Player != null)
				return _Player.Character.CreateVisual();

			return null;
		}
	}
}
