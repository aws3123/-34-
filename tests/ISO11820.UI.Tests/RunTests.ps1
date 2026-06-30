# ═══════════════════════════════════════════════════════════════════
#  ISO 11820 UI 自动化验收测试 — 运行脚本
# ═══════════════════════════════════════════════════════════════════
#
#  用法:
#    .\RunTests.ps1                     # 运行全部测试
#    .\RunTests.ps1 -Filter "TC01"      # 运行登录测试
#    .\RunTests.ps1 -Filter "TC10"      # 运行端到端流程
#    .\RunTests.ps1 -ListTests          # 列出所有测试
#
# ═══════════════════════════════════════════════════════════════════

param(
    [string]$Filter = "",
    [switch]$ListTests,
    [string]$Configuration = "Debug",
    [string]$ResultsDir = ".\TestResults"
)

$ErrorActionPreference = "Stop"

# 路径配置
$SolutionDir = Split-Path -Parent $PSScriptRoot
$TestProject = Join-Path $PSScriptRoot "ISO11820.UI.Tests.csproj"
$AppProject = Join-Path $SolutionDir "src\ISO11820.App\ISO11820.App.csproj"

Write-Host ""
Write-Host "═══════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  ISO 11820 UI 自动化验收测试" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# Step 1: 编译主程序
Write-Host "📦 [1/4] 编译主程序..." -ForegroundColor Yellow
dotnet build $AppProject -c $Configuration --verbosity quiet
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ 主程序编译失败！" -ForegroundColor Red
    exit 1
}
Write-Host "✅ 主程序编译成功" -ForegroundColor Green

# Step 2: 编译测试项目
Write-Host "📦 [2/4] 编译测试项目..." -ForegroundColor Yellow
dotnet build $TestProject -c $Configuration --verbosity quiet
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ 测试项目编译失败！" -ForegroundColor Red
    exit 1
}
Write-Host "✅ 测试项目编译成功" -ForegroundColor Green

# Step 3: 列出测试（可选）
if ($ListTests) {
    Write-Host ""
    Write-Host "📋 可用测试列表:" -ForegroundColor Cyan
    dotnet test $TestProject --no-build --list-tests
    exit 0
}

# Step 4: 运行测试
Write-Host "🚀 [3/4] 运行 UI 自动化测试..." -ForegroundColor Yellow
Write-Host ""

# 确保截图目录存在
$ScreenshotDir = Join-Path $PSScriptRoot "Screenshots"
New-Item -ItemType Directory -Force -Path $ScreenshotDir | Out-Null

# 构建 dotnet test 命令
$testArgs = @(
    "--no-build",
    "-c", $Configuration,
    "--logger", "console;verbosity=detailed",
    "--results-directory", $ResultsDir
)

if ($Filter -ne "") {
    $testArgs += @("--filter", "FullyQualifiedName~$Filter")
    Write-Host "  🔍 过滤: $Filter" -ForegroundColor Magenta
}

Write-Host "  📸 截图输出: $ScreenshotDir" -ForegroundColor Magenta
Write-Host ""

dotnet test $TestProject @testArgs
$testExitCode = $LASTEXITCODE

# Step 5: 汇总
Write-Host ""
Write-Host "═══════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  测试结果汇总" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════" -ForegroundColor Cyan

if ($testExitCode -eq 0) {
    Write-Host "✅ 全部测试通过！" -ForegroundColor Green
} else {
    Write-Host "⚠️ 部分测试未通过，请检查输出" -ForegroundColor Yellow
}

# 列出截图
$screenshots = Get-ChildItem -Path $ScreenshotDir -Recurse -Filter "*.png" -ErrorAction SilentlyContinue
if ($screenshots.Count -gt 0) {
    Write-Host ""
    Write-Host "📸 已生成 $($screenshots.Count) 张截图:" -ForegroundColor Cyan
    Write-Host "  路径: $ScreenshotDir" -ForegroundColor Magenta
    Write-Host ""
    $screenshots | ForEach-Object {
        Write-Host "  $($_.Directory.Name)\$($_.Name)" -ForegroundColor Gray
    }
}

Write-Host ""
exit $testExitCode
