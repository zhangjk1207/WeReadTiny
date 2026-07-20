param([switch]$SkipTests)

$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath($PSScriptRoot)
$buildRoot = Join-Path $projectRoot '.build'
$stagingDirectory = Join-Path $buildRoot 'publish-staging'
$publishDirectory = Join-Path $projectRoot 'publish'
$zipStagingPath = Join-Path $buildRoot 'WeReadStealth.zip'
$zipOutputPath = Join-Path $publishDirectory 'WeReadStealth.zip'
$solutionPath = Join-Path $projectRoot 'WeReadStealth.sln'
$applicationProject = Join-Path $projectRoot 'src\WeReadStealth\WeReadStealth.csproj'

function Assert-SafeBuildPath {
    param([Parameter(Mandatory = $true)][string]$Path)
    $resolved = [IO.Path]::GetFullPath($Path)
    $rootWithSeparator = $projectRoot.TrimEnd('\') + '\'
    if (-not $resolved.StartsWith($rootWithSeparator, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to modify a path outside the project: $resolved"
    }
    if ((Split-Path $resolved -Leaf) -notin @('.build', 'publish', 'publish-staging')) {
        throw "Refusing to reset unexpected directory: $resolved"
    }
}

function Reset-BuildDirectory {
    param([Parameter(Mandatory = $true)][string]$Path)
    Assert-SafeBuildPath -Path $Path
    if (Test-Path -LiteralPath $Path) {
        Remove-Item -LiteralPath $Path -Recurse -Force
    }
    New-Item -ItemType Directory -Path $Path | Out-Null
}

function Write-PngIconContainer {
    param(
        [Parameter(Mandatory = $true)][string]$PngPath,
        [Parameter(Mandatory = $true)][string]$IconPath
    )

    $pngBytes = [IO.File]::ReadAllBytes($PngPath)
    $stream = [IO.File]::Open($IconPath, [IO.FileMode]::Create, [IO.FileAccess]::Write, [IO.FileShare]::None)
    try {
        $writer = New-Object IO.BinaryWriter($stream)
        $writer.Write([UInt16]0)
        $writer.Write([UInt16]1)
        $writer.Write([UInt16]1)
        $writer.Write([Byte]0)
        $writer.Write([Byte]0)
        $writer.Write([Byte]0)
        $writer.Write([Byte]0)
        $writer.Write([UInt16]1)
        $writer.Write([UInt16]32)
        $writer.Write([UInt32]$pngBytes.Length)
        $writer.Write([UInt32]22)
        $writer.Write($pngBytes)
        $writer.Flush()
    }
    finally {
        $stream.Dispose()
    }
}

$iconPng = Join-Path $projectRoot 'src\WeReadStealth\Assets\icon.png'
$iconIco = Join-Path $projectRoot 'src\WeReadStealth\Assets\icon.ico'
if (-not (Test-Path -LiteralPath $iconIco) -or (Get-Item $iconPng).LastWriteTimeUtc -gt (Get-Item $iconIco).LastWriteTimeUtc) {
    Write-PngIconContainer -PngPath $iconPng -IconPath $iconIco
}

Write-Host '[1/4] Restoring packages...'
dotnet restore $solutionPath --locked-mode
if ($LASTEXITCODE -ne 0) { throw 'Package restore failed.' }

if (-not $SkipTests) {
    Write-Host '[2/4] Running tests...'
    dotnet run --project (Join-Path $projectRoot 'tests\WeReadStealth.Tests\WeReadStealth.Tests.csproj') -c Release
    if ($LASTEXITCODE -ne 0) { throw 'Tests failed.' }
}
else {
    Write-Host '[2/4] Tests skipped by request.'
}

$publishedExecutable = Join-Path $publishDirectory 'WeReadStealth.exe'
if (Test-Path -LiteralPath $publishedExecutable) {
    $publishedExecutablePath = [IO.Path]::GetFullPath($publishedExecutable)
    $runningInstance = Get-Process -Name 'WeReadStealth' -ErrorAction SilentlyContinue |
        Where-Object { $_.Path -and [IO.Path]::GetFullPath($_.Path) -eq $publishedExecutablePath } |
        Select-Object -First 1
    if ($runningInstance) {
        throw "WeReadStealth is running from the publish directory (PID $($runningInstance.Id)). Exit it from the tray and rebuild."
    }
}

Reset-BuildDirectory -Path $buildRoot
New-Item -ItemType Directory -Path $stagingDirectory | Out-Null
Reset-BuildDirectory -Path $publishDirectory

Write-Host '[3/4] Publishing x64 application...'
dotnet publish $applicationProject -c Release -r win-x64 --self-contained false --no-restore -o $stagingDirectory
if ($LASTEXITCODE -ne 0) { throw 'Publish failed.' }

$requiredFiles = @(
    'WeReadStealth.exe',
    'WeReadStealth.exe.config',
    'Microsoft.Web.WebView2.Core.dll',
    'Microsoft.Web.WebView2.WinForms.dll'
)
foreach ($fileName in $requiredFiles) {
    $sourcePath = Join-Path $stagingDirectory $fileName
    if (-not (Test-Path -LiteralPath $sourcePath)) {
        throw "Required publish file is missing: $fileName"
    }
    Copy-Item -LiteralPath $sourcePath -Destination $publishDirectory
}

$loader = Get-ChildItem -Path $stagingDirectory -Recurse -File -Filter 'WebView2Loader.dll' |
    Where-Object { $_.FullName -match 'win-x64|\\x64\\|publish-staging\\WebView2Loader.dll$' } |
    Select-Object -First 1
if (-not $loader) {
    throw 'The x64 WebView2Loader.dll was not found in publish output.'
}
Copy-Item -LiteralPath $loader.FullName -Destination (Join-Path $publishDirectory 'WebView2Loader.dll')

$assetOutput = Join-Path $publishDirectory 'Assets'
New-Item -ItemType Directory -Path $assetOutput | Out-Null
Copy-Item -LiteralPath $iconPng -Destination $assetOutput

Write-Host '[4/4] Creating portable zip...'
if (Test-Path -LiteralPath $zipStagingPath) {
    Remove-Item -LiteralPath $zipStagingPath -Force
}
$filesToArchive = Get-ChildItem -LiteralPath $publishDirectory
Compress-Archive -Path $filesToArchive.FullName -DestinationPath $zipStagingPath -CompressionLevel Optimal
Copy-Item -LiteralPath $zipStagingPath -Destination $zipOutputPath

$publishedFiles = Get-ChildItem -LiteralPath $publishDirectory -File -Recurse
$folderBytes = ($publishedFiles | Where-Object { $_.FullName -ne $zipOutputPath } | Measure-Object Length -Sum).Sum
$exeBytes = (Get-Item (Join-Path $publishDirectory 'WeReadStealth.exe')).Length
$zipBytes = (Get-Item $zipOutputPath).Length

Write-Host ''
Write-Host 'Build completed.'
Write-Host ("EXE:    {0:N2} MB" -f ($exeBytes / 1MB))
Write-Host ("Folder: {0:N2} MB" -f ($folderBytes / 1MB))
Write-Host ("ZIP:    {0:N2} MB" -f ($zipBytes / 1MB))
Write-Host "Output: $publishDirectory"
