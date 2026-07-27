
using System;
using System.Collections.Generic;

namespace EGame
{
	public static class AbstractModelSubtypes
	{
		public static IReadOnlyList<Type> AllSubTypes => _ModelSubtypes;

		private static Type[] _ModelSubtypes = new Type[]
		{
			//Monster
			typeof(ZombieModel),

			//Character
			typeof(RobotModel),

			//Encounter
			typeof(DebugEncounterModel),
			
			//World
			typeof(InitWorldModel)
		};
	}
}
