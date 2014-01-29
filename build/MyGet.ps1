param(
    [string]$packageVersion = $null,
    [string]$config = "Release",
    [string[]]$targetFrameworks = @("v4.0", "v4.5", "v4.5.1"),
    [string[]]$platforms = @("AnyCpu"),
    [ValidateSet("rebuild", "build")]
    [string]$target = "rebuild",
    [ValidateSet("quiet", "minimal", "normal", "detailed", "diagnostic")]
    [string]$verbosity = "minimal",
    [bool]$alwaysClean = $true
)
if (!$packageVersion)
{
    $packageVersion = $env:PackageVersion;
}
$here = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)";
$psakePath = Join-Path $here -Child "lib\psake\psake.psm1";
Import-Module $psakePath;
Invoke-psake "$here/Default.ps1" -properties @{'config'=$config; 'packageVersion'=$packageVersion};
