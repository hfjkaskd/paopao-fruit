param(
    [Parameter(Mandatory = $true)][string]$Domain,
    [Parameter(Mandatory = $true)][string]$AppId,
    [Parameter(Mandatory = $true)][string]$Bundle,
    [string]$EncryptionKey = $env:APIREPLACE_KEY,
    [string]$SwaggerPath = 'C:\Users\pc\Desktop\PlayCloudSdk\PlayCloud_API\LocalAPIData\swagger-doc.json'
)

$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrEmpty($EncryptionKey)) { throw 'Missing required EncryptionKey / APIREPLACE_KEY.' }
if ($Domain.TrimEnd('/') -cne 'https://app.fruittile.xin') { throw 'This reviewed test is restricted to the explicitly authorized API domain.' }
$workspace = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..\..'))
$project = if (Test-Path -LiteralPath (Join-Path $workspace 'ProjectSettings\ProjectVersion.txt')) { $workspace } else { Join-Path $workspace 'BizzaWZ' }
$apiRoot = Join-Path $project 'Assets\BizzaWZ\Final\Connect_SDK\SDK_WKY\API'
$sourceFile = Join-Path $apiRoot 'xxtea-dotnet-master\XXTEA\XXTEA.cs'
# Compile the actual project encryption implementation; do not substitute a crypto algorithm.
Add-Type -TypeDefinition ("#define BIZZA_REAL_WITHDRAW`n" + [IO.File]::ReadAllText($sourceFile))
$swagger = [IO.File]::ReadAllText($SwaggerPath) | ConvertFrom-Json -AsHashtable
$settings = [IO.File]::ReadAllText((Join-Path $project 'ProjectSettings\ProjectSettings.asset'))
$version = [regex]::Match($settings, '(?m)^  bundleVersion: (.+)$').Groups[1].Value.Trim()
$versionCode = [int][regex]::Match($settings, '(?m)^  AndroidBundleVersionCode: (\d+)$').Groups[1].Value
$outFile = Join-Path $PSScriptRoot 'communication-results.json'
$report = [ordered]@{
    generatedAtUtc = [DateTime]::UtcNow.ToString('o')
    domain = $Domain.TrimEnd('/')
    appId = $AppId
    bundle = $Bundle
    tag = 'FruitTileMatch'
    projectVersion = $version
    status = 'running'
    scope = 'One synthetic login and read-only configuration/account queries. No real device identity, withdrawal, reward claim, feedback submission, attribution, or ad/event reporting.'
    execution = 'External PowerShell HTTP integration harness; not Unity Editor, Android, or iOS runtime execution.'
    contract = [ordered]@{
        encryption = 'Actual project Xxtea.XXTEA implementation compiled with BIZZA_REAL_WITHDRAW; UTF-8 input, length suffix, 16-byte zero-pad/truncate key.'
        encryptionSourceSha256 = (Get-FileHash -LiteralPath $sourceFile -Algorithm SHA256).Hash
        body = '{"sign":"base64(XXTEA(UTF8(payload),key))"}'
        contentType = 'application/octet-stream (Unity UploadHandlerRaw default; HttpUtil does not explicitly set Content-Type)'
        headers = @('X-Bundle', 'access_token only if returned by the synthetic session')
        response = 'HTTP outer JSON code/message/msg/st/data; code 200 data decrypted by same XXTEA implementation.'
        secrets = 'Key, encrypted bodies, user IDs, client IDs, and tokens are not persisted.'
        cryptoSelfCheck = 'pending'
    }
    tests = [Collections.Generic.List[object]]::new()
    notExecuted = @(
        @{ path='/Api/Match/AddRe'; reason='Changes revenue; no fabricated ad revenue reporting.' },
        @{ path='/Api/Match/Pay'; reason='Submits a real withdrawal.' },
        @{ path='/Api/Match/TaskPay'; reason='Submits a cash/voucher withdrawal.' },
        @{ path='/Api/Match/New'; reason='Claims a new-user reward.' },
        @{ path='/Api/Match/Notice'; reason='Submits feedback to other people; not authorized by this test.' },
        @{ path='/Api/Match/Come'; reason='Would submit synthetic attribution; validated locally by API replacement checks.' },
        @{ path='/Api/Match/Level'; reason='Changes gameplay progress.' },
        @{ path='/Api/Match/ReId'; reason='Allocates an ad revenue tracking ID; unnecessary for configuration/account communication verification.' },
        @{ path='/Api/Match/Name'; reason='Changes a profile nickname.' },
        @{ path='/Api/MatchLog/AdShow'; reason='Would submit a fabricated ad event.' },
        @{ path='/Api/MatchLog/AppEven'; reason='Would submit a fabricated app event.' },
        @{ path='/Sport/Balls/OrderName'; reason='No exact FruitTileMatch endpoint confirmed; unresolved endpoint is not called.' }
    )
}

function Save-Report {
    $report | ConvertTo-Json -Depth 60 | Set-Content -LiteralPath $outFile -Encoding utf8
}

function Protect-Value($Value, [string]$FieldName = '') {
    if ($FieldName -match '(?i)(token|secret|password|sign|^(uid|FtMuId|FtMaDd|FtMgAd|FtMsSd)$|email|account|address|phone)') { return '[REDACTED]' }
    if ($null -eq $Value) { return $null }
    if ($Value -is [Collections.IDictionary]) {
        $copy = [ordered]@{}
        foreach ($entry in $Value.GetEnumerator()) { $copy[$entry.Key] = Protect-Value $entry.Value $entry.Key }
        return $copy
    }
    if ($Value -is [array]) {
        $copy = @()
        foreach ($item in $Value) { $copy += ,(Protect-Value $item) }
        return ,$copy
    }
    return $Value
}

$probe = '{"protocolSelfCheck":"APIReplace","unicode":"通信"}'
$cipher = [Xxtea.XXTEA]::Encrypt($probe, $EncryptionKey)
if ([Xxtea.XXTEA]::DecryptToString($cipher, $EncryptionKey) -cne $probe) { throw 'Project XXTEA self-check failed.' }
$report.contract.cryptoSelfCheck = 'passed: actual project encryption/decryption round trip with Unicode payload'
Save-Report

$http = [Net.Http.HttpClient]::new()
$http.Timeout = [TimeSpan]::FromSeconds(15)
$sessionToken = ''

function Invoke-ApiProbe([string]$Path, [Collections.IDictionary]$Payload, [string]$Purpose) {
    $operation = $swagger.paths[$Path]['post']
    if ($null -eq $operation -or -not ($operation.tags -ccontains 'FruitTileMatch')) { throw "Exact Tag/path not found: $Path" }
    $requestSchema = @($operation.parameters | Where-Object { $_['in'] -eq 'body' })[0].schema
    $definitionName = $requestSchema['$ref'] -replace '^#/definitions/', ''
    $definition = $swagger.definitions[$definitionName]
    $unknownKeys = @($Payload.Keys | Where-Object { -not $definition.properties.Contains($_) })
    if ($unknownKeys.Count -gt 0) { throw "Request has keys absent from exact Swagger schema at $Path : $($unknownKeys -join ',')" }
    $entry = [ordered]@{
        path = $Path
        method = 'POST'
        purpose = $Purpose
        startedAtUtc = [DateTime]::UtcNow.ToString('o')
        status = 'running'
        requestSchema = $definitionName
        request = Protect-Value $Payload
        requestKeysCheckedAgainstSwagger = $true
        httpStatus = $null
        businessCode = $null
        message = $null
        response = $null
    }
    $report.tests.Add($entry)
    Save-Report
    $request = [Net.Http.HttpRequestMessage]::new([Net.Http.HttpMethod]::Post, ($Domain.TrimEnd('/') + $Path))
    $request.Headers.Add('X-Bundle', $Bundle)
    if (-not [string]::IsNullOrEmpty($script:sessionToken)) { $request.Headers.Add('access_token', $script:sessionToken) }
    $plain = $Payload | ConvertTo-Json -Depth 40 -Compress
    $sign = [Convert]::ToBase64String([Xxtea.XXTEA]::Encrypt($plain, $EncryptionKey))
    $wrapper = @{ sign = $sign } | ConvertTo-Json -Compress
    $request.Content = [Net.Http.ByteArrayContent]::new([Text.Encoding]::UTF8.GetBytes($wrapper))
    $request.Content.Headers.ContentType = [Net.Http.Headers.MediaTypeHeaderValue]::new('application/octet-stream')
    $watch = [Diagnostics.Stopwatch]::StartNew()
    $response = $null
    $result = $null
    try {
        $response = $http.SendAsync($request).GetAwaiter().GetResult()
        $entry.httpStatus = [int]$response.StatusCode
        $body = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
        try { $outer = $body | ConvertFrom-Json -AsHashtable } catch {
            $entry.status = 'failed'
            $entry.message = 'HTTP response was not valid outer JSON.'
            $entry.response = @{ length=$body.Length; sha256=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($body))) }
            return $null
        }
        $entry.businessCode = [string]$outer.code
        $entry.message = if ($outer.message) { $outer.message } else { $outer.msg }
        $entry.response = Protect-Value ([ordered]@{code=$outer.code; msg=$outer.msg; message=$outer.message; st=$outer.st; uid=$outer.uid})
        if (-not $response.IsSuccessStatusCode -or [string]$outer.code -cne '200') {
            $entry.status = 'failed'
            return $null
        }
        $decrypted = [Xxtea.XXTEA]::DecryptBase64StringToString([string]$outer.data, $EncryptionKey)
        $result = $decrypted | ConvertFrom-Json -AsHashtable -NoEnumerate
        $entry.response['decryptedData'] = Protect-Value $result
        $entry.response['decryptedRootType'] = if ($null -eq $result) { 'null' } elseif ($result -is [Collections.IDictionary]) { 'object' } elseif ($result -is [array]) { 'array' } else { $result.GetType().Name }
        $entry.status = 'passed'
        # The production code does not infer tokens from user IDs; neither does this harness.
        foreach ($tokenKey in @('access_token','token','accessToken')) {
            if ($outer[$tokenKey]) { $script:sessionToken = [string]$outer[$tokenKey] }
            if ($result -is [Collections.IDictionary] -and $result[$tokenKey]) { $script:sessionToken = [string]$result[$tokenKey] }
        }
    } catch {
        $entry.status = 'failed'
        $entry.message = $_.Exception.GetBaseException().Message.Replace($EncryptionKey, '[REDACTED]')
        $entry.errorType = $_.Exception.GetBaseException().GetType().FullName
    } finally {
        $entry.elapsedMs = $watch.ElapsedMilliseconds
        $entry.completedAtUtc = [DateTime]::UtcNow.ToString('o')
        Save-Report
        $request.Dispose()
        if ($null -ne $response) { $response.Dispose() }
        Write-Output -InputObject ([pscustomobject]@{ path=$Path; status=$entry.status; http=$entry.httpStatus; code=$entry.businessCode; message=$entry.message }) | Out-Host
    }
    return $result
}

try {
    $config = Invoke-ApiProbe '/Api/Match/AppConfig' ([ordered]@{FtMaId=$AppId; vn=$version; FtMCkn='look_ad_reward_mul'}) 'Read the actual configured Other key; keep its runtime value unchanged.'
    # Clearly marked synthetic identifiers. Re-running uses the same synthetic device identity.
    $testId = [Guid]::new([Security.Cryptography.MD5]::HashData([Text.Encoding]::UTF8.GetBytes("APIReplace:20260915:$AppId"))).ToString()
    $login = Invoke-ApiProbe '/Api/Match/Login' ([ordered]@{
        FtMaDb=1; FtMaDd='APIReplace-Synthetic-20260915'; FtMaId=$AppId; FtMbRd='APIReplaceTest';
        FtMcHl=''; FtMcHr=[DateTime]::UtcNow.Hour; FtMcPu='x86_64'; FtMcTy='';
        FtMdDy='1'; FtMdHt=1080; FtMdWh=1920; FtMgAd=''; FtMlGe='en';
        FtMmDl='Synthetic HTTP integration test'; FtMnWk='wifi'; FtMoVs='Windows Test Harness';
        FtMrOt=0; FtMsIm=0; FtMsSd=$testId; FtMtZe='Etc/UTC';
        FtMuA='APIReplace Communication Test'; FtMuId=''; vc=$versionCode; vn=$version
    }) 'One explicitly synthetic test login; may create a server-side test account. No real user/device identifier used.'
    $uid = if ($login -is [Collections.IDictionary]) { [string]$login['FtMuId'] } else { '' }
    $readOnlyPaths = @('/Api/Match/Info','/Api/Match/NoticeList','/Api/Match/Order','/Api/Match/Page','/Api/Match/Task')
    if ([string]::IsNullOrEmpty($uid)) {
        foreach ($path in $readOnlyPaths) { $report.tests.Add([ordered]@{path=$path; method='POST'; status='not_executed'; reason='Synthetic login did not return a valid user ID; dependent account query cannot be formed.'}) }
    } else {
        foreach ($path in $readOnlyPaths) {
            $payload = [ordered]@{FtMaId=$AppId; FtMuId=$uid}
            if ($path -in @('/Api/Match/NoticeList','/Api/Match/Page','/Api/Match/Task')) { $payload['vn']=$version }
            $null = Invoke-ApiProbe $path $payload 'Read-only query scoped to the synthetic test account.'
        }
    }
    $passed = @($report.tests | Where-Object {$_.status -eq 'passed'}).Count
    $failed = @($report.tests | Where-Object {$_.status -eq 'failed'}).Count
    $notExecuted = @($report.tests | Where-Object {$_.status -eq 'not_executed'}).Count
    $report['summary'] = @{passed=$passed; failed=$failed; dependentNotExecuted=$notExecuted; excludedNotExecuted=$report.notExecuted.Count}
    $report.status = if ($failed -gt 0) { 'completed_with_failures' } elseif ($notExecuted -gt 0) { 'completed_with_unexecuted_tests' } else { 'passed' }
} finally {
    $report['completedAtUtc'] = [DateTime]::UtcNow.ToString('o')
    $http.Dispose()
    Save-Report
}
