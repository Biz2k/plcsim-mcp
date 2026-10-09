param(
    [string]$Calls = '',
    [string]$Exe = (Join-Path $PSScriptRoot '..\src\PlcSimMcpServer\bin\Debug\net48\PlcSimMcpServer.exe')
)

$mcp = Join-Path $PSScriptRoot 'mcp-call.ps1'
if (-not $Calls) { $Calls = Join-Path $PSScriptRoot 'smoke\smoke.json' }

function Invoke-Mcp([string]$callsPath, [int]$max) {
    $p = @{ Calls = $callsPath; Exe = $Exe; Max = $max }
    @(& $mcp @p 2>&1 | ForEach-Object { "$_" })
}

function ConvertTo-Answers($lines) {
    $answers = @()
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -match '^\[(\d+) (\S+)\] (.*)$') {
            $text = ''
            if ($i + 1 -lt $lines.Count -and $lines[$i + 1] -like '   *') { $text = $lines[$i + 1].Substring(3) }
            $answers += [pscustomobject]@{ N = [int]$Matches[1]; Tool = $Matches[2]; Verdict = $Matches[3]; Text = $text }
        }
    }
    $answers
}

function Test-Good($verdict) { $verdict -eq 'isError=False' -or $verdict -eq 'isError=True (expected)' }

Write-Host "--- Running PLCSIM MCP Smoke Test ---"
$parsed = Get-Content $Calls -Raw -Encoding UTF8 | ConvertFrom-Json
$answers = ConvertTo-Answers (Invoke-Mcp $Calls 8000)

$bad = @()
$answered = 0
foreach ($a in $answers) {
    $answered++
    if (-not (Test-Good $a.Verdict)) { $bad += $a }
}

$total = $parsed.Count
if ($answered -lt $total) { Write-Host "Stopped early: $answered of $total calls answered" -ForegroundColor Yellow }

foreach ($a in $bad) { 
    Write-Host "[$($a.N) $($a.Tool)] $($a.Verdict)" -ForegroundColor Red
    Write-Host "   $($a.Text)" -ForegroundColor Red
}

Write-Host "Smoke test: $answered of $total calls answered, $($bad.Count) with an error."
if ($bad.Count -gt 0 -or $answered -lt $total) { exit 1 }

Write-Host "SUCCESS: PLCSIM MCP Smoke Test passed." -ForegroundColor Green
exit 0
