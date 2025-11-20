$ErrorActionPreference = 'Stop'
$toolsDir = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"
$fileLocation = Join-Path $toolsDir 'CoreBus_3.3.3_win_x64_installer.exe'

$packageArgs = @{
  packageName    = $env:ChocolateyPackageName
  fileType       = 'EXE'
  file           = $fileLocation

  silentArgs     = '/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /SP- /TASKS="desktopicon"'
  validExitCodes = @(0)
}

Install-ChocolateyInstallPackage @packageArgs