$ErrorActionPreference = 'Stop'
$auditRoot = $PSScriptRoot
$unityData = 'C:\Program Files\Unity\Hub\Editor\2022.3.62f3\Editor\Data'
$runtime = Join-Path $unityData 'NetCoreRuntime\shared\Microsoft.NETCore.App\6.0.21'
$compiler = Join-Path $unityData 'DotNetSdkRoslyn'
$response = @('-nologo', '-target:exe', '-langversion:9.0', ('-out:"' + (Join-Path $auditRoot 'AuditMethods.dll') + '"'))
$response += Get-ChildItem -LiteralPath $runtime -Filter '*.dll' | Where-Object { ($_.Name -match '^System\.' -and $_.Name -notmatch '\.Native\.') -or $_.Name -in @('mscorlib.dll','netstandard.dll') } | ForEach-Object { '-r:"' + $_.FullName + '"' }
$response += '-r:"' + (Join-Path $compiler 'Microsoft.CodeAnalysis.dll') + '"'
$response += '-r:"' + (Join-Path $compiler 'Microsoft.CodeAnalysis.CSharp.dll') + '"'
$response += '"' + (Join-Path $auditRoot 'AuditMethods.cs') + '"'
$rspPath = Join-Path $auditRoot 'audit-methods.rsp'
$response | Set-Content -LiteralPath $rspPath -Encoding utf8
& (Join-Path $unityData 'NetCoreRuntime\dotnet.exe') (Join-Path $compiler 'csc.dll') ('@' + $rspPath)
if ($LASTEXITCODE -ne 0) { throw "Audit compiler failed: $LASTEXITCODE" }
Copy-Item -LiteralPath (Join-Path $compiler 'Microsoft.CodeAnalysis.dll'), (Join-Path $compiler 'Microsoft.CodeAnalysis.CSharp.dll') -Destination $auditRoot
'{"runtimeOptions":{"tfm":"net6.0","framework":{"name":"Microsoft.NETCore.App","version":"6.0.21"}}}' | Set-Content -LiteralPath (Join-Path $auditRoot 'AuditMethods.runtimeconfig.json') -Encoding utf8
