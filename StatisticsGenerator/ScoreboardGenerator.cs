using System;
using DemoInfo;
using System.Linq;

namespace StatisticsGenerator
{
	public static class ScoreboardGenerator
	{
		public static void GenerateScoreboards(DemoParser parser) {
			int i = 0;

			parser.ParseHeader ();

			Console.WriteLine ("map: " + parser.Map);

			int roundEndedCount = 0;

			parser.RoundEnd += (object sender, RoundEndedEventArgs e) => {
				// The reason I'm doing this after tick_done is that
				// entity-updates only come in the same tick as round_end
				// comes, meaning the score-update might not be transmitted yet
				// which would be sad - we always want the current score!
				// so we wait 1 second. 

				roundEndedCount = 1;
			};

			parser.TickDone += (object sender, TickDoneEventArgs e) => {
				if(roundEndedCount == 0)
					return;

				roundEndedCount ++;

				// Wait twice the tickrate of the demo (~2 seconds) to make sure the 
				// screen has been updated. I *LOVE* demo files :)
				if(roundEndedCount < parser.TickRate * 2) {
					return;
				}

				roundEndedCount = 0;


				Console.WriteLine ("------------------------------------------------------------");

				Console.WriteLine ("Round {0}, CT: {1}, T: {2}", ++i, parser.CTScore, parser.TScore);

				Console.WriteLine("Ts\t" + parser.TClanName);
				Console.WriteLine("Tag\tName\tSteamID\tKills\tDeaths\tAssists\tScore\t");
				foreach(var player in parser.PlayingParticipants.Where(a => a.Team == Team.Terrorist))
					Console.WriteLine (
						"{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}", 
						player.AdditionaInformations.Clantag, 
						player.Name, player.SteamID, 
						player.AdditionaInformations.Kills, 
						player.AdditionaInformations.Deaths, 
						player.AdditionaInformations.Assists,
						player.AdditionaInformations.Score
					);

				Console.WriteLine("CTs\t" + parser.CTClanName);
				Console.WriteLine("Tag\tName\tSteamID\tKills\tDeaths\tAssists\tScore\t");
				foreach(var player in parser.PlayingParticipants.Where(a => a.Team == Team.CounterTerrorist))
					Console.WriteLine (
						"{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}", 
						player.AdditionaInformations.Clantag, 
						player.Name, player.SteamID, 
						player.AdditionaInformations.Kills, 
						player.AdditionaInformations.Deaths, 
						player.AdditionaInformations.Assists,
						player.AdditionaInformations.Score
					);


				Console.WriteLine ();

			};


			parser.ParseToEnd ();
		}
	}
}

