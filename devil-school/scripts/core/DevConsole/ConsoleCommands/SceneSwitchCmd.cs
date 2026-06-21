
namespace EGame
{
	public class SceneSwitchCmd : AbstractConsoleCmd
	{
		public override string CmdName => "scene";
		public override string Args => "<string:scene_name>";
		public override bool DebugOnly => true;

		public override CmdResult Execute(string[] args)
		{
			if (args.Length < 1)
				return new CmdResult(false, "Must supply the log level as the first argument!");
			else
			{
				var scene_name = args[0];
				var lower_name = scene_name.ToLowerInvariant();

				if (lower_name.Equals("mainmenu"))
					NGame.Instance.EnterMainMenu();
				else if (lower_name.Equals("run"))
					NGame.Instance.EnterRun();
				else
					return new CmdResult(false, $"Unkonw Scene Name : {scene_name}");

				return new CmdResult(true, $"Scene Switch To {scene_name} Successed");
			}
		}
	}
}
