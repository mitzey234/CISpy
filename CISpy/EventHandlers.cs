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

		private bool isDisplayFriendly = false;
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
						MakeSpy(ev.Player);
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

			CheckSpies(ev.Target);
		}

		public void OnPlayerLeave(LeftEventArgs ev)
		{
			CheckSpies(ev.Player);
		}

		public void OnHandcuffing(HandcuffingEventArgs ev)
		{
			if ((spies.ContainsKey(ev.Target) && ev.Cuffer.Team == Team.CHI) ||
				(spies.ContainsKey(ev.Cuffer) && ev.Target.Team == Team.CHI))
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

			if (spies.ContainsKey(ev.Attacker) && !spies.ContainsKey(ev.Target) && (ev.Target.Team == Team.RSC || ev.Target.Team == Team.MTF) && !scp035.Contains(ev.Target))
			{
				if (!spies[ev.Attacker])
				{
					spies[ev.Attacker] = true;
				}
				CISpy.FFGrants.Add(ev.Handler.Base.GetHashCode());
			}
			else if (spies.ContainsKey(ev.Target) && !spies.ContainsKey(ev.Attacker) && (ev.Attacker.Team == Team.MTF || ev.Attacker.Team == Team.RSC))
			{
				if (spies[ev.Target])
				{
					CISpy.FFGrants.Add(ev.Handler.Base.GetHashCode());
				}
			}
		}

		public void OnShoot(ShotEventArgs ev)
		{
			if (ev.Target == null || ev.Shooter == null) return;

			List<Player> scp035 = null;

			if (CISpy.isScp035)
			{
				scp035 = TryGet035();
			}
			 
			if (spies.ContainsKey(ev.Target) && !spies.ContainsKey(ev.Shooter) && ev.Target.Id != ev.Shooter.Id && (ev.Shooter.Team == Team.CHI || ev.Shooter.Team == Team.CDP) && !scp035.Contains(ev.Shooter))
			{
				if (!isDisplayFriendly)
				{
					isDisplayFriendly = true;
				}
				Timing.CallDelayed(3f, () =>
				{
					isDisplayFriendly = false;
				});
				ev.CanHurt = false;
			}
			else if (!spies.ContainsKey(ev.Target) && spies.ContainsKey(ev.Shooter) && (ev.Target.Team == Team.CHI || ev.Target.Team == Team.CDP) && !scp035.Contains(ev.Shooter))
			{
				ev.CanHurt = false;
			}
		}
	}
}
