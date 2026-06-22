$ErrorActionPreference = "Stop"

Set-Location $PSScriptRoot

$Product = "POS_Data_Editor"
$Version = "1.0.0"
$Runtime = "win-x64"
$PublishDir = Join-Path $PSScriptRoot "release\${Product}_v${Version}_${Runtime}"
$ZipPath = Join-Path $PSScriptRoot "release\${Product}_v${Version}_${Runtime}.zip"

Write-Host "Cleaning old release..."
Remove-Item -Recurse -Force $PublishDir -ErrorAction SilentlyContinue
Remove-Item -Force $ZipPath -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path $PublishDir | Out-Null

Write-Host "Publishing self-contained Windows x64 build..."
dotnet publish .\ALNOUR.InvoiceManager.csproj `
  -c Release `
  -r $Runtime `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:EnableCompressionInSingleFile=true `
  -p:DebugType=None `
  -p:DebugSymbols=false `
  -o $PublishDir

Copy-Item .\README_RELEASE.md $PublishDir\README.txt -Force
Copy-Item .\LICENSE.txt $PublishDir\LICENSE.txt -Force
Copy-Item .\logo_app.png $PublishDir\logo_app.png -Force -ErrorAction SilentlyContinue
Copy-Item .\logo.png $PublishDir\logo.png -Force -ErrorAction SilentlyContinue
Copy-Item .\logo_default.png $PublishDir\logo_default.png -Force -ErrorAction SilentlyContinue

Write-Host "Creating ZIP..."
Compress-Archive -Path "$PublishDir\*" -DestinationPath $ZipPath -Force

Write-Host "DONE"
Write-Host "Folder: $PublishDir"
Write-Host "ZIP:    $ZipPath"
