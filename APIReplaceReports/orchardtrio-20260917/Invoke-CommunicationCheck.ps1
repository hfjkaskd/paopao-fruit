param(
    [string]$ProjectPath = 'C:\Projects\paopao\BizzaWZ',
    [string]$SwaggerPath = 'C:\Users\pc\Desktop\PlayCloudSdk\PlayCloud_API\LocalAPIData\swagger-doc.json'
)
$ErrorActionPreference = 'Stop'
$key = $env:ORCHARDTRIO_API_TEST_KEY
if ([string]::IsNullOrWhiteSpace($key)) { throw 'ORCHARDTRIO_API_TEST_KEY must be supplied in this process environment.' }
$domain = 'https://snakes.xin'
$appId = 'orchardtrio'
$bundle = 'com.wiwitsugeh.orchardtrio'
$tag = 'SnakeOutJame'
$api = Join-Path $ProjectPath 'Assets\BizzaWZ\Final\Connect_SDK\SDK_WKY\API'
$cryptoFile = Join-Path $api 'xxtea-dotnet-master\XXTEA\XXTEA.cs'
$projectSettings = [IO.File]::ReadAllText((Join-Path $ProjectPath 'ProjectSettings\ProjectSettings.asset'))
$version = [regex]::Match($projectSettings, '(?m)^  bundleVersion: (.+)$').Groups[1].Value.Trim()
$versionCode = [int][regex]::Match($projectSettings, '(?m)^  AndroidBundleVersionCode: (\d+)$').Groups[1].Value
$schema = Get-Content -LiteralPath $SwaggerPath -Raw | ConvertFrom-Json -AsHashtable
Add-Type -TypeDefinition ("#define BIZZA_REAL_WITHDRAW`n" + [IO.File]::ReadAllText($cryptoFile))
$canary = '{"check":"OrchardTrio 通信"}'
if ([Xxtea.XXTEA]::DecryptToString([Xxtea.XXTEA]::Encrypt($canary, $key), $key) -cne $canary) { throw 'Project XXTEA round-trip failed.' }
$reportFile = Join-Path $PSScriptRoot 'communication-results.json'
$report = [ordered]@{
    startedUtc = [DateTime]::UtcNow.ToString('o'); project=$ProjectPath; domain=$domain; appId=$appId; bundle=$bundle; tag=$tag
    version=$version; versionCode=$versionCode; status='running'
    scope='Read-only configuration, one synthetic login, and read-only account/list queries; no withdrawal, revenue/reward, progress, feedback, attribution, or advertising/event submissions.'
    execution='External HTTP harness using the actual current project XXTEA.cs. This does not execute Unity transport or Unity DTO deserialization.'
    protocol=@{body='UTF8 JSON {sign: base64(XXTEA(UTF8(request), key))}'; contentType='application/octet-stream'; headers=@('X-Bundle','optional access_token'); cryptoRoundTrip='passed'; cryptoSourceSha256=(Get-FileHash -LiteralPath $cryptoFile -Algorithm SHA256).Hash; secrets='Key, token, encrypted bodies, identifiers, and public user nicknames are omitted or redacted.'}
    tests=[Collections.Generic.List[object]]::new()
    notExecuted=@(
        @{path='/Out/Jam/AddIncome';status='not_executed';reason='Changes earnings; do not fabricate advertising revenue.'},
        @{path='/Out/Jam/ApplyOrder';status='not_executed';reason='Submits a withdrawal.'},
        @{path='/Out/Jam/ApplyOrderTask';status='not_executed';reason='Submits a cash/voucher withdrawal.'},
        @{path='/Out/Jam/Come';status='not_executed';reason='Would fabricate attribution.'},
        @{path='/Out/Jam/IncomeId';status='not_executed';reason='Allocates revenue tracking state; unnecessary for the safe communication check.'},
        @{path='/Out/Jam/NewUser';status='not_executed';reason='Claims a reward.'},
        @{path='/Out/Jam/NickName';status='not_executed';reason='Changes a profile.'},
        @{path='/Out/Jam/Talk';status='not_executed';reason='Sends a feedback message to others.'},
        @{path='/Out/Jam/Times';status='not_executed';reason='Changes gameplay progress.'},
        @{path='/Out/JamLog/AdInfo';status='not_executed';reason='Would fabricate an advertisement event.'},
        @{path='/Out/JamLog/AppInfo';status='not_executed';reason='Would fabricate an application event.'},
        @{path='/Out/Jam/Lottery';status='not_executed';reason='Not an existing integrated AccountModuleCfg endpoint; creates lottery state.'},
        @{path='/Out/Jam/LotteryInfo';status='not_executed';reason='Not an existing integrated AccountModuleCfg endpoint.'}
    )
}
function Save-CommunicationReport {
    $report | ConvertTo-Json -Depth 60 | Set-Content -LiteralPath $reportFile -Encoding utf8
}
function Redact($value, [string]$name='') {
    if ($name -match '(?i)(token|password|secret|^sign$|^uid$|SoJ(Usid|Anid|Gaid|Seid|Nnm)$|email|address|phone)') { return '[REDACTED]' }
    if ($null -eq $value) { return $null }
    if ($value -is [Collections.IDictionary]) {
        $clean=[ordered]@{}
        foreach ($part in $value.GetEnumerator()) { $clean[$part.Key]=Redact $part.Value $part.Key }
        return $clean
    }
    if ($value -is [array]) {
        $clean=@()
        foreach ($item in $value) { $clean += ,(Redact $item) }
        return ,$clean
    }
    return $value
}
$client = [Net.Http.HttpClient]::new()
$client.Timeout = [TimeSpan]::FromSeconds(20)
$script:testToken=''
function Send-Probe([string]$path, [Collections.IDictionary]$payload, [string]$purpose) {
    $operation=$schema.paths[$path]['post']
    if (-not ($operation.tags -ccontains $tag)) { throw "No exact Tag/path match for $path" }
    $bodySchema=@($operation.parameters | Where-Object { $_['in'] -eq 'body' })[0].schema
    $schemaName=$bodySchema['$ref'] -replace '^#/definitions/',''
    $expected=$schema.definitions[$schemaName].properties
    $extra=@($payload.Keys | Where-Object { -not $expected.Contains($_) })
    if ($extra.Count -ne 0) { throw "Unconfirmed request keys at $path : $($extra -join ',')" }
    $entry=[ordered]@{path=$path;method='POST';purpose=$purpose;requestSchema=$schemaName;request=(Redact $payload);status='running';startedUtc=[DateTime]::UtcNow.ToString('o');httpStatus=$null;businessCode=$null;message=$null;response=$null}
    $report.tests.Add($entry)
    Save-CommunicationReport
    $request=[Net.Http.HttpRequestMessage]::new([Net.Http.HttpMethod]::Post, $domain+$path)
    $request.Headers.Add('X-Bundle',$bundle)
    if ($script:testToken) { $request.Headers.Add('access_token',$script:testToken) }
    $json=$payload | ConvertTo-Json -Depth 40 -Compress
    $wrapper=@{sign=[Convert]::ToBase64String([Xxtea.XXTEA]::Encrypt($json,$key))} | ConvertTo-Json -Compress
    $request.Content=[Net.Http.ByteArrayContent]::new([Text.Encoding]::UTF8.GetBytes($wrapper))
    $request.Content.Headers.ContentType=[Net.Http.Headers.MediaTypeHeaderValue]::new('application/octet-stream')
    $watch=[Diagnostics.Stopwatch]::StartNew()
    $reply=$null
    $data=$null
    try {
        $reply=$client.SendAsync($request).GetAwaiter().GetResult()
        $entry.httpStatus=[int]$reply.StatusCode
        $text=$reply.Content.ReadAsStringAsync().GetAwaiter().GetResult()
        try { $outer=$text | ConvertFrom-Json -AsHashtable } catch { throw 'Response is not valid outer JSON.' }
        $entry.businessCode=[string]$outer.code
        $entry.message=if($outer.message){$outer.message}else{$outer.msg}
        $entry.response=[ordered]@{code=$outer.code;message=$entry.message;st=$outer.st}
        if (-not $reply.IsSuccessStatusCode -or [string]$outer.code -cne '200') { $entry.status='failed'; return $null }
        $plain=[Xxtea.XXTEA]::DecryptBase64StringToString([string]$outer.data,$key)
        $data=$plain | ConvertFrom-Json -AsHashtable -NoEnumerate
        $entry.response.decryptedData=Redact $data
        $entry.response.rootType=if($null -eq $data){'null'}elseif($data -is [Collections.IDictionary]){'object'}elseif($data -is [array]){'array'}else{$data.GetType().Name}
        $entry.status='passed'
        foreach ($tokenName in @('access_token','accessToken','token')) {
            if($outer[$tokenName]) { $script:testToken=[string]$outer[$tokenName] }
            if($data -is [Collections.IDictionary] -and $data[$tokenName]) { $script:testToken=[string]$data[$tokenName] }
        }
    } catch {
        $entry.status='failed'
        $entry.message=$_.Exception.GetBaseException().Message.Replace($key,'[REDACTED]')
        $entry.errorType=$_.Exception.GetBaseException().GetType().FullName
    } finally {
        $entry.elapsedMs=$watch.ElapsedMilliseconds
        $entry.finishedUtc=[DateTime]::UtcNow.ToString('o')
        $request.Dispose()
        if($null -ne $reply) { $reply.Dispose() }
        Save-CommunicationReport
        Write-Host "$path $($entry.status) HTTP=$($entry.httpStatus) code=$($entry.businessCode)"
    }
    return ,$data
}
try {
    $null=Send-Probe '/Out/Jam/BaseData' ([ordered]@{SoJApid=$appId;SoJVn=$version;SoJcfn='look_ad_reward_mul'}) 'Read the unchanged project Other configuration key.'
    $syntheticId=[Guid]::new([Security.Cryptography.MD5]::HashData([Text.Encoding]::UTF8.GetBytes('APIReplace Synthetic orchardtrio 20260917'))).ToString()
    $login=Send-Probe '/Out/Jam/Login' ([ordered]@{
        SoJAnid='APIReplace-Synthetic-20260917';SoJApid=$appId;SoJAtd=1;SoJBbd='APIReplaceTest';SoJCal='';SoJCpu='x86_64';
        SoJCtr=[DateTime]::UtcNow.Hour;SoJDtd='1';SoJDth=1080;SoJDtw=1920;SoJGaid='';SoJLag='en';
        SoJMbl='Synthetic HTTP integration test';SoJNbt='wifi';SoJObv='Windows test harness';SoJRtt=0;SoJSbi=0;
        SoJSeid=$syntheticId;SoJTry='';SoJTtz='Etc/UTC';SoJUa='APIReplace Communication Test';SoJUsid='';SoJVc=$versionCode;SoJVn=$version
    }) 'One clearly synthetic test login; may create a test account; no real person/device identity.'
    $uid=if($login -is [Collections.IDictionary]){[string]$login.SoJUsid}else{''}
    foreach ($path in @('/Out/Jam/UserInfo','/Out/Jam/TalkList','/Out/Jam/Order','/Out/Jam/OrderNick','/Out/Jam/PayList','/Out/Jam/Task')) {
        if(-not $uid) { $report.tests.Add(@{path=$path;status='not_executed';reason='Synthetic login did not yield a usable user ID.'});continue }
        $payload=[ordered]@{SoJApid=$appId;SoJUsid=$uid}
        if($path -notin @('/Out/Jam/UserInfo','/Out/Jam/Order')) { $payload.SoJVn=$version }
        $null=Send-Probe $path $payload 'Read-only account/list query with synthetic session; returned public nicknames redacted.'
    }
    $report.summary=@{passed=@($report.tests|Where-Object status -eq 'passed').Count;failed=@($report.tests|Where-Object status -eq 'failed').Count;dependentNotExecuted=@($report.tests|Where-Object status -eq 'not_executed').Count;excludedNotExecuted=$report.notExecuted.Count}
    $report.status=if($report.summary.failed -gt 0){'completed_with_failures'}elseif($report.summary.dependentNotExecuted -gt 0){'completed_with_unexecuted_tests'}else{'passed'}
} finally {
    $report.finishedUtc=[DateTime]::UtcNow.ToString('o')
    Save-CommunicationReport
    $client.Dispose()
    $key=$null
    $script:testToken=$null
}
