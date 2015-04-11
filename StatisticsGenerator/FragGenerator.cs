using System;
using DemoInfo;

namespace StatisticsGenerator
{
	public static class FragGenerator
	{
		public static void GenerateFrags(DemoParser parser) {
			parser.ParseHeader ();

			// Make a print on round-start so you can see the actual frags per round. 
			parser.RoundStart += (sender, e) => Console.WriteLine ("New Round, Current Score: T {0} : {1} CT", parser.TScore, parser.CTScore);

			parser.PlayerKilled += (sender, e) => {
				if(e.Killer == null) {
					// The player has murdered himself (falling / own nade / ...)
					Console.WriteLine("<World><0><None>");
				} else {
					Console.Write("<{0}><{1}><{2}>", e.Killer.Name, e.Killer.SteamID, ShortTeam(e.Killer.Team));
				}

				if(e.Assister == null) {
					// nothing
				} else {
					Console.Write(" + <{0}><{1}><{2}>", e.Assister.Name, e.Assister.SteamID, ShortTeam(e.Assister.Team));
				}

				Console.Write(" [{0}]", e.Weapon.Weapon);

				if(e.Headshot) {
					Console.Write("[HS]");
				}

				if(e.PenetratedObjects > 0) {
					Console.Write("[Wall]");
				}

				Console.Write(" ");

				Console.Write("<{0}><{1}><{2}>", e.DeathPerson.Name, e.DeathPerson.SteamID, ShortTeam(e.DeathPerson.Team));

				Console.WriteLine();
			};

			parser.ParseToEnd ();
		}


		private static string ShortTeam(Team team) {
			switch (team) {
			case Team.Spectate:
				return "None";
			case Team.Terrorist:
				return "T";
			case Team.CounterTerrorist:
				return "CT";
			}

			return "None";
		}
	}
}

