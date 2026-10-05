<#
.SYNOPSIS
    Compares the old API (Conetix) with the new one (SmartASP) field by field: History for every period,
    Current, Stations and Chart. Read-only. Run before switching DNS; everything must match.

.EXAMPLE
    .\compare-apis.ps1 -NewUrl "https://yoursite.smartasp.net"
#>
param(
    [string]$OldUrl = "https://api.homeweatherhub.com",
    [Parameter(Mandatory = $true)][string]$NewUrl,
    [int[]]$Stations = @(1, 2)
)

$ErrorActionPreference = "Stop"
$OldUrl = $OldUrl.TrimEnd('/'); $NewUrl = $NewUrl.TrimEnd('/')

$historyFields = "outsideTemperatureMin", "outsideTemperatureMax", "insideTemperatureMin", "insideTemperatureMax",
    "outsideHumidityMin", "outsideHumidityMax", "insideHumidityMin", "insideHumidityMax", "pressureMin", "pressureMax",
    "windSpeedMax", "windGustMax", "windDirectionAngleAvg", "windDirectionAvg", "rainRateMax", "totalRain", "uvIndexMax",
    "startDate", "endDate", "success", "wsName", "measurementSymbol"

# Periods: today and yesterday are skipped for Day/Week on purpose (new readings arrive between the two calls).
$today = (Get-Date).Date
$cases = @(
    @{ p = 1; d = $today.AddDays(-2) }, @{ p = 1; d = $today.AddDays(-30) }, @{ p = 1; d = [DateTime]"2025-03-08" }, @{ p = 1; d = [DateTime]"2024-01-15" },
    @{ p = 2; d = $today.AddDays(-14 - (([int]$today.DayOfWeek + 6) % 7)) }, @{ p = 2; d = [DateTime]"2025-03-24" },
    @{ p = 3; d = $today.AddMonths(-1).AddDays(1 - $today.Day) }, @{ p = 3; d = [DateTime]"2025-03-01" }, @{ p = 3; d = [DateTime]"2024-01-01" },
    @{ p = 4; d = [DateTime]"2025-01-01" }, @{ p = 4; d = [DateTime]"2022-01-01" }
)

$diffs = 0; $n = 0
foreach ($id in $Stations) {
    foreach ($ms in 1, 0) {
        foreach ($c in $cases) {
            $u = "History/$id/$($c.p)/$($c.d.ToString('yyyy-MM-dd'))/$ms/"
            $a = Invoke-RestMethod -TimeoutSec 120 "$OldUrl/$u"
            $b = Invoke-RestMethod -TimeoutSec 120 "$NewUrl/$u"
            $n++
            foreach ($f in $historyFields) {
                if ("$($a.$f)" -ne "$($b.$f)") { $diffs++; Write-Host "DIFF $u $f old='$($a.$f)' new='$($b.$f)'" -ForegroundColor Red }
            }
        }
    }

    # Current: same latest reading (allow for one new reading arriving between the two calls).
    $a = Invoke-RestMethod "$OldUrl/Current/$id/1"; $b = Invoke-RestMethod "$NewUrl/Current/$id/1"
    if ($a.lastUpdated -ne $b.lastUpdated) { Write-Host "Current $id lastUpdated old=$($a.lastUpdated) new=$($b.lastUpdated) (fine if only a minute apart)" -ForegroundColor Yellow }
    if ($a.wsName -ne $b.wsName) { $diffs++; Write-Host "DIFF Current $id wsName" -ForegroundColor Red }

    # Chart for last month: same points.
    $m = $today.AddMonths(-1).AddDays(1 - $today.Day).ToString("yyyy-MM-dd")
    $a = Invoke-RestMethod "$OldUrl/Chart/$id/3/$m/1"; $b = Invoke-RestMethod "$NewUrl/Chart/$id/3/$m/1"
    if (($a.points | ConvertTo-Json -Compress) -ne ($b.points | ConvertTo-Json -Compress)) { $diffs++; Write-Host "DIFF Chart $id month $m" -ForegroundColor Red }
}

# Station list (names and locations; settings come later with logins).
$a = Invoke-RestMethod "$OldUrl/Stations/1/20/"; $b = Invoke-RestMethod "$NewUrl/Stations/1/20/"
if (($a.stations | Select-Object id, name, address, suburb, state, country, coordinates, hasPower | ConvertTo-Json -Compress) -ne
    ($b.stations | Select-Object id, name, address, suburb, state, country, coordinates, hasPower | ConvertTo-Json -Compress)) {
    $diffs++; Write-Host "DIFF Stations list" -ForegroundColor Red
}

Write-Host ""
Write-Host "Compared $n History reports x $($historyFields.Count) fields, plus Current, Chart and Stations: $diffs difference(s)."
if ($diffs -gt 0) { exit 1 }
