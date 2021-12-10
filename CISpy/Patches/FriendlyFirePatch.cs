using HarmonyLib;
using System.Linq;

namespace CISpy.Patches
{
    [HarmonyPatch(typeof(PlayerStatsSystem.AttackerDamageHandler), nameof(PlayerStatsSystem.AttackerDamageHandler.ProcessDamage))]
	class FriendlyFirePatch
	{
		public static bool Prefix(ReferenceHub ply, PlayerStatsSystem.AttackerDamageHandler __instance)
		{
			if (CISpy.FFGrants.Contains(__instance.GetHashCode()))
			{
				__instance.ForceFullFriendlyFire = true;
				CISpy.FFGrants.Remove(__instance.GetHashCode());
			}
			return true;
		}
	}
}
