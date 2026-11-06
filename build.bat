@echo off
setlocal
set ROOT=%~dp0
set CSC=C:\WINDOWS\Microsoft.NET\Framework64\v4.0.30319\csc.exe
set FW=C:\WINDOWS\Microsoft.NET\Framework64\v4.0.30319

if not exist "%ROOT%dist" mkdir "%ROOT%dist"

"%CSC%" -nologo -target:winexe -optimize+ -codepage:65001 ^
 -out:"%ROOT%dist\app.exe" ^
 -win32icon:"%ROOT%assets\app.ico" ^
 -r:System.dll -r:System.Drawing.dll -r:System.Windows.Forms.dll ^
 -r:"%FW%\WPF\PresentationCore.dll" -r:"%FW%\WPF\WindowsBase.dll" -r:"%FW%\System.Xaml.dll" ^
 "%ROOT%src\AssemblyInfo.cs" ^
 "%ROOT%src\IcoWriter.cs" ^
 "%ROOT%src\ImageLoader.cs" ^
 "%ROOT%src\MainForm.cs" ^
 "%ROOT%src\Program.cs"

if errorlevel 1 (
    echo BUILD FAILED
) else (
    echo BUILD OK
)
endlocal