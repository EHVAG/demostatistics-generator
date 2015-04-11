# demostatistics-generator
Generates some statistics for CS:GO demos. 



This project was mainly made as an example for [demoinfo-public](https://github.com/moritzuehling/demoinfo-public). If you want to want an entry-code how demoinfo-public should be used, look at [the `Main()`-method](https://github.com/moritzuehling/demostatistics-generator/blob/master/StatisticsGenerator/Program.cs). If you think that this code is waaaay to long for what it does look at the [version where I stripped it of the boilerplate](https://github.com/moritzuehling/demostatistics-generator/blob/master/StatisticsGenerator/MainClassSmall.cs).

I tried to document everything. If you have useage-questions you can post them here as an issue!

**This code only works for Valve demos (i.e. from majors and matchmaking), for other demos all might not work! This is because there might be mutliple match_started events when the match was restarted (lo3), and many of those details. This must all be handled in your application-logic, because the parser can't account for that. It isn't handled in this project because this is only an example to show how the parser is used generally**

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
