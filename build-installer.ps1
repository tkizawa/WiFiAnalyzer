# WiFiAnalyzer Build and Package Script (Installer & Portable)
[CmdletBinding()]
param (
    [ValidateSet("x64", "arm64", "all", "")]
    [string]$Architecture = "all",

    [ValidateSet("all", "installer", "portable")]
    [string]$Type = "all"
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
Write-Host " WiFiAnalyzer Package Build Tool" -ForegroundColor Cyan
Write-Host " Version:       $version" -ForegroundColor Cyan
Write-Host " Target Arch:   $($targets -join ', ')" -ForegroundColor Cyan
Write-Host " Package Type:  $Type" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan

# 2. Output directory: .\Installer
$installerDir = Join-Path $ScriptRoot "Installer"
if (-not (Test-Path -Path $installerDir)) {
    New-Item -ItemType Directory -Path $installerDir | Out-Null
}

# 3. Locate ISCC.exe if installer is needed
$needInstaller = ($Type -eq "all" -or $Type -eq "installer")
$needPortable  = ($Type -eq "all" -or $Type -eq "portable")
$isccExe = $null

if ($needInstaller) {
    $isccPaths = @(
        "C:\Program Files\Inno Setup 6\ISCC.exe",
        "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
        "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe",
        "$env:LOCALAPPDATA\Programs\Inno Setup 7\ISCC.exe"
    )

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
        Write-Error "Inno Setup Compiler (ISCC.exe) was not found. Please install Inno Setup 6 or add it to PATH."
        exit 1
    }

    Write-Host "Found ISCC.exe: $isccExe" -ForegroundColor Cyan
}

# 4. Build and package each architecture
$createdArtifacts = @()
$issPath = Join-Path $ScriptRoot "setup.iss"

foreach ($arch in $targets) {
    $rid = "win-$arch"
    Write-Host "`n------------------------------------------" -ForegroundColor Yellow
    Write-Host "Processing Architecture: $arch (RID: $rid)" -ForegroundColor Yellow
    Write-Host "------------------------------------------" -ForegroundColor Yellow

    # Package: Setup Installer (Inno Setup)
    if ($needInstaller) {
        $publishDir = Join-Path $ScriptRoot "bin\Release\net10.0-windows10.0.19041.0\$rid\publish"
        Write-Host "Publishing application for installer ($rid)..." -ForegroundColor Cyan
        dotnet publish "$csprojPath" -c Release -r "$rid" --self-contained true
        if ($LASTEXITCODE -ne 0) {
            Write-Error "dotnet publish failed for installer ($rid)."
            exit $LASTEXITCODE
        }

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
            $createdArtifacts += (Get-Item -Path $expectedInstaller)
        } else {
            Write-Warning "Installer exe not found: $expectedInstaller"
        }
    }

    # Package: Portable single-file executable
    if ($needPortable) {
        $portablePublishDir = Join-Path $ScriptRoot "bin\Release\net10.0-windows10.0.19041.0\$rid\portable"
        Write-Host "Publishing portable single-file application ($rid)..." -ForegroundColor Cyan
        dotnet publish "$csprojPath" -c Release -r "$rid" --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o "$portablePublishDir"
        if ($LASTEXITCODE -ne 0) {
            Write-Error "dotnet publish failed for portable ($rid)."
            exit $LASTEXITCODE
        }

        $portableSourceExe = Join-Path $portablePublishDir "WiFiAnalyzer.exe"
        $portableDestExe = Join-Path $installerDir "WiFiAnalyzer_Portable_v${version}_${arch}.exe"

        if (Test-Path -Path $portableSourceExe) {
            Copy-Item -Path $portableSourceExe -Destination $portableDestExe -Force
            $createdArtifacts += (Get-Item -Path $portableDestExe)
        } else {
            Write-Warning "Portable source exe not found: $portableSourceExe"
        }
    }
}

# 5. Summary
Write-Host "`n==========================================" -ForegroundColor Green
Write-Host " Packaging Completed Successfully!" -ForegroundColor Green
Write-Host "==========================================" -ForegroundColor Green
foreach ($item in $createdArtifacts) {
    $sizeMB = [math]::Round($item.Length / 1MB, 2)
    Write-Host "File: $($item.Name)" -ForegroundColor Green
    Write-Host "Path: $($item.FullName)" -ForegroundColor Green
    Write-Host "Size: $sizeMB MB`n" -ForegroundColor Green
}
