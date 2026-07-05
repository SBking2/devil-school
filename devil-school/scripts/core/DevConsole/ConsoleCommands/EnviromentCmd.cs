
namespace EGame
{
    public class EnviromentCmd : AbstractConsoleCmd
    {
        public override string CmdName => "enviroment";

        public override string Args => "<string:enviroment_name>";

        public override CmdResult Execute(string[] args)
        {
            if (NRun.Instance == null)
                return new CmdResult(false, "Must be running game!");

            if (args.Length < 1)
                return new CmdResult(false, "Must has at least one argument!");

            var enviroment_name = args[0];
            enviroment_name += "Model";
            var result = RunManager.Instance.DebugEnterEnviroment(enviroment_name);
            var msg = result ? $"Successed enter enviroment : {enviroment_name}" : $"Unknow enviroment : {enviroment_name}";
            return new CmdResult(result, msg);
        }
    }
}