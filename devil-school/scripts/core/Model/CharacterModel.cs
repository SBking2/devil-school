
namespace EGame
{
    public class CharacterModel : AbstractModel
    {
        public virtual int MaxHP => 10;
        public virtual int MoveSpeed => 300;

        protected virtual string _VisualsPath => $"creature_visuals/" + ID.Entry.ToLowerInvariant();

        public NCreatureVisual CreateVisual()
        {
            return SceneHelper.LoadScene<NCreatureVisual>(_VisualsPath);
        }
    }
}