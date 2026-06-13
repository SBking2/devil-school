
using System;

namespace EGame
{
    public static class TypeExtension
    {
        public static ModelID ToModelID(this Type type)
        {
            return new ModelID(type.Catogory(), type.Entry());
        }

        private static string Catogory(this Type type)
        {
            var ct = type.CatogoryType();
            var result = ct.Name.Slugify();
            if(result.EndsWith("_MODEL"))
            {
                int length = "_MODEL".Length;
                result.Substring(0, result.Length - length);
            }
            return result;
        }

        public static string Entry(this Type type)
        {
            var ct = type.EntryType();
            var result = ct.Name.Slugify();
            if (result.EndsWith("_MODEL"))
            {
                int length = "_MODEL".Length;
                result.Substring(0, result.Length - length);
            }
            return result;
        }

        private static Type CatogoryType(this Type type)
        {
            var tmp_type = type;

            while(tmp_type.BaseType != typeof(AbstractModel) && tmp_type.BaseType != null)
                tmp_type = tmp_type.BaseType;

            if (tmp_type.BaseType == null)
                throw new ModelDBException("Try to get the catogory in the class which is not the subtype of abstract_model!");

            return tmp_type;
        }

        private static Type EntryType(this Type type)
        {
            return type;
        }
    }
}