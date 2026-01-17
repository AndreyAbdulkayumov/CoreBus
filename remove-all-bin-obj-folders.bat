@echo off

REM === СПИСОК ПАПОК ДЛЯ УДАЛЕНИЯ ===
set FOLDERS=bin obj

echo Cleaning folders: %FOLDERS%...

REM Единый цикл для всех папок из списка
for %%F in (%FOLDERS%) do (
    for /d /r %%i in (%%F) do (
        if exist "%%i" (
            echo Cleaning: %%i
            rd /s /q "%%i"
        )
    )
)

echo.
echo Done!
pause