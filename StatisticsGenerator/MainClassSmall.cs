using System;
using System.IO;
using DemoInfo;
using System.Collections.Generic;
using System.Linq;

namespace StatisticsGenerator
{
	public static class MainClassSmall
	{
		public static void DoStuff (string[] args)
		{
			foreach (var fileName in args) {
				using (var fileStream = File.OpenRead (fileName)) {
					Console.WriteLine ("Parsing demo " + fileName);
					using (var parser = new DemoParser (fileStream)) {
						parser.ParseHeader ();
						string map = parser.Map, outputFileName = fileName + "." + map + ".csv";
						var outputStream = new StreamWriter (outputFileName);
						bool hasMatchStarted = false;
						int ctStartroundMoney = 0, tStartroundMoney = 0, ctEquipValue = 0, tEquipValue = 0, ctSaveAmount = 0, tSaveAmount = 0;
						float ctWay = 0, tWay = 0;
						int defuses = 0; 
						int plants = 0;
						Dictionary<Player, int> killsThisRound = new Dictionary<Player, int> ();

						parser.MatchStarted += (sender, e) => {
							hasMatchStarted = true;
						};

						parser.PlayerKilled += (object sender, PlayerKilledEventArgs e) => {
							if(e.Killer != null) {
								if(!killsThisRound.ContainsKey(e.Killer))
									killsThisRound[e.Killer] = 0;
								killsThisRound[e.Killer]++;
							}
						};

						parser.RoundStart += (sender, e) => {
							if(!hasMatchStarted)
								return;
							ctStartroundMoney = parser.Participants.Where(a => a.Team == Team.CounterTerrorist).Sum(a => a.Money);
							tStartroundMoney = parser.Participants.Where(a => a.Team == Team.Terrorist).Sum(a => a.Money);
							ctSaveAmount = parser.Participants.Where(a => a.Team == Team.CounterTerrorist && a.IsAlive).Sum(a => a.CurrentEquipmentValue);
							tSaveAmount = parser.Participants.Where(a => a.Team == Team.Terrorist && a.IsAlive).Sum(a => a.CurrentEquipmentValue);
							ctWay = 0; tWay = 0;
							plants = 0; defuses = 0;
							killsThisRound.Clear();
						};

						parser.FreezetimeEnded += (sender, e) => {
							if(!hasMatchStarted)
								return;
							ctEquipValue = parser.Participants.Where(a => a.Team == Team.CounterTerrorist).Sum(a => a.CurrentEquipmentValue);
							tEquipValue = parser.Participants.Where(a => a.Team == Team.Terrorist).Sum(a => a.CurrentEquipmentValue);
						};

						parser.BombPlanted += (sender, e) => {
							if(!hasMatchStarted)
								return;
							plants++;
						};

						parser.BombDefused += (sender, e) => {
							if(!hasMatchStarted)
								return;
							defuses++;
						};

						parser.TickDone += (sender, e) => {
							if(!hasMatchStarted)
								return;
							foreach(var player in parser.PlayingParticipants)
							{
								float currentWay = (float)(player.Velocity.Absolute * parser.TickTime);
								if(player.Team == Team.CounterTerrorist)
									ctWay += currentWay;
								else if(player.Team == Team.Terrorist)
									tWay += currentWay;
							}
						};

						parser.RoundEnd += (sender, e) => {
							if(!hasMatchStarted)
								return;
							PrintRoundResults (parser, outputStream, ctStartroundMoney, tStartroundMoney, ctEquipValue, tEquipValue, ctSaveAmount, tSaveAmount, ctWay, tWay, defuses, plants, killsThisRound);
						};

						parser.ParseToEnd ();
						PrintRoundResults (parser, outputStream, ctStartroundMoney, tStartroundMoney, ctEquipValue, tEquipValue, ctSaveAmount, tSaveAmount, ctWay, tWay, defuses, plants, killsThisRound);
						outputStream.Close ();
					}
				}
			}
		}

		static void PrintRoundResults (DemoParser parser, StreamWriter outputStream, int ctStartroundMoney, int tStartroundMoney, int ctEquipValue, int tEquipValue, int ctSaveAmount, int tSaveAmount, float ctWay, float tWay, int defuses, int plants, Dictionary<Player, int> killsThisRound)
		{
			//okay, get the topfragger:
			var topfragger = killsThisRound.OrderByDescending (x => x.Value).FirstOrDefault ();
			if (topfragger.Equals (default(KeyValuePair<Player, int>)))
				topfragger = new KeyValuePair<Player, int> (new Player (), 0);
			//At the end of each round, let's write down some statistics!
			outputStream.WriteLine (string.Format ("{0};{1};{2};{3};{4};{5};{6};{7};{8};{9};{10};{11};{12};{13};{14};{15};{16};{17};{18};{19};{20};{21};{22};{23};", parser.CTScore + parser.TScore, //Round-Number
				parser.CTScore, parser.TScore, //how many CTs are still alive?
				parser.PlayingParticipants.Count (a => a.IsAlive && a.Team == Team.CounterTerrorist), //how many Ts are still alive?
				parser.PlayingParticipants.Count (a => a.IsAlive && a.Team == Team.Terrorist), ctStartroundMoney, tStartroundMoney, ctEquipValue, tEquipValue, ctSaveAmount, tSaveAmount, ctWay, tWay, //The kills of all CTs so far
				parser.PlayingParticipants.Where (a => a.Team == Team.CounterTerrorist).Sum (a => a.AdditionaInformations.Kills), parser.PlayingParticipants.Where (a => a.Team == Team.Terrorist).Sum (a => a.AdditionaInformations.Kills), //The deaths of all CTs so far
				parser.PlayingParticipants.Where (a => a.Team == Team.CounterTerrorist).Sum (a => a.AdditionaInformations.Deaths), parser.PlayingParticipants.Where (a => a.Team == Team.Terrorist).Sum (a => a.AdditionaInformations.Deaths), //The assists of all CTs so far
				parser.PlayingParticipants.Where (a => a.Team == Team.CounterTerrorist).Sum (a => a.AdditionaInformations.Assists), parser.PlayingParticipants.Where (a => a.Team == Team.Terrorist).Sum (a => a.AdditionaInformations.Assists), plants, defuses, "\"" + topfragger.Key.Name + "\"", //The name of the topfragger this round
				topfragger.Key.SteamID, //The steamid of the topfragger this round
				topfragger.Value//The amount of kills he got
			));
		}
	}
}

