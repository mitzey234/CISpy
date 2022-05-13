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
		internal static Dictionary<Player, RoleType> spyOriginalRole = new Dictionary<Player, RoleType>();
		internal static Queue<Player> spyLoadoutQueue = new Queue<Player>();
		internal static Dictionary<Player, bool> spies = new Dictionary<Player, bool> ();
		internal static Dictionary<Player, Vector3> spawnPos = new Dictionary<Player, Vector3>();

		private bool spiesRevealed = false;

		private System.Random rand = new System.Random();

		public void OnRoundStart()
		{
			spies.Clear();
			spyOriginalRole.Clear();
			spyLoadoutQueue.Clear();
			spawnPos.Clear();
			spiesRevealed = false;
			CISpy.FFGrants.Clear();
			/*if (rand.Next(1, 100) < CISpy.instance.Config.GuardSpawnChance)
			{
				Player player = Player.List.FirstOrDefault(x => x.Role == RoleType.FacilityGuard);
				if (player != null)
				{
					MakeSpy(player, RoleType.FacilityGuard);
				}
			}*/
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

		public void OnPlayerJoin(VerifiedEventArgs ev)
		{
			foreach (var entry in spies)
			{
				if (spiesRevealed)
				{
					MirrorExtensions.SendFakeSyncVar(ev.Player, entry.Key.ReferenceHub.networkIdentity, typeof(CharacterClassManager), nameof(CharacterClassManager.NetworkCurClass), (sbyte)RoleType.ChaosConscript);
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
				ev.IsAllowed = false;
				Queue<RoleType> queue = new Queue<RoleType>();
				RespawnWaveGenerator.SpawnableTeams.TryGetValue(ev.NextKnownTeam, out var spawnableTeamHandlerBase);
				spawnableTeamHandlerBase.GenerateQueue(queue, ev.Players.Count);
				List<RoleType> roleList = queue.ToList();

				// index 0 is always commander -- skip
				//int indx = rand.Next(1, ev.Players.Count);
				int indx = rand.Next(0, ev.Players.Count);
				RoleType originalRole = roleList[indx];
				roleList[indx] = RoleType.ChaosConscript;

				for (int i = 0; i < ev.Players.Count; i++)
				{
					RoleType role = roleList[i];
					Player player = ev.Players[i];
					if (role == RoleType.ChaosConscript)
					{
						MakeSpy(player, role, originalRole, true, false);
					}
					else ev.Players[i].SetRole(role);
				}

				ServerLogs.AddLog(ServerLogs.Modules.ClassChange, string.Concat(new object[]
				  {
					"RespawnManager has successfully spawned ",
					ev.Players.Count,
					" players as ",
					ev.NextKnownTeam.ToString(),
					"!"
				  }), ServerLogs.ServerLogType.GameEvent, false);
				RespawnTickets.Singleton.GrantTickets(SpawnableTeamType.NineTailedFox, -roleList.Count * spawnableTeamHandlerBase.TicketRespawnCost, false);
				Respawning.NamingRules.UnitNamingRule unitNamingRule;
				if (Respawning.NamingRules.UnitNamingRules.TryGetNamingRule(SpawnableTeamType.NineTailedFox, out unitNamingRule))
				{
					string text;
					unitNamingRule.GenerateNew(ev.NextKnownTeam, out text);
					foreach (ReferenceHub referenceHub2 in ev.Players.Select(x => x.ReferenceHub))
					{
						referenceHub2.characterClassManager.NetworkCurSpawnableTeamType = (byte)ev.NextKnownTeam;
						referenceHub2.characterClassManager.NetworkCurUnitName = text;
					}
					unitNamingRule.PlayEntranceAnnouncement(text);
				}
				RespawnEffectsController.ExecuteAllEffects(RespawnEffectsController.EffectType.UponRespawn, ev.NextKnownTeam);
			}
			RespawnManager.Singleton.NextKnownTeam = SpawnableTeamType.None;
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

			if (target != null && spies.ContainsKey(target))
			{
				if (ev.Shooter.Role.Team != Team.RSC && ev.Shooter.Role.Team != Team.MTF)
				{
					if (!spiesRevealed)
					{
						string hint = $"\n\n\n\n\n\n\n\n\n\n\n\n\nYou are shooting a <color=#03811a><b>CISpy!</b></color>";
						CISpy.AccessHintSystem(ev.Shooter, hint, 2f, 0);
					}
				}
				else if (!spies[target])
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
			if (ev.Player.Role == RoleType.ClassD && ev.Player.IsCuffed && spies.ContainsKey(ev.Player.Cuffer))
			{
				MakeSpy(ev.Player, RoleType.ChaosConscript, ev.NewRole);
				RoundSummary.EscapedScientists--;
				RoundSummary.EscapedClassD++;
			}
		}

		public void OnSetClass(ChangingRoleEventArgs ev)
		{
			if (spawnPos.ContainsKey(ev.Player))
			{
				ev.Ammo.Clear();
			}
			if (ev.Reason == Exiled.API.Enums.SpawnReason.ForceClass && spies.ContainsKey(ev.Player))
			{
				spyOriginalRole.Remove(ev.Player);
				Timing.CallDelayed(0.1f, () => spies.Remove(ev.Player));
			}
		}

		// todo: refine reveal conditions

		public void OnPlayerDie(DiedEventArgs ev)
		{
			if (spies.ContainsKey(ev.Target))
			{
				if (spyOriginalRole.ContainsKey(ev.Target)) spyOriginalRole.Remove(ev.Target);
				spies.Remove(ev.Target);
			}
			if (ev.Target != null && ev.Killer != null && spies.ContainsKey(ev.Killer) && (ev.Target.Role == RoleType.Scientist || ev.Target.Role == RoleType.NtfCaptain || ev.Target.Role == RoleType.NtfPrivate || ev.Target.Role == RoleType.NtfSergeant || ev.Target.Role == RoleType.NtfSpecialist || ev.Target.Role == RoleType.FacilityGuard))
            {
				RespawnTickets.Singleton._tickets[Respawning.SpawnableTeamType.ChaosInsurgency]++;
            }
			CheckSpies(ev.Target);
		}

		public void OnPlayerLeave(LeftEventArgs ev)
		{
			CheckSpies(ev.Player);
		}

		public void OnHandcuffing(HandcuffingEventArgs ev)
		{
			if ((spies.ContainsKey(ev.Target) && ev.Cuffer.Role.Team == Team.CHI) ||
				(spies.ContainsKey(ev.Cuffer) && ev.Target.Role.Team == Team.CHI))
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

			if (spies.ContainsKey(ev.Attacker) &&
				!spies.ContainsKey(ev.Target) &&
				(ev.Target.Role.Team == Team.RSC || ev.Target.Role.Team == Team.MTF) &&
				!scp035.Contains(ev.Target) &&
				!spies[ev.Attacker])
			{
				spies[ev.Attacker] = true;
				ev.Attacker.Broadcast(8, "<size=60><b>You are now <color=red>vulnerable</color></b>\n<i>You have damaged an <color=#058df1>MTF</color> or <color=#ffff7c>Scientist</color></i>");
			}
			else if (spies.ContainsKey(ev.Target) && !spies.ContainsKey(ev.Attacker) && (ev.Attacker.Role.Team == Team.MTF || ev.Attacker.Role.Team == Team.RSC) && !spies[ev.Target])
			{
				ev.IsAllowed = false;
			} 
		}
	}
}
