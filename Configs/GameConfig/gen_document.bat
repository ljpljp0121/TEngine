Cd /d %~dp0
echo %CD%

set WORKSPACE=../..
set CONF_ROOT=.
set LUBAN_DLL=%WORKSPACE%\Configs\Luban\Luban.dll
set CLIENT_OUTPUT_CODE=%WORKSPACE%\Configs\Docs\static

dotnet %LUBAN_DLL% ^
    -t all ^
    -c doc ^
    --conf %CONF_ROOT%\luban.conf ^
    --customTemplateDir %CONF_ROOT%\CustomTemplate\Templates ^
    -x outputCodeDir=%CLIENT_OUTPUT_CODE% 
pause