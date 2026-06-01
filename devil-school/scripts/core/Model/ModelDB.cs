
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

        public static void OnInit()
        {
            var all_subtypes = AbstractModelSubtypes.AllSubTypes;
            foreach(var type in all_subtypes)
            {
                var instance = (AbstractModel)Activator.CreateInstance(type);
                var id = type.ToModelID();
                _ModelInstance.Add(id, instance);
                Logger.Debug($"Loaded Model : {id.ToString()}");
            }
        }

        public static MonsterModel Monster<T>() where T : MonsterModel
        {
            return Get<T>() as MonsterModel;
        }

        private static AbstractModel Get<T>() where T : AbstractModel
        {
            return Get(typeof(T));
        }

        private static AbstractModel Get(Type type)
        {
            var id = type.ToModelID();
            if (_ModelInstance.ContainsKey(id))
                return _ModelInstance[id];
            return null;
        }
    }
}