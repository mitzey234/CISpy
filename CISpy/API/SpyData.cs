using Exiled.API.Features;
using System.Collections.Generic;

namespace CISpy.API
{
	public static class SpyData
	{
		public static Dictionary<Player, bool> GetSpies()
		{
			return EventHandlers.spies;
		}

		public static void MakeSpy(Player player, RoleType spyRole, RoleType originalRole, bool full = false, bool isVulenrable  = true)
		{
			EventHandlers.MakeSpy(player, spyRole, originalRole, full, isVulenrable);
		}
	}
}
