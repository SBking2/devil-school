
using System;

namespace EGame
{
    public static class TypeExtension
    {
        public static ModelID ToModelID(this Type type)
        {
            return new ModelID(type.Catogory(), type.Entry());
        }

        public static string Catogory(this Type type)
        {
            var tmp_type = type;

            while(tmp_type.BaseType != typeof(AbstractModel) && tmp_type.BaseType != null)
                tmp_type = tmp_type.BaseType;

            if (tmp_type.BaseType == null)
                return "";
            return tmp_type.Name;
        }

        public static string Entry(this Type type)
        {
            return type.Name;
        }
    }
}