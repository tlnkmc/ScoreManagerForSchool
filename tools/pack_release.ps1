#!/usr/bin/env pwsh
set-strictmode -version latest
$ErrorActionPreference = 'Stop'

# Paths
$root = Resolve-Path "$PSScriptRoot\.."
$uiProj = Join-Path $root 'ScoreManagerForSchool.UI\ScoreManagerForSchool.UI.csproj'
$updaterProj = Join-Path $root 'ScoreManagerForSchool.Updater\ScoreManagerForSchool.Updater.csproj'
$releasesDir = Join-Path $root 'releases'
$buildDir = Join-Path $releasesDir 'build'

# Read version from UI csproj (InformationalVersion fallback to AssemblyVersion)
[xml]$csproj = Get-Content -Raw $uiProj
$version = $csproj.Project.PropertyGroup.InformationalVersion
if (-not $version) { $version = $csproj.Project.PropertyGroup.AssemblyVersion }
if (-not $version) { throw 'Cannot read version from ScoreManagerForSchool.UI.csproj' }

$ridMap = @(
    @{ rid = 'win-x64';   name = 'ScoreManagerForSchool-win-x64.zip';   type = 'zip'   },
    @{ rid = 'win-arm64'; name = 'ScoreManagerForSchool-win-arm64.zip'; type = 'zip'   },
    @{ rid = 'linux-x64'; name = 'ScoreManagerForSchool-linux-x64.tar.gz'; type = 'targz' },
    @{ rid = 'osx-x64';   name = 'ScoreManagerForSchool-osx-x64.tar.gz';   type = 'targz' },
    @{ rid = 'osx-arm64'; name = 'ScoreManagerForSchool-osx-arm64.tar.gz'; type = 'targz' }
)

New-Item -ItemType Directory -Force -Path $buildDir | Out-Null

function Publish-OneRid {
    param(
        [string]$rid,
        [string]$artifactName,
        [string]$packType
    )

    Write-Host "==== Publishing $rid ====" -ForegroundColor Cyan

    # 1) Publish Updater first (so UI target can copy updater into publish dir)
    dotnet publish $updaterProj -c Release -r $rid --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true | Out-Host

    # 2) Publish UI
    dotnet publish $uiProj -c Release -r $rid --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true | Out-Host

    $uiPub = Join-Path $root "ScoreManagerForSchool.UI\\bin\\Release\\net8.0\\$rid\\publish"
    if (-not (Test-Path $uiPub)) { throw "UI publish output not found: $uiPub" }

    # Verify updater exists in UI publish (update or update.exe)
    $updaterBin = Get-ChildItem -Path $uiPub -Filter 'update*' -File -ErrorAction SilentlyContinue | Select-Object -First 1
    if (-not $updaterBin) { Write-Warning "Updater binary not found in UI publish for $rid. Continuing, but update flow may not work." }

    # 3) Copy raw publish to releases/build/<rid>
    $ridBuildDir = Join-Path $buildDir $rid
    if (Test-Path $ridBuildDir) { Remove-Item $ridBuildDir -Recurse -Force }
    New-Item -ItemType Directory -Force -Path $ridBuildDir | Out-Null
    Copy-Item -Path (Join-Path $uiPub '*') -Destination $ridBuildDir -Recurse -Force

    # 4) Pack archive in releases/
    $artifactPath = Join-Path $releasesDir $artifactName
    if (Test-Path $artifactPath) { Remove-Item $artifactPath -Force }

    switch ($packType) {
        'zip'   {
            Write-Host "Creating ZIP: $artifactName" -ForegroundColor Yellow
            Compress-Archive -Path (Join-Path $uiPub '*') -DestinationPath $artifactPath -Force
        }
        'targz' {
            Write-Host "Creating TAR.GZ: $artifactName" -ForegroundColor Yellow
            # tar available on modern Windows; use -C to avoid wrapping folder level
            & tar -C $uiPub -czf $artifactPath .
        }
        Default { throw "Unknown pack type: $packType" }
    }
}

Write-Host "Version: $version" -ForegroundColor Green
Write-Host "Output: $releasesDir" -ForegroundColor Green

foreach ($item in $ridMap) {
    Publish-OneRid -rid $item.rid -artifactName $item.name -packType $item.type
}

Write-Host "==== Done ====" -ForegroundColor Green
Write-Host "Artifacts:" -ForegroundColor Green
Get-ChildItem $releasesDir -File | Select-Object Name,Length | Format-Table -AutoSize | Out-Host
