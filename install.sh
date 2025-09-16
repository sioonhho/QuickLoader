modsdir="$HOME/.steam/steam/steamapps/compatdata/3805420/pfx/drive_c/users/steamuser/AppData/Local/StickZ_001/mods"
mkdir -p $modsdir

# if [ -d $modsdir/QuickLoader ]; then
#     echo "It looks like QuickLoader is already installed, if not please ensure there isn't a QuickLoader folder in $modsdir"
#     exit 1
# fi

tmpdir="/tmp/$(uuidgen)"

wget -q --show-progress https://github.com/sioonhho/QuickLoader/releases/download/v0.1.0/QuickLoader-v0.1.0-linux.zip -P $tmpdir

echo Unzipping QuickLoader-v0.1.0-linux.zip ...
unzip -q $tmpdir/QuickLoader-v0.1.0-linux.zip -d $tmpdir

echo Copying QuickLoader to the mods directory ...
cp -r $tmpdir/QuickLoader $modsdir/QuickLoader

echo Copying wrapper script to the game directory ...
cp $tmpdir/QuickLoader/QuickLoader.sh "$HOME/.steam/steam/steamapps/common/A Few Quick Matches/QuickLoaderTmp.sh"

echo Installation complete!
