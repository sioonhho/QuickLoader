#!/usr/bin/env bash

modsdir="$HOME/.steam/steam/steamapps/compatdata/3805420/pfx/drive_c/users/steamuser/AppData/Local/StickZ_001/mods"

if [ ! -d "$modsdir" ]; then
    echo Creating the mods directory...
    mkdir "$modsdir"
fi

if [ -d "$modsdir/QuickLoader" ]; then
    echo Updating QuickLoader...
else
    echo Installing QuickLoader...
fi

echo Copying QuickLoader to the mods directory...
cp -r . "$modsdir/QuickLoader"
rm "$modsdir/QuickLoader/install.sh"

echo Moving the wrapper script to the game directory...
mv "$modsdir/QuickLoader/QuickLoader.sh" "$HOME/.steam/steam/steamapps/common/A Few Quick Matches/QuickLoader.sh"

echo Installation complete!
read -n 1 -s -r -p 'Press any key to continue . . .'
echo
