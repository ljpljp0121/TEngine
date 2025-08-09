Cd /d %~dp0
echo %CD%

set WORKSPACE=../..
set LUBAN_DLL=%WORKSPACE%\Tools\Luban\Luban.dll
set CONF_ROOT=.
set DATA_OUTPATH=%WORKSPACE%/UnityProject/Assets/Bundle/Configs/bytes/
set CODE_OUTPATH=%WORKSPACE%/UnityProject/Assets/Client/HotFix/Client_Config/GameConfig/

xcopy /s /e /i /y "%CONF_ROOT%\CustomTemplate\ConfigSystem.cs" "%WORKSPACE%\UnityProject\Assets\Client\HotFix\Client_Config\ConfigSystem.cs"
xcopy /s /e /i /y "%CONF_ROOT%\CustomTemplate\ExternalTypeUtil.cs" "%WORKSPACE%\UnityProject\Assets\Client\HotFix\Client_Config\ExternalTypeUtil.cs"

dotnet %LUBAN_DLL% ^
    -t client ^
    -c cs-bin ^
    -d bin^
    --conf %CONF_ROOT%\luban.conf ^
    --customTemplateDir %CONF_ROOT%\CustomTemplate\CustomTemplate_Client_LazyLoad ^
    -x code.lineEnding=crlf ^
    -x outputCodeDir=%CODE_OUTPATH% ^
    -x outputDataDir=%DATA_OUTPATH% 
pause

