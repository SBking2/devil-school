
using System;
using System.Collections.Generic;

namespace EGame
{
    /// <summary>
    /// 相当于GameDataManager
    /// </summary>
    public static class ModelDB
    {
        private static Dictionary<ModelID, AbstractModel> _ModelInstance = new Dictionary<ModelID, AbstractModel>();

        private static Log.Logger _Logger = new Log.Logger(Log.LogType.Generic);

        public static void OnInit()
        {
            var all_subtypes = AbstractModelSubtypes.AllSubTypes;
            foreach(var type in all_subtypes)
            {
                var instance = (AbstractModel)Activator.CreateInstance(type);
                var id = ToModelID(type);
                _ModelInstance.Add(id, instance);
                _Logger.Debug($"Loaded Data : {id.ToString()}");
            }
        }

        public static ModelID GetID<T>() where T : AbstractModel
        {
            return GetID(typeof(T));
        }

        public static ModelID GetID(Type type)
        {
            return ToModelID(type);
        }

        public static MonsterModel Monster<T>() where T : MonsterModel
        {
            return Get<T>() as MonsterModel;
        }

        public static NPCModel NPC<T>() where T : NPCModel
        {
            return Get<T>() as NPCModel;
        }

        public static MonsterModel Monster(string name)
        {
            return Get($"Monster.{name}") as MonsterModel;
        }

        public static PlayerModel Player<T>() where T : PlayerModel
        {
            return Get<T>() as PlayerModel;
        }

        public static WeaponModel Weapon<T>() where T : WeaponModel
        {
            return Get<T>() as WeaponModel;
        }

        private static AbstractModel Get<T>() where T : AbstractModel
        {
            return Get(typeof(T));
        }

        private static AbstractModel Get(Type type)
        {
            var id = ToModelID(type);
            if (_ModelInstance.ContainsKey(id))
                return _ModelInstance[id];
            return null;
        }

        private static AbstractModel Get(string id)
        {
            var slipt = id.Split(".");

            if (slipt.Length != 2)
                throw new InvalidOperationException($"Invalid Data ID : {id}!");

            var category = slipt[0].Slugify();
            var entry = slipt[1].Slugify();

            var model_id = new ModelID(category, entry);
            if (_ModelInstance.ContainsKey(model_id))
                return _ModelInstance[model_id];
            return null;
        }

        public static bool Contains<T>() where T : AbstractModel
        {
            return Contains(typeof(T));
        }
        public static bool Contains(Type type)
        {
            var id = ToModelID(type);
            return _ModelInstance.ContainsKey(id);
        }

        ///////////////////////////////////////////// 获取ModelID ////////////////////////////////////

        private static ModelID ToModelID(Type type)
        {
            return new ModelID(Catogory(type), Entry(type));
        }

        private static string Catogory(Type type)
        {
            var ct = CatogoryType(type);
            var result = ct.Name.Slugify();
            if (result.EndsWith("_MODEL"))
            {
                int length = "_MODEL".Length;
                result = result.Substring(0, result.Length - length);
            }
            return result;
        }

        public static string Entry(Type type)
        {
            var ct = EntryType(type);
            var result = ct.Name.Slugify();
            if (result.EndsWith("_MODEL"))
            {
                int length = "_MODEL".Length;
                result = result.Substring(0, result.Length - length);
            }
            return result;
        }

        private static Type CatogoryType(Type type)
        {
            var tmp_type = type;

            //查找Category类（要么有标记，要么是Abstract直系继承）
            while (
                Attribute.IsDefined(tmp_type, typeof(ModelCategoryAttribute)) == false 
                && tmp_type.BaseType != typeof(AbstractModel)
                && tmp_type.BaseType != null
                )
                tmp_type = tmp_type.BaseType;

            if (tmp_type.BaseType == null)
                throw new ModelDBException($"Try to get the catogory in the class {type.Name} which is not the subtype of abstract_model!");

            return tmp_type;
        }

        private static Type EntryType(Type type)
        {
            return type;
        }
    }
}