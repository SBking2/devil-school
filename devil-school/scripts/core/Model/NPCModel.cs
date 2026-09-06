
namespace EGame
{
    [ModelCategory]
    public abstract class NPCModel : AgentModel
    {
        public override string PrefabPath => "npc/" + ID.Entry.Slugify().ToLowerInvariant();
    }
}