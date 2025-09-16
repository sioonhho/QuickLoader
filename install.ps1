$ModsDir = "$env:LocalAppData\Stickz_001\mods"
New-Item -ItemType Directory -Force $ModsDir | Out-Null

if (Test-Path "$ModsDir\QuickLoader") {
    Write-Host "It looks like QuickLoader is already installed, if not please ensure there isn't a QuickLoader folder in $ModsDir"

    Write-Host 'Press any key to exit ...'
    [System.Console]::ReadKey($false)

    exit 1
}

Write-Host 'Installing QuickLoader ...'

$TempDir = Join-Path $env:Temp (New-Guid).Guid
New-Item -ItemType Directory $TempDir | Out-Null

$ZipFile = Join-Path $TempDir QuickLoader-v0.1.0-windows.zip
$QuickLoaderDir = Join-Path $TempDir QuickLoader-v0.1.0-windows

$ProgressPreference = 'SilentlyContinue'

Write-Host 'Downloading QuickLoader-v0.1.0-windows.zip ...'
Invoke-WebRequest "https://github.com/sioonhho/QuickLoader/releases/download/v0.1.0/QuickLoader-v0.1.0-windows.zip" -OutFile "$TempDir\QuickLoader-v0.1.0-windows.zip"

$ProgressPreference = 'Continue'

Expand-Archive "$TempDir\QuickLoader-v0.1.0-windows.zip" $TempDir

Write-Host 'Copying QuickLoader to the mods directory ...'
Copy-Item -Recurse "$TempDir\QuickLoader" "$ModsDir\QuickLoader"

Write-Host 'Copying wrapper script to the game directory ...'
Move-Item "$ModsDir\QuickLoader\QuickLoader.ps1" "C:\Program Files (x86)\Steam\steamapps\common\A Few Quick Matches\QuickLoader.ps1"

Write-Host 'Installation complete!'

Write-Host 'Press any key to exit ...'
[System.Console]::ReadKey($false)
