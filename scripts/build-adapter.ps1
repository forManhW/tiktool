# Build live_adapter.py thành tiktok-adapter.exe (không cần Python trên máy người dùng).
# Cần một Python có TikTokLive + PyInstaller. Mặc định dùng ..\tiktoklive-demo\.venv, đổi bằng $env:TIKTOOL_PYTHON.
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$python = if ($env:TIKTOOL_PYTHON) { $env:TIKTOOL_PYTHON } else { Join-Path $root "..\tiktoklive-demo\.venv\Scripts\python.exe" }
if (-not (Test-Path $python)) { throw "Không thấy Python: $python. Đặt `$env:TIKTOOL_PYTHON trỏ tới python.exe có TikTokLive và PyInstaller." }
$work = Join-Path $root "vendor\.adapter-build"
& $python -m PyInstaller --noconfirm --clean --onedir --console --name tiktok-adapter `
  --collect-all TikTokLive --collect-all TikTokLiveProto --collect-all EulerApiSdk `
  --distpath (Join-Path $root "vendor") --workpath $work --specpath $work (Join-Path $root "live_adapter.py")
if ($LASTEXITCODE -ne 0) { throw "PyInstaller lỗi ($LASTEXITCODE)" }
Write-Host "OK: vendor\tiktok-adapter\tiktok-adapter.exe"
