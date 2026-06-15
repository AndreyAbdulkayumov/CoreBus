$ErrorActionPreference = 'Stop'
$toolsDir = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"

$packageName = $env:ChocolateyPackageName
$softwareName = 'CoreBus*'

$fileLocation = Join-Path $toolsDir 'CoreBus_3.5.1_win_x64_installer.exe'

$checksum = 'e379ae7a92e3b4c711c0308c84cf28134b773c157cd05cb8f059b878b6c194f3'

$packageArgs = @{
  packageName    = $packageName
  fileType       = 'EXE'
  file           = $fileLocation  
  softwareName   = $softwareName  
  checksum       = $checksum
  checksumType   = 'sha256'
  silentArgs     = '/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /SP- /TASKS="desktopicon"'
  validExitCodes = @(0)
}

Install-ChocolateyInstallPackage @packageArgs
