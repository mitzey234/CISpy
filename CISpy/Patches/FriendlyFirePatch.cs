using Exiled.API.Extensions;
using Exiled.API.Features;
using Exiled.Loader;
using HarmonyLib;
using static HarmonyLib.AccessTools;
using InventorySystem.Items;
using InventorySystem.Items.Firearms;
using InventorySystem.Items.Firearms.Modules;
using NorthwoodLib.Pools;
using PlayerStatsSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using Mirror;

namespace CISpy.Patches
{
	/*[HarmonyPatch(typeof(BuckshotHitreg), nameof(BuckshotHitreg.ShootPellet))]
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
	}*/

	/*[HarmonyPatch(typeof(CharacterClassManager), nameof(CharacterClassManager.SetClassIDAdv))]
	class FriendlyFirePatch
	{
		private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			List<CodeInstruction> newInstructions = ListPool<CodeInstruction>.Shared.Rent(instructions);

			int index = newInstructions.FindIndex(x => x.opcode == OpCodes.Call && (MethodInfo)x.operand == Method(typeof(CharacterClassManager), "set_NetworkCurClass")) + 1;
			newInstructions.InsertRange(index, new[]
			{
				new CodeInstruction(OpCodes.Ldarg_0),
				new CodeInstruction(OpCodes.Ldfld, Field(typeof(CharacterClassManager), nameof(CharacterClassManager._hub))),
				new CodeInstruction(OpCodes.Call, Method(typeof(EventHandlers), nameof(EventHandlers.UpdateAppearance)))
			});

			for (int z = 0; z < newInstructions.Count; z++)
				yield return newInstructions[z];

			ListPool<CodeInstruction>.Shared.Return(newInstructions);
		}
	}*/

	/*[HarmonyPatch(typeof(CharacterClassManager), "set_NetworkCurClass")]
	class fuck
	{
		public static bool Prefix(CharacterClassManager __instance, RoleType value)
		{
			Player player = Player.Get(__instance._hub);
			if (player != null && EventHandlers.spies.ContainsKey(player))
			{
				return false;
			}

			NetworkBehaviour b = (NetworkBehaviour)__instance;

			if (!b.SyncVarEqual<global::RoleType>(value, ref __instance.CurClass))
			{
				global::RoleType curClass = __instance.CurClass;
				//__instance.SetSyncVar<global::RoleType>(value, ref __instance.CurClass, 8UL);
				if (EventHandlers.spyOriginalRole.ContainsKey(Player.Get(__instance._hub)))
				{
					RoleType role = EventHandlers.spyOriginalRole[Player.Get(__instance._hub)];
					__instance.SetSyncVar<global::RoleType>(value, ref role, 8UL);
				}
				else
				{
					__instance.SetSyncVar<global::RoleType>(value, ref __instance.CurClass, 8UL);
				}
				if (NetworkServer.localClientActive && !__instance.getSyncVarHookGuard(8UL))
				{
					__instance.setSyncVarHookGuard(8UL, true);
					__instance.SetClassIDHook(curClass, value);
					__instance.setSyncVarHookGuard(8UL, false);
				}
			}

			return false;
		}
	}*/

	/*[HarmonyPatch(typeof(CharacterClassManager), nameof(CharacterClassManager.SerializeSyncVars))]
	class fuck2
	{
		public static bool Prefix(CharacterClassManager __instance, NetworkWriter writer, bool forceAll, ref bool __result)
		{
			Player player = Player.Get(__instance._hub);
			if (player == null) return false;

			NetworkBehaviour b = (NetworkBehaviour)__instance;

			bool result = false;
			if (forceAll)
			{
				writer.WriteString(__instance.Pastebin);
				writer.WriteBoolean(__instance.IntercomMuted);
				writer.WriteBoolean(__instance.NoclipEnabled);
				GeneratedNetworkCode._Write_RoleType(writer, __instance.CurClass);
				writer.WriteByte(__instance.CurSpawnableTeamType);
				writer.WriteString(__instance.CurUnitName);
				writer.WriteBoolean(__instance.RoundStarted);
				writer.WriteBoolean(__instance.IsVerified);
				writer.WriteString(__instance.SyncedUserId);
				__result = true;
			}
			writer.WriteUInt64(b.syncVarDirtyBits);
			if ((b.syncVarDirtyBits & 1UL) != 0UL)
			{
				writer.WriteString(__instance.Pastebin);
				result = true;
			}
			if ((b.syncVarDirtyBits & 2UL) != 0UL)
			{
				writer.WriteBoolean(__instance.IntercomMuted);
				result = true;
			}
			if ((b.syncVarDirtyBits & 4UL) != 0UL)
			{
				writer.WriteBoolean(__instance.NoclipEnabled);
				result = true;
			}
			if ((b.syncVarDirtyBits & 8UL) != 0UL)
			{
				GeneratedNetworkCode._Write_RoleType(writer, EventHandlers.spyOriginalRole.ContainsKey(player) ? EventHandlers.spyOriginalRole[player] : __instance.CurClass);
				result = true;
			}
			if ((b.syncVarDirtyBits & 16UL) != 0UL)
			{
				writer.WriteByte(__instance.CurSpawnableTeamType);
				result = true;
			}
			if ((b.syncVarDirtyBits & 32UL) != 0UL)
			{
				writer.WriteString(__instance.CurUnitName);
				result = true;
			}
			if ((b.syncVarDirtyBits & 64UL) != 0UL)
			{
				writer.WriteBoolean(__instance.RoundStarted);
				result = true;
			}
			if ((b.syncVarDirtyBits & 128UL) != 0UL)
			{
				writer.WriteBoolean(__instance.IsVerified);
				result = true;
			}
			if ((b.syncVarDirtyBits & 256UL) != 0UL)
			{
				writer.WriteString(__instance.SyncedUserId);
				result = true;
			}
			__result = result;
			return false;
		}
	}*/

	[HarmonyPatch(typeof(CharacterClassManager), nameof(CharacterClassManager.SerializeSyncVars))]
	class FriendlyFirePatch
	{
		private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			List<CodeInstruction> newInstructions = ListPool<CodeInstruction>.Shared.Rent(instructions);

			int index = newInstructions.FindLastIndex(x => x.opcode == OpCodes.Call && (MethodInfo)x.operand == Method(typeof(GeneratedNetworkCode), nameof(GeneratedNetworkCode._Write_RoleType))) - 2;
			newInstructions.RemoveRange(index, 2);

			newInstructions.InsertRange(index, new[]
			{
				new CodeInstruction(OpCodes.Ldarg_0),
				new CodeInstruction(OpCodes.Ldfld, Field(typeof(CharacterClassManager), nameof(CharacterClassManager._hub))),
				new CodeInstruction(OpCodes.Ldarg_0),
				new CodeInstruction(OpCodes.Ldfld, Field(typeof(CharacterClassManager), nameof(CharacterClassManager.CurClass))),
				new CodeInstruction(OpCodes.Call, Method(typeof(EventHandlers), nameof(EventHandlers.GetSpyRoleType)))
			});

			//Label l = generator.DefineLabel();
			//Label l2 = generator.DefineLabel();
			//Label l3 = generator.DefineLabel();
			//LocalBuilder roletype = generator.DeclareLocal(typeof(RoleType));

			/*newInstructions.InsertRange(index, new[]
			{
				new CodeInstruction(OpCodes.Ldarg_0),
				new CodeInstruction(OpCodes.Ldfld, Field(typeof(CharacterClassManager), nameof(CharacterClassManager._hub))),
				new CodeInstruction(OpCodes.Call, Method(typeof(Player), nameof(Player.Get), new[] { typeof(ReferenceHub) })),
				new CodeInstruction(OpCodes.Dup),
				new CodeInstruction(OpCodes.Dup),
				new CodeInstruction(OpCodes.Ldc_I4_0),
				new CodeInstruction(OpCodes.Beq_S, l),

				// else
				new CodeInstruction(OpCodes.Ldfld, Field(typeof(EventHandlers), nameof(EventHandlers.spyOriginalRole))),
				new CodeInstruction(OpCodes.Call, Method(typeof(Dictionary<Player, RoleType>), nameof(Dictionary<Player, RoleType>.ContainsKey))),
				new CodeInstruction(OpCodes.Ldc_I4_0),
				new CodeInstruction(OpCodes.Beq_S, l3),
				new CodeInstruction(OpCodes.Callvirt, Method(typeof(Dictionary<Player, RoleType>), "get_Item")),
				new CodeInstruction(OpCodes.Stloc, roletype.LocalIndex),
				new CodeInstruction(OpCodes.Br_S, l2),

				// if null
				new CodeInstruction(OpCodes.Pop).WithLabels(l),
				new CodeInstruction(OpCodes.Pop).WithLabels(l3),
				new CodeInstruction(OpCodes.Ldarg_0),
				new CodeInstruction(OpCodes.Ldfld, Field(typeof(CharacterClassManager), nameof(CharacterClassManager.CurClass))),
				new CodeInstruction(OpCodes.Stloc, roletype.LocalIndex),

				new CodeInstruction(OpCodes.Ldloc, roletype.LocalIndex).WithLabels(l2)
			});*/

			for (int z = 0; z < newInstructions.Count; z++)
			{
				//Log.Info(newInstructions[z].opcode + " | " + newInstructions[z].operand);
				yield return newInstructions[z];
			}

			ListPool<CodeInstruction>.Shared.Return(newInstructions);
		}
	}
}
