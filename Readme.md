# GCDTracker
A plugin for goat's [XivLauncher](https://github.com/goaaats/FFXIVQuickLauncher) designed to help familiarize yourself with basic combats such as GCD and animation lock in the game.
Once you install the plugin and start attacking the nearest thing a circle showing the current status of your weaponskill GCD will be displayed as well as a graph showing your combo position. OGCDs will be added to the wheel with their correspondent animation locks as they get pressed.
 
![GCDTracker at work](example.gif)

## FAQ

### Does the gcd tracking work for casters?
Not for now, need to think a little more on how to make it useful for them.

### Why does the animation lock get bigger inmediately after appearing?
All animation locks start equal, then they get an update from the server indicating their remaining length accounting for the ability animation lock+lag.