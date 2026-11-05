@echo off
setlocal EnableExtensions EnableDelayedExpansion

REM === СПИСОК ПАПОК ДЛЯ УДАЛЕНИЯ ===
set FOLDERS=bin obj
set ERRORS=0

echo Cleaning folders: %FOLDERS%...

REM Единый цикл для всех папок из списка
for %%F in (%FOLDERS%) do (
    for /d /r %%i in (%%F) do (
        if exist "%%i" (
            echo Cleaning: %%i
            rd /s /q "%%i"
            if exist "%%i" (
                echo Failed to remove: %%i
                set /a ERRORS+=1
            )
        )
    )
)

echo.
if !ERRORS! neq 0 (
    echo Done with !ERRORS! error^(s^).
    pause
    exit /b 1
)

echo Done!
pause
exit /b 0
