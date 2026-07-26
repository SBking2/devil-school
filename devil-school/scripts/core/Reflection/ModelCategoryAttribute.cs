
using System;

namespace EGame
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]    //说明这个Attribute只能用在Class上, 并且子类不携带
    public class ModelCategoryAttribute : Attribute
    {
        
    }
}