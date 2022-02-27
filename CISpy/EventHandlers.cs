using Exiled.API.Features;
using MEC;
using System;
using System.Collections.Generic;
using System.Linq;
using Exiled.Events.EventArgs;

namespace CISpy
{
	partial class EventHandlers
	{
		internal static Dictionary<Player, bool> spies = new Dictionary<Player, bool> ();

		private static bool isDisplayFriendly = false;
		//private bool isDisplaySpy = false;

		private Random rand = new Random();

		public void OnRoundStart()
		{
			spies.Clear();
			CISpy.FFGrants.Clear();
			if (rand.Next(1, 101) <= CISpy.instance.Config.GuardSpawnChance)
			{
				Player player = Player.List.FirstOrDefault(x => x.Role == RoleType.FacilityGuard);
				if (player != null)
				{
					Timing.CallDelayed(0.8f, () =>
					{
						MakeSpy(player);
					});
				}
			}
		}

		public void OnTeamRespawn(RespawningTeamEventArgs ev)
		{
			if (ev.NextKnownTeam == Respawning.SpawnableTeamType.NineTailedFox && rand.Next(1, 101) <= CISpy.instance.Config.SpawnChance && ev.Players.Count >= CISpy.instance.Config.MinimumSquadSize)
			{
				List<Player> respawn = new List<Player>(ev.Players);
				Timing.CallDelayed(0.1f, () =>
				{
					List<Player> roleList = respawn.Where(x => CISpy.instance.Config.SpyRoles.Contains(x.Role)).ToList();
					if (roleList.Count > 0)
					{
						Player player = roleList[rand.Next(roleList.Count)];
						if (player != null)
						{
							Timing.CallDelayed(0.8f, () =>
							{
								MakeSpy(player);
							});
						}
					}
				});
			}
		}

		public void OnEscaping(EscapingEventArgs ev)
		{
			if (ev.Player.Role == RoleType.ClassD && ev.Player.IsCuffed && spies.ContainsKey(ev.Player.Cuffer))
			{
				Timing.CallDelayed(0.1f, () =>
				{
					Timing.CallDelayed(0.8f, () =>
					{
						MakeSpy(ev.Player, true);
					});
					RoundSummary.EscapedScientists--;
					RoundSummary.EscapedClassD++;

				});
			}
		}

		public void OnSetClass(ChangingRoleEventArgs ev)
		{
			if (spies.ContainsKey(ev.Player))
			{
				Timing.CallDelayed(0.1f, () => spies.Remove(ev.Player));
			}
		}

		public void OnPlayerDie(DiedEventArgs ev)
		{
			if (spies.ContainsKey(ev.Target))
			{
				spies.Remove(ev.Target);
			}
			if (ev.Target != null && ev.Killer != null && spies.ContainsKey(ev.Killer) && (ev.Target.Role == RoleType.Scientist || ev.Target.Role == RoleType.NtfCaptain || ev.Target.Role == RoleType.NtfPrivate || ev.Target.Role == RoleType.NtfSergeant || ev.Target.Role == RoleType.NtfSpecialist || ev.Target.Role == RoleType.FacilityGuard))
            {
				Respawning.RespawnTickets.Singleton._tickets[Respawning.SpawnableTeamType.ChaosInsurgency]++;
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

			if (spies.ContainsKey(ev.Attacker) && !spies.ContainsKey(ev.Target) && (ev.Target.Role.Team == Team.RSC || ev.Target.Role.Team == Team.MTF) && !scp035.Contains(ev.Target))
			{
				if (!spies[ev.Attacker])
				{
					spies[ev.Attacker] = true;
				}
				CISpy.FFGrants.Add(ev.Handler.Base.GetHashCode());
			}
			else if (spies.ContainsKey(ev.Target) && !spies.ContainsKey(ev.Attacker) && (ev.Attacker.Role.Team == Team.MTF || ev.Attacker.Role.Team == Team.RSC))
			{
				if (spies[ev.Target])
				{
					CISpy.FFGrants.Add(ev.Handler.Base.GetHashCode());
				}
			} 
			else if (spies.ContainsKey(ev.Target) && !spies.ContainsKey(ev.Attacker) && ev.Target.Id != ev.Attacker.Id && (ev.Attacker.Role.Team == Team.CHI || ev.Attacker.Role.Team == Team.CDP) && !scp035.Contains(ev.Attacker))
            {
				ev.IsAllowed = false;
            } 
			else if (!spies.ContainsKey(ev.Target) && spies.ContainsKey(ev.Attacker) && (ev.Target.Role.Team == Team.CHI || ev.Target.Role.Team == Team.CDP) && !scp035.Contains(ev.Attacker))
			{
				ev.IsAllowed = false;
			}
		}

		public static bool OnShoot(Player attacker, Player target)
		{
			if (target == null || attacker == null) return true;
			List<Player> scp035 = new List<Player>();

			if (CISpy.isScp035)
			{
				scp035 = TryGet035();
			}
			//If target is spy, and attacker is not spy, and its not suicide, and team is chaos, and player is not 035
			if (spies.ContainsKey(target) && !spies.ContainsKey(attacker) && target.Id != attacker.Id && (attacker.Role.Team == Team.CHI || attacker.Role.Team == Team.CDP) && !scp035.Contains(attacker))
			{
				if (!isDisplayFriendly)
				{
					isDisplayFriendly = true;
				}
				Timing.CallDelayed(3f, () =>
				{
					isDisplayFriendly = false;
				});
				return false;
			}
			else if (!spies.ContainsKey(target) && spies.ContainsKey(attacker) && (target.Role.Team == Team.CHI || target.Role.Team == Team.CDP) && !scp035.Contains(attacker))
			{
				return false;
			}
			return true; 
		}
	}
}
