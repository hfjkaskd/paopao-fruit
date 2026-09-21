$ErrorActionPreference = 'Stop'
$releaseWork = $PSScriptRoot
$projectRoot = [IO.Path]::GetFullPath((Join-Path $releaseWork '../../../BizzaWZ'))
$unityData = 'C:/Program Files/Unity/Hub/Editor/2022.3.62f3/Editor/Data'
$framework = Join-Path $unityData 'MonoBleedingEdge/lib/mono/4.8-api'
$api = Join-Path $projectRoot 'Assets/BizzaWZ/Final/Connect_SDK/SDK_WKY/API'
$jsonDll = Join-Path $projectRoot 'Library/PackageCache/com.unity.nuget.newtonsoft-json@3.2.2/Runtime/Newtonsoft.Json.dll'
$response = @(
    '/nologo', '/noconfig', '/nostdlib+', '/target:exe', '/langversion:9', '/define:BIZZA_REAL_WITHDRAW',
    ('/out:"' + (Join-Path $releaseWork 'ReleaseConfigHarness.exe') + '"'),
    ('/reference:"' + (Join-Path $framework 'mscorlib.dll') + '"'),
    ('/reference:"' + (Join-Path $framework 'System.dll') + '"'),
    ('/reference:"' + (Join-Path $framework 'System.Core.dll') + '"'),
    ('/reference:"' + (Join-Path $framework 'Facades/netstandard.dll') + '"'),
    ('/reference:"' + $jsonDll + '"'),
    ('"' + (Join-Path $api 'ConfigUtils/ChannelConfig.cs') + '"'),
    ('"' + (Join-Path $api 'ConfigUtils/ChannelConfigBinarySerializer.cs') + '"'),
    ('"' + (Join-Path $api 'xxtea-dotnet-master/XXTEA/XXTEA.cs') + '"'),
    ('"' + (Join-Path $releaseWork 'ReleaseConfigHarness.Stubs.cs') + '"'),
    ('"' + (Join-Path $releaseWork 'ReleaseConfigHarness.cs') + '"')
)
$responsePath = Join-Path $releaseWork 'release-harness.rsp'
[IO.File]::WriteAllLines($responsePath, $response)
& (Join-Path $unityData 'NetCoreRuntime/dotnet.exe') (Join-Path $unityData 'DotNetSdkRoslyn/csc.dll') ('@' + $responsePath)
if ($LASTEXITCODE -ne 0) { throw "Harness compilation failed: $LASTEXITCODE" }
Copy-Item -LiteralPath $jsonDll -Destination (Join-Path $releaseWork 'Newtonsoft.Json.dll')
