$ErrorActionPreference = 'Continue'
$root   = Split-Path -Parent $PSScriptRoot
$user   = Join-Path $env:APPDATA 'Godot\app_userdata\泡沫年代 - Bubble Years'
$stamp  = Get-Date -Format 'yyyyMMdd-HHmmss'
$tmp    = Join-Path $env:TEMP ('by-diag-' + $stamp)
$out    = Join-Path $root ('诊断包-' + $stamp + '.zip')

New-Item -ItemType Directory -Force -Path $tmp | Out-Null

function CopyIfExists($src, $dst) {
    if (Test-Path -LiteralPath $src) { Copy-Item -LiteralPath $src -Destination $dst -Recurse -Force -ErrorAction SilentlyContinue }
}

CopyIfExists (Join-Path $user 'logs')         (Join-Path $tmp 'logs')
CopyIfExists (Join-Path $user 'saves')        (Join-Path $tmp 'saves')
CopyIfExists (Join-Path $user 'settings.json') $tmp

# 存档自检报告
$doctor = Join-Path $root '工具\SaveDoctor\SaveDoctor.exe'
if (Test-Path -LiteralPath $doctor) {
    $env:DOTNET_ROLL_FORWARD = 'LatestMajor'
    & $doctor 2>&1 | Out-File -FilePath (Join-Path $tmp 'savedoctor.txt') -Encoding utf8
} else {
    '未找到 SaveDoctor.exe' | Out-File -FilePath (Join-Path $tmp 'savedoctor.txt') -Encoding utf8
}

# 环境信息
$info = Join-Path $tmp 'system.txt'
"时间      : $(Get-Date -Format o)"                       | Out-File -FilePath $info -Encoding utf8
"系统      : $([Environment]::OSVersion.VersionString)"    | Out-File -FilePath $info -Append -Encoding utf8
"64位进程  : $([Environment]::Is64BitProcess)"             | Out-File -FilePath $info -Append -Encoding utf8
"CPU       : $((Get-CimInstance Win32_Processor | Select-Object -First 1).Name)" | Out-File -FilePath $info -Append -Encoding utf8
"显卡      : $((Get-CimInstance Win32_VideoController | Select-Object -First 1).Name)" | Out-File -FilePath $info -Append -Encoding utf8
"用户目录  : $user"                                        | Out-File -FilePath $info -Append -Encoding utf8
""                                                         | Out-File -FilePath $info -Append -Encoding utf8
"--- dotnet --list-runtimes ---"                           | Out-File -FilePath $info -Append -Encoding utf8
(& dotnet --list-runtimes 2>&1)                            | Out-File -FilePath $info -Append -Encoding utf8
""                                                         | Out-File -FilePath $info -Append -Encoding utf8
"--- 游戏目录文件 ---"                                     | Out-File -FilePath $info -Append -Encoding utf8
(Get-ChildItem -LiteralPath (Join-Path $root 'game') -Force | Select-Object Name, Length) | Format-Table -AutoSize | Out-File -FilePath $info -Append -Encoding utf8

if (Test-Path -LiteralPath $out) { Remove-Item -LiteralPath $out -Force }
Compress-Archive -Path (Join-Path $tmp '*') -DestinationPath $out -Force
Remove-Item -LiteralPath $tmp -Recurse -Force -ErrorAction SilentlyContinue

Write-Host ''
Write-Host '已生成诊断包：' -ForegroundColor Green
Write-Host "  $out"
Write-Host '把它发给我即可（内含 logs / saves / 存档自检报告 / 系统信息）。'
