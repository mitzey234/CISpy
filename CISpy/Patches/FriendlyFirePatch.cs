using Exiled.API.Extensions;
using Exiled.API.Features;
using Exiled.Loader;
using HarmonyLib;
using InventorySystem.Items.Firearms;
using InventorySystem.Items.Firearms.Modules;
using PlayerStatsSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace CISpy.Patches
{
	[HarmonyPatch(typeof(BuckshotHitreg), nameof(BuckshotHitreg.ShootPellet))]
	class DamagePatch
	{
		public static bool Prefix(Vector2 pelletSettings, Ray originalRay, Vector2 offsetVector, BuckshotHitreg __instance)
		{
			Player a = null;
			Player t = null;
			if (__instance.Hub != null && Player.Get(__instance.Hub) != null) a = Player.Get(__instance.Hub);

			Vector2 vector = Vector2.Lerp(pelletSettings, __instance.GenerateRandomPelletDirection, __instance.BuckshotRandomness) * __instance.BuckshotScale;
			Vector3 vector2 = originalRay.direction;
			vector2 = Quaternion.AngleAxis(vector.x + offsetVector.x, __instance.Hub.PlayerCameraReference.up) * vector2;
			vector2 = Quaternion.AngleAxis(vector.y + offsetVector.y, __instance.Hub.PlayerCameraReference.right) * vector2;
			Ray ray = new Ray(originalRay.origin, vector2);
			RaycastHit hit;
			if (Physics.Raycast(ray, out hit, __instance.Firearm.BaseStats.MaxDistance(), StandardHitregBase.HitregMask))
			{
				global::IDestructible destructible;
				if (hit.collider.TryGetComponent<global::IDestructible>(out destructible))
				{
					if (Player.Get(destructible.NetworkId) != null) t = Player.Get(destructible.NetworkId);
				}
			}
			//Log.Info("Inside Patch: " + (t != null ? t.Nickname : null) + " - " + (a != null ? a.Nickname : null));
			bool test = true;
			try
            {
				test = EventHandlers.OnShoot(a, t);
			} catch (Exception e)
            {
				Log.Error(e);
				//ignore
            }
			//Log.Info("DP: " + test);
			return EventHandlers.OnShoot(a, t);
		}
	}

	[HarmonyPatch(typeof(SingleBulletHitreg), nameof(SingleBulletHitreg.ServerPerformShot))]
	class DamagePatch2
	{
		public static bool Prefix(Ray ray, SingleBulletHitreg __instance)
		{
			Player a = null;
			Player t = null;
			if (__instance.Hub != null && Player.Get(__instance.Hub) != null) a = Player.Get(__instance.Hub);

			RaycastHit hit;
			FirearmBaseStats baseStats = __instance.Firearm.BaseStats;
			float num = baseStats.GetInaccuracy(__instance.Firearm, __instance.Firearm.AdsModule.ServerAds, __instance.Hub.playerMovementSync.PlayerVelocity.magnitude, __instance.Hub.playerMovementSync.Grounded);
			if (Physics.Raycast(ray, out hit, baseStats.MaxDistance(), StandardHitregBase.HitregMask))
			{
				global::IDestructible destructible;
				if (hit.collider.TryGetComponent<global::IDestructible>(out destructible))
				{
					if (Player.Get(destructible.NetworkId) != null) t = Player.Get(destructible.NetworkId);
				}
			}
			bool test = true;
			try
			{
				test = EventHandlers.OnShoot(a, t);
			}
			catch (Exception e)
			{
				Log.Error(e);
				//ignore
			}
			return EventHandlers.OnShoot(a, t);
		}
	}

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
