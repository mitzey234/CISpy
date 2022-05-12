using Exiled.API.Features;
using Exiled.Loader;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CISpy
{
	public class CISpy : Plugin<Config>
	{
		internal static CISpy instance;
		private EventHandlers ev;

		internal static bool isScp035 = false;

		private Harmony hInstance;

		private bool state = false;

		public static List<int> FFGrants = new List<int>();

		public override void OnEnabled() 
		{
			if (state) return;
			hInstance = new Harmony("cm.cispy");
			instance = this;
			hInstance.PatchAll();

			Check035();
			ev = new EventHandlers();

			Exiled.Events.Handlers.Server.RoundStarted += ev.OnRoundStart;
			Exiled.Events.Handlers.Server.RespawningTeam += ev.OnTeamRespawn;
			Exiled.Events.Handlers.Server.RoundEnded += ev.OnRoundEnded;
			Exiled.Events.Handlers.Player.ChangingRole += ev.OnSetClass;
			Exiled.Events.Handlers.Player.Died += ev.OnPlayerDie;
			Exiled.Events.Handlers.Player.Hurting += ev.OnPlayerHurt;
			Exiled.Events.Handlers.Player.Left += ev.OnPlayerLeave;
			Exiled.Events.Handlers.Player.Handcuffing += ev.OnHandcuffing;
			Exiled.Events.Handlers.Player.Escaping += ev.OnEscaping;
			Exiled.Events.Handlers.Player.Spawning += ev.OnSpawning;
			Exiled.Events.Handlers.Player.Spawned += ev.OnSpawned;
			Exiled.Events.Handlers.Player.Shooting += ev.OnShoot;
			Exiled.Events.Handlers.Player.Verified += ev.OnPlayerJoin;

			FFGrants = new List<int>();

			state = true;
			base.OnEnabled();
		}

		public override void OnDisabled() 
		{
			if (!state) return;
			hInstance.UnpatchAll(hInstance.Id);
			hInstance = null;

			Exiled.Events.Handlers.Server.RoundStarted -= ev.OnRoundStart;
			Exiled.Events.Handlers.Server.RespawningTeam -= ev.OnTeamRespawn;
			Exiled.Events.Handlers.Server.RoundEnded -= ev.OnRoundEnded;
			Exiled.Events.Handlers.Player.ChangingRole -= ev.OnSetClass;
			Exiled.Events.Handlers.Player.Died -= ev.OnPlayerDie;
			Exiled.Events.Handlers.Player.Hurting -= ev.OnPlayerHurt;
			Exiled.Events.Handlers.Player.Left -= ev.OnPlayerLeave;
			Exiled.Events.Handlers.Player.Handcuffing -= ev.OnHandcuffing;
			Exiled.Events.Handlers.Player.Escaping -= ev.OnEscaping;
			Exiled.Events.Handlers.Player.Spawning -= ev.OnSpawning;
			Exiled.Events.Handlers.Player.Spawned -= ev.OnSpawned;
			Exiled.Events.Handlers.Player.Shooting -= ev.OnShoot;
			Exiled.Events.Handlers.Player.Verified -= ev.OnPlayerJoin;

			ev = null;

			FFGrants = null;

			state = false;
			base.OnDisabled();
		}

		public override string Name => "CiSpy";
		public override string Author => "Cyanox";

		internal void Check035()
		{
			foreach (var plugin in Loader.Plugins)
			{
				if (plugin.Name == "scp035")
				{
					isScp035 = true;
					return;
				}
			}
		}

		internal static void AccessHintSystem(Player p, string hint, float time, int lineBuffer)
		{
			Loader.Plugins.FirstOrDefault(pl => pl.Name == "HintSystem")?.Assembly?.GetType("HintSystem.HintSystem")?.GetMethod("ShowHint", BindingFlags.Public | BindingFlags.Static)?.Invoke(null, new object[] { p, hint, time, lineBuffer });
		}
	}
}
