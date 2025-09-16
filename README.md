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

(OLD) check below
1. Download the [latest](https://github.com/sioo.nhho/QuickLoader/releases/latest) release for your platform (currently only Windows and Linux/Steam Deck are supported)
2. Create a directory called `mods` in the game's save directory, this should be:
    * On Windows: `%LocalAppData%\StickZ_001`
    * On Linux/Steam Deck: `~/.steam/steam/steamapps/compatdata/3805420/pfx/drive_c/users/steamuser/AppData/Local/StickZ_001`
3. Navigate to the `mods` directory you created, and unzip the file you downloaded
    * At this point the folder structure should look like this:
        ```
        StickZ_001
        |-- mods
        |   `-- QuickLoader
        |       `-- ...
        `-- ...
        ```
4. Move the wrapper script from the `QuickLoader` folder into the game directory, this should be:
    * On Windows (`QuickLoader.ps1`): `C:\Program Files (x86)\Steam\steamapps\common\A Few Quick Matches`
    * On Linux/Steam Deck (`QuickLoader.sh`): `~/.steam/steam/steamapps/common/A Few Quick Matches`
5. Set the game's launch options to the following depending on your platform:
    * On Windows: `C:\Windows\System32\WindowsPowerShell\v1.0\powersell.exe -ExecutionPolicy Bypass .\QuickLoader.ps1 ""%command""`
    * On Linux/Steam Deck: `./QuickLoader.sh %command%`

Windows: Press Windows+R, paste the following code, and run: `powershell.exe "(curl 'https://raw.githubusercontent.com/sioonhho/QuickLoader/refs/heads/main/install.ps1').Content | iex"`

Linux/Steam Deck: Run the following in a terminal: `wget -qO- 'https://raw.githubusercontent.com/sioonhho/QuickLoader/refs/heads/main/install.sh' | bash`

Then set your launch options like in the final step of the old installation instructions above.

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
