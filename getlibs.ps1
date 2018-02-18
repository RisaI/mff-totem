# If you get this message:
# getlibs.ps1 cannot be loaded because running scripts is disabled on this system
# Execute PowerShell as admin and paste:
# Set-ExecutionPolicy RemoteSigned

$ErrorActionPreference = "Stop"

function Expand-Tar($TarFile, $Destination) {

    if (-not (Get-Command Expand-7Zip -ErrorAction Ignore)) {
        Install-Package -Scope CurrentUser -Force 7Zip4PowerShell > $null
    }

    Expand-7Zip $TarFile $Destination
}

Write-Output "Downloading libraries..."

Invoke-WebRequest http://fna.flibitijibibo.com/archive/fnalibs.tar.bz2 -OutFile libs.tar.bz2

if (Test-Path -Path libs) {
    Remove-Item libs -Force -Recurse
}

New-Item -ItemType directory -Name libs

Write-Output "Exctracting..."

Expand-Tar -TarFile libs.tar.bz2 -Destination .
Remove-Item libs.tar.bz2
Expand-Tar -TarFile libs.tar -Destination libs
Remove-Item libs.tar

Write-Output "Libs have been extracted to the libs/ directory. Copy files for your platform to the debug folder."
Write-Output "It will look something like this: mff-totem\Mff.Totem\bin\DesktopGL\AnyCPU\Debug"