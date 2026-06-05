
using Godot;

namespace EGame
{
    public class LoadManager
    {
        public static T LoadScene<T>(string path) where T : Node
        {
            var scene = GD.Load<PackedScene>(path);
            var instance = scene.Instantiate();
            return instance as T;
        }
    }
}