param(
    [string]$BaseOutputDir = "..\dist",
    [string]$InstallerScript = "..\installer\setup.iss"
)

$ErrorActionPreference = "Stop"

function Publish-Variant {
    param(
        [string]$Variant
    )

    $variantOutputDir = Join-Path $PSScriptRoot (Join-Path $BaseOutputDir "SilentPrintBridge-$Variant")

    if (Test-Path $variantOutputDir) {
        Remove-Item -Path $variantOutputDir -Recurse -Force
    }

    New-Item -ItemType Directory -Force -Path $variantOutputDir | Out-Null

    $serviceProject = Join-Path $PSScriptRoot "..\src\SilentPrintBridge\SilentPrintBridge.csproj"
    $uiProject = Join-Path $PSScriptRoot "..\src\SilentPrintBridge.UI\SilentPrintBridge.UI.csproj"

    $publishCommonArgs = @(
        "-c", "Release",
        "-r", "win-x64",
        "--self-contained", "true",
        "-p:PublishSingleFile=true",
        "-p:IncludeNativeLibrariesForSelfExtract=true",
        "-p:BrandingVariant=$Variant",
        "-o", $variantOutputDir
    )

    & dotnet publish $serviceProject @publishCommonArgs | Out-Host
    if ($LASTEXITCODE -ne 0) {
        throw "Service publish failed for $Variant"
    }

    & dotnet publish $uiProject @publishCommonArgs | Out-Host
    if ($LASTEXITCODE -ne 0) {
        throw "UI publish failed for $Variant"
    }

    Copy-Item (Join-Path $PSScriptRoot "..\src\SilentPrintBridge\appsettings.json") -Destination (Join-Path $variantOutputDir "appsettings.json") -Force

    $samplesDir = Join-Path $variantOutputDir "samples"
    New-Item -ItemType Directory -Force -Path $samplesDir | Out-Null
    Copy-Item (Join-Path $PSScriptRoot "..\samples\*") -Destination $samplesDir -Recurse -Force

    return $variantOutputDir
}

function Build-Installer {
    param(
        [string]$Variant,
        [string]$VariantOutputDir
    )

    $isccPath = "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe"
    if (-not (Test-Path $isccPath)) {
        Write-Warning "Inno Setup compiler not found. Published files are ready at $VariantOutputDir"
        return
    }

    $defineArgs = @(
        "/DSourceDir=$VariantOutputDir",
        "/DOutputBaseName=SilentPrintBridge-$Variant-Setup"
    )

    if ($Variant -eq "Customer") {
        $defineArgs += "/DCustomerBuild=1"
    }

    & $isccPath $defineArgs (Join-Path $PSScriptRoot $InstallerScript)
    if ($LASTEXITCODE -ne 0) {
        throw "Installer build failed for $Variant"
    }
}

Write-Host "Building developer and customer variants..." -ForegroundColor Cyan

$developerOutput = Publish-Variant -Variant "Developer"
$customerOutput = Publish-Variant -Variant "Customer"

Build-Installer -Variant "Developer" -VariantOutputDir $developerOutput
Build-Installer -Variant "Customer" -VariantOutputDir $customerOutput

Write-Host ""
Write-Host "Build completed." -ForegroundColor Green
Write-Host "Developer output: $developerOutput" -ForegroundColor Gray
Write-Host "Customer output:  $customerOutput" -ForegroundColor Gray
