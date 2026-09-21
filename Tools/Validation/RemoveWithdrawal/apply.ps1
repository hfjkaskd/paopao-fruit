$ErrorActionPreference = 'Stop'
$taskManifest = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'manifest.json') -Raw | ConvertFrom-Json
$taskRoot = [IO.Path]::GetFullPath($taskManifest.root).TrimEnd('\')
if ($taskRoot -ne 'C:\Projects\DJS_Apple\FruitsHarvestMaster-source') { throw 'Unexpected project root' }
if (-not (Test-Path -LiteralPath $taskManifest.backup)) { throw 'Backup is missing' }
$taskTargets = @()
foreach ($taskItem in $taskManifest.items) {
    $taskTarget = [IO.Path]::GetFullPath((Join-Path $taskRoot $taskItem.path))
    if (-not $taskTarget.StartsWith($taskRoot + '\', [StringComparison]::OrdinalIgnoreCase) -or $taskTarget.Contains('\.git\')) { throw "Invalid target: $taskTarget" }
    if ((Get-FileHash -LiteralPath $taskTarget -Algorithm SHA256).Hash -ne $taskItem.originalSha256) { throw "Concurrent edit: $taskTarget" }
    if ($taskItem.action -eq 'replace' -and (Get-FileHash -LiteralPath $taskItem.stagedPath -Algorithm SHA256).Hash -ne $taskItem.stagedSha256) { throw 'Staged content changed' }
    $taskTargets += [pscustomobject]@{ Target = $taskTarget; Item = $taskItem }
}
# All targets and their hashes have been verified before the first write.
foreach ($taskOperation in $taskTargets) {
    if ($taskOperation.Item.action -eq 'replace') {
        Copy-Item -LiteralPath $taskOperation.Item.stagedPath -Destination $taskOperation.Target -Force
    } elseif ($taskOperation.Item.action -eq 'delete') {
        Remove-Item -LiteralPath $taskOperation.Target
    } else { throw 'Unknown operation' }
}
# Remove only empty directories, never recursively and never outside this project.
$taskFolders = $taskTargets | Where-Object { $_.Item.action -eq 'delete' } | ForEach-Object { Split-Path -Parent $_.Target } | Sort-Object -Unique | Sort-Object Length -Descending
foreach ($taskFolderStart in $taskFolders) {
    $taskFolder = $taskFolderStart
    while ($taskFolder.StartsWith($taskRoot + '\', [StringComparison]::OrdinalIgnoreCase) -and (Test-Path -LiteralPath $taskFolder)) {
        if (@(Get-ChildItem -LiteralPath $taskFolder -Force).Count -ne 0) { break }
        Remove-Item -LiteralPath $taskFolder
        $taskFolder = Split-Path -Parent $taskFolder
    }
}
'Applied simulation removal: ' + $taskTargets.Count + ' verified files.'
