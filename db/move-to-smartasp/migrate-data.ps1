<#
.SYNOPSIS
    Copies Home Weather Hub data from Conetix (source) to SmartASP (target), then checks the copy.

.DESCRIPTION
    Runs on your PC (Windows PowerShell 5.1 or later). Both databases must accept remote connections.
    Connection strings come from move-config.json next to this script (git-ignored; see move-config.example.json).
    Nothing is ever changed on the source.

    -Mode Full   First copy into an EMPTY target (run 01_schema.sql and migrations 001-003 there first):
                   all stations, station settings and users; ALL hourly rollups (history back to 2021);
                   per-entry readings (WSReport) from midnight KeepRawDays ago; log (WSData) for KeepLogDays.
                   Original IDs are kept. Refuses a non-empty target unless -Force (which empties it first).
    -Mode Delta  Copies only readings and log rows added on the source since the last run (tracked in
                   move-state.json), e.g. stations posting to Conetix during the DNS switch. The target gives
                   them new IDs, so this is safe even after the target has started receiving its own readings.
                   Then rebuilds the target's hourly rollups for the affected days.
    -Mode Verify Only runs the checks.

.EXAMPLE
    .\migrate-data.ps1 -Mode Full
    .\migrate-data.ps1 -Mode Delta
    .\migrate-data.ps1 -Mode Verify
#>
param(
    [Parameter(Mandatory = $true)][ValidateSet("Full", "Delta", "Verify")][string]$Mode,
    [string]$ConfigFile,
    [string]$StateFile,
    [switch]$Force
)

$ErrorActionPreference = "Stop"

# $PSScriptRoot can be empty in parameter defaults under Windows PowerShell 5.1, so resolve the defaults here.
$here = if ($PSScriptRoot) { $PSScriptRoot } else { Split-Path -Parent $MyInvocation.MyCommand.Path }
if (-not $ConfigFile) { $ConfigFile = Join-Path $here "move-config.json" }
if (-not $StateFile) { $StateFile = Join-Path $here "move-state.json" }

if (-not (Test-Path $ConfigFile)) { throw "Missing $ConfigFile. Copy move-config.example.json to move-config.json and fill it in." }
$config = Get-Content -Raw $ConfigFile | ConvertFrom-Json

# Source defaults to the API's Visual Studio user secrets (the Conetix connection string used for local debugging),
# so that password doesn't need to be stored a second time.
if (-not $config.SourceConnectionString) {
    $secretsFile = "$env:APPDATA\Microsoft\UserSecrets\9de5f632-5599-4899-b592-02534c5249cb\secrets.json"
    if (-not (Test-Path $secretsFile)) { throw "No SourceConnectionString in $ConfigFile and no API user secrets found." }
    $config | Add-Member -NotePropertyName SourceConnectionString -NotePropertyValue ((Get-Content -Raw $secretsFile | ConvertFrom-Json).ConnectionStrings.WeatherDb) -Force
}
if ($config.TargetConnectionString -match "YOUR_") { throw "Fill in the real details in TargetConnectionString in $ConfigFile first." }
$keepRawDays = if ($config.KeepRawDays) { [int]$config.KeepRawDays } else { 7 }
$keepLogDays = if ($config.KeepLogDays) { [int]$config.KeepLogDays } else { 30 }

function Open-Db([string]$cs, [string]$label) {
    $c = New-Object System.Data.SqlClient.SqlConnection $cs
    $c.Open()
    Write-Host ("{0}: {1} / {2}" -f $label, $c.DataSource, $c.Database)
    return $c
}

function Invoke-Scalar($conn, [string]$sql, [hashtable]$params = @{}) {
    $cmd = $conn.CreateCommand(); $cmd.CommandText = $sql; $cmd.CommandTimeout = 600
    foreach ($k in $params.Keys) { [void]$cmd.Parameters.AddWithValue($k, $params[$k]) }
    $v = $cmd.ExecuteScalar()
    if ($v -is [DBNull]) { return $null } else { return $v }
}

function Invoke-Rows($conn, [string]$sql, [hashtable]$params = @{}) {
    $cmd = $conn.CreateCommand(); $cmd.CommandText = $sql; $cmd.CommandTimeout = 600
    foreach ($k in $params.Keys) { [void]$cmd.Parameters.AddWithValue($k, $params[$k]) }
    $t = New-Object System.Data.DataTable
    $t.Load($cmd.ExecuteReader())
    return ,$t
}

# Finds a table by name on a connection, whatever its schema (on Conetix some tables are in "storrs").
function Get-QualifiedName($conn, [string]$table) {
    $q = Invoke-Scalar $conn "SELECT TOP 1 QUOTENAME(SCHEMA_NAME(schema_id)) + '.' + QUOTENAME(name) FROM sys.tables WHERE name = @n ORDER BY CASE SCHEMA_NAME(schema_id) WHEN 'dbo' THEN 0 ELSE 1 END" @{ "@n" = $table }
    if (-not $q) { throw "Table $table not found on $($conn.DataSource)/$($conn.Database)" }
    return $q
}

# Columns present in both tables (by name), in the target's order.
function Get-CommonColumns($src, $srcName, $dst, $dstName, [switch]$WithoutIdentity) {
    $sql = "SELECT c.name, c.is_identity FROM sys.columns c WHERE c.object_id = OBJECT_ID(@t) ORDER BY c.column_id"
    $s = Invoke-Rows $src $sql @{ "@t" = $srcName }
    $d = Invoke-Rows $dst $sql @{ "@t" = $dstName }
    $srcCols = @($s | ForEach-Object { $_.name })
    $cols = @()
    foreach ($row in $d) {
        if ($WithoutIdentity -and $row.is_identity) { continue }
        if ($srcCols -contains $row.name) { $cols += $row.name }
    }
    return $cols
}

function Copy-Table($src, $dst, [string]$table, [string]$where, [hashtable]$params, [bool]$keepIdentity) {
    $srcName = Get-QualifiedName $src $table
    $dstName = Get-QualifiedName $dst $table
    $cols = Get-CommonColumns $src $srcName $dst $dstName -WithoutIdentity:(-not $keepIdentity)
    $colList = ($cols | ForEach-Object { "[$_]" }) -join ", "
    $sql = "SELECT $colList FROM $srcName WITH (NOLOCK)"
    if ($where) { $sql += " WHERE $where" }

    $cmd = $src.CreateCommand(); $cmd.CommandText = $sql; $cmd.CommandTimeout = 0
    foreach ($k in $params.Keys) { [void]$cmd.Parameters.AddWithValue($k, $params[$k]) }
    $reader = $cmd.ExecuteReader()

    $options = [System.Data.SqlClient.SqlBulkCopyOptions]::CheckConstraints
    if ($keepIdentity) { $options = $options -bor [System.Data.SqlClient.SqlBulkCopyOptions]::KeepIdentity }
    $bulk = New-Object System.Data.SqlClient.SqlBulkCopy($dst, $options, $null)
    $bulk.DestinationTableName = $dstName
    $bulk.BatchSize = 5000
    $bulk.BulkCopyTimeout = 0
    foreach ($c in $cols) { [void]$bulk.ColumnMappings.Add($c, $c) }

    $sw = [Diagnostics.Stopwatch]::StartNew()
    try { $bulk.WriteToServer($reader) } finally { $reader.Close() }
    Write-Host ("  {0,-18} copied in {1:N1} s" -f $table, $sw.Elapsed.TotalSeconds)
}

$src = Open-Db $config.SourceConnectionString "Source (Conetix)"
$dst = Open-Db $config.TargetConnectionString "Target (SmartASP)"

if ($src.DataSource -eq $dst.DataSource -and $src.Database -eq $dst.Database) { throw "Source and target are the same database." }

foreach ($p in "sp_WSReportData", "sp_WSRollupRange", "sp_WSRollupBackfill", "sp_WSPurgeOldData") {
    if (-not (Invoke-Scalar $dst "SELECT OBJECT_ID(@n, 'P')" @{ "@n" = "dbo.$p" })) {
        throw "Target is missing dbo.$p. Run 01_schema.sql and migrations 001, 002 and 003 on the target first."
    }
}

# Same clock as DateAdded (both hosts store Brisbane time).
$brisbane = [TimeZoneInfo]::FindSystemTimeZoneById("E. Australia Standard Time")
$now = [TimeZoneInfo]::ConvertTimeFromUtc([DateTime]::UtcNow, $brisbane)
$rawFrom = $now.Date.AddDays(-$keepRawDays)
$logFrom = $now.AddDays(-$keepLogDays)

$state = if (Test-Path $StateFile) { Get-Content -Raw $StateFile | ConvertFrom-Json } else { $null }

if ($Mode -eq "Full") {
    $tables = "WSStations", "WSStationSettings", "WSUsers", "WSReport", "WSData", "WSReportHourly"
    $nonEmpty = @($tables | Where-Object { [long](Invoke-Scalar $dst "SELECT COUNT_BIG(*) FROM $(Get-QualifiedName $dst $_)") -gt 0 })
    if ($nonEmpty.Count -gt 0) {
        if (-not $Force) { throw "Target tables are not empty: $($nonEmpty -join ', '). Use -Force to empty them and copy again." }
        Write-Host "Emptying target tables: $($nonEmpty -join ', ')"
        foreach ($t in $nonEmpty) { [void](Invoke-Scalar $dst "TRUNCATE TABLE $(Get-QualifiedName $dst $t)") }
    }

    # Fix the upper ID bound first, so rows arriving during the copy are left for the next Delta run.
    $maxReport = [long](Invoke-Scalar $src "SELECT ISNULL(MAX(ID), 0) FROM $(Get-QualifiedName $src 'WSReport')")
    $maxData = [long](Invoke-Scalar $src "SELECT ISNULL(MAX(ID), 0) FROM $(Get-QualifiedName $src 'WSData')")

    Write-Host ("Copying (per-entry readings from {0:yyyy-MM-dd}, log from {1:yyyy-MM-dd HH:mm})..." -f $rawFrom, $logFrom)
    Copy-Table $src $dst "WSStations" $null @{} $true
    Copy-Table $src $dst "WSStationSettings" $null @{} $false
    Copy-Table $src $dst "WSUsers" $null @{} $true
    Copy-Table $src $dst "WSReport" "DateAdded >= @from AND ID <= @max" @{ "@from" = $rawFrom; "@max" = $maxReport } $true
    Copy-Table $src $dst "WSData" "DateAdded >= @from AND ID <= @max" @{ "@from" = $logFrom; "@max" = $maxData } $true
    Copy-Table $src $dst "WSReportHourly" $null @{} $false

    # The source's latest hours may include readings above @max; rebuild the copied days from the target's own rows.
    [void](Invoke-Scalar $dst "EXEC dbo.sp_WSRollupBackfill @FromDay = @d" @{ "@d" = $rawFrom.Date })

    $state = [pscustomobject]@{ FullCopyAt = $now.ToString("s"); RawFrom = $rawFrom.ToString("s"); LastReportId = $maxReport; LastDataId = $maxData }
    $state | ConvertTo-Json | Set-Content -Encoding UTF8 $StateFile
}
elseif ($Mode -eq "Delta") {
    if (-not $state) { throw "No $StateFile. Run -Mode Full first." }
    $maxReport = [long](Invoke-Scalar $src "SELECT ISNULL(MAX(ID), 0) FROM $(Get-QualifiedName $src 'WSReport')")
    $maxData = [long](Invoke-Scalar $src "SELECT ISNULL(MAX(ID), 0) FROM $(Get-QualifiedName $src 'WSData')")
    $newReport = [long](Invoke-Scalar $src "SELECT COUNT_BIG(*) FROM $(Get-QualifiedName $src 'WSReport') WITH (NOLOCK) WHERE ID > @a AND ID <= @b" @{ "@a" = [long]$state.LastReportId; "@b" = $maxReport })
    $firstDay = Invoke-Scalar $src "SELECT CAST(MIN(DateAdded) AS DATE) FROM $(Get-QualifiedName $src 'WSReport') WITH (NOLOCK) WHERE ID > @a AND ID <= @b" @{ "@a" = [long]$state.LastReportId; "@b" = $maxReport }

    Write-Host "New on source since the last run: $newReport readings."
    Copy-Table $src $dst "WSReport" "ID > @a AND ID <= @b" @{ "@a" = [long]$state.LastReportId; "@b" = $maxReport } $false
    Copy-Table $src $dst "WSData" "ID > @a AND ID <= @b" @{ "@a" = [long]$state.LastDataId; "@b" = $maxData } $false

    if ($firstDay) {
        Write-Host ("Rebuilding target hourly rollups from {0:yyyy-MM-dd}..." -f $firstDay)
        [void](Invoke-Scalar $dst "EXEC dbo.sp_WSRollupBackfill @FromDay = @d" @{ "@d" = $firstDay })
    }

    $state.LastReportId = $maxReport
    $state.LastDataId = $maxData
    $state | ConvertTo-Json | Set-Content -Encoding UTF8 $StateFile
}

# ---------- Checks ----------
if (-not $state) { throw "No $StateFile. Run -Mode Full first." }
$rawFrom = [DateTime]$state.RawFrom
$results = New-Object System.Collections.Generic.List[object]
function Add-Check([string]$name, $source, $target) {
    $ok = "$source" -eq "$target"
    $results.Add([pscustomobject]@{ Check = $name; Source = "$source"; Target = "$target"; Result = $(if ($ok) { "PASS" } else { "FAIL" }) })
}

# Small tables: every row and column (the target may have newer LastActive/SampleData once it receives posts).
foreach ($t in @(
        @{ n = "WSStations"; cols = "ID, PassKey, UserID, StationName, Status, Suburb, State, Country, Latitude, Longitude, HasPower" },
        @{ n = "WSStationSettings"; cols = "StationID, SettingName, SettingValue" },
        @{ n = "WSUsers"; cols = "UserID, Username, Password, EmailAddress, AccessLevel" })) {
    $sql = "SELECT CAST(COUNT_BIG(*) AS VARCHAR(20)) + ' rows, checksum ' + CAST(ISNULL(CHECKSUM_AGG(BINARY_CHECKSUM($($t.cols))), 0) AS VARCHAR(20)) FROM {0}"
    Add-Check $t.n (Invoke-Scalar $src ($sql -f (Get-QualifiedName $src $t.n))) (Invoke-Scalar $dst ($sql -f (Get-QualifiedName $dst $t.n)))
}

# Hourly history before the per-entry window: must be identical.
$hsql = "SELECT CAST(COUNT_BIG(*) AS VARCHAR(20)) + ' rows, ' + CAST(ISNULL(SUM(CAST(SampleCount AS BIGINT)), 0) AS VARCHAR(20)) + ' readings, checksum ' + CAST(ISNULL(CHECKSUM_AGG(BINARY_CHECKSUM(StationID, HourStart, IsDailySummary, SampleCount, TempOutMin, TempOutMax, HumidityOutMin, HumidityOutMax, BaromRelMin, BaromRelMax, WindGustMax, RainInch)), 0) AS VARCHAR(20)) FROM dbo.WSReportHourly WHERE HourStart < @d"
Add-Check "WSReportHourly (before $($rawFrom.ToString('yyyy-MM-dd')))" (Invoke-Scalar $src $hsql @{ "@d" = $rawFrom }) (Invoke-Scalar $dst $hsql @{ "@d" = $rawFrom })

# Per-entry readings in the window, per station-day. Everything the source had (up to the last copied ID)
# must be on the target. The target may have MORE once it receives posts itself, so "fewer" is the failure.
# (IDs aren't compared: Delta runs give copied rows new IDs on the target.)
$lastId = [long]$state.LastReportId
# Once the target's daily clean-up runs (migration 003) it removes readings older than KeepRawDays (their hourly
# rollups stay), so only compare from that cutoff onwards.
$cleanupCutoff = $now.Date.AddDays(-$keepRawDays)
if ($cleanupCutoff -gt $rawFrom) { $rawFrom = $cleanupCutoff }
$daysql = "SELECT CONVERT(VARCHAR(10), CAST(DateAdded AS DATE), 120) + ' ' + Passkey AS K, COUNT(*) AS N FROM {0} WITH (NOLOCK) WHERE DateAdded >= @d {1} GROUP BY CAST(DateAdded AS DATE), Passkey"
$srcDays = Invoke-Rows $src ($daysql -f (Get-QualifiedName $src 'WSReport'), "AND ID <= @id") @{ "@d" = $rawFrom; "@id" = $lastId }
$dstDays = Invoke-Rows $dst ($daysql -f (Get-QualifiedName $dst 'WSReport'), "") @{ "@d" = $rawFrom }
$dstMap = @{}; foreach ($r in $dstDays) { $dstMap[$r.K] = [int]$r.N }
$short = 0; $srcTotal = 0
foreach ($r in $srcDays) { $srcTotal += [int]$r.N; if (-not $dstMap.ContainsKey($r.K) -or $dstMap[$r.K] -lt [int]$r.N) { $short++ } }
$dstTotal = [long](($dstDays | Measure-Object -Property N -Sum).Sum)
Add-Check "WSReport station-days with fewer readings on target" 0 $short
Write-Host ("WSReport readings since {0:yyyy-MM-dd}: source {1} (IDs <= {2}), target {3}" -f $rawFrom, $srcTotal, $lastId, $dstTotal)

# Target rollups agree with the target's own per-entry rows for the window.
$rollsql = "SELECT COUNT(*) FROM (SELECT s.ID, CAST(r.DateAdded AS DATE) AS D, COUNT(*) AS N FROM dbo.WSReport r JOIN dbo.WSStations s ON s.PassKey = r.Passkey WHERE r.DateAdded >= @d GROUP BY s.ID, CAST(r.DateAdded AS DATE)) r LEFT JOIN (SELECT StationID, CAST(HourStart AS DATE) AS D, SUM(SampleCount) AS N FROM dbo.WSReportHourly WHERE HourStart >= @d GROUP BY StationID, CAST(HourStart AS DATE)) h ON h.StationID = r.ID AND h.D = r.D WHERE h.N IS NULL OR h.N <> r.N"
Add-Check "Target station-days where hourly rollup <> readings" 0 (Invoke-Scalar $dst $rollsql @{ "@d" = $rawFrom })

Write-Host ""
$results | Format-Table -AutoSize | Out-String -Width 250 | Write-Host
$failed = @($results | Where-Object Result -eq "FAIL").Count
if ($failed -gt 0) { Write-Host "$failed check(s) FAILED. Do not switch over until they pass." -ForegroundColor Red; exit 1 }
Write-Host "All checks passed." -ForegroundColor Green

$src.Close(); $dst.Close()
