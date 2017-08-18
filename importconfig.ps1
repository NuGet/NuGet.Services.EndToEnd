[CmdletBinding()]
param (
    [string]$Instance,
    [string]$Project,
    [string]$PersonalAccessToken,
    [string]$Repository,
    [string]$FileName,
    [string]$OutputDirectory,
    [string]$OutputFile
)

Add-Type -AssemblyName System.IO.Compression.FileSystem

New-Item $OutputDirectory -type directory -force

$basicAuth = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes(("{0}:{1}" -f 'PAT', $PersonalAccessToken)))
$headers = @{ Authorization = ("Basic {0}" -f $basicAuth) }

$requestUri = "https://$Instance.visualstudio.com/DefaultCollection/$Project/_apis/git/repositories/$Repository/items?api-version=1.0&scopePath=$FileName"
Invoke-WebRequest -UseBasicParsing -Uri $requestUri -OutFile "$OutputDirectory\$OutputFile" -Headers $headers
