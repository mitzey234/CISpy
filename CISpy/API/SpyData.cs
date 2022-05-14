using Exiled.API.Features;
using System.Collections.Generic;

namespace CISpy.API
{
	public static class SpyData
	{
		public static Dictionary<Player, bool> GetSpies()
		{
			return EventHandlers.spyVulnerability;
		}

		public static void MakeSpy(Player player, RoleType originalRole, bool isVulnerable = false, bool full = true)
		{
			EventHandlers.MakeSpy(player, originalRole, isVulnerable, full);
		}
	}
}
