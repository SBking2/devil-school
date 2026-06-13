
using System;

namespace EGame
{
	public class AbstractModel : IComparable<AbstractModel>
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
				throw new ModelDBException($"Abstract Model can't initialize repeatly! {type}");
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
	}
}
