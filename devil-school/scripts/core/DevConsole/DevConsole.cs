
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EGame
{
    public class DevConsole
    {
        public static DevConsole Instance { get; } = new DevConsole(false);

        private Dictionary<string, AbstractConsoleCmd> _CmdDic = new Dictionary<string, AbstractConsoleCmd>();

        private FixedSizeQueue<string> _Historys;

        private int _HistoryIndex;

        private string _HistoryFilePath;

        public DevConsole(bool is_only_debug)
        {
            //记录命令
            _HistoryFilePath = UserDataPathProvider.GetAccountScopedBasePath("console_history.log");
            _Historys = new FixedSizeQueue<string>(40);
            LoadHistory();

            var all_cmd_type = AbstractConsoleCmdSubtypes.All;
            foreach(var cmd_type in all_cmd_type)
            {
                var instance = Activator.CreateInstance(cmd_type) as AbstractConsoleCmd;
                if(instance.DebugOnly == false || is_only_debug == false)
                    RegisterCmd(instance);
            }
        }

        private void RegisterCmd(AbstractConsoleCmd cmd)
        {
            if (_CmdDic.ContainsKey(cmd.CmdName) == false)
                _CmdDic[cmd.CmdName] = cmd;
        }

        private void LoadHistory()
        {
            var file = FileAccess.Open(_HistoryFilePath, FileAccess.ModeFlags.Read);
            if(file != null && file.IsOpen())
            {
                _Historys.Clear();
                while(file.GetPosition() < file.GetLength())
                    _Historys.EnQueue(file.GetLine());
                file.Close();
            }
        }

        private void SaveHistory()
        {
            var file = FileAccess.Open(_HistoryFilePath, FileAccess.ModeFlags.Write);
            if(file != null && file.IsOpen())
            {
                foreach(var cmd in _Historys)
                    file.StoreLine(cmd);

                file.Close();
            }
        }

        public CmdResult ProcessCmd(string cmd)
        {
            cmd = cmd.Trim();   //删除开头和结尾的空白字符
            _Historys.EnQueue(cmd);
            SaveHistory();

            var result = ProcessCommandInternal(cmd);
            var task = result.Task;

            if(task != null)
                TaskHelper.RunSafely(task);

            return result;
        }

        private CmdResult ProcessCommandInternal(string input_line)
        {
            var array = input_line.Split(' ');
            var cmd_name = array[0];
            var args = array.Skip(1).ToArray();
            args = args.Where((string a) =>
            {
                return !string.IsNullOrEmpty(a);
            }).ToArray();

            AbstractConsoleCmd cmd;
            if(_CmdDic.TryGetValue(cmd_name, out cmd))
                return cmd.Execute(args);

            //返回失败的结果
            return new CmdResult(
                false
                , "The command '" + cmd_name + "' does not exist."
                , null
                );
        }
    }
}