$ErrorActionPreference = "Stop"
Write-Host "[1/4] Checking .NET SDK..." -ForegroundColor Cyan
dotnet --info
Write-Host "[2/4] Restoring packages..." -ForegroundColor Cyan
dotnet restore
Write-Host "[3/4] Building Release..." -ForegroundColor Cyan
dotnet build --configuration Release --no-restore
Write-Host "[4/4] Static delivery checks..." -ForegroundColor Cyan
$forbidden = @("bin", "obj", ".vs")
foreach ($name in $forbidden) {
  if (Get-ChildItem -Recurse -Directory -Force | Where-Object Name -eq $name) {
    Write-Warning "Found generated directory: $name"
  }
}
Write-Host "Build completed. Next: run Database/CREATE_DATABASE_FULL.sql, then dotnet run." -ForegroundColor Green
