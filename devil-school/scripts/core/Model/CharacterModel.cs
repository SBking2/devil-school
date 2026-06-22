
namespace EGame
{
    public class CharacterModel : AbstractModel
    {
        public virtual int MaxHP => 10;

        protected virtual string VisualsPath => $"creature_visuals/" + ID.Entry.ToLowerInvariant();

        public NCreatureVisual CreateVisual()
        {
            return SceneHelper.LoadScene<NCreatureVisual>(VisualsPath);
        }
    }
}