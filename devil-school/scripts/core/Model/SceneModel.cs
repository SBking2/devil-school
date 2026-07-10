
using Godot;

namespace EGame
{
    public abstract class WorldModel : AbstractModel
    {
        protected virtual string _WORLD_PATH => $"scenes/" + ID.Entry.ToLowerInvariant();
        public Node2D CreateWorld()
        {
            var enviroment = SceneHelper.LoadScene<Node2D>(_WORLD_PATH);
            return enviroment;
        }
    }
}