$ErrorActionPreference = 'Stop'

$installRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$serverExe = Join-Path $installRoot 'Server\UniRemoteExam.exe'
$clientExe = Join-Path $installRoot 'Client\UniRemoteExam.Client.exe'
$healthUrl = 'http://127.0.0.1:5113/health'

function Test-UniRemoteServer {
    try {
        $response = Invoke-WebRequest -UseBasicParsing -Uri $healthUrl -TimeoutSec 2
        return $response.StatusCode -eq 200
    }
    catch {
        return $false
    }
}

if (-not (Test-UniRemoteServer)) {
    $env:UNIREMOTE_LAN_MODE = 'true'
    Start-Process -FilePath $serverExe -WorkingDirectory (Split-Path -Parent $serverExe) -WindowStyle Hidden

    $ready = $false
    for ($attempt = 0; $attempt -lt 30; $attempt++) {
        Start-Sleep -Milliseconds 500
        if (Test-UniRemoteServer) {
            $ready = $true
            break
        }
    }

    if (-not $ready) {
        Add-Type -AssemblyName PresentationFramework
        [System.Windows.MessageBox]::Show(
            'تعذر تشغيل خادم نظام الاختبارات. أعد تشغيل الكمبيوتر ثم افتح النظام مرة أخرى.',
            'نظام الاختبارات الإلكترونية',
            'OK',
            'Error') | Out-Null
        exit 1
    }
}

Start-Process -FilePath $clientExe -WorkingDirectory (Split-Path -Parent $clientExe)

$lanAddress = Get-NetIPConfiguration |
    Where-Object { $_.IPv4DefaultGateway -and $_.NetAdapter.Status -eq 'Up' } |
    ForEach-Object { $_.IPv4Address.IPAddress } |
    Where-Object { $_ -match '^(10\.|192\.168\.|172\.(1[6-9]|2[0-9]|3[0-1])\.)' } |
    Select-Object -First 1

if ($lanAddress) {
    $phoneUrl = "http://${lanAddress}:5113"
    try { Set-Clipboard -Value $phoneUrl } catch { }
    Add-Type -AssemblyName PresentationFramework
    [System.Windows.MessageBox]::Show(
        "رابط الجوال هو:`n`n$phoneUrl`n`nتم نسخه. يجب أن يكون الجوال والكمبيوتر على شبكة Wi-Fi نفسها.",
        'ربط الجوال بالكمبيوتر',
        'OK',
        'Information') | Out-Null
}
