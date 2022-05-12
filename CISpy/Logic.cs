using Exiled.API.Features;
using Exiled.Loader;
using MEC;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Exiled.API.Extensions;
using System.Reflection;

namespace CISpy
{
	partial class EventHandlers
	{
		internal static void MakeSpy(Player player, bool isVulnerable = false, bool full = true)
		{
			try
			{
				/*List<ItemType> savedItems = player.Items.Select(x => x.Type).ToList();
				if (!CISpy.instance.Config.SpawnWithGrenade && full)
				{
					for (int i = player.Items.Count - 1; i >= 0; i--)
					{
						if (player.Items.ElementAt(i).Type == ItemType.GrenadeHE)
						{
							player.RemoveItem(player.Items.ElementAt(i));
						}
					}
				}*/
				if (spyOriginalRole.ContainsKey(player))
				{
					InventorySystem.InventoryRoleInfo inventory = InventorySystem.Configs.StartingInventories.DefinedInventories[spyOriginalRole[player]];
					player.ResetInventory(inventory.Items);
					for (int i = inventory.Ammo.Count - 1; i >= 0; i--)
					{
						var entry = inventory.Ammo.ElementAt(i);
						player.Ammo[entry.Key] = entry.Value;
					}
				}
				player.AddItem(ItemType.KeycardChaosInsurgency);
				if (!spies.ContainsKey(player)) spies.Add(player, isVulnerable);
				else spies[player] = isVulnerable;
				player.Broadcast(10, "<i><size=60>You are a <b><color=\"green\">CISpy</color></b></size>\nCheck your console by pressing [`] or [~] for more info.</i>");
				player.ReferenceHub.characterClassManager.TargetConsolePrint(player.ReferenceHub.scp079PlayerScript.connectionToClient, "You are a Chaos Insurgency Spy! You are immune to MTF for now, but as soon as you damage an MTF, your spy immunity will turn off.\n\nHelp Chaos win the round and kill as many MTF and Scientists as you can!", "yellow");
			} catch(Exception e)
			{
				Log.Error(e);
			}
		}

		static private List<Player> TryGet035()
		{
			List<Player> scp035 = null;
			if (CISpy.isScp035)
			{
				try
				{
					scp035 = (List<Player>)Loader.Plugins.First(pl => pl.Name == "scp035").Assembly.GetType("scp035.API.Scp035Data").GetMethod("GetScp035s", BindingFlags.Public | BindingFlags.Static).Invoke(null, null);
				}
				catch (Exception e)
				{
					Log.Debug("Failed getting 035s: " + e);
					scp035 = new List<Player>();
				}
			}
			else
			{
				scp035 = new List<Player>();
			}
			return scp035;
		}

		private void RevealSpies()
		{
			for (int i = spies.Count - 1; i >= 0; i--)
			{
				var spy = spies.ElementAt(i);
				if (spy.Key != null && spy.Key.IsAlive && spy.Key.IsConnected)
				{
					spy.Key.CurrentItem = default;
					spy.Key.Broadcast(10, "<i>Your fellow <color=\"green\">Chaos Insurgency</color> have died.\nYou have been revealed!</i>");
					MirrorExtensions.SendFakeSyncVar(spy.Key, spy.Key.ReferenceHub.networkIdentity, typeof(CharacterClassManager), nameof(CharacterClassManager.NetworkCurClass), (sbyte)RoleType.ChaosConscript);
					spy.Key.ChangeAppearance(RoleType.ChaosConscript);
					spiesRevealed = true;
					spies[spy.Key] = true;
				}
			}
		}

		private int CountRoles(Team team, List<Player> pList)
		{
			int count = 0;
			foreach (Player pl in pList) if (pl.Role.Team == team) count++;
			return count;
		}

		private void CheckSpies(Player exclude = null)
		{
			List<Player> scp035 = null;

			try
			{
				scp035 = TryGet035();
			}
			catch (Exception x)
			{
				Log.Debug("SCP-035 not installed, skipping method call...");
			}

			List<Player> pList = Player.Get(x => !scp035.Contains(x)).ToList();
			if (pList.Where(x => x.Role.Team == Team.CHI).All(x => spies.ContainsKey(x)) && !pList.Any(x => x.Role == RoleType.ClassD))
			{
				RevealSpies();
			}
		}

		internal static RoleType GetSpyRoleType(ReferenceHub hub, RoleType role)
		{
			Player player = Player.Get(hub);
			return player != null && spyOriginalRole.ContainsKey(player) ? spyOriginalRole[player] : role;
		}
	}
}
