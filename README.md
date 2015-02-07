# demostatistics-generator
Generates some statistics for CS:GO demos. 

This project was mainly made as an example for [demoinfo-public](https://github.com/moritzuehling/demoinfo-public). If you want to want an entry-code how demoinfo-public should be used, look at [the `Main()`-method](https://github.com/moritzuehling/demostatistics-generator/blob/master/StatisticsGenerator/Program.cs). 

I tried to document everything. If you have useage-questions you can post them here as an issue!

![Example output](http://i.imgur.com/3AseeH4.png)

[(And the raw data)](https://gist.github.com/moritzuehling/a047a995deb69f24af57)

#Usage of [demoinfo-public](https://github.com/moritzuehling/demoinfo-public)
Include the DemoInfo-Project. You can use the [NuGet-Packet](https://www.nuget.org/packages/DemoInfo/)

You can then can create an instance of the ``DemoParser``-Class. 
```csharp
DemoParser parser = new DemoParser(File.OpenRead("file.dem"));
```
Then you can subscribe to events: 
```csharp
parser.TickDone += parser_TickDone;
parser.MatchStarted += parser_MatchStarted;
parser.PlayerKilled += HandlePlayerKilled;
parser.WeaponFired += HandleWeaponFired;
```
For starting parsing, you first need to parse the Header of the Demo by calling the ``ParseHeader``-Method of the ``DemoParser``. You can either parse the whole demo (then you call ``parser.ParseToEnd()``), or parse tick by tick. (then call  repeatedly ``parser.ParseNextTick ()`` to parse the next tick). The method returns ``true`` as long as there is an other tick. 
