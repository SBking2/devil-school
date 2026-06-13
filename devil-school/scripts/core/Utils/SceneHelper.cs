
using Godot;

namespace EGame
{
	public static class SceneHelper
	{
		private static string GetScenePath(string path)
		{
			return "res://scenes/" + path; 
		}
		public static T LoadScene<T>(string path) where T : Node
		{
			var scene = GD.Load<PackedScene>(GetScenePath(path + ".tscn"));
			var instance = scene.Instantiate();
			return instance as T;
		}
	}
}
