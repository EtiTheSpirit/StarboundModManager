# Starbound Mod Manager

> [!WARNING]
> SBMM is currently a **beta test.** It may have issues managing your modpacks, and mod lists might be broken spontaneously. I did my testing, but it might not be perfect.

Starbound Mod Manager (SBMM) is a cross-platform tool which allows you to manage and share modpacks for Starbound.

**Note:** SBMM is not affiliated with Chucklefish. This is a piece of fan-made software.

## Download

Downloads are on [the releases page](https://github.com/XansWorkshop/StarboundModManager/releases/latest). Scroll down to the bottom.

Setup is automatic and it should do everything for you the moment it starts.

# FAQ / Features

### ℹ Does this work with OpenStarbound?
**Yes.** In fact, it *only* works with OpenStarbound. SBMM does not support the standard release of the game.

### ℹ Does this work on Mac/Linux?
**Yes.** SBMM is cross-platform. The only exception is that Mac cannot run dedicated servers.

### ℹ Can I use mods/collections from the Steam Workshop?
**Yes.** It even works if you don't have the game on Steam at all! This applies to both individual mods, and entire collections.

The only limit is that the mods/collections must be **public.** Unlisted, hidden, or friends-only items can't be installed by SBMM.

### ℹ I forgot to unsubscribe from my Steam Workshop mods! Did I break my save?
**No.** SBMM keeps its own private mod storage, and doesn't use Steam's workshop folder. You can (un)subscribe to whatever you want, at any time, and it will never mess with any of your modpacks.

### ℹ Do I need to back up my save files to play on another modpack?
**No.** SBMM creates separate save files for each modpack. This is also true for Dedicated Servers; you can host separate servers for different modpacks and they will each use their own saves.

### ℹ Does multiplayer still work through Steam?
**Yes.** Granted you and whoever is joining own the game, you can use Steam's Join Game feature with SBMM just like you can with normal Starbound.

### ℹ Do I need to have the game on Steam?
**Ideally.** SBMM uses your Steam installation to get ahold of `packed.pak` and `tiled` - the game's assets. 

If you don't have it available to you for whatever reason, you can place a `starbound.zip` file in the same location that you install the program. If that zip file contains `assets/packed.pak` and `tiled/` (aka, a real Starbound installation) then SBMM will use that instead of your Steam install for its first-time setup. You can delete it afterwards.

### ℹ Can I still play the base game using Steam?
**Yes.** Your Steam installation of the game is separate from SBMM, so it will work just like it always has.

***

**More coming soon! Check the [to-do list](https://github.com/XansWorkshop/StarboundModManager/blob/master/TODO.md).**