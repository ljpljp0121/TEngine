Cd /d %~dp0
echo %CD%

set WORKSPACE=../..
set LUBAN_DLL=..\Luban\Luban.dll
set CONF_ROOT=.
set DATA_OUTPATH=%WORKSPACE%/UnityProject/Assets/AssetRaw/Configs/bytes/
set CODE_OUTPATH=%WORKSPACE%/UnityProject/Assets/PFGAS/Generated/LubanConfig

dotnet %LUBAN_DLL% ^
    -t client ^
    -c cs-bin ^
    -d bin^
    --conf %CONF_ROOT%\luban.conf ^
    -x outputCodeDir=%CODE_OUTPATH%\ ^
    -x outputDataDir=%DATA_OUTPATH% 
pause

