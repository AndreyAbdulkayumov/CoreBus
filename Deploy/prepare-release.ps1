#Requires -Version 5.1
<#
.SYNOPSIS
    Локальная сборка релиза CoreBus.

.DESCRIPTION
    1. Читает версию из CoreBus.Base.csproj.
    2. Запускает remove-all-bin-obj-folders.bat перед публикацией.
    3. Публикует win-x64 и linux-x64.
	4. Версию win portable архивирует, версию linux portable кладет папкой без архива.
	5. Если задано, то собирает установщик и считает у него SHA256.
    6. Складывает артефакты в Deploy/Artifacts/<version>/.
	
	Алгоритм работы начинается в секции main.

.PARAMETER SkipInstaller
    Не собирать установщик и не включать его в артефакты.

.PARAMETER IsccPath
    Полный путь к ISCC.exe. Если не задан — ищется в стандартных местах и PATH.

.EXAMPLE
    .\Deploy\prepare-release.ps1
.EXAMPLE
    .\Deploy\prepare-release.ps1 -SkipInstaller
#>
[CmdletBinding()]
param(
    [switch] $SkipInstaller,
    [string] $IsccPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Write-Step
{
    param([string] $Message)
    Write-Host ""
    Write-Host "==> $Message" -ForegroundColor Cyan
}

function Get-RepoRoot
{
    return (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
}

function Get-ProjectVersion
{
    param([string] $CsprojPath)

    [xml] $xml = Get-Content -LiteralPath $CsprojPath -Raw -Encoding UTF8
    $node = $xml.Project.PropertyGroup.Version | Select-Object -First 1
    if ([string]::IsNullOrWhiteSpace($node))
    {
        throw "Не найден тег <Version> в '$CsprojPath'."
    }

    return $node.Trim()
}

function Get-TargetFramework
{
    param([string] $CsprojPath)

    [xml] $xml = Get-Content -LiteralPath $CsprojPath -Raw -Encoding UTF8
    $node = $xml.Project.PropertyGroup.TargetFramework | Select-Object -First 1
    if ([string]::IsNullOrWhiteSpace($node))
    {
        throw "Не найден тег <TargetFramework> в '$CsprojPath'."
    }

    return $node.Trim()
}

function Assert-InnoSetupDefines
{
    param(
        [string] $IssPath,
        [string] $ExpectedVersion,
        [string] $ExpectedPublishDirectory
    )

    if (-not (Test-Path -LiteralPath $IssPath -PathType Leaf))
    {
        throw "Не найден скрипт Inno Setup: '$IssPath'."
    }

    $content = Get-Content -LiteralPath $IssPath -Raw -Encoding UTF8

    $versionMatch = [regex]::Match(
        $content,
        '#define\s+MyAppVersion\s+"([^"]+)"')
    if (-not $versionMatch.Success)
    {
        throw "В '$IssPath' не найден #define MyAppVersion."
    }

    $issVersion = $versionMatch.Groups[1].Value
    if ($issVersion -ne $ExpectedVersion)
    {
        throw ("MyAppVersion в .iss ('{0}') не совпадает с версией проекта ('{1}')." -f `
            $issVersion, $ExpectedVersion)
    }

    $publishMatch = [regex]::Match(
        $content,
        "#define\s+PublishDirectory\s+'([^']+)'")
    if (-not $publishMatch.Success)
    {
        throw "В '$IssPath' не найден #define PublishDirectory."
    }

    $issPublishDirectory = $publishMatch.Groups[1].Value
    $issDirectory = Split-Path -Parent $IssPath
    if ([System.IO.Path]::IsPathRooted($issPublishDirectory))
    {
        $issPublishFullPath = [System.IO.Path]::GetFullPath($issPublishDirectory)
    }
    else
    {
        $issPublishFullPath = [System.IO.Path]::GetFullPath(
            (Join-Path $issDirectory $issPublishDirectory))
    }

    $expectedPublishFullPath = [System.IO.Path]::GetFullPath($ExpectedPublishDirectory)
    if (-not [string]::Equals(
            $issPublishFullPath,
            $expectedPublishFullPath,
            [System.StringComparison]::OrdinalIgnoreCase))
    {
        throw ("PublishDirectory в .iss ('{0}' → '{1}') не совпадает с ожидаемым ('{2}')." -f `
            $issPublishDirectory, $issPublishFullPath, $expectedPublishFullPath)
    }

    Write-Host "MyAppVersion и PublishDirectory в .iss совпадают с ожидаемыми."
}

function Find-Iscc
{
    param([string] $ExplicitPath)

    if (-not [string]::IsNullOrWhiteSpace($ExplicitPath))
    {
        if (-not (Test-Path -LiteralPath $ExplicitPath))
        {
            throw "ISCC.exe не найден по пути '$ExplicitPath'."
        }

        return (Resolve-Path -LiteralPath $ExplicitPath).Path
    }

    $candidates = @(
        "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
        "${env:ProgramFiles}\Inno Setup 6\ISCC.exe",
        "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe"
    )

    foreach ($candidate in $candidates)
    {
        if (Test-Path -LiteralPath $candidate)
        {
            return $candidate
        }
    }

    $fromPath = Get-Command 'ISCC.exe' -ErrorAction SilentlyContinue
    if ($null -ne $fromPath)
    {
        return $fromPath.Source
    }

    throw "ISCC.exe не найден. Установите Inno Setup 6 или укажите -IsccPath."
}

function Invoke-CleanBinObj
{
    param([string] $RepoRoot)

    $cleanBat = Join-Path $RepoRoot 'remove-all-bin-obj-folders.bat'
    if (-not (Test-Path -LiteralPath $cleanBat))
    {
        throw "Не найден скрипт очистки: '$cleanBat'."
    }

    Write-Host $cleanBat
    Push-Location $RepoRoot
    try
    {
        # echo. закрывает pause в конце .bat, чтобы релиз не ждал клавишу
        cmd.exe /c "echo.| `"$cleanBat`""
        if ($LASTEXITCODE -ne 0)
        {
            throw "remove-all-bin-obj-folders.bat завершился с кодом $LASTEXITCODE."
        }
    }
    finally
    {
        Pop-Location
    }
}

function Assert-DotnetAvailable
{
    $dotnet = Get-Command 'dotnet' -ErrorAction SilentlyContinue
    if ($null -eq $dotnet)
    {
        throw "dotnet не найден в PATH. Установите .NET SDK и повторите."
    }

    Write-Host "Окружение dotnet найдено."
}

function Invoke-DotnetPublish
{
    param(
        [string] $ProjectPath,
        [string] $Runtime
    )

    Write-Host "dotnet publish ($Runtime)..."
    & dotnet publish $ProjectPath `
        -c Release `
        -r $Runtime `
        --self-contained true `
        -p:PublishSingleFile=true

    if ($LASTEXITCODE -ne 0)
    {
        throw "dotnet publish для '$Runtime' завершился с кодом $LASTEXITCODE."
    }
}

function Get-PublishDirectory
{
    param(
        [string] $RepoRoot,
        [string] $TargetFramework,
        [string] $Runtime
    )

    $path = Join-Path $RepoRoot "CoreBus.Desktop\bin\Release\$TargetFramework\$Runtime\publish"
    if (-not (Test-Path -LiteralPath $path))
    {
        throw "Папка публикации не найдена: '$path'."
    }

    return $path
}

function Assert-PublishedExecutable
{
    param(
        [string] $PublishDirectory,
        [string] $Runtime
    )

    $fileName = if ($Runtime -like 'win-*') { 'CoreBus.exe' } else { 'CoreBus' }
    $exePath = Join-Path $PublishDirectory $fileName
    if (-not (Test-Path -LiteralPath $exePath -PathType Leaf))
    {
        throw "После publish не найден исполняемый файл: '$exePath'."
    }

    Write-Host "Проверка: $exePath"
}

function Remove-PdbFiles
{
    param([string] $Directory)

    Get-ChildItem -LiteralPath $Directory -Filter '*.pdb' -Recurse -File |
        Remove-Item -Force
}

function New-PortableFolder
{
    param(
        [string] $SourcePublishDir,
        [string] $DestinationDir
    )

    if (Test-Path -LiteralPath $DestinationDir)
    {
        Remove-Item -LiteralPath $DestinationDir -Recurse -Force
    }

    New-Item -ItemType Directory -Path $DestinationDir -Force | Out-Null
    Copy-Item -Path (Join-Path $SourcePublishDir '*') -Destination $DestinationDir -Recurse -Force
    Remove-PdbFiles -Directory $DestinationDir
}

function New-PortableZip
{
    param(
        [string] $SourcePublishDir,
        [string] $StagingRoot,
        [string] $FolderName,
        [string] $OutputZipPath
    )

    $stagingDir = Join-Path $StagingRoot $FolderName
    New-PortableFolder -SourcePublishDir $SourcePublishDir -DestinationDir $stagingDir

    if (Test-Path -LiteralPath $OutputZipPath)
    {
        Remove-Item -LiteralPath $OutputZipPath -Force
    }

    Compress-Archive -Path $stagingDir -DestinationPath $OutputZipPath -CompressionLevel Optimal
    Remove-Item -LiteralPath $stagingDir -Recurse -Force
}

function Get-FileSha256
{
    param([string] $FilePath)

    return (Get-FileHash -LiteralPath $FilePath -Algorithm SHA256).Hash.ToLowerInvariant()
}

function Copy-Artifact
{
    param(
        [string] $SourcePath,
        [string] $ArtifactsDir
    )

    if (-not (Test-Path -LiteralPath $SourcePath))
    {
        throw "Артефакт не найден: '$SourcePath'."
    }

    Copy-Item -LiteralPath $SourcePath -Destination $ArtifactsDir -Force
}

# --- main ---

$repoRoot = Get-RepoRoot
$baseCsproj = Join-Path $repoRoot 'CoreBus.Base\CoreBus.Base.csproj'
$desktopCsproj = Join-Path $repoRoot 'CoreBus.Desktop\CoreBus.Desktop.csproj'
$issPath = Join-Path $repoRoot 'Deploy\WindowsInstaller\WindowsInstallerScript.iss'
$installerOutputDir = Join-Path $repoRoot 'Deploy\WindowsInstaller'

Write-Step "Проверка окружения"
Assert-DotnetAvailable

Write-Step "Версия из CoreBus.Base.csproj"
$Version = Get-ProjectVersion -CsprojPath $baseCsproj
Write-Host "Версия: $Version"

$targetFramework = Get-TargetFramework -CsprojPath $desktopCsproj
Write-Host "TargetFramework: $targetFramework"

$iscc = $null
if (-not $SkipInstaller)
{
    $expectedIssPublishDirectory = [System.IO.Path]::GetFullPath(
        (Join-Path $repoRoot "CoreBus.Desktop\bin\Release\$targetFramework\win-x64\publish"))

    Write-Step "Проверка define в WindowsInstallerScript.iss"
    Assert-InnoSetupDefines `
        -IssPath $issPath `
        -ExpectedVersion $Version `
        -ExpectedPublishDirectory $expectedIssPublishDirectory

    Write-Step "Проверка ISCC"
    $iscc = Find-Iscc -ExplicitPath $IsccPath
    Write-Host "ISCC найден: $iscc"
}

$winPortableName = "CoreBus_${Version}_win_x64_portable"
$linuxPortableName = "CoreBus_${Version}_linux_x64"
$installerName = "CoreBus_${Version}_win_x64_installer"
$installerFileName = "$installerName.exe"

Write-Step "Очистка bin/obj (remove-all-bin-obj-folders.bat)"
Invoke-CleanBinObj -RepoRoot $repoRoot

Write-Step "dotnet publish (win-x64, linux-x64)"
Invoke-DotnetPublish -ProjectPath $desktopCsproj -Runtime 'win-x64'
Invoke-DotnetPublish -ProjectPath $desktopCsproj -Runtime 'linux-x64'

$winPublishDir = Get-PublishDirectory -RepoRoot $repoRoot `
    -TargetFramework $targetFramework -Runtime 'win-x64'
$linuxPublishDir = Get-PublishDirectory -RepoRoot $repoRoot `
    -TargetFramework $targetFramework -Runtime 'linux-x64'

Write-Step "Проверка исполняемых файлов после publish"
Assert-PublishedExecutable -PublishDirectory $winPublishDir -Runtime 'win-x64'
Assert-PublishedExecutable -PublishDirectory $linuxPublishDir -Runtime 'linux-x64'

$artifactsDir = Join-Path $repoRoot "Deploy\Artifacts\$Version"
$stagingRoot = Join-Path $artifactsDir '_staging'

# Старый релиз той же версии чистим только после успешного publish обеих платформ
if (Test-Path -LiteralPath $artifactsDir)
{
    Write-Step "Очистка существующих артефактов $Version"
    Remove-Item -LiteralPath $artifactsDir -Recurse -Force
}

New-Item -ItemType Directory -Path $stagingRoot -Force | Out-Null

$installerPath = Join-Path $installerOutputDir $installerFileName

if (-not $SkipInstaller)
{
    Write-Step "Сборка Inno Setup-установщика"
    Write-Host "ISCC: $iscc"
    & $iscc $issPath
    if ($LASTEXITCODE -ne 0)
    {
        throw "ISCC завершился с кодом $LASTEXITCODE."
    }

    if (-not (Test-Path -LiteralPath $installerPath))
    {
        throw "Установщик не создан: '$installerPath'."
    }
}
else
{
    Write-Step "Пропуск установщика (-SkipInstaller)"
}

Write-Step "Portable: win-x64 (zip), linux-x64 (папка)"
$winZipPath = Join-Path $artifactsDir "$winPortableName.zip"
$linuxFolderPath = Join-Path $artifactsDir $linuxPortableName
New-PortableZip -SourcePublishDir $winPublishDir -StagingRoot $stagingRoot `
    -FolderName $winPortableName -OutputZipPath $winZipPath
New-PortableFolder -SourcePublishDir $linuxPublishDir -DestinationDir $linuxFolderPath

if (-not $SkipInstaller)
{
    Write-Step "Копирование установщика в Artifacts"
    Copy-Artifact -SourcePath $installerPath -ArtifactsDir $artifactsDir
    $artifactInstallerPath = Join-Path $artifactsDir $installerFileName

    Write-Step "SHA256 установщика"
    $checksum = Get-FileSha256 -FilePath $artifactInstallerPath
    Write-Host "SHA256: $checksum"

    $checksumsPath = Join-Path $artifactsDir 'SHA256SUMS.txt'
    "$checksum  $installerFileName" | Set-Content -LiteralPath $checksumsPath -Encoding ASCII
}

Remove-Item -LiteralPath $stagingRoot -Recurse -Force -ErrorAction SilentlyContinue

Write-Step "Готово"
Write-Host "Версия:           $Version"
Write-Host "TargetFramework:  $targetFramework"
Write-Host "Дата публикации:  $(Get-Date -Format 'dd.MM.yyyy HH:mm:ss')"
Write-Host "Артефакты:        $artifactsDir"
Get-ChildItem -LiteralPath $artifactsDir | ForEach-Object {
    if ($_.PSIsContainer)
    {
        Write-Host ("  - {0}\ (папка)" -f $_.Name)
    }
    else
    {
        Write-Host ("  - {0} ({1:N0} байт)" -f $_.Name, $_.Length)
    }
}
