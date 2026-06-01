
using Godot;

namespace EGame
{
	public partial class Example : Node
	{
		private ActionQueueSet _QueueSet = new ActionQueueSet(1);
		private ActionExecutor _Executor;

		public override void _Ready()
		{
			base._Ready();
			_Executor = new ActionExecutor(_QueueSet);
			ModelDB.OnInit();

			var goblin_model = ModelDB.Monster<GoblinModel>();
			Logger.Error(goblin_model.ToString());
		}

		public override void _Input(InputEvent @event)
		{
			base._Input(@event);
			if(@event is InputEventKey key_event && key_event.IsPressed())
			{
				if(key_event.Keycode == Key.F)
					ExampleFunc();

				if (key_event.Keycode == Key.A)
					Action1();

				if (key_event.Keycode == Key.D)
					Action2();
			}
		}

		private void ExampleFunc()
		{
			Logger.Debug("Test Debug");
			Logger.Warn("Test Warn");
			Logger.Error("Test Error");
		}

		private void Action1()
		{
			_QueueSet.EnQueue(new DelayAction());
		}

		private void Action2()
		{
			_QueueSet.EnQueue(new Delay2Action());
		}
	}
}
