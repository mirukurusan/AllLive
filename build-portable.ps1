# ============================================================
#  build-portable.ps1 — 一键构建 AllLive 便携版（非打包 + 自包含）
#
#  产物为单个文件夹，拷贝到任何 Win10/11 机器双击 exe 即用，
#  无需安装 MSIX、无需预装 Windows App SDK 运行时、无需 .NET。
#  压缩包命名带版本号: AllLive-portable-<版本>-<架构>.zip（版本读取自 Package.appxmanifest）
#
#  用法（在仓库根目录运行）:
#    .\build-portable.ps1                 # 构建 x64 并打 zip
#    .\build-portable.ps1 -Arch all       # 构建 x64 + x86 + ARM64
#    .\build-portable.ps1 -Arch x86       # 只构建 x86
#    .\build-portable.ps1 -SkipZip        # 只发布，不压缩
# ============================================================

param(
    [ValidateSet("x64", "x86", "ARM64", "all")]
    [string]$Arch = "x64",
    [switch]$SkipZip
)

$ErrorActionPreference = "Stop"
Set-Location $PSScriptRoot

$Project = "AllLive.WinUI\AllLive.WinUI.csproj"
$OutRoot = "dist\portable"
$archs   = if ($Arch -eq "all") { @("x64", "x86", "ARM64") } else { @($Arch) }

# 从 Package.appxmanifest 读取版本号，用于压缩包命名
[xml]$manifest = Get-Content "AllLive.WinUI\Package.appxmanifest"
$version = $manifest.Package.Identity.Version
Write-Host "应用版本: $version" -ForegroundColor Cyan

foreach ($a in $archs) {
    $rid    = "win-" + $a.ToLower()
    $outDir = Join-Path $OutRoot $a

    Write-Host ""
    Write-Host "=== 构建便携版: $a ($rid) ===" -ForegroundColor Cyan
    if (Test-Path $outDir) { Remove-Item $outDir -Recurse -Force }

    dotnet publish $Project -c Release -p:Platform=$a -r $rid --self-contained `
        -p:WindowsPackageType=None -o $outDir
    if ($LASTEXITCODE -ne 0) { throw "发布失败: $a ($rid)" }
}

if ($SkipZip) {
    Write-Host "已跳过压缩。输出目录: $OutRoot" -ForegroundColor Yellow
}
else {
    foreach ($a in $archs) {
        $srcDir  = Join-Path $OutRoot $a
        $zipPath = Join-Path $OutRoot "AllLive-portable-$version-$a.zip"
        Write-Host "=== 压缩: $zipPath ===" -ForegroundColor Cyan
        if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
        Compress-Archive -Path (Join-Path $srcDir "*") -DestinationPath $zipPath -Force
    }
}

Write-Host ""
Write-Host "完成！便携版位于 $OutRoot" -ForegroundColor Green
