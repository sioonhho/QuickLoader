@echo off

set "ModsDir=%LocalAppData%\Stickz_001\mods"

if not exist "%ModsDir%" (
    echo Creating the mods directory ...
    md "%ModsDir%"
)

if exist "%ModsDir%\QuickLoader" (
    echo Updating QuickLoader installation ...
) else (
    echo Installing QuickLoader ...
)

echo Copying QuickLoader to the mods directory ...
xcopy . "%ModsDir%\QuickLoader" /ehiksy > NUL
del "%ModsDir%\QuickLoader\install.bat"

echo Moving the wrapper script to the game directory ...
move "%ModsDir%\QuickLoader\QuickLoader.ps1" "C:\Program Files (x86)\Steam\steamapps\common\A Few Quick Matches\QuickLoader.ps1" > NUL

echo Installation complete!
pause
