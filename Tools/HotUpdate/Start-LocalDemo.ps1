[CmdletBinding()]
param(
    [string]$PlayerPath,
    [string]$PackageDirectory,
    [int]$Port = 18780,
    [switch]$ServerOnly
)

$ErrorActionPreference = 'Stop'
if (!$PlayerPath) { $PlayerPath = Join-Path $PSScriptRoot 'MmHotUpdateDemo.exe' }
if (!$PackageDirectory) { $PackageDirectory = Join-Path $PSScriptRoot '../HotUpdateServer/StandaloneWindows64/DefaultPackage' }
$TaskPackage = (Resolve-Path -LiteralPath $PackageDirectory).Path
$TaskPython = (Get-Command python -ErrorAction Stop).Source
$TaskLogs = Join-Path $PSScriptRoot 'Logs'
[IO.Directory]::CreateDirectory($TaskLogs) | Out-Null
$TaskSession = [Guid]::NewGuid().ToString('N')
$TaskServer = $null
$TaskPlayer = $null
try {
    $TaskListener = [Net.Sockets.TcpListener]::new([Net.IPAddress]::Loopback, $Port)
    $TaskListener.Start()
    $TaskListener.Stop()
    $TaskServer = Start-Process -FilePath $TaskPython -ArgumentList @(
        '-u', '-m', 'http.server', $Port, '--bind', '127.0.0.1', '--directory', ('"' + $TaskPackage + '"')
    ) -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $TaskLogs ($TaskSession + '.http.stdout.log')) -RedirectStandardError (Join-Path $TaskLogs ($TaskSession + '.http.stderr.log'))
    $TaskDeadline = [DateTime]::UtcNow.AddSeconds(10)
    do {
        if ($TaskServer.HasExited) { throw '本机资源服务启动失败 请查看 Logs' }
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
    Write-Host "本机资源服务 http://127.0.0.1:$Port"
    Write-Host "发布目录 $TaskPackage"
    if ($ServerOnly) {
        Write-Host '保持此窗口运行 双击 EXE 可随时重新进入 Ctrl+C 结束服务'
        $TaskServer.WaitForExit()
        throw "本机服务已退出 $($TaskServer.ExitCode)"
    }
    $TaskPlayerPath = (Resolve-Path -LiteralPath $PlayerPath).Path
    $TaskPlayer = Start-Process -FilePath $TaskPlayerPath -ArgumentList @(
        '-screen-fullscreen', '0', '-screen-width', '1000', '-screen-height', '650',
        ('--hotupdate-url=http://127.0.0.1:' + $Port), '-logFile', ('"' + (Join-Path $TaskLogs ($TaskSession + '.Player.log')) + '"')
    ) -PassThru
    $TaskPlayer.WaitForExit()
    if ($TaskPlayer.ExitCode -ne 0) { throw "Player 退出码 $($TaskPlayer.ExitCode) 请查看 Logs" }
} finally {
    if ($null -ne $TaskServer -and !$TaskServer.HasExited) { Stop-Process -Id $TaskServer.Id }
}
