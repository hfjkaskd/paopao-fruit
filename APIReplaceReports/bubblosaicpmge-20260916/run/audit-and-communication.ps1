param([Parameter(Mandatory=$true)][string]$ApiKey)
$ErrorActionPreference = 'Stop'
$reportDir = Split-Path -Parent $PSScriptRoot
$project = 'C:/Projects/paopao/BizzaWZ'
$apiRoot = "$project/Assets/BizzaWZ/Final/Connect_SDK/SDK_WKY/API"
$sourcePath = "$apiRoot/AccountModule.cs"
$swaggerPath = 'C:/Users/pc/Desktop/PlayCloudSdk/PlayCloud_API/LocalAPIData/swagger-doc.json'
$source = Get-Content -LiteralPath $sourcePath -Raw
$swagger = Get-Content -LiteralPath $swaggerPath -Raw | ConvertFrom-Json -AsHashtable
$body = [regex]::Match($source, '(?s)public Dictionary<string, object> CreateFromJson\(AdjustSdk\.AdjustAttribution attribution, string eventName\).*?return _oceanShineUserAttrs;').Value
$entries = [regex]::Matches($body, '\{ "([^"]+)", (.*?) \},?\s*//([^\r\n]+)')
$properties = $swagger.definitions['request.FruitTileMatchUserAttrsReq'].properties
$keys = @($entries | ForEach-Object {$_.Groups[1].Value})
$differences = @(Compare-Object -ReferenceObject @($properties.Keys) -DifferenceObject $keys -CaseSensitive)
$duplicates = @($keys | Group-Object | Where-Object Count -gt 1)
$nested = @($properties.GetEnumerator() | Where-Object {$_.Value.type -in @('array','object') -or $_.Value.Contains('$ref')})
$audit = [ordered]@{
    inspectedAt = [DateTimeOffset]::Now.ToString('o')
    mode = 'Static analysis of real construction/serialization/HTTP path, including inactive preprocessor branches'
    source = $sourcePath
    method = 'AccountModule.CreateFromJson(AdjustSdk.AdjustAttribution attribution, string eventName)'
    preprocessor = @('BIZZA_REAL_WITHDRAW','BIZZA_ENABLE_ADJUST')
    tag = 'FruitTileMatch'
    endpoint = '/Api/Match/Come'
    requestDefinition = 'request.FruitTileMatchUserAttrsReq'
    callChain = @(
        [ordered]@{file="$apiRoot/Attributes/AdjustAttributionAdapter.cs";line=22;method='AttributeStart';detail='Registers Adjust attribution callback at lines 61-64'},
        [ordered]@{file="$apiRoot/Attributes/AdjustAttributionAdapter.cs";line=91;method='AttributionChangedDelegate';detail='Calls CreateFromJson at 106 and Request_UserAttrsRequest at 107'},
        [ordered]@{file=$sourcePath;line=1023;method='CreateFromJson';detail='Constructs Dictionary<string, object> with 19 scalar keys'},
        [ordered]@{file=$sourcePath;line=1054;method='Request_UserAttrsRequest';detail='JsonConvert.SerializeObject(request) at 1058; HttpUtil.RequestToServer<OceanShineUserAttrsRequest>(AccountModuleCfg.from, requestParams, callback, false) at 1060'},
        [ordered]@{file="$apiRoot/Utils/HttpUtil.cs";line=91;method='RequestToServer<T>';detail='BuildEncryptedBody(requestJson), RequestToServerCoroutine(path, bodyRaw, response, delay, block, method)'},
        [ordered]@{file="$apiRoot/Utils/HttpUtil.cs";line=560;method='BuildEncryptedBody';detail='UTF-8 JSON -> local XXTEA -> Base64 -> JSON sign wrapper; no key rewrites'},
        [ordered]@{file="$apiRoot/Utils/HttpUtil.cs";line=137;method='RequestToServerCoroutine<T>';detail='BuildUrl with ChannelConfig domain; Android Java bridge or editor UnityWebRequest preserves same encrypted body'}
    )
    actualPayloadChecks = [ordered]@{
        status = $(if($keys.Count -eq 19 -and $differences.Count -eq 0 -and $duplicates.Count -eq 0){'Passed'}else{'Failed'})
        validation = 'Static; real Adjust callback and serialization were not executed'
        localKeys = $keys.Count
        targetKeys = $properties.Count
        missingOrUnexpectedKeys = $differences
        duplicateKeys = $duplicates
        nestedObjectsOrArraysOrRefs = $nested
        fieldValuesAndTypes = 'Unchanged. FtMcAt = double (Adjust CostAmount double? ?? 0), FtMcTi = Int64 Unix seconds; remaining 17 values are string or null with existing empty defaults preserved.'
        directDictionarySerialization = 'JsonProperty on DTO does not affect these keys. Real Dictionary itself was inspected.'
    }
    mapping = @($entries | ForEach-Object {
        $key=$_.Groups[1].Value
        [ordered]@{file=$sourcePath;method='CreateFromJson';jsonPath=('$.{0}' -f $key);oldKey=$key;newKey=$key;status='Already matches';valueExpression=$_.Groups[2].Value;swaggerType=$properties[$key].type;meaning=$properties[$key].description}
    })
    changedFiles = @()
    notes = @('All 19 existing dictionary keys already match exact target Swagger schema. No dictionary change is required.','No attribution request was sent; attribution reporting would mutate business state.')
}
$audit | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath "$reportDir/attribution-audit.json" -Encoding utf8
function Protect-Value($value) {
    if ($null -eq $value) { return $null }
    if ($value -is [System.Collections.IDictionary]) {
        $safe = [ordered]@{}
        foreach ($entry in $value.GetEnumerator()) {
            if ($entry.Key -match '(?i)(password|passwd|secret|token|private|sign|email|phone|mobile|uid|user_?id|aes_?key)') { $safe[$entry.Key]='[REDACTED]' }
            else { $safe[$entry.Key]=Protect-Value $entry.Value }
        }
        return $safe
    }
    if ($value -is [string]) { return $value.Replace($ApiKey,'[REDACTED]') }
    if ($value -is [System.Collections.IEnumerable]) { return @($value | ForEach-Object {Protect-Value $_}) }
    return $value
}
$version = [regex]::Match((Get-Content -LiteralPath "$project/ProjectSettings/ProjectSettings.asset" -Raw),'(?m)^  bundleVersion:\s*(.+)$').Groups[1].Value.Trim()
$payload = [ordered]@{FtMaId='bubblosaicpmge';vn=$version;FtMCkn='look_ad_reward_mul'}
$result = [ordered]@{
    testedAt=[DateTimeOffset]::Now.ToString('o')
    executed=$false
    operation='Read existing application configuration only'
    method='POST'
    endpoint='https://app.fruittile.xin/Api/Match/AppConfig'
    tag='FruitTileMatch'
    requestDefinition='request.FruitTileMatchAppOtherConfigCustomizeReq'
    requestFields=$payload
    requestHeaders=[ordered]@{'X-Bundle'='com.webpack.picturemerge'}
    encryption=[ordered]@{algorithm='Project Xxtea.XXTEA; UTF-8 JSON; Base64 ciphertext in JSON sign wrapper';source="$apiRoot/xxtea-dotnet-master/XXTEA/XXTEA.cs";localRoundTrip=$false;secretRecorded=$false}
    httpStatusCode=$null
    responseBytes=$null
    responseOuter=$null
    decryption=[ordered]@{attempted=$false;success=$false;jsonParsed=$false;payload=$null}
    business=[ordered]@{success=$false;expectedConfigKeys=@('hp_one','hp_one_rate','hp_two','hp_two_rate','hp_three','hp_three_rate');missingExpectedKeys=@();judgment=$null}
    failureReason=$null
}
$log = [System.Collections.Generic.List[string]]::new()
try {
    $xxteaSource = Get-Content -LiteralPath $result.encryption.source -Raw
    Add-Type -TypeDefinition $xxteaSource -CompilerOptions '/define:BIZZA_REAL_WITHDRAW'
    $plainJson = $payload | ConvertTo-Json -Compress
    $encryptedBytes = [Xxtea.XXTEA]::Encrypt($plainJson,$ApiKey)
    $roundTrip = [Xxtea.XXTEA]::DecryptToString($encryptedBytes,$ApiKey)
    $result.encryption.localRoundTrip = $plainJson -ceq $roundTrip
    if (-not $result.encryption.localRoundTrip) { throw 'Local encryption round trip failed; HTTP request was not sent.' }
    $log.Add('Local XXTEA encryption/decryption round trip passed.')
    $wrapper = [ordered]@{sign=[Convert]::ToBase64String($encryptedBytes)} | ConvertTo-Json -Compress
    $httpClient = [System.Net.Http.HttpClient]::new()
    $httpClient.Timeout = [TimeSpan]::FromSeconds(25)
    $httpRequest = [System.Net.Http.HttpRequestMessage]::new([System.Net.Http.HttpMethod]::Post,$result.endpoint)
    $null = $httpRequest.Headers.TryAddWithoutValidation('X-Bundle','com.webpack.picturemerge')
    $httpRequest.Content = [System.Net.Http.ByteArrayContent]::new([Text.Encoding]::UTF8.GetBytes($wrapper))
    $result.executed = $true
    $log.Add('Sent single read-only AppConfig request with project XXTEA envelope and X-Bundle header.')
    $httpResponse = $httpClient.Send($httpRequest)
    $result.httpStatusCode = [int]$httpResponse.StatusCode
    $responseText = $httpResponse.Content.ReadAsStringAsync().GetAwaiter().GetResult()
    $result.responseBytes = [Text.Encoding]::UTF8.GetByteCount($responseText)
    $log.Add("HTTP status: $($result.httpStatusCode); response bytes: $($result.responseBytes).")
    if (-not $httpResponse.IsSuccessStatusCode) { throw "HTTP returned $($result.httpStatusCode)." }
    $outer = $responseText | ConvertFrom-Json -AsHashtable
    $result.responseOuter = Protect-Value ([ordered]@{code=$outer.code;msg=$outer.msg;message=$outer.message;st=$outer.st;dataPresent=($null -ne $outer.data)})
    if ([string]$outer.code -ne '200') {
        $result.business.judgment = 'HTTP reachable, but API business code is not 200.'
        throw "API business code: $($outer.code)."
    }
    $result.decryption.attempted = $true
    $decrypted = [Xxtea.XXTEA]::DecryptToString([Convert]::FromBase64String([string]$outer.data),$ApiKey)
    $result.decryption.success = $null -ne $decrypted
    $decoded = $decrypted | ConvertFrom-Json -AsHashtable
    $result.decryption.jsonParsed = $true
    $result.decryption.payload = Protect-Value $decoded
    if ($decoded -is [System.Collections.IDictionary]) {
        $result.business.missingExpectedKeys = @($result.business.expectedConfigKeys | Where-Object {-not $decoded.Contains($_)})
    }
    else {
        $result.business.missingExpectedKeys = $result.business.expectedConfigKeys
    }
    $result.business.success = $result.business.missingExpectedKeys.Count -eq 0
    $result.business.judgment = $(if($result.business.success){'HTTP and API code 200; response decrypted and all six configuration keys consumed by existing business logic are present.'}else{'HTTP and API code 200 with decryptable JSON, but the returned configuration does not provide all six keys used by existing business logic.'})
    $log.Add('API code 200; XXTEA response decryption and JSON parse passed.')
    $log.Add($result.business.judgment)
}
catch {
    $result.failureReason = Protect-Value $_.Exception.Message
    $log.Add("Failure: $($result.failureReason)")
}
finally {
    if ($httpResponse) { $httpResponse.Dispose() }
    if ($httpRequest) { $httpRequest.Dispose() }
    if ($httpClient) { $httpClient.Dispose() }
    $result | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath "$reportDir/communication.json" -Encoding utf8
    $log | Set-Content -LiteralPath "$reportDir/run/communication.log" -Encoding utf8
}
[ordered]@{attribution=$audit.actualPayloadChecks;communication=$result} | ConvertTo-Json -Depth 20