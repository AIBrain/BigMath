$here = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)";
Import-Module psake;
Invoke-psake "$here/Default.ps1" -properties @{'config'='Debug'};
