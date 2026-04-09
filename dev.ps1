# dev.ps1
$ApiDir = Join-Path $PSScriptRoot "QuizAI.Api"
$DesktopDir = Join-Path $PSScriptRoot "QuizAI.Desktop"

Write-Host "[1/2] Starting API on http://localhost:5127 ..." -ForegroundColor Cyan
Start-Process "dotnet" -ArgumentList "run --launch-profile http" -WorkingDirectory $ApiDir

Write-Host "Waiting 5 seconds for API to start..." -ForegroundColor Yellow
Start-Sleep -Seconds 5

Write-Host "[2/2] Starting Desktop (pointing to localhost)..." -ForegroundColor Cyan
$env:QUIZAI_API_URL="http://localhost:5127/api/"
Set-Location $DesktopDir
dotnet run
