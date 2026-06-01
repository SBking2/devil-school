
using System;

namespace EGame
{
    public class AbstractModel : IComparable<AbstractModel>
    {
        public ModelID ID { get; protected set; }
        public bool IsMutable { get; protected set; }
        
        public AbstractModel MutableClone()
        {
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