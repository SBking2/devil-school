
using Godot;

namespace EGame
{
    public class UserData
    {
        public static UserData Instance { get; } = new UserData();

        private const string _CfgPath = "user://settings.cfg";

        private ConfigFile _ConfigFile;
        public UserData()
        {
            _ConfigFile = new ConfigFile();
            _ConfigFile.Load(_CfgPath);
        }

        public int GetInt(string name, int default_value)
        {
            return Get<int>(name, default_value);
        }

        public void SetInt(string name, int value)
        {
            Set<int>(name, value);
        }

        public float GetFloat(string name, float default_value)
        {
            return Get<float>(name, default_value);
        }

        public void SetFloat(string name, float value)
        {
            Set<float>(name, value);
        }

        public bool GetBool(string name, bool default_value)
        {
            return Get<bool>(name, default_value);
        }

        public void SetBool(string name, bool value)
        {
            Set<bool>(name, value);
        }

        private T Get<[MustBeVariant] T>(string name, T default_value)
        {
            var res = _ConfigFile.GetValue("settings", name, Variant.From(default_value));
            return res.As<T>();
        }

        private void Set<[MustBeVariant] T>(string name, T value)
        {
            _ConfigFile.SetValue("settings", name, Variant.From(value));
            _ConfigFile.Save(_CfgPath);
        }
    }
}