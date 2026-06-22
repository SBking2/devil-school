
namespace EGame
{
    /// <summary>
    /// 管理着一次游戏运行
    /// </summary>
    public class RunManager
    {
        public static RunManager Instance { get; } = new RunManager();
        public RunState RunState { get; private set; }
        
        public void SetUpForSinglePlayer(RunState state)
        {
            RunState = state;
        }

        public void DebugEnterRoom()
        {
            var combat_room = CreateRoom();
            var ncombat_room = NCombatRoom.Create(combat_room);
            NRun.Instance.SetCurrentRoom(ncombat_room);
        }

        private CombatRoom CreateRoom()
        {
            return new CombatRoom(ModelDB.Encounter<DebugEncounterModel>().MutableClone() as EncounterModel);
        }
    }
}