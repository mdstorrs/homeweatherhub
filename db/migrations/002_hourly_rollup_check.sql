-- Checks for migration 002. Read-only. Change the station and dates at the top and run.
-- Each pair of rows should match: rollups (WSReportHourly) vs the raw readings (WSReport).

DECLARE @StationID INT = 1;
DECLARE @From DATETIME = '2026-09-01';
DECLARE @To DATETIME = '2026-10-01';

DECLARE @PassKey NVARCHAR(50) = (SELECT Passkey FROM dbo.WSStations WHERE ID = @StationID);

-- 1. Min/max must match exactly. Readings must add up to the raw count.
SELECT 'rollup' AS Source,
       MIN(TempOutMin) AS TempOutMin, MAX(TempOutMax) AS TempOutMax,
       MIN(HumidityOutMin) AS HumOutMin, MAX(HumidityOutMax) AS HumOutMax,
       MIN(BaromRelMin) AS BaromMin, MAX(BaromRelMax) AS BaromMax,
       MAX(WindGustMax) AS GustMax, MAX(RainRateMax) AS RainRateMax,
       SUM(SampleCount) AS Readings
FROM dbo.WSReportHourly
WHERE StationID = @StationID AND HourStart >= @From AND HourStart < @To
UNION ALL
SELECT 'raw',
       MIN(CASE WHEN TempOutF <> 0 THEN TempOutF END), MAX(CASE WHEN TempOutF <> 0 THEN TempOutF END),
       MIN(CASE WHEN TempOutF <> 0 THEN HumidityOut END), MAX(CASE WHEN TempOutF <> 0 THEN HumidityOut END),
       MIN(CASE WHEN TempOutF <> 0 THEN BaromRelIn END), MAX(CASE WHEN TempOutF <> 0 THEN BaromRelIn END),
       MAX(CASE WHEN TempOutF <> 0 THEN WindGustMPH END), MAX(CASE WHEN TempOutF <> 0 THEN RainRateInch END),
       COUNT(*)
FROM dbo.WSReport WITH (NOLOCK)
WHERE Passkey = @PassKey AND DateAdded >= @From AND DateAdded < @To;

-- 2. Rain: the rollup total (sum of DailyRainInch increases) vs the current method (sum of each day's highest DailyRainInch).
--    These should be close; small differences around midnight are expected.
SELECT 'rollup' AS Method, SUM(RainInch) AS RainInch
FROM dbo.WSReportHourly
WHERE StationID = @StationID AND HourStart >= @From AND HourStart < @To
UNION ALL
SELECT 'raw (daily max)', SUM(DayMax)
FROM (SELECT MAX(DailyRainInch) AS DayMax
      FROM dbo.WSReport WITH (NOLOCK)
      WHERE Passkey = @PassKey AND DateAdded >= @From AND DateAdded < @To
      GROUP BY CAST(DateAdded AS DATE)) x;

-- 3. Days that differ most on rain, to look at if section 2 is far apart.
SELECT TOP 10 h.Day, h.RainCounter, r.RainDailyMax, ABS(h.RainCounter - r.RainDailyMax) AS Diff
FROM (SELECT CAST(HourStart AS DATE) AS Day, SUM(RainInch) AS RainCounter
      FROM dbo.WSReportHourly
      WHERE StationID = @StationID AND HourStart >= @From AND HourStart < @To
      GROUP BY CAST(HourStart AS DATE)) h
JOIN (SELECT CAST(DateAdded AS DATE) AS Day, MAX(DailyRainInch) AS RainDailyMax
      FROM dbo.WSReport WITH (NOLOCK)
      WHERE Passkey = @PassKey AND DateAdded >= @From AND DateAdded < @To
      GROUP BY CAST(DateAdded AS DATE)) r ON r.Day = h.Day
ORDER BY Diff DESC;

-- 4. Overview: buckets per station, daily summaries, and the newest hour (should be the current hour).
SELECT StationID, COUNT(*) AS Buckets, SUM(CASE WHEN IsDailySummary = 1 THEN 1 ELSE 0 END) AS DailySummaries,
       MIN(HourStart) AS FirstBucket, MAX(HourStart) AS LatestBucket, SUM(SampleCount) AS Readings
FROM dbo.WSReportHourly
GROUP BY StationID;
