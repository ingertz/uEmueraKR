# エンジンのテストを走らせる。Unityを開かずに Assets/Scripts をRoslynでコンパイルし、
# Tools/EngineTests がそのDLLを反射で呼んで確かめる。リリースのビルド前に回す。
#
#   powershell -ExecutionPolicy Bypass -File Tools/EngineTests/run.ps1
#
# 要る物: .NET SDK(10以上)、Unityが作る Assembly-CSharp.csproj(Unityで一度開けば出来る)。
# 終了コードは 0 なら全部通った、1 なら失敗かコンパイルエラー
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$csprojPath = Join-Path $root 'Assembly-CSharp.csproj'
if (-not (Test-Path $csprojPath)) {
    Write-Host 'Assembly-CSharp.csproj がありません。Unityでプロジェクトを一度開いてください'
    exit 1
}
[xml]$csproj = Get-Content $csprojPath -Encoding UTF8

$work = Join-Path $env:TEMP 'uemuera_enginetests'
New-Item -ItemType Directory -Force $work | Out-Null
$dll = Join-Path $work 'Assembly-CSharp.dll'
$rsp = Join-Path $work 'engine.rsp'

# --- Roslyn の場所: 一番新しいSDKの中にある
$sdkLine = (& dotnet --list-sdks | Select-Object -Last 1)
if (-not $sdkLine) { Write-Host '.NET SDK が見つかりません'; exit 1 }
$m = [regex]::Match($sdkLine, '^(\S+)\s+\[(.+)\]$')
$csc = Join-Path (Join-Path $m.Groups[2].Value $m.Groups[1].Value) 'Roslyn\bincore\csc.dll'
if (-not (Test-Path $csc)) { Write-Host "csc.dll が見つかりません: $csc"; exit 1 }

# --- csproj から参照・定義・ソースを取る
$lines = New-Object System.Collections.Generic.List[string]
$lines.Add('-nostdlib'); $lines.Add('-noconfig'); $lines.Add('-t:library'); $lines.Add('-nologo')
$lang = ($csproj.Project.PropertyGroup | ForEach-Object { $_.LangVersion } | Where-Object { $_ } | Select-Object -First 1)
if (-not $lang) { $lang = '9.0' }
$lines.Add("-langversion:$lang")
$lines.Add('-nowarn:CS0169,CS0649,CS0414,CS0219,CS0168,CS0162,CS0618,CS1701,CS1702')
$lines.Add("-out:`"$dll`"")
$defines = ($csproj.Project.PropertyGroup | ForEach-Object { $_.DefineConstants } | Where-Object { $_ } | Select-Object -First 1)
if ($defines) { $lines.Add("-define:$defines") }

$unityDir = $null
$refs = @{}
foreach ($hp in $csproj.SelectNodes('//Reference/HintPath')) {
    $p = $hp.InnerText
    if (-not [System.IO.Path]::IsPathRooted($p)) { $p = Join-Path $root $p }
    if ($refs.ContainsKey($p)) { continue }
    $refs[$p] = $true
    $lines.Add("-r:`"$p`"")
    if (-not $unityDir -and $p -match '\\Managed\\UnityEngine\\UnityEngine\.dll$') { $unityDir = Split-Path $p }
}
# WebPはcsprojに載らないことがある
$webp = Join-Path $root 'Library\ScriptAssemblies\WebP.dll'
if ((Test-Path $webp) -and -not $refs.ContainsKey($webp)) { $lines.Add("-r:`"$webp`"") }

$sources = @{}
foreach ($c in $csproj.SelectNodes('//Compile')) {
    $p = Join-Path $root $c.GetAttribute('Include')
    $sources[(Resolve-Path $p).Path] = $true
}
# Unityがまだcsprojへ載せていない新しいファイルも入れる(Editorフォルダは別のアセンブリ)
Get-ChildItem (Join-Path $root 'Assets\Scripts') -Recurse -Filter *.cs | Where-Object {
    $_.FullName -notmatch '\\Editor\\'
} | ForEach-Object { $sources[$_.FullName] = $true }
foreach ($s in $sources.Keys) { $lines.Add("`"$s`"") }

[System.IO.File]::WriteAllLines($rsp, $lines)

Write-Host "compiling $($sources.Count) files..."
$out = & dotnet $csc "@$rsp" 2>&1
if ($LASTEXITCODE -ne 0) {
    $out | Where-Object { $_ -match 'error' } | ForEach-Object { Write-Host $_ }
    Write-Host 'COMPILE FAILED'
    exit 1
}
if (-not $unityDir) { Write-Host 'UnityEngine.dll の場所が分かりません'; exit 1 }

& dotnet run --project (Join-Path $PSScriptRoot 'EngineTests.csproj') -c Release -- $dll $unityDir
exit $LASTEXITCODE
