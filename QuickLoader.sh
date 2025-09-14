#!/usr/bin/env bash

# enable overwriting with redirects
set -o noclobber

levels=(DEBUG INFO WARN ERROR FATAL)
verbosity=1
mods_directory="$HOME/.steam/steam/steamapps/compatdata/3805420/pfx/drive_c/users/steamuser/AppData/Local/StickZ_001/mods"

usage() {
    echo <<EOF
usage: $0 [options]

options:
  -v, --verbosity   sets the verbosity level of logs written to the console, each level includes itself and all levels to the right of it. allowed values are quiet, debug, info, warn/warning, and error.
EOF

    exit $1
}

while [[ $# -gt 0 ]]; do
    case "$1" in
        -v|--verbosity)
            case "${2^^}" in
                QUIET) verbosity=-1;;
                DEBUG) verbosity=0;;
                INFO) verbosity=1;;
                WARN|WARNING) verbosity=2;;
                ERROR) verbosity=3;;
                *) usage 1;;
            esac
            shift 2;;
        -h|--help) usage 0;;
        -*) echo "Unsupported option: $1"; usage 1;;
        *) break;;
    esac
done

# temporarily run in the context of the mods directory, for nothing other than slightly less clunky code
pushd "$mods_directory" > /dev/null

# calcalate the sha256sum of sha256sums of every file in mods (excluding loader) recursively
checksum=$(find . -type f -not -path './QuickLoader/*' -print0 | sort -z | xargs -0 sha256sum | sha256sum)

# check if the mods.checksum file has been created
if [ -f QuickLoader/mods.checksum ]; then
    # if the current and previous checksums are equal then no changes have been
    # made and we can run the game in the background then exit the script
    if [[ "$checksum" == "$(cat QuickLoader/mods.checksum)" ]]; then
        echo 'No changes detected, launching game ...'

        "$@" &

        sleep 5
        exit 0
    fi
else
    # if both the mods.checksum and original.win files don't exist then assume
    # this is a fresh install and copy the original data.win for later use
    if [ ! -f QuickLoader/original.win ]; then
        cp "$OLDPWD/data.win" QuickLoader/original.win
    fi
fi

echo 'Changes detected, rebuilding data.win ...'

# return to the game directory
popd > /dev/null

# run the mod loader on the original data.win and write the result to the data.win in the game directory
QLMODSPATH="$mods_directory" "$mods_directory/QuickLoader/cli/UndertaleModCli" load "$mods_directory/QuickLoader/original.win" -s "$mods_directory/QuickLoader/scripts/Patcher.csx" -o data.win &
patcher_pid=$!

# only display logs if verbosity is at a level higher than quiet
if (( verbosity >= 0 )); then
    touch "$mods_directory/QuickLoader/log.txt"
    # filter newly written logs to the level requested, and write them to the console
    tail --pid $patcher_pid -n0 -f "$mods_directory/QuickLoader/log.txt" | grep -E --line-buffered "^\[$(IFS=\|; echo "${levels[*]:$verbosity}")\]" &
fi

# wait to launch the game until the patcher has finished 
wait $patcher_pid
if [ $? -ne 0 ]; then
    echo "[FATAL] [QuickLoader] QuickLoader did not run successfully, aborting."
    exit 1
fi

# update the mods checksum only if the patcher is successful
echo "$checksum" >| $mods_directory/QuickLoader/mods.checksum

"$@" &

sleep 5
