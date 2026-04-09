$ErrorActionPreference = 'Stop'
$toolsDir = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"

$packageName = $env:ChocolateyPackageName
$softwareName = 'CoreBus*'

$fileLocation = Join-Path $toolsDir 'CoreBus_3.4.0_win_x64_installer.exe'

$checksum = 'c9886cab4e64a70aad52c04b11b3f190ca2b9e660cab03822dacf8fff6d70495'

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
