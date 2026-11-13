[CmdletBinding(DefaultParameterSetName = 'Success')]
param(
    [Parameter(Mandatory = $true)][string]$PlayerPath,
    [Parameter(Mandatory = $true)][string]$PackageDirectory,
    [Parameter(Mandatory = $true, ParameterSetName = 'Success')][string]$ExpectedResult,
    [Parameter(Mandatory = $true, ParameterSetName = 'Failure')][string]$ExpectedError,
    [string]$EvidenceDirectory = 'Builds/HotUpdateEvidence',
    [int]$Port = 18780,
    [int]$TimeoutSeconds = 120
)

$ErrorActionPreference = 'Stop'
$TaskPlayer = (Resolve-Path -LiteralPath $PlayerPath).Path
$TaskPackage = (Resolve-Path -LiteralPath $PackageDirectory).Path
$TaskPython = (Get-Command python -ErrorAction Stop).Source
$TaskEvidence = [IO.Path]::GetFullPath($EvidenceDirectory)
$TaskRun = Join-Path $TaskEvidence ([Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($TaskRun) | Out-Null
$TaskResult = Join-Path $TaskRun 'result.txt'
$TaskLog = Join-Path $TaskRun 'Player.log'
$TaskServer = $null
$TaskProcess = $null
try {
    $TaskListener = [Net.Sockets.TcpListener]::new([Net.IPAddress]::Loopback, $Port)
    $TaskListener.Start()
    $TaskListener.Stop()
    $TaskServer = Start-Process -FilePath $TaskPython -ArgumentList @(
        '-u', '-m', 'http.server', $Port, '--bind', '127.0.0.1', '--directory', ('"' + $TaskPackage + '"')
    ) -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $TaskRun 'server.stdout.log') -RedirectStandardError (Join-Path $TaskRun 'server.stderr.log')
    $TaskDeadline = [DateTime]::UtcNow.AddSeconds(10)
    do {
        if ($TaskServer.HasExited) { throw "本地资源服务启动失败 请查看 $TaskRun" }
        $TaskClient = [Net.Sockets.TcpClient]::new()
        try {
            $TaskClient.Connect('127.0.0.1', $Port)
        } catch [Net.Sockets.SocketException] {
            if ([DateTime]::UtcNow -ge $TaskDeadline) { throw }
            Start-Sleep -Milliseconds 100
        } finally {
            $TaskConnected = $TaskClient.Connected
            $TaskClient.Dispose()
        }
    } until ($TaskConnected)
    $TaskProcess = Start-Process -FilePath $TaskPlayer -ArgumentList @(
        '-batchmode', '-nographics', '-logFile', ('"' + $TaskLog + '"'),
        ('--hotupdate-url=http://127.0.0.1:' + $Port), ('"--hotupdate-result=' + $TaskResult + '"'), '--hotupdate-quit'
    ) -WindowStyle Hidden -PassThru
    if (!$TaskProcess.WaitForExit($TimeoutSeconds * 1000)) { throw "热更 Player 验证超时 请查看 $TaskLog" }
    if ($PSCmdlet.ParameterSetName -eq 'Failure') {
        if ($TaskProcess.ExitCode -eq 0 -or [IO.File]::Exists($TaskResult)) { throw '错误热更包仍执行了业务入口' }
        if (![IO.File]::ReadAllText($TaskLog).Contains($ExpectedError)) { throw "失败原因不符 预期 $ExpectedError 请查看 $TaskLog" }
        [pscustomobject]@{ passed = $true; expectedError = $ExpectedError; exitCode = $TaskProcess.ExitCode; evidence = $TaskRun } | ConvertTo-Json
    } else {
        if ($TaskProcess.ExitCode -ne 0) { throw "热更 Player 退出码 $($TaskProcess.ExitCode) 请查看 $TaskLog" }
        $TaskActual = [IO.File]::ReadAllText($TaskResult).Trim()
        if ($TaskActual -ne $ExpectedResult) { throw "热更结果不符 预期 $ExpectedResult 实际 $TaskActual" }
        [pscustomobject]@{ passed = $true; result = $TaskActual; evidence = $TaskRun } | ConvertTo-Json
    }
} finally {
    if ($null -ne $TaskProcess -and !$TaskProcess.HasExited) { Stop-Process -Id $TaskProcess.Id }
    if ($null -ne $TaskServer -and !$TaskServer.HasExited) { Stop-Process -Id $TaskServer.Id }
}
