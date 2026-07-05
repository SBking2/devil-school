
using System;

namespace EGame
{
    public class CombatEncounterCmd : AbstractConsoleCmd
    {
        public override string CmdName => "encounter";

        public override string Args => "<string:encounter_room_name>";

        public override bool DebugOnly => false;
        
        public override CmdResult Execute(string[] args)
        {
            if(NRun.Instance == null)
                return new CmdResult(false, "Must be running game!");

            if (args.Length < 1)
                return new CmdResult(false, "Must has at least one argument!");

            var encounter_name = args[0];
            encounter_name += "Model";
            var result = RunManager.Instance.DebugEnterRoom(encounter_name);
            var msg = result ? $"Successed enter encouter : {encounter_name}" : $"Unknow encounter : {encounter_name}";
            return new CmdResult(result, msg);
        }
    }
}