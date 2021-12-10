using Exiled.API.Features;
using Exiled.Loader;
using HarmonyLib;
using System.Collections.Generic;

namespace CISpy
{
	public class CISpy : Plugin<Config>
	{
		internal static CISpy instance;
		private EventHandlers ev;

		internal static bool isScp035 = false;

		private Harmony hInstance;

		public static List<int> FFGrants = new List<int>();

		public override void OnEnabled() 
		{

			hInstance = new Harmony("cm.cispy");
			hInstance.PatchAll();


			if (!Config.IsEnabled) return;
			instance = this;
			Check035();
			ev = new EventHandlers();

			Exiled.Events.Handlers.Server.RoundStarted += ev.OnRoundStart;
			Exiled.Events.Handlers.Server.RespawningTeam += ev.OnTeamRespawn;
			Exiled.Events.Handlers.Player.ChangingRole += ev.OnSetClass;
			Exiled.Events.Handlers.Player.Died += ev.OnPlayerDie;
			Exiled.Events.Handlers.Player.Hurting += ev.OnPlayerHurt;
			Exiled.Events.Handlers.Player.Shot += ev.OnShoot;
			Exiled.Events.Handlers.Player.Left += ev.OnPlayerLeave;
			Exiled.Events.Handlers.Player.Handcuffing += ev.OnHandcuffing;
			Exiled.Events.Handlers.Player.Escaping += ev.OnEscaping;
			Exiled.Events.Handlers.Player.ChangingRole += ev.OnRoleChange;

			base.OnEnabled();
		}

		public override void OnDisabled() 
		{

			hInstance.UnpatchAll();
			hInstance = null;

			Exiled.Events.Handlers.Server.RoundStarted -= ev.OnRoundStart;
			Exiled.Events.Handlers.Server.RespawningTeam -= ev.OnTeamRespawn;
			Exiled.Events.Handlers.Player.ChangingRole -= ev.OnSetClass;
			Exiled.Events.Handlers.Player.Died -= ev.OnPlayerDie;
			Exiled.Events.Handlers.Player.Hurting -= ev.OnPlayerHurt;
			Exiled.Events.Handlers.Player.Shot -= ev.OnShoot;
			Exiled.Events.Handlers.Player.Left -= ev.OnPlayerLeave;
			Exiled.Events.Handlers.Player.Handcuffing -= ev.OnHandcuffing;
			Exiled.Events.Handlers.Player.Escaping -= ev.OnEscaping;
			Exiled.Events.Handlers.Player.ChangingRole -= ev.OnRoleChange;

			ev = null;

			FFGrants = null;
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
	}
}
