Cd /d %~dp0
echo %CD%

set WORKSPACE=../..
set LUBAN_DLL=%WORKSPACE%\Tools\Luban\Luban.dll
set CONF_ROOT=.
set DATA_OUTPATH=%WORKSPACE%/UnityProject/Assets/Bundle/Configs/bytes/
set CODE_OUTPATH=%WORKSPACE%/UnityProject/Assets/Client/HotFix/Client_Base/GameConfig/

xcopy /s /e /i /y "%CONF_ROOT%\CustomTemplate\TableSystem.cs" "%WORKSPACE%\UnityProject\Assets\Client\HotFix\Client_Base\TableSystem.cs"
xcopy /s /e /i /y "%CONF_ROOT%\CustomTemplate\ExternalTypeUtil.cs" "%WORKSPACE%\UnityProject\Assets\Client\HotFix\Client_Base\ExternalTypeUtil.cs"

dotnet %LUBAN_DLL% ^
    -t client ^
    -c cs-simple-json ^
    -d json ^
    --conf %CONF_ROOT%\luban.conf ^
    --customTemplateDir %CONF_ROOT%\CustomTemplate\Templates ^
    -x outputCodeDir=%CODE_OUTPATH% ^
    -x outputDataDir=%DATA_OUTPATH%

pause