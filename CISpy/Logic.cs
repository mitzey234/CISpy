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
		// full - spawns with loadout
		internal static void MakeSpy(Player player, RoleType originalRole, bool isVulnerable = false, bool full = true, bool delayFull = false)
		{
			try
			{
				if (!spyVulnerability.ContainsKey(player))
				{
					Log.Warn("CREATING SPY: " + player.Nickname);

					spawnPos.Add(player, SpawnpointManager.GetRandomPosition(RoleType.NtfPrivate).transform.position);
					spyOriginalRole.Add(player, originalRole);

					spyVulnerability.Add(player, isVulnerable);

					//if (spawn) player.SetRole(spawnRole, Exiled.API.Enums.SpawnReason.Respawn);
					player.Broadcast(10, "<i><size=60>You are a <b><color=\"green\">CISpy</color></b></size>\nCheck your console by pressing [`] or [~] for more info.</i>");
					player.ReferenceHub.characterClassManager.TargetConsolePrint(player.ReferenceHub.scp079PlayerScript.connectionToClient, "You are a Chaos Insurgency Spy! You are immune to MTF for now, but as soon as you damage an MTF, your spy immunity will turn off.\n\nHelp Chaos win the round and kill as many MTF and Scientists as you can!", "yellow");

					if (full) spyLoadoutQueue.Enqueue(player);
				}
			}
			catch(Exception e)
			{
				Log.Error(e);
			}
		}

		private static void GrantSpyLoadout(Player player)
		{
			if (spyOriginalRole.ContainsKey(player))
			{
				InventorySystem.InventoryRoleInfo inventory = InventorySystem.Configs.StartingInventories.DefinedInventories[spyOriginalRole[player]];
				player.ClearInventory();
				for (int i = 0; i < inventory.Items.Length; i++)
				{
					ItemType type = inventory.Items[i];
					if (type == ItemType.GrenadeHE && !CISpy.instance.Config.SpawnWithGrenade)
					{
						type = ItemType.GrenadeFlash;
					}
					player.AddItem(type);
				}
				player.AddItem(ItemType.KeycardChaosInsurgency);
				Timing.CallDelayed(0.1f, () =>
				{
					player.Ammo.Clear();
					for (int i = inventory.Ammo.Count - 1; i >= 0; i--)
					{
						var entry = inventory.Ammo.ElementAt(i);
						player.Ammo[entry.Key] = entry.Value;
					}
					player.Inventory.SendAmmoNextFrame = true;
				});
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
			for (int i = spyVulnerability.Count - 1; i >= 0; i--)
			{
				var spy = spyVulnerability.ElementAt(i);
				if (spy.Key != null && spy.Key.IsAlive && spy.Key.IsConnected)
				{
					spy.Key.CurrentItem = default;
					spy.Key.Broadcast(10, "<size=60><b>You have been <color=red>Revealed</color></b></size>\nYour fellow <color=\"green\">Chaos Insurgency</color> have died");
					//spy.Key.Broadcast(10, "<i>Your fellow <color=\"green\">Chaos Insurgency</color> have died.\nYou have been revealed!</i>");
					MirrorExtensions.SendFakeSyncVar(spy.Key, spy.Key.ReferenceHub.networkIdentity, typeof(CharacterClassManager), nameof(CharacterClassManager.NetworkCurClass), (sbyte)spyRole);
					spy.Key.ChangeAppearance(spyRole);
					spiesRevealed = true;
					spyVulnerability[spy.Key] = true;
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

			List<Player> Serpents;
			if (Loader.Plugins.Where(pl => pl.Name == "SerpentsHand").ToList().Count > 0)
			{
				try
				{
					Serpents = (List<Player>)Loader.Plugins.First(pl => pl.Name == "SerpentsHand").Assembly.GetType("SerpentsHand.API.SerpentsHand").GetMethod("GetSHPlayers", BindingFlags.Public | BindingFlags.Static).Invoke(null, null);
				}
				catch (System.Exception e)
				{
					Serpents = new List<Player>();
				}
			}
			else
			{
				Serpents = new List<Player>();
			}

			List<Player> pList = Player.List.Where(x =>
			!scp035.Contains(x) &&
			!spyVulnerability.ContainsKey(x)).ToList();

			bool CiAlive = CountRoles(Team.CHI, pList) > 0;
			bool ScpAlive = CountRoles(Team.SCP, pList) > 0 + scp035.Count + Serpents.Count;
			bool DClassAlive = CountRoles(Team.CDP, pList) > 0;
			bool ScientistsAlive = CountRoles(Team.RSC, pList) > 0;
			bool MTFAlive = CountRoles(Team.MTF, pList) > 0;

			//List<Player> pList = Player.Get(x => !scp035.Contains(x)).ToList();
			//if (pList.Where(x => x.Role.Team == Team.CHI).All(x => spies.ContainsKey(x)) && !pList.Any(x => x.Role == RoleType.ClassD))
			if  (
					((ScpAlive || DClassAlive) && !ScientistsAlive && !MTFAlive) ||
					((ScientistsAlive || MTFAlive) && !CiAlive && !ScpAlive && !DClassAlive) ||
					(!CiAlive && !ScpAlive && !DClassAlive && !ScientistsAlive && !MTFAlive && spyVulnerability.Count > 0)
				)
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
