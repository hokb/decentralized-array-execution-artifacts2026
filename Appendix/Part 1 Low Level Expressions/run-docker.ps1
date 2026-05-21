$ErrorActionPreference = "Stop"

$imageName = "artefact1:latest"
$outDir = Join-Path (Get-Location) "result"

New-Item -ItemType Directory -Force -Path $outDir | Out-Null

Write-Host "Building Docker image..."
docker build -t $imageName .

Write-Host "Running container..."
docker run `
  -v "${outDir}:/Artefact1/result" `
  $imageName

Write-Host "Done. Output files are in: $outDir"