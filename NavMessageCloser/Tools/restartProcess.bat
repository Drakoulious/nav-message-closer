if "%1" == "" goto endx

wmic process where handle=%1 get commandline /VALUE|FIND "CommandLine" >proccmd.txt
set /P cmd=<proccmd.txt
taskkill /PID %1 /F
echo %cmd:~12%>>restartprocess.log
cmd.exe /C "%cmd:~12%"

:endx