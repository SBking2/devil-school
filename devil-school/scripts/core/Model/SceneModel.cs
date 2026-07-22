
using Godot;

namespace EGame
{
    public abstract class WorldModel : AbstractModel
    {
        protected virtual string _WORLD_PATH => $"worlds/" + ID.Entry.ToLowerInvariant();
        public Node3D CreateWorld()
        {
            var enviroment = SceneHelper.LoadScene<Node3D>(_WORLD_PATH);
            return enviroment;
        }
    }
}