param(
    [string]$config = $null,
    [string]$packageVersion = $null
)

if (!$config) 
{    
    $config = if (Test-Path Env:Configuration) { Get-Content Env:Configuration } else { 'Debug' }
}

if (!$packageVersion -and (Test-Path Env:PackageVersion)) { $packageVersion = Get-Content env:PackageVersion };

$here = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)";
$psakePath = Join-Path $here -Child "lib\psake\psake.psm1";
Import-Module $psakePath;
Invoke-psake "$here/Default.ps1" -properties @{'config'=$config; 'packageVersion'=$packageVersion};
