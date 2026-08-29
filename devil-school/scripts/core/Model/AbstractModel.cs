
using System;

namespace EGame
{
	public abstract class AbstractModel : IComparable<AbstractModel>
	{
		public ModelID ID { get; protected set; }
		public bool IsMutable { get; protected set; }
		
		/// <summary>
		/// 这个构造函数只能由静态数据使用，而且只能使用一次,动态数据全都由另一种方法创建
		/// </summary>
		public AbstractModel()
		{
			Type type = this.GetType();
			if (ModelDB.Contains(type))
				throw new ModelDBException($"Abstract Data can't initialize repeatly! {type}");
			ID = ModelDB.GetID(type);
		}

		public AbstractModel MutableClone()
		{
			//使用内存拷贝，不走构造函数
			AbstractModel model = this.MemberwiseClone() as AbstractModel;
			model.IsMutable = true;
			model.DeepCopy();
			return model;
		}

		protected void AssertMutable()
		{
			if (IsMutable == false)
				throw new ModelDBException($"UnSuccessed Assert Mutable of {ID.ToString()}");
		}

		protected void AssertCanonical()
		{
			if (IsMutable == true)
				throw new ModelDBException($"UnSuccessed Assert Canonical of {ID.ToString()}");
		}

		/// <summary>
		/// 深拷贝，用于清理掉浅拷贝的引用关系
		/// </summary>
		protected virtual void DeepCopy()
		{

		}

		public int CompareTo(AbstractModel other)
		{
			if (this == other)
				return 0;
			if (other == null)
				return 1;
			return ID.CompareTo(other.ID);
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		///////												Hook
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		
		public virtual void OnCharacterCreated(INCharacter character)
		{

		}

		public virtual void OnPlayerCreated(NPlayer player)
		{

		}

		public virtual void OnAgentCreated(NAgent agent)
		{

		}

		public virtual void OnWeaponCreated(NWeapon weapon)
		{

		}
	}
}
