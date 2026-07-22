
using Godot;
using System.Collections.Generic;

namespace EGame
{
	public partial class NSceneContainer : Node
	{
		private Node _CurrentScene = null;

		public Node CurrentScene
		{
			get
			{
				if (_CurrentScene == null)
					return null;

				if (_CurrentScene.IsQueuedForDeletion())
					return null;
				
				return _CurrentScene;
			}

			private set
			{
				_CurrentScene = value;
			}
		}

		public void SetScene(Node scene)
		{
			var remove_list = new List<Node>();
			foreach(var child in GetChildren())
				remove_list.Add(child);

			for (int i = 0; i < remove_list.Count; i++)
				remove_list[i].QueueFree();

			CurrentScene = scene;

			if (CurrentScene.GetParent() == null)
				AddChild(CurrentScene);
			else
				CurrentScene.Reparent(this);
		}
	}
}
