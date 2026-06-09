
using Godot;

namespace EGame
{
	public partial class NDevConsole : Control
	{
		public static NDevConsole Instance { get; private set; }
		private RichTextLabel _OutputBuffer;
		private LineEdit _InputBuffer;
		private DevConsole _DevConsole;
		public override void _EnterTree()
		{
			base._EnterTree();

			if (Instance == null)
				Instance = this;
			else
				this.QueueFree();
		}

		public override void _Ready()
		{
			base._Ready();

			_DevConsole = new DevConsole(false);
			_OutputBuffer = GetNode<RichTextLabel>("%OutputBuffer");
			_InputBuffer = GetNode<LineEdit>("%InputBuffer");
		}

		public override void _Input(InputEvent @event)
		{
			base._Input(@event);

			if(@event is InputEventKey)
			{
				var key_event = @event as InputEventKey;
				if(key_event.Pressed)
				{
					if (key_event.Keycode == Key.Enter && IsVisibleInTree())
						TryProcessCmd();
					else if (key_event.Keycode == Key.Asciitilde || key_event.Keycode == Key.Quoteleft)
					{
						if (this.Visible == false)
							ShowConsole();
						else
							HideConsole();
					}
					else if(key_event.Keycode == Key.Up)
					{
						var cmd = _DevConsole.GetNextCommand();
						if(cmd != string.Empty)
							_InputBuffer.Text = cmd;
					}
					else if(key_event.Keycode == Key.Down)
					{
						var cmd = _DevConsole.GetPreviousCommand();
						if(cmd != string.Empty)
							_InputBuffer.Text = cmd;
					}
				}
			}
		}

		private void TryProcessCmd()
		{
			var input = _InputBuffer.Text;
			input = input.Trim();
			input = input.ToLower();

			if (input == string.Empty)
				return;

			_InputBuffer.Text = string.Empty;
			if(input.Equals("clear"))
			{
				_OutputBuffer.Text = string.Empty;
			}
			else if(input.Equals("exit"))
			{
				HideConsole();
			}else
			{
				ProcessCmd(input);
			}
		}

		private void ProcessCmd(string input)
		{
			var result = _DevConsole.ProcessCmd(input);
			var color_code = result.Success ? "#76FF56" : "#E74C3C";
			var result_str = result.Success ? "Success" : "Failed";
			_OutputBuffer.Text = _OutputBuffer.Text + $"[b][color={color_code}][{result_str}][/color][/b] : " + result.Message + "\n";
		}

		private void ShowConsole()
		{
			this.Visible = true;
			_InputBuffer.CallDeferred(Control.MethodName.GrabFocus);
		}
		private void HideConsole()
		{
			this.Visible = false;
			GetViewport()?.GuiReleaseFocus();   //让所有控件都失去焦点
		}
	}
}
