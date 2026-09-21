$ErrorActionPreference = 'Stop'
$taskManifest = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'manifest.json') -Raw | ConvertFrom-Json
$taskRoot = [IO.Path]::GetFullPath($taskManifest.root).TrimEnd('\')
if ($taskRoot -ne 'C:\Projects\DJS_Apple\FruitsHarvestMaster-source') { throw 'Unexpected project root' }
if (-not (Test-Path -LiteralPath $taskManifest.backup)) { throw 'Backup is missing' }
$taskOperations = @()
foreach ($taskItem in $taskManifest.items) {
    $taskPath = [IO.Path]::GetFullPath((Join-Path $taskRoot $taskItem.path))
    if (-not $taskPath.StartsWith($taskRoot + '\', [StringComparison]::OrdinalIgnoreCase) -or $taskPath.Contains('\.git\')) { throw 'Invalid target path' }
    if ($taskItem.action -eq 'create') {
        if (Test-Path -LiteralPath $taskPath) { throw "Recreated file: $taskPath" }
    } elseif ($taskItem.action -eq 'replace') {
        if ((Get-FileHash -LiteralPath $taskPath -Algorithm SHA256).Hash -ne $taskItem.originalSha256) { throw "Concurrent edit: $taskPath" }
    } else { throw 'Unexpected action' }
    if ((Get-FileHash -LiteralPath $taskItem.stagedPath -Algorithm SHA256).Hash -ne $taskItem.stagedSha256) { throw 'Staged content changed' }
    $taskOperations += [pscustomobject]@{ Target = $taskPath; Source = $taskItem.stagedPath }
}
foreach ($taskGuard in $taskManifest.unchangedGuards) {
    if ((Get-FileHash -LiteralPath (Join-Path $taskRoot $taskGuard.path) -Algorithm SHA256).Hash -ne $taskGuard.sha256) { throw 'Homepage changed during preparation' }
}
foreach ($taskOperation in $taskOperations) {
    $taskParent = Split-Path -Parent $taskOperation.Target
    if (-not (Test-Path -LiteralPath $taskParent)) { New-Item -ItemType Directory -Path $taskParent -Force | Out-Null }
    Copy-Item -LiteralPath $taskOperation.Source -Destination $taskOperation.Target -Force
}
'Restored withdrawal functionality: ' + $taskOperations.Count + ' verified files; homepage removals preserved.'
