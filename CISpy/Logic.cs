using Exiled.API.Features;
using Exiled.Loader;
using MEC;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CISpy
{
	using Exiled.API.Enums;
	using System.Reflection;

	partial class EventHandlers
	{
		internal static void MakeSpy(Player player, bool isVulnerable = false, bool full = true)
		{
			try
			{
				if (!CISpy.instance.Config.SpawnWithGrenade && full)
				{
					for (int i = player.Items.Count - 1; i >= 0; i--)
					{
						if (player.Items.ElementAt(i).Type == ItemType.GrenadeHE)
						{
							player.RemoveItem(player.Items.ElementAt(i));
						}
					}
				}
				player.AddItem(ItemType.KeycardChaosInsurgency);
				spies.Add(player, isVulnerable);
				player.Broadcast(10, "<i><size=60>You are a <b><color=\"green\">CISpy</color></b></size>\nCheck your console by pressing [`] or [~] for more info.</i>");
				player.ReferenceHub.characterClassManager.TargetConsolePrint(player.ReferenceHub.scp079PlayerScript.connectionToClient, "You are a Chaos Insurgency Spy! You are immune to MTF for now, but as soon as you damage an MTF, your spy immunity will turn off.\n\nHelp Chaos win the round and kill as many MTF and Scientists as you can.", "yellow");
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
			foreach (KeyValuePair<Player, bool> spy in spies)
			{
				if (spy.Key != null && spy.Key.IsAlive && spy.Key.IsConnected)
                {
					if (spy.Key.Zone != ZoneType.Surface && ((Warhead.IsInProgress && Warhead.DetonationTimer <= 1) || Warhead.IsDetonated))
                    {
						spies.Remove(spy.Key);
					}
					int health = (int)spy.Key.Health;
					Dictionary<global::ItemType, ushort> ammo = new Dictionary<global::ItemType, ushort>();
					foreach (global::ItemType ammoType in spy.Key.Ammo.Keys)
					{
						ammo.Add(ammoType, spy.Key.Ammo[ammoType]);
					}
					Vector3 saved = spy.Key.Position;
					List<ItemType> savedItems = spy.Key.Items.Select(x => x.Type).ToList();
					spy.Key.ClearInventory();
					spy.Key.Role.Type = RoleType.ChaosConscript;

					Timing.CallDelayed(0.5f, () =>
					{
						spy.Key.Health = health;
						spy.Key.Position = saved;
						spy.Key.ResetInventory(savedItems);
						foreach (global::ItemType ammoType in ammo.Keys) spy.Key.Ammo[ammoType] = ammo[ammoType];
					});
					spy.Key.Broadcast(10, "<i>Your fellow <color=\"green\">Chaos Insurgency</color> have died.\nYou have been revealed!</i>");
				}
			}
			spies.Clear();
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

			List<Player> Serpants;
			if (Loader.Plugins.Where(pl => pl.Name == "SerpentsHand").ToList().Count > 0)
			{
				try
				{
					Serpants = (List<Player>)Loader.Plugins.First(pl => pl.Name == "SerpentsHand").Assembly.GetType("SerpentsHand.API.SerpentsHand").GetMethod("GetSHPlayers", BindingFlags.Public | BindingFlags.Static).Invoke(null, null);
				}
				catch (System.Exception e)
				{
					Serpants = new List<Player>();
				}
			}
			else
			{
				Serpants = new List<Player>();
			}

			int playerid = -1;
			if (exclude != null) playerid = exclude.Id;
			List<Player> pList = Player.List.Where(x =>
			x.Id != playerid &&
			!scp035.Contains(x) &&
			!spies.ContainsKey(x)).ToList();

			bool CiAlive = CountRoles(Team.CHI, pList) > 0;
			bool ScpAlive = CountRoles(Team.SCP, pList) > 0 + scp035.Count + Serpants.Count;
			bool DClassAlive = CountRoles(Team.CDP, pList) > 0;
			bool ScientistsAlive = CountRoles(Team.RSC, pList) > 0;
			bool MTFAlive = CountRoles(Team.MTF, pList) > 0;

			if
			(
				((CiAlive || (CiAlive && ScpAlive) || (CiAlive && DClassAlive)) && !ScientistsAlive && !MTFAlive) ||
				((ScpAlive || DClassAlive) && !ScientistsAlive && !MTFAlive) ||
				((ScientistsAlive || MTFAlive || (ScientistsAlive && MTFAlive)) && !CiAlive && !ScpAlive && !DClassAlive) ||
				(!CiAlive && !ScpAlive && !DClassAlive && !ScientistsAlive && !MTFAlive && spies.Count > 0)
			)
			{
				RevealSpies();
			}
		}
	}
}
