$here = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)";
psake "$here/Default.ps1" -properties "@{'config'=$Configuration}";
