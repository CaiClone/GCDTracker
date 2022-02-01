![Header](images/header.png)
# GCDTracker
A plugin for goat's [XIVLauncher](https://github.com/goaaats/FFXIVQuickLauncher) that helps you learn the basics of combat, such as GCD and animation lock.
Once you enter combat, the plugin will show you a circle with the current status of your GCD, as well as a graph showing the current step in your combo. OGCDs will be added with their correspondent animation locks as they get pressed.

![GCDTracker at work](images/example.gif)

## FAQ

### X action does not trigger the combo tracker?
The game files are used to detect combos, and anything that isn't considered a combo by the game won't be detected.  Trying yet to find a balance between major classes working and not having to add too many exceptions. 


### Why does the animation lock get longer immediately after appearing?
All skills have a default animation lock and later get an update from the server with the remaining animation lock length. this update accounts for the original lock + ping.

### Can I help by testing?
Please do! I don't have all the jobs leveled yet so I really appreciate testing, to do so:

- Add the following custom plugin repository to dalamud: `https://gist.githubusercontent.com/CaiClone/0aad66569dbf63a9bbeec6a8e95a123f/raw/pluginmaster.json`
- Report any issue you find to this repo.
