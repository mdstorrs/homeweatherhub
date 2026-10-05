<#
.SYNOPSIS
    Runs a SQL script (with GO separators, like SSMS) against the Target (SmartASP) or Source (Conetix) database
    from move-config.json. Stops at the first error.

.EXAMPLE
    .\run-sql.ps1 -Database Target -SqlFile .\01_schema.sql
#>
param(
    [Parameter(Mandatory = $true)][ValidateSet("Target", "Source")][string]$Database,
    [Parameter(Mandatory = $true)][string]$SqlFile,
    [string]$ConfigFile = (Join-Path $PSScriptRoot "move-config.json"),
    [int]$TimeoutSeconds = 3600
)

$ErrorActionPreference = "Stop"
$config = Get-Content -Raw $ConfigFile | ConvertFrom-Json
if ($Database -eq "Target") {
    $cs = $config.TargetConnectionString
    if ($cs -match "YOUR_") { throw "Fill in the real details in TargetConnectionString in $ConfigFile first." }
}
else {
    $cs = $config.SourceConnectionString
    if (-not $cs) { $cs = (Get-Content -Raw "$env:APPDATA\Microsoft\UserSecrets\9de5f632-5599-4899-b592-02534c5249cb\secrets.json" | ConvertFrom-Json).ConnectionStrings.WeatherDb }
}

$batches = [regex]::Split((Get-Content -Raw $SqlFile), '(?im)^\s*GO\s*$') | Where-Object { $_.Trim() -ne "" }

$conn = New-Object System.Data.SqlClient.SqlConnection $cs
$conn.add_InfoMessage([System.Data.SqlClient.SqlInfoMessageEventHandler] {
    param($s, $e)
    foreach ($err in $e.Errors) { if ($err.Message -notmatch "Null value is eliminated") { Write-Host "  $($err.Message)" } }
})
$conn.Open()
try {
    Write-Host "$Database database: $($conn.DataSource) / $($conn.Database)"
    $n = 0
    foreach ($b in $batches) {
        $n++
        $cmd = $conn.CreateCommand(); $cmd.CommandText = $b; $cmd.CommandTimeout = $TimeoutSeconds
        $reader = $cmd.ExecuteReader()
        # DataTable.Load moves to the next result set itself (and closes the reader after the last one).
        while (-not $reader.IsClosed) {
            if ($reader.FieldCount -gt 0) {
                $t = New-Object System.Data.DataTable; $t.Load($reader)
                $t | Format-Table -AutoSize | Out-String -Width 250 | Write-Host
            }
            elseif (-not $reader.NextResult()) { break }
        }
        $reader.Close()
    }
    Write-Host "OK: $n batch(es) from $(Split-Path -Leaf $SqlFile)"
}
finally { $conn.Close() }
