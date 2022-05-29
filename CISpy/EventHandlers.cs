using Exiled.API.Features;
using MEC;
using System;
using System.Collections.Generic;
using System.Linq;
using Exiled.Events.EventArgs;
using UnityEngine;
using Respawning;
using Exiled.API.Extensions;
using Exiled.API.Features.Items;

namespace CISpy
{
	partial class EventHandlers
	{
		// The "true" role for a spy
		internal static RoleType spyRole = RoleType.ChaosConscript;

		// Save the "fake" roles for each player
		internal static Dictionary<Player, RoleType> spyOriginalRole = new Dictionary<Player, RoleType>();

		// Queue for granting loadouts due to a needed delay
		internal static Queue<Player> spyLoadoutQueue = new Queue<Player>();

		// TeamRespawnQueue
		internal static List<Player> teamRespawnQueue = new List<Player>();
		internal static bool hasChosenSpy = false;

		// Dictionary saving whether or not a spy is vulnerable
		internal static Dictionary<Player, bool> spyVulnerability = new Dictionary<Player, bool> ();

		// Queue for spawning spies in the proper place
		internal static Dictionary<Player, Vector3> spawnPos = new Dictionary<Player, Vector3>();

		private bool spiesRevealed = false;

		private System.Random rand = new System.Random();

		public void OnRoundStart()
		{
			spyVulnerability.Clear();
			spyOriginalRole.Clear();
			spyLoadoutQueue.Clear();
			spawnPos.Clear();
			teamRespawnQueue.Clear();
			spiesRevealed = false;
			hasChosenSpy = true;
			if (rand.Next(100) < CISpy.instance.Config.GuardSpawnChance)
			{
				Player player = Player.List.FirstOrDefault(x => x != null && x.Role == RoleType.FacilityGuard);
				if (player != null)
				{
					MakeSpy(player, RoleType.FacilityGuard);
				}
			}
		}

		public void OnSpawning(SpawningEventArgs ev)
		{
			if (spawnPos.ContainsKey(ev.Player))
			{
				ev.Position = spawnPos[ev.Player];
				spawnPos.Remove(ev.Player);
			}
		}

		public void OnSpawned(SpawnedEventArgs ev)
		{
			if (spyLoadoutQueue.Contains(ev.Player))
			{
				GrantSpyLoadout(spyLoadoutQueue.Dequeue());
			}
		}

		public void OnTeslaGate(TriggeringTeslaEventArgs ev)
		{
			if (spyVulnerability.ContainsKey(ev.Player) && !spiesRevealed)
			{
				ev.IsTriggerable = false;
				ev.IsInIdleRange = false;
			}
		}

		public void OnPlayerJoin(VerifiedEventArgs ev)
		{
			foreach (var entry in spyVulnerability)
			{
				if (spiesRevealed)
				{
					MirrorExtensions.SendFakeSyncVar(ev.Player, entry.Key.ReferenceHub.networkIdentity, typeof(CharacterClassManager), nameof(CharacterClassManager.NetworkCurClass), (sbyte)spyRole);
				}
				else if (spyOriginalRole.ContainsKey(entry.Key))
				{
					MirrorExtensions.SendFakeSyncVar(ev.Player, entry.Key.ReferenceHub.networkIdentity, typeof(CharacterClassManager), nameof(CharacterClassManager.NetworkCurClass), (sbyte)spyOriginalRole[entry.Key]);
				}
			}
		}

		public void OnTeamRespawn(RespawningTeamEventArgs ev)
		{
			if (ev.NextKnownTeam == SpawnableTeamType.NineTailedFox && rand.Next(1, 100) < CISpy.instance.Config.SpawnChance && ev.Players.Count >= CISpy.instance.Config.MinimumSquadSize)
			{
				teamRespawnQueue = new List<Player>(ev.Players);
				teamRespawnQueue.ShuffleList();
				hasChosenSpy = false;
			}
		}

		public void OnRoundEnded(RoundEndedEventArgs ev)
		{
			RevealSpies();
		}

		public void OnShoot(ShootingEventArgs ev)
		{
			Player target = Player.Get(ev.TargetNetId);

			List<Player> scp035 = null;
			try
			{
				scp035 = TryGet035();
			}
			catch (Exception x)
			{
				Log.Debug("SCP-035 not installed, skipping method call...");
			}

			if (scp035 != null && scp035.Contains(ev.Shooter)) return;

			if (target != null && spyVulnerability.ContainsKey(target))
			{
				if (ev.Shooter.Role.Team != Team.RSC && ev.Shooter.Role.Team != Team.MTF)
				{
					if (!spiesRevealed)
					{
						string hint = $"\n\n\n\n\n\n\n\n\n\n\n\n\nYou are shooting a <color=#03811a><b>CISpy!</b></color>";
						CISpy.AccessHintSystem(ev.Shooter, hint, 2f, 0);
					}
				}
				else if (!spyVulnerability[target])
				{
					ev.IsAllowed = false;
					if (ev.Shooter.CurrentItem is Firearm firearm)
					{
						firearm.Ammo -= 1;
					}
				}
			}
		}

		public void OnEscaping(EscapingEventArgs ev)
		{
			if (ev.Player.Role == RoleType.ClassD && ev.Player.IsCuffed && spyVulnerability.ContainsKey(ev.Player.Cuffer))
			{
				MakeSpy(ev.Player, ev.NewRole);
				RoundSummary.EscapedScientists--;
				RoundSummary.EscapedClassD++;
			}
		}

		public void OnSetClass(ChangingRoleEventArgs ev)
		{
			if (ev.Reason == Exiled.API.Enums.SpawnReason.ForceClass && spyVulnerability.ContainsKey(ev.Player))
			{
				spyOriginalRole.Remove(ev.Player);
				Timing.CallDelayed(0.1f, () => spyVulnerability.Remove(ev.Player));
			}

			if (ev.NewRole == RoleType.Spectator) return;

			// Case for respawn wave
			if (!hasChosenSpy && teamRespawnQueue.Contains(ev.Player) && CISpy.instance.Config.SpyRoles.Contains(ev.NewRole))
			{
				MakeSpy(ev.Player, ev.NewRole);
				ev.NewRole = spyRole;
				teamRespawnQueue.Clear();
				hasChosenSpy = true;
			}
			// Case for any other spawn
			else if (spyOriginalRole.ContainsKey(ev.Player))
			{
				ev.NewRole = spyRole;
			}

			if (spawnPos.ContainsKey(ev.Player))
			{
				ev.Ammo.Clear();
				ev.Player.Inventory.SendAmmoNextFrame = true;
			}
		}

		// todo: refine reveal conditions

		public void OnPlayerDie(DiedEventArgs ev)
		{
			if (spyVulnerability.ContainsKey(ev.Target))
			{
				if (spyOriginalRole.ContainsKey(ev.Target)) spyOriginalRole.Remove(ev.Target);
				spyVulnerability.Remove(ev.Target);
			}
			/*if (ev.Target != null && ev.Killer != null && spyVulnerability.ContainsKey(ev.Killer) && (ev.Target.Role == RoleType.Scientist || ev.Target.Role == RoleType.NtfCaptain || ev.Target.Role == RoleType.NtfPrivate || ev.Target.Role == RoleType.NtfSergeant || ev.Target.Role == RoleType.NtfSpecialist || ev.Target.Role == RoleType.FacilityGuard))
            {
				RespawnTickets.Singleton._tickets[SpawnableTeamType.ChaosInsurgency]++;
            }*/
			CheckSpies(ev.Target);
		}

		public void OnPlayerLeave(LeftEventArgs ev)
		{
			CheckSpies(ev.Player);
		}

		public void OnHandcuffing(HandcuffingEventArgs ev)
		{
			if ((spyVulnerability.ContainsKey(ev.Target) && ev.Cuffer.Role.Team == Team.CHI) ||
				(spyVulnerability.ContainsKey(ev.Cuffer) && ev.Target.Role.Team == Team.CHI))
			{
				ev.IsAllowed = false;
			}
		}

		public void OnPlayerHurt(HurtingEventArgs ev)
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

			if (ev.Attacker == null || ev.Target == null) return;

			if (spyVulnerability.ContainsKey(ev.Attacker) &&
				!spyVulnerability.ContainsKey(ev.Target) &&
				(ev.Target.Role.Team == Team.RSC || ev.Target.Role.Team == Team.MTF) &&
				!scp035.Contains(ev.Target) &&
				!spyVulnerability[ev.Attacker])
			{
				spyVulnerability[ev.Attacker] = true;
				ev.Attacker.Broadcast(8, "<size=60><b>You are now <color=red>Vulnerable</color></b></size>\n<i>You have damaged an <color=#058df1>MTF</color> or <color=#ffff7c>Scientist</color></i>");
			}
			else if (spyVulnerability.ContainsKey(ev.Target) && !spyVulnerability.ContainsKey(ev.Attacker) && (ev.Attacker.Role.Team == Team.MTF || ev.Attacker.Role.Team == Team.RSC) && !spyVulnerability[ev.Target])
			{
				ev.IsAllowed = false;
			} 
		}
	}
}
