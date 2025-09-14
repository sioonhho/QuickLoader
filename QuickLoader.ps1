param([switch]$Verbose, [string]$SteamCommand)

$ModsDirectory = Join-Path $env:LocalAppData StickZ_001\mods

Push-Location $ModsDirectory

# calcalate the sha256sum of sha256sums of every file in mods (excluding loader) recursively
$Hashes = (Get-ChildItem -Directory -Exclude QuickLoader | Get-ChildItem -File -Recurse  | Sort-Object | Get-FileHash -Algorithm SHA256).Hash | Out-String
$Checksum = (Get-FileHash -InputStream ([IO.MemoryStream]::new([char[]]$Hashes))).Hash

# check if the mods.checksum file has been created
if (Test-Path QuickLoader\mods.checksum -PathType Leaf) {
    # if the current and previous checksums are equal then no changes have been
    # made and we can run the game in the background then exit the script
    if ($Checksum -eq (Get-Content QuickLoader\mods.checksum)) {
        Write-Host 'No changes detected, launching game ...'
        & $SteamCommand
        [System.Console]::ReadKey($false)
        exit
    }
} elseif (!(Test-Path QuickLoader\original.win -PathType Leaf)) {
    # if both the mods.checksum and original.win files don't exist then assume
    # this is a fresh install and copy the original data.win for later use
    Copy-Item (Join-Path (Get-Location -Stack).Path data.win) QuickLoader\original.win
}

# update the mods checksum
Set-Content QuickLoader\mods.checksum $Checksum

Pop-Location

Write-Host "Changes detected, rebuilding data.win ...`n"

$env:QLMODSPATH = $ModsDirectory

# run the mod loader on the original data.win and write the result to the data.win in the game directory
$Patcher = Start-Process -PassThru -NoNewWindow -RedirectStandardOutput NUL (Join-Path $ModsDirectory QuickLoader\cli\UndertaleModCli.exe) "load $(Join-Path $ModsDirectory QuickLoader\original.win) -s $(Join-Path $ModsDirectory QuickLoader\scripts\Patcher.csx) -o data.win"

if (!(Test-Path (Join-Path $ModsDirectory QuickLoader\log.txt) -PathType Leaf)) {
    New-Item -ItemType File (Join-Path $ModsDirectory QuickLoader\log.txt) | Out-Null
}

$Tail = Start-Job { Get-Content -Wait -Tail 0 (Join-Path $using:ModsDirectory QuickLoader\log.txt) }

# tail the log file until the mod loader finishes
while (Get-Process -Id $Patcher.Id -ErrorAction SilentlyContinue) {
    Receive-Job $Tail | ForEach-Object {
        if (!$Verbose -and !($_.StartsWith("[DEBUG]"))) {
            $_
        } else {
            $_
        }
    }
    Start-Sleep -Milliseconds 200
}

# get any remaining log output and stop the tail process
Receive-Job $Tail
Stop-Job $Tail

Remove-Item Env:\QLMODSPATH

Write-Host "`nQuickLoader finished, launching game ..."
& $SteamCommand
[System.Console]::ReadKey($false)
