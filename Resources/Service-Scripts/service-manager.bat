@echo off
setlocal enabledelayedexpansion

if "%1"=="" (
    echo Usage: %~n0 [command] [service_name] [path]
    echo Commands: install, start, stop, delete
    echo Example: %~n0 install MyService "C:\Path\To\Service.exe --args"
    goto :eof
)

set command=%1
set service_name=%2
set service_path=%3

if "%command%"=="install" (
    if "%service_path%"=="" (
        echo Error: Path required for installation
        goto :eof
    )

    :: Extract directory from path (removes executable name)
    for %%A in ("%service_path%") do set "exe_path=%%~dpA"
    set "contentRoot=!exe_path:~0,-1!"  :: Remove trailing backslash

    echo Installing service %service_name%...
    sc create "%service_name%" binpath= "%service_path% --contentRoot !contentRoot!" start= auto
    if !errorlevel! equ 0 (
        echo Service %service_name% installed successfully
        sc start "%service_name%"
    )
) else if "%command%"=="start" (
    echo Starting service %service_name%...
    sc start "%service_name%"
) else if "%command%"=="stop" (
    echo Stopping service %service_name%...
    sc stop "%service_name%"
) else if "%command%"=="delete" (
    echo Deleting service %service_name%...
    sc stop "%service_name%"
    timeout /t 2 /nobreak >nul
    sc delete "%service_name%"
) else (
    echo Invalid command: %command%
    echo Valid commands are: install, start, stop, delete
)

endlocal