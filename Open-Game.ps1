[CmdletBinding()]
param(
    [string]$UnityEditor
)

$ErrorActionPreference = 'Stop'

$taskProjectPath = Join-Path $PSScriptRoot 'BizzaWZ'
$taskVersionFile = Join-Path $taskProjectPath 'ProjectSettings\ProjectVersion.txt'
if (-not (Test-Path -LiteralPath $taskVersionFile -PathType Leaf)) {
    throw "Unity project version file not found: $taskVersionFile"
}

$taskVersionMatch = Select-String -LiteralPath $taskVersionFile -Pattern '^m_EditorVersion:\s*(\S+)\s*$'
if ($null -eq $taskVersionMatch) {
    throw "Cannot read the Unity version from: $taskVersionFile"
}
$taskUnityVersion = $taskVersionMatch.Matches[0].Groups[1].Value

if ([string]::IsNullOrWhiteSpace($UnityEditor)) {
    $taskInstallRoots = @($env:ProgramFiles, ${env:ProgramFiles(x86)}) |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
        Select-Object -Unique
    foreach ($taskInstallRoot in $taskInstallRoots) {
        $taskCandidate = Join-Path $taskInstallRoot "Unity\Hub\Editor\$taskUnityVersion\Editor\Unity.exe"
        if (Test-Path -LiteralPath $taskCandidate -PathType Leaf) {
            $UnityEditor = $taskCandidate
            break
        }
    }
}

if ([string]::IsNullOrWhiteSpace($UnityEditor) -or
    -not (Test-Path -LiteralPath $UnityEditor -PathType Leaf)) {
    throw "Unity $taskUnityVersion was not found. Install it with Unity Hub or pass -UnityEditor with its Unity.exe path."
}

$taskProjectPath = (Resolve-Path -LiteralPath $taskProjectPath).Path
$taskEditorPath = (Resolve-Path -LiteralPath $UnityEditor).Path
$taskEditorVersion = (Get-Item -LiteralPath $taskEditorPath).VersionInfo.ProductVersion
if ($taskEditorVersion -notmatch ('^' + [regex]::Escape($taskUnityVersion) + '(?:_|$)')) {
    throw "The selected editor version '$taskEditorVersion' does not match project version '$taskUnityVersion'."
}

$taskInstanceFile = Join-Path $taskProjectPath 'Library\EditorInstance.json'
$taskLockFile = Join-Path $taskProjectPath 'Temp\UnityLockfile'
$taskExistingConfirmed = $false
$taskExistingPid = 0
if (Test-Path -LiteralPath $taskInstanceFile -PathType Leaf) {
    try {
        $taskInstance = Get-Content -LiteralPath $taskInstanceFile -Raw | ConvertFrom-Json
        $taskExistingPid = [int]$taskInstance.process_id
        if ($taskExistingPid -gt 0) {
            $taskProcess = Get-Process -Id $taskExistingPid -ErrorAction Stop
            $taskRecordedExe = [System.IO.Path]::GetFullPath([string]$taskInstance.app_path)
            # PID alone is insufficient: it may have been reused by another process.
            if ($taskProcess.Path -and
                [System.IO.Path]::GetFullPath($taskProcess.Path) -ieq $taskRecordedExe -and
                [System.IO.Path]::GetFileName($taskRecordedExe) -ieq 'Unity.exe') {
                $taskProcessInfo = Get-CimInstance Win32_Process -Filter "ProcessId = $taskExistingPid" -ErrorAction Stop
                $taskProjectArgument = [regex]::Match([string]$taskProcessInfo.CommandLine,
                    '(?i)(?:^|\s)-projectPath\s+(?:"(?<quoted>[^"]+)"|(?<plain>\S+))')
                if ($taskProcessInfo.ExecutablePath -and $taskProjectArgument.Success -and
                    [System.IO.Path]::GetFullPath($taskProcessInfo.ExecutablePath) -ieq $taskRecordedExe) {
                    $taskRunningProject = $taskProjectArgument.Groups['quoted'].Value
                    if (-not $taskRunningProject) {
                        $taskRunningProject = $taskProjectArgument.Groups['plain'].Value
                    }
                    $taskRunningProject = [System.IO.Path]::GetFullPath($taskRunningProject).TrimEnd('\', '/')
                    $taskExistingConfirmed = $taskRunningProject -ieq $taskProjectPath.TrimEnd('\', '/')
                }
            }
        }
    }
    catch {
        # Missing process, stale metadata or denied process access cannot confirm an instance.
        $taskExistingConfirmed = $false
    }
}

if ($taskExistingConfirmed) {
    Write-Host "BizzaWZ is already running in Unity (PID $taskExistingPid). Switch to its existing editor window."
    Write-Host 'No second instance was started.'
    return
}
if (Test-Path -LiteralPath $taskLockFile -PathType Leaf) {
    try {
        $taskUnityProcesses = @(Get-CimInstance Win32_Process -Filter "Name = 'Unity.exe'" -ErrorAction Stop)
    }
    catch {
        throw 'Cannot read all running Unity processes. Refusing to start while BizzaWZ has a UnityLockfile.'
    }
    foreach ($taskUnityProcess in $taskUnityProcesses) {
        $taskProjectArgument = [regex]::Match([string]$taskUnityProcess.CommandLine,
            '(?i)(?:^|\s)-projectPath\s+(?:"(?<quoted>[^"]+)"|(?<plain>\S+))')
        if (-not $taskUnityProcess.ExecutablePath -or -not $taskProjectArgument.Success) {
            throw 'Cannot identify every running Unity project. Refusing to start while BizzaWZ has a UnityLockfile.'
        }
        $taskRunningProject = $taskProjectArgument.Groups['quoted'].Value
        if (-not $taskRunningProject) {
            $taskRunningProject = $taskProjectArgument.Groups['plain'].Value
        }
        $taskRunningProject = [System.IO.Path]::GetFullPath($taskRunningProject).TrimEnd('\', '/')
        if ($taskRunningProject -ieq $taskProjectPath.TrimEnd('\', '/')) {
            Write-Host "BizzaWZ is already running in Unity (PID $($taskUnityProcess.ProcessId)). Switch to its existing editor window."
            Write-Host 'No second instance was started.'
            return
        }
    }

    $taskLockProbe = $null
    try {
        $taskLockProbe = [System.IO.File]::Open($taskLockFile,
            [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::None)
    }
    catch {
        throw 'BizzaWZ UnityLockfile is in use or cannot be checked. Refusing to start a duplicate instance.'
    }
    finally {
        if ($null -ne $taskLockProbe) { $taskLockProbe.Dispose() }
    }
    Write-Host 'No BizzaWZ editor is running and its remaining lock file is unused. Unity will handle it on startup.'
}

Write-Host "Opening $taskProjectPath with Unity $taskUnityVersion"
Write-Host 'Start gameplay from Assets/Game/Resources/Scenes/InitWZ.unity.'
Start-Process -FilePath $taskEditorPath -ArgumentList @('-projectPath', ('"{0}"' -f $taskProjectPath)) -WindowStyle Normal
