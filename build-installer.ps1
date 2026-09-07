# WiFiAnalyzer Build and Installer Script
[CmdletBinding()]
param (
    [ValidateSet("x64", "arm64", "all", "")]
    [string]$Architecture = "all"
)

$ErrorActionPreference = "Stop"
$ScriptRoot = $PSScriptRoot
if (-not $ScriptRoot) {
    $ScriptRoot = (Get-Location).Path
}

# Determine target architectures
$targets = @()
if ([string]::IsNullOrWhiteSpace($Architecture) -or $Architecture -eq "all") {
    $targets = @("x64", "arm64")
} else {
    $targets = @($Architecture)
}

# 1. Version from csproj
$csprojPath = Join-Path $ScriptRoot "WiFiAnalyzer.csproj"
[xml]$csprojXml = Get-Content -Path $csprojPath -Raw -Encoding UTF8
$version = $csprojXml.Project.PropertyGroup.Version
if (-not $version) {
    $version = "1.0.0.0"
}
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host " WiFiAnalyzer Installer Build Tool" -ForegroundColor Cyan
Write-Host " Version: $version" -ForegroundColor Cyan
Write-Host " Architectures: $($targets -join ', ')" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan

# 2. Output directory: .\Installer
$installerDir = Join-Path $ScriptRoot "Installer"
if (-not (Test-Path -Path $installerDir)) {
    New-Item -ItemType Directory -Path $installerDir | Out-Null
}

# 3. Locate ISCC.exe
$isccPaths = @(
    "C:\Program Files\Inno Setup 6\ISCC.exe",
    "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
    "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe",
    "$env:LOCALAPPDATA\Programs\Inno Setup 7\ISCC.exe"
)

$isccExe = $null
foreach ($path in $isccPaths) {
    if (Test-Path -Path $path) {
        $isccExe = $path
        break
    }
}

if (-not $isccExe) {
    $cmd = Get-Command "ISCC.exe" -ErrorAction SilentlyContinue
    if ($cmd) {
        $isccExe = $cmd.Source
    }
}

if (-not $isccExe) {
    Write-Error "Inno Setup Compiler (ISCC.exe) was not found."
    exit 1
}

Write-Host "Found ISCC.exe: $isccExe" -ForegroundColor Cyan

# 4. Build and package each architecture
$createdInstallers = @()
$issPath = Join-Path $ScriptRoot "setup.iss"

foreach ($arch in $targets) {
    $rid = "win-$arch"
    Write-Host "`n------------------------------------------" -ForegroundColor Yellow
    Write-Host "Processing Architecture: $arch (RID: $rid)" -ForegroundColor Yellow
    Write-Host "------------------------------------------" -ForegroundColor Yellow

    # dotnet publish
    $publishDir = Join-Path $ScriptRoot "bin\Release\net10.0-windows10.0.19041.0\$rid\publish"
    Write-Host "Publishing application ($rid)..." -ForegroundColor Cyan
    dotnet publish "$csprojPath" -c Release -r "$rid" --self-contained true
    if ($LASTEXITCODE -ne 0) {
        Write-Error "dotnet publish failed for $rid."
        exit $LASTEXITCODE
    }

    # Compile Installer
    Write-Host "Compiling installer for $arch..." -ForegroundColor Cyan
    $isccArgs = @(
        "/DMyAppVersion=$version",
        "/DMyAppArch=$arch",
        "/DMySourceDir=$publishDir",
        "/DMyOutputDir=$installerDir",
        "$issPath"
    )

    & "$isccExe" $isccArgs
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Installer compilation failed for $arch."
        exit $LASTEXITCODE
    }

    $expectedInstaller = Join-Path $installerDir "WiFiAnalyzer_Setup_v${version}_${arch}.exe"
    if (Test-Path -Path $expectedInstaller) {
        $createdInstallers += (Get-Item -Path $expectedInstaller)
    } else {
        Write-Warning "Installer exe not found: $expectedInstaller"
    }
}

# 5. Summary
Write-Host "`n==========================================" -ForegroundColor Green
Write-Host " All Installers Created Successfully!" -ForegroundColor Green
Write-Host "==========================================" -ForegroundColor Green
foreach ($item in $createdInstallers) {
    $sizeMB = [math]::Round($item.Length / 1MB, 2)
    Write-Host "File: $($item.Name)" -ForegroundColor Green
    Write-Host "Path: $($item.FullName)" -ForegroundColor Green
    Write-Host "Size: $sizeMB MB`n" -ForegroundColor Green
}
