param([Parameter(Mandatory=$true)][string]$Command)
$folder=Join-Path $PSScriptRoot '../OrchardImplementation-20260928'
$began=Get-Date
$temp=Join-Path $folder 'command.tmp'
[IO.File]::WriteAllText($temp,$Command)
Move-Item -LiteralPath $temp -Destination (Join-Path $folder 'command.txt') -Force
do {
    Start-Sleep -Milliseconds 300
    $result=Get-Item (Join-Path $folder 'result.txt') -ErrorAction SilentlyContinue
} while (($null -eq $result -or $result.LastWriteTime -lt $began) -and ((Get-Date)-$began).TotalSeconds -lt 55)
if ($null -eq $result -or $result.LastWriteTime -lt $began) { throw "Command pending: $Command" }
$text=Get-Content -LiteralPath $result.FullName -Raw
Write-Output $text
if ($text.StartsWith('FAILED')) { exit 1 }
