@echo off
chcp 65001 >nul
setlocal

rem ============================================================
rem  Double-click to build the Android APK.
rem
rem  The real build logic lives in Tools\build-android.sh. This
rem  file is only a launcher and deliberately reimplements none
rem  of it, so the two can never drift apart.
rem
rem  KEEP THIS FILE PURE ASCII. A .bat containing UTF-8 text
rem  desyncs the cmd batch parser (even with chcp 65001): a
rem  multi-byte character gets split and the fragments run as
rem  commands. That is not theoretical -- the first version of
rem  this file did exactly that and reported success while
rem  building nothing. Chinese output comes from the .sh.
rem
rem  chcp 65001 is still wanted: it is what makes the .sh's
rem  UTF-8 Chinese render correctly in this console.
rem
rem  Exit codes: 0 = ok, 1 = build failed, 2 = environment problem
rem ============================================================

set "PROJECT_DIR=%~dp0"
set "BUILD_SCRIPT=%PROJECT_DIR%Tools\build-android.sh"
set "OUTPUT_APK=%PROJECT_DIR%Builds\EasyMoney.apk"

if not exist "%BUILD_SCRIPT%" (
    echo [ERROR] Build script not found:
    echo         %BUILD_SCRIPT%
    goto :fail
)

rem ---- Locate Git Bash: PATH first, then the usual install dirs ----
set "BASH_EXE="
for %%P in (bash.exe) do if not defined BASH_EXE set "BASH_EXE=%%~$PATH:P"
if not defined BASH_EXE if exist "%ProgramFiles%\Git\bin\bash.exe" set "BASH_EXE=%ProgramFiles%\Git\bin\bash.exe"
if not defined BASH_EXE if exist "%ProgramFiles(x86)%\Git\bin\bash.exe" set "BASH_EXE=%ProgramFiles(x86)%\Git\bin\bash.exe"
if not defined BASH_EXE if exist "%LocalAppData%\Programs\Git\bin\bash.exe" set "BASH_EXE=%LocalAppData%\Programs\Git\bin\bash.exe"

if not defined BASH_EXE (
    echo [ERROR] Git Bash not found.
    echo         Install Git for Windows: https://git-scm.com/download/win
    goto :fail
)

echo Using Git Bash: %BASH_EXE%
echo Building... IL2CPP takes about 4-5 minutes. Do not close this window.
echo.

rem Delete the old artifact first. The .sh does this too, but doing it here
rem makes the success check below meaningful even if the .sh ever changes.
if exist "%OUTPUT_APK%" del /q "%OUTPUT_APK%"

"%BASH_EXE%" "%BUILD_SCRIPT%"
set "EXITCODE=%ERRORLEVEL%"

echo.

rem Success is decided by "is the artifact there", NOT by the exit code.
rem A zero exit code with no APK really happens -- see the header note.
if exist "%OUTPUT_APK%" (
    echo [OK] APK built:
    echo      %OUTPUT_APK%
    echo      Copy it to your phone and install.
    set "RESULT=0"
) else if "%EXITCODE%"=="2" (
    echo [ENV] Build could not start. Unity Editor is most likely still open.
    echo       Close Unity and try again.
    set "RESULT=2"
) else (
    echo [FAIL] Build failed, exit code %EXITCODE%.
    echo        Full log: %PROJECT_DIR%Tools\build-android.log
    set "RESULT=1"
)

echo.
pause
exit /b %RESULT%

:fail
echo.
pause
exit /b 2
