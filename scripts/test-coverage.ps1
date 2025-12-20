# Test Coverage Script for MinimalEndpoints
# Generates code coverage reports with detailed analysis

param(
    [string]$Configuration = "Debug",
    [switch]$OpenReport,
    [switch]$FailOnLowCoverage
)

Write-Host "Running MinimalEndpoints Test Coverage Analysis..." -ForegroundColor Cyan
Write-Host ""

# Clean previous results
Write-Host "Cleaning previous test results..." -ForegroundColor Yellow
Remove-Item -Path "TestResults" -Recurse -Force -ErrorAction SilentlyContinue

# Run tests with coverage
Write-Host "Running tests with code coverage..." -ForegroundColor Green
$testResult = dotnet test `
    --configuration $Configuration `
    --collect:"XPlat Code Coverage" `
    --results-directory TestResults `
    --verbosity minimal `
    /p:CollectCoverage=true `
    /p:CoverletOutputFormat=cobertura `
    /p:Exclude="[xunit.*]*"

if ($LASTEXITCODE -ne 0) {
    Write-Host "Tests failed!" -ForegroundColor Red
    exit $LASTEXITCODE
}

Write-Host "Tests passed!" -ForegroundColor Green
Write-Host ""

# Find coverage file
$coverageFile = Get-ChildItem -Path "TestResults" -Filter "coverage.cobertura.xml" -Recurse | Select-Object -First 1

if (-not $coverageFile) {
    Write-Host "Coverage file not found!" -ForegroundColor Yellow
    exit 1
}

Write-Host "Generating coverage report..." -ForegroundColor Cyan

# Install ReportGenerator if not present
$reportGeneratorPath = dotnet tool list --global | Select-String "reportgenerator"
if (-not $reportGeneratorPath) {
    Write-Host "Installing ReportGenerator..." -ForegroundColor Yellow
    dotnet tool install --global dotnet-reportgenerator-globaltool
}

# Generate HTML report
reportgenerator `
    "-reports:$($coverageFile.FullName)" `
    "-targetdir:TestResults/CoverageReport" `
    "-reporttypes:Html;Badges;TextSummary;Cobertura" `
    "-classfilters:-xunit*"

Write-Host ""
Write-Host "Coverage Summary:" -ForegroundColor Cyan
Write-Host ""

# Read and display summary
$summaryFile = "TestResults/CoverageReport/Summary.txt"
if (Test-Path $summaryFile) {
    Get-Content $summaryFile | Write-Host
}

# Parse coverage percentage
$coberturaXml = [xml](Get-Content $coverageFile.FullName)
$lineCoverage = [math]::Round($coberturaXml.coverage.'line-rate' * 100, 2)
$branchCoverage = [math]::Round($coberturaXml.coverage.'branch-rate' * 100, 2)

Write-Host ""
Write-Host "Coverage Metrics:" -ForegroundColor Cyan

# Determine color for line coverage
$lineCoverageColor = "Yellow"
if ($lineCoverage -ge 80) {
    $lineCoverageColor = "Green"
}
Write-Host "   Line Coverage:   $lineCoverage%" -ForegroundColor $lineCoverageColor

# Determine color for branch coverage
$branchCoverageColor = "Yellow"
if ($branchCoverage -ge 75) {
    $branchCoverageColor = "Green"
}
Write-Host "   Branch Coverage: $branchCoverage%" -ForegroundColor $branchCoverageColor

# Check thresholds
$threshold = 80
if ($FailOnLowCoverage -and $lineCoverage -lt $threshold) {
    Write-Host ""
    Write-Host "Coverage below threshold ($threshold%)!" -ForegroundColor Red
    exit 1
}

# Open report if requested
if ($OpenReport) {
    Write-Host ""
    Write-Host "Opening coverage report..." -ForegroundColor Green
    Start-Process "TestResults/CoverageReport/index.html"
}

Write-Host ""
Write-Host "Coverage analysis complete!" -ForegroundColor Green
Write-Host "Report: TestResults/CoverageReport/index.html" -ForegroundColor Cyan
Write-Host ""
Write-Host "Tip: Run with -OpenReport to view the HTML report" -ForegroundColor Gray
Write-Host "Tip: Run with -FailOnLowCoverage to enforce coverage thresholds" -ForegroundColor Gray
