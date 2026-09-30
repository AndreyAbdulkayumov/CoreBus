$ErrorActionPreference = 'Stop'
$toolsDir = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"

$packageName = $env:ChocolateyPackageName
$softwareName = 'CoreBus*'

$fileLocation = Join-Path $toolsDir 'CoreBus_3.5.3_win_x64_installer.exe'

$checksum = '6199b25ee9e1031954973c031dbe2715943bb206e9897d0a9e5562e582dc012b'

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
