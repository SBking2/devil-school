
using System;
using System.Diagnostics.Metrics;
using System.Reflection;

namespace EGame
{
    /// <summary>
    /// 管理着一次游戏运行
    /// </summary>
    public class RunManager
    {
        public static RunManager Instance { get; } = new RunManager();
        public RunState RunState { get; private set; }
        public void SetUpForNewRun(RunState state)
        {
            RunState = state;
        }

        public bool DebugEnterRoom(string encounter_name)
        {
            var assemble = GetType().Assembly;
            var namespace_name = GetType().Namespace;
            var type = assemble.GetType($"{namespace_name}.{encounter_name}");

            if (type == null)
                return false;

            //获取Encounter
            var method = GetType().GetMethod(nameof(EnterRoom));

            //把泛型绑到函数上
            var generic_method = method.MakeGenericMethod(type);
            generic_method.Invoke(this, null);

            return true;
        }

        public bool DebugEnterEnviroment(string enviroment_name)
        {
            var assemble = GetType().Assembly;
            var namespace_name = GetType().Namespace;
            var type = assemble.GetType($"{namespace_name}.{enviroment_name}");

            if (type == null)
                return false;

            //获取Encounter
            var method = GetType().GetMethod(nameof(EnterEnviroment));

            //把泛型绑到函数上
            var generic_method = method.MakeGenericMethod(type);
            generic_method.Invoke(this, null);

            return true;
        }

        public void EnterRoom<T>() where T : EncounterModel
        {
            var model = ModelDB.Encounter<T>().MutableClone() as EncounterModel;
            CombatRoom room = new CombatRoom(model);
            room.EnterRoom();
        }

        public void EnterEnviroment<T>() where T : EnviromentModel
        {
            var model = ModelDB.Enviroment<T>().MutableClone() as EnviromentModel;
            Enviroment enviroment = new Enviroment(model);
            enviroment.EnterEnviroment();
        }
    }
}