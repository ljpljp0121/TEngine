Cd /d %~dp0
echo %CD%

set WORKSPACE=../..
set CONF_ROOT=.
set LUBAN_DLL=%WORKSPACE%\Tools\Luban\Luban.dll
set CLIENT_OUTPUT_CODE=%WORKSPACE%\Tools\Docs\static

dotnet %LUBAN_DLL% ^
    -t all ^
    -c doc ^
    --conf %CONF_ROOT%\luban.conf ^
    -x outputCodeDir=%CLIENT_OUTPUT_CODE% 
pause