# Сборка NetLimiter Parser (C# WinForms, .NET Framework 4.8)
# Запуск: powershell -ExecutionPolicy Bypass -File .\build.ps1
param(
    [string]$Output = "",
    [switch]$NoIcon
)

$ErrorActionPreference = "Stop"
$root = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($root)) { $root = (Get-Location).Path }

$srcDir = Join-Path $root "src"
if (-not (Test-Path $srcDir)) {
    Write-Host "Не найдена папка src: $srcDir" -ForegroundColor Red
    exit 1
}

$csc = Join-Path $env:WINDIR "Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if (-not (Test-Path $csc)) {
    Write-Host "Не найден компилятор .NET Framework 4.x: $csc" -ForegroundColor Red
    Write-Host "Установите .NET Framework 4.8 или запустите сборку на Windows 10/11." -ForegroundColor Yellow
    exit 1
}

if ([string]::IsNullOrWhiteSpace($Output)) {
    $Output = Join-Path $root "NetLimiterParser.exe"
}
$outDir = Split-Path -Parent $Output
if ($outDir -and -not (Test-Path $outDir)) {
    New-Item -ItemType Directory -Path $outDir | Out-Null
}

$sources = Get-ChildItem -Path $srcDir -Filter "*.cs" | Sort-Object Name | ForEach-Object { $_.FullName }
if ($sources.Count -eq 0) {
    Write-Host "В папке src нет исходных файлов." -ForegroundColor Red
    exit 1
}

$references = @(
    "System.dll",
    "System.Core.dll",
    "System.Drawing.dll",
    "System.Web.Extensions.dll",
    "System.Windows.Forms.dll"
)

$arguments = @(
    "/nologo",
    "/target:winexe",
    "/platform:x64",
    "/optimize+",
    "/codepage:65001",
    "/out:$Output"
)

foreach ($reference in $references) {
    $arguments += "/r:$reference"
}

$manifest = Join-Path $root "app.manifest"
if (Test-Path $manifest) {
    $arguments += "/win32manifest:$manifest"
}

$icon = Join-Path $root "app.ico"
if (-not $NoIcon -and (Test-Path $icon)) {
    $arguments += "/win32icon:$icon"
}

$arguments += $sources

Write-Host "Компиляция NetLimiter Parser..." -ForegroundColor Cyan
Write-Host "  csc: $csc"
Write-Host "  out: $Output"
Write-Host "  src: $($sources.Count) файлов"

$outputText = & $csc $arguments 2>&1
$exitCode = $LASTEXITCODE

if ($outputText) {
    foreach ($line in $outputText) { Write-Host $line }
}

if ($exitCode -ne 0) {
    Write-Host "Сборка завершилась с ошибкой (код $exitCode)." -ForegroundColor Red
    exit $exitCode
}

$file = Get-Item $Output
Write-Host ("Готово: {0} ({1:N0} байт)" -f $file.FullName, $file.Length) -ForegroundColor Green
exit 0
