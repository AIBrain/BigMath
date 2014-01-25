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

$here = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)";
psake "$here/Default.ps1" -properties "@{'config'=$config}";
