# QuickLoader

A collection of [UndertaleModTool](https://github.com/UnderminersTeam/UndertaleModTool) scripts designed for seamless modding in A Few Quick Matches.

# Features

* Smart Reloading
   * Only rebuilds the `data.win` if changes are detected in the `mods` directory
* Currently Supported Modifications
   * Sprites
      * Also creates collision masks from any sprites with names ending in `_hb`
   * GML and Assembly
      * Includes an extensive "patching" system that provides in place modification of code (inspired by [lovely](https://github.com/ethangreen-dev/lovely-injector))
   * Game Objects
   * Rooms

# Installation

1. Download the [latest](https://github.com/sioo.nhho/QuickLoader/releases/latest) release for your platform (currently only Windows and Linux/Steam Deck are supported)
2. Run the install script by double clicking on `install.bat` if you're on Windows, and `install.sh` if you're on Linux/Steam Deck.
3. Set the game's launch options to the following depending on your platform:
    * On Windows: `C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe -ExecutionPolicy Bypass .\QuickLoader.ps1 ""%command%""`
    * On Linux/Steam Deck: `./QuickLoader.sh %command%`

# Usage

## Installing Mods

It's as simple as downloading a new mod, unzipping it into it's own folder (this is important) in the mods folder, and running the game through steam. The loader will pick up on the fact that the mods have changed and rebuild the `data.win` before launching the game as normal.

## Uninstalling Mods

Deleting the mod's folder is all you have to do, then the next time you run the game it'll rebuild the `data.win` without that mod.

## Disabling/Enabling Mods

If a mod's folder contains a file called `.modignore` then that mod will be skipped when building the `data.win`. Currently these have to be created and removed manually, but I *may* have something in the works related to that.

# License

This software is release under the [GPL-3.0 License](https://www.gnu.org/licenses/gpl-3.0.en.html).

# Special Thanks

A huge shoutout to the AFQM Modding Community over on [Discord](https://discord.gg/afqm)
