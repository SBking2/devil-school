
using Godot;

namespace EGame
{
    public abstract class EnviromentModel : AbstractModel
    {
        protected virtual string _ENVIROMENT_PATH => $"enviroments/" + ID.Entry.ToLowerInvariant();
        public Control CreateEnviroment()
        {
            var enviroment = SceneHelper.LoadScene<Control>(_ENVIROMENT_PATH);
            return enviroment;
        }
    }
}