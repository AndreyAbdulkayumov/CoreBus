$ErrorActionPreference = 'Stop'
$toolsDir = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"

$packageName = $env:ChocolateyPackageName
$softwareName = 'CoreBus*'

$fileLocation = Join-Path $toolsDir 'CoreBus_3.5.0_win_x64_installer.exe'

$checksum = 'c3c47175ebc51cfcdcaad2fa439ce7001742639d5183bc2f10c9eb5fcf324de6'

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
