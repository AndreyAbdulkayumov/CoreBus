$ErrorActionPreference = 'Stop'
$toolsDir = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"

$packageName = $env:ChocolateyPackageName
$softwareName = 'CoreBus*'

$fileLocation = Join-Path $toolsDir 'CoreBus_3.4.0_win_x64_installer.exe'

$checksum = '9235379f3f254c7667ec1d3c1c53632bc1542fb6d7ca3ed2536eae4b643feaf7'

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
