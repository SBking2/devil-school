using Godot;
using System;
namespace EGame
{
	public partial class NCreature : Control 
	{
		private const string NCREATURE_PREFAB_PATH = "combat/creature";
		public Creature Data { get; protected set;}

        private NCreatureVisual _Visual;
		private CreatureAnimator _SpineAnimator;

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////										节点定位
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private NCreatureStateDisplay _StateDisplay;

		private Control _VisualParent; 

		public static NCreature Create(Creature creature)
		{
			var instance = SceneHelper.LoadScene<NCreature>(NCREATURE_PREFAB_PATH);
			instance.Data = creature;
			return instance;
		}

		public override void _Ready()
		{
			base._Ready();

			_StateDisplay = GetNode<NCreatureStateDisplay>("%StateDisplay");
            _VisualParent = GetNode<Control>("%VisualParent");

			_StateDisplay.SetCreature(this);

			//创建Visual
			GenerateVisual();
			GenerateAnimator();
		}

		private void GenerateVisual()
		{
			if (_Visual != null)
				throw new InvalidOperationException($"{Name} already has CreatureVisual!");

			if (Data.IsPlayer)
				_Visual = Data.Player.PlayerModel.CreateVisual();
			else
				_Visual = Data.MonsterModel.CreateVisual();

			if(_Visual != null)
			{
                var parent = _VisualParent == null ? this : _VisualParent;
                parent.AddChild(_Visual);
                parent.MoveChild(_Visual, 0);
            }
		}

		private void GenerateAnimator()
		{
/*            if (_Visual != null)
            {
                if (_Visual.IsSpine)
                {
                    if (Data.IsPlayer == false)
                        _SpineAnimator = Data.MonsterModel.CreateAnimator(_Visual.SpineSprite);
                    else
                        _SpineAnimator = Data.Player.PlayerModel.CreateAnimator(_Visual.SpineSprite);
                }
            }*/
        }
	}
}
