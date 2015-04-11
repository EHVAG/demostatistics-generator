using System;
using System.Reflection;
using System.IO;
using DemoInfo;
using System.Collections.Generic;
using System.Linq;

namespace StatisticsGenerator
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			// First, check wether the user needs assistance:
			if (args.Length == 0 || args [0] == "--help") {
				PrintHelp ();
				return;
			}
			
			// Every argument is a file, so let's iterate over all the arguments
			// So you can call this program like
			// > StatisticsGenerator.exe hello.dem bye.dem
			// It'll generate the statistics.
			foreach (var fileName in args) {
				// Okay, first we need to initalize a demo-parser
				// It takes a stream, so we simply open with a filestream
				using (var fileStream = File.OpenRead (fileName)) {
					// By using "using" we make sure that the fileStream is properly disposed
					// the same goes for the DemoParser which NEEDS to be disposed (else it'll
					// leak memory and kittens will die. 

					Console.WriteLine ("Parsing demo " + fileName);

					using (var parser = new DemoParser (fileStream)) {
						// So now we've initialized a demo-parser. 
						// let's parse the head of the demo-file to get which map the match is on!
						// this is always the first step you need to do.
						parser.ParseHeader ();

						// and now, do some magic: grab the match!
						string map = parser.Map;

						// And now, generate the filename of the resulting file
						string outputFileName = fileName + "." + map + ".csv";
						// and open it. 
						var outputStream = new StreamWriter (outputFileName);

						//And write a header so you know what is what in the resulting file
						outputStream.WriteLine (GenerateCSVHeader ());

						// Cool! Now let's get started generating the analysis-data. 

						//Let's just declare some stuff we need to remember

						// Here we'll save how far a player has travelled each round. 
						// Here we remember wheter the match has started yet. 
						bool hasMatchStarted = false;

						int ctStartroundMoney = 0, tStartroundMoney = 0, ctEquipValue = 0, tEquipValue = 0, ctSaveAmount = 0, tSaveAmount = 0;

						float ctWay = 0, tWay = 0;

						int defuses = 0; 
						int plants = 0;


						Dictionary<Player, int> killsThisRound = new Dictionary<Player, int> ();

						List<Player> ingame = new List<Player> ();

						// Since most of the parsing is done via "Events" in CS:GO, we need to use them. 
						// So you bind to events in C# as well. 

						// AFTER we have bound the events, we start the parser!


						parser.MatchStarted += (sender, e) => {
							hasMatchStarted = true;
							//Okay let's output who's really in this game!

							Console.WriteLine("Participants: ");
							Console.WriteLine("  Terrorits \"{0}\": ", parser.CTClanName);

							foreach(var player in parser.PlayingParticipants.Where(a => a.Team == Team.Terrorist))
								Console.WriteLine ("    {0} {1} (Steamid: {2})", player.AdditionaInformations.Clantag, player.Name, player.SteamID);

							Console.WriteLine("  Counter-Terrorits \"{0}\": ", parser.TClanName);
							foreach(var player in parser.PlayingParticipants.Where(a => a.Team == Team.CounterTerrorist))
								Console.WriteLine ("    {0} {1} (Steamid: {2})", player.AdditionaInformations.Clantag, player.Name, player.SteamID);

							// Okay, problem: At the end of the demo
							// a player might have already left the game,
							// so we need to store some information
							// about the players before they left :)
							ingame.AddRange(parser.PlayingParticipants);
						};

						parser.PlayerKilled += (object sender, PlayerKilledEventArgs e) => {
							//the killer is null if you're killed by the world - eg. by falling
							if(e.Killer != null) {
								if(!killsThisRound.ContainsKey(e.Killer))
									killsThisRound[e.Killer] = 0;

								//Remember how many kills each player made this rounds
								killsThisRound[e.Killer]++;
							}
						};

						parser.RoundStart += (sender, e) => {
							if(!hasMatchStarted)
								return;

							//How much money had each team at the start of the round?
							ctStartroundMoney = parser.Participants.Where(a => a.Team == Team.CounterTerrorist).Sum(a => a.Money);
							tStartroundMoney = parser.Participants.Where(a => a.Team == Team.Terrorist).Sum(a => a.Money);

							//And how much they did they save from the last round?
							ctSaveAmount = parser.Participants.Where(a => a.Team == Team.CounterTerrorist && a.IsAlive).Sum(a => a.CurrentEquipmentValue);
							tSaveAmount = parser.Participants.Where(a => a.Team == Team.Terrorist && a.IsAlive).Sum(a => a.CurrentEquipmentValue);

							//And let's reset those statistics
							ctWay = 0; tWay = 0;
							plants = 0; defuses = 0;

							killsThisRound.Clear();
						};

						parser.FreezetimeEnded += (sender, e) => {
							if(!hasMatchStarted)
								return;

							// At the end of the freezetime (when players can start walking)
							// calculate the equipment value of each team!
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

							// Okay, let's measure how far each team travelled. 
							// As you might know from school the amount walked
							// by a player is the sum of it's velocities

							foreach(var player in parser.PlayingParticipants)
							{
								// We multiply it by the time of one tick
								// Since the velocity is given in 
								// ingame-units per second
								float currentWay = (float)(player.Velocity.Absolute * parser.TickTime);

								// This is just an example of what kind of stuff you can do
								// with this parser. 
								// Of course you could find out who makes the most footsteps, and find out
								// which player ninjas the most - just to give you an example

								if(player.Team == Team.CounterTerrorist)
									ctWay += currentWay;
								else if(player.Team == Team.Terrorist)
									tWay += currentWay;
							}
						};

						//So now lets do some fancy output
						parser.RoundEnd += (sender, e) => {
							if(!hasMatchStarted)
								return;

							// We do this in a method-call since we'd else need to duplicate code
							// The much parameters are there because I simply extracted a method
							// Sorry for this - you should be able to read it anywys :)
							PrintRoundResults (parser, outputStream, ctStartroundMoney, tStartroundMoney, ctEquipValue, tEquipValue, ctSaveAmount, tSaveAmount, ctWay, tWay, defuses, plants, killsThisRound);
						};

						//Now let's parse the demo!
						parser.ParseToEnd ();

						//And output the result of the last round again. 
						PrintRoundResults (parser, outputStream, ctStartroundMoney, tStartroundMoney, ctEquipValue, tEquipValue, ctSaveAmount, tSaveAmount, ctWay, tWay, defuses, plants, killsThisRound);



						//Lets just display an end-game-scoreboard!

						Console.WriteLine("Finished! Results: ");
						Console.WriteLine("  Terrorits \"{0}\": ", parser.CTClanName);

						foreach(var player in ingame.Where(a => a.Team == Team.Terrorist))
							Console.WriteLine (
								"    {0} {1} (Steamid: {2}): K: {3}, D: {4}, A: {5}", 
								player.AdditionaInformations.Clantag, 
								player.Name, player.SteamID, 
								player.AdditionaInformations.Kills, 
								player.AdditionaInformations.Deaths, 
								player.AdditionaInformations.Assists
							);

						Console.WriteLine("  Counter-Terrorits \"{0}\": ", parser.TClanName);
						foreach(var player in ingame.Where(a => a.Team == Team.CounterTerrorist))
							Console.WriteLine (
								"    {0} {1} (Steamid: {2}): K: {3}, D: {4}, A: {5}", 
								player.AdditionaInformations.Clantag, 
								player.Name, player.SteamID, 
								player.AdditionaInformations.Kills, 
								player.AdditionaInformations.Deaths, 
								player.AdditionaInformations.Assists
							);


						outputStream.Close ();
					}

					


				}
			}
		
		}

		static string GenerateCSVHeader()
		{
			return string.Format(
				"{0};{1};{2};{3};{4};{5};{6};{7};{8};{9};{10};{11};{12};{13};{14};{15};{16};{17};{18};{19};{20};{21};{22};{23};",
				"Round-Number", // parser.CTScore + parser.TScore, //Round-Number
				"CT-Score", // parser.CTScore,
				"T-Score", // parser.TScore,
				//how many CTs are still alive?
				"SurvivingCTs", // parser.PlayingParticipants.Count(a => a.IsAlive && a.Team == Team.CounterTerrorist),
				//how many Ts are still alive?
				"SurvivingTs", // parser.PlayingParticipants.Count(a => a.IsAlive && a.Team == Team.Terrorist),
				"CT-StartMoney", // ctStartroundMoney,
				"T-StartMoney", // tStartroundMoney,
				"CT-EquipValue", // ctEquipValue,
				"T-EquipValue", // tEquipValue,
				"CT-SavedFromLastRound", // ctSaveAmount,
				"T-SavedFromLastRound", // tSaveAmount,
				"WalkedCTWay", // ctWay,
				"WalkedTWay", // tWay,
				//The kills of all CTs so far
				"CT-Kills", // parser.PlayingParticipants.Where(a => a.Team == Team.CounterTerrorist).Sum(a => a.AdditionaInformations.Kills),
				"T-Kills", // parser.PlayingParticipants.Where(a => a.Team == Team.Terrorist).Sum(a => a.AdditionaInformations.Kills),
				//The deaths of all CTs so far
				"CT-Deaths", // parser.PlayingParticipants.Where(a => a.Team == Team.CounterTerrorist).Sum(a => a.AdditionaInformations.Deaths),
				"T-Deaths", // parser.PlayingParticipants.Where(a => a.Team == Team.Terrorist).Sum(a => a.AdditionaInformations.Deaths),
				//The assists of all CTs so far
				"CT-Assists", // parser.PlayingParticipants.Where(a => a.Team == Team.CounterTerrorist).Sum(a => a.AdditionaInformations.Assists),
				"T-Assists", // parser.PlayingParticipants.Where(a => a.Team == Team.Terrorist).Sum(a => a.AdditionaInformations.Assists),
				"BombPlanted", // plants,
				"BombDefused", // defuses,
				"TopfraggerName", // "\"" + topfragger.Key.Name + "\"", //The name of the topfragger this round
				"TopfraggerSteamid", // topfragger.Key.SteamID, //The steamid of the topfragger this round
				"TopfraggerKillsThisRound" // topfragger.Value //The amount of kills he got
			);
		}

		static void PrintHelp ()
		{
			string fileName = Path.GetFileName((Assembly.GetExecutingAssembly().Location));
			Console.WriteLine ("CS:GO Demo-Statistics-Generator");
			Console.WriteLine ("http://github.com/moritzuehling/demostatistics-creator");
			Console.WriteLine ("------------------------------------------------------");
			Console.WriteLine ("Usage: {0} [--help] file1.dem [file2.dem ...]");
			Console.WriteLine ("--help");
			Console.WriteLine ("    Displays this help");
			Console.WriteLine ("file1.dem");
			Console.WriteLine ("    Path to a demo to be parsed. The resulting file with have the same name, ");
			Console.WriteLine ("    except that it'll end with \".dem.[map].csv\", where [map] is the map.");
			Console.WriteLine ("    The resulting file will be a CSV-File containing some statistics generated");
			Console.WriteLine ("    by this program, and can be viewed with (for example) LibreOffice");
			Console.WriteLine ("[file2.dem ...]");
			Console.WriteLine ("    You can specify more than one file at a time.");




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
