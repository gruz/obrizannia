@echo off
set "CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%CSC%" set "CSC=C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe"
if not exist "%CSC%" (echo csc.exe not found - need .NET Framework 4.x & pause & exit /b 1)
if not exist "%~dp0Version.cs" (
    (
        echo using System.Reflection;
        echo [assembly: AssemblyTitle^("Mp4TailWatcher"^)]
        echo [assembly: AssemblyProduct^("Mp4TailWatcher"^)]
        echo [assembly: AssemblyCopyright^("2025"^)]
        echo [assembly: AssemblyVersion^("0.0.0.0"^)]
        echo [assembly: AssemblyFileVersion^("0.0.0.0"^)]
        echo static class BuildInfo { public const string Version = "dev"; }
    ) > "%~dp0Version.cs"
)
"%CSC%" /nologo /target:winexe /platform:anycpu /optimize+ /out:"%~dp0Mp4TailWatcher.exe" "%~dp0Mp4TailWatcher.cs" "%~dp0Version.cs" /win32manifest:"%~dp0Mp4TailWatcher.exe.manifest" /r:System.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll /r:System.IO.Compression.dll /r:System.IO.Compression.FileSystem.dll
pause
