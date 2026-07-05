
namespace EGame
{
	public class RunControlCmd : AbstractConsoleCmd
	{
		public override string CmdName => "run";
		public override string Args => "<string:on/off>";
		public override bool DebugOnly => true;

		public override CmdResult Execute(string[] args)
		{
			if (args.Length < 1)
				return new CmdResult(false, $"Must has at least one argument!");
			else
			{
				var arg = args[0];
				var lower_name = arg.ToLowerInvariant();

				if (lower_name.Equals("off"))
					NGame.Instance.EnterMainMenu();
				else if (lower_name.Equals("on"))
					NGame.Instance.StartSinglePlayerGame();
				else
					return new CmdResult(false, $"Unkonw argument : {arg}");

				return new CmdResult(true, $"Run turn to {arg} Successed");
			}
		}
	}
}
