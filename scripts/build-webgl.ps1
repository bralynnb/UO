param([string]$UnityPath = "C:\Program Files\Unity\Hub\Editor\6000.0.62f1\Editor\Unity.exe")
$ErrorActionPreference = "Stop"
if (!(Test-Path $UnityPath)) { throw "Install Unity 6000.0.62f1 with Web Build Support, or pass -UnityPath." }
$ProjectPath = Split-Path $PSScriptRoot -Parent
$BuildLog = Join-Path $ProjectPath "unity-build.log"
$process = Start-Process -FilePath $UnityPath -ArgumentList @("-batchmode", "-quit", "-projectPath", "`"$ProjectPath`"", "-buildTarget", "WebGL", "-executeMethod", "TSFM.Editor.BuildGame.WebGL", "-logFile", "`"$BuildLog`"") -Wait -PassThru
if ($process.ExitCode -ne 0) { throw "Unity build failed. See $BuildLog" }
Write-Output "Build created in server/public/game. Run npm ci and npm start, then open http://localhost:8080."
