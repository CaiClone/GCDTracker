![Header](images/header.png)
# GCDTracker
A plugin for goat's [XIVLauncher](https://github.com/goaaats/FFXIVQuickLauncher) designed to help familiarize yourself with basic combats such as GCD and animation lock in the game.
Once you install the plugin and start attacking the nearest thing a circle showing the current status of your weaponskill GCD will be displayed as well as a graph showing your combo position. OGCDs will be added to the wheel with their correspondent animation locks as they get pressed.

![GCDTracker at work](images/example.gif)

## FAQ

### X action does not trigger the combo tracker?
Combos are automatically extracted from the game files so that means that whatever the game doesn't consider strictly as a combo won't be detected as one. Trying yet to find a balance between major classes working and not having to add exceptions for every possible combo.


### Why does the animation lock get longer immediately after appearing?
All animation locks start equal, then they get an update from the server indicating their remaining length accounting for the ability animation lock+lag.

### Can I help by testing?
Please do! I don't have all the jobs leveled yet so I really appreciate testing, to do so:

- Add the following custom plugin repository to dalamaud: `https://gist.githubusercontent.com/CaiClone/0aad66569dbf63a9bbeec6a8e95a123f/raw/pluginmaster.json`
- Report any issue you find to this repo.
