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
using System.Reflection;
using System.Reflection.Emit;
using Mirror;
using System.Collections.Generic;

namespace CISpy.Patches
{
	[HarmonyPatch(typeof(CharacterClassManager), nameof(CharacterClassManager.SerializeSyncVars))]
	class SyncVarPatch
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

			for (int z = 0; z < newInstructions.Count; z++)
			{
				yield return newInstructions[z];
			}

			ListPool<CodeInstruction>.Shared.Return(newInstructions);
		}
	}
}
