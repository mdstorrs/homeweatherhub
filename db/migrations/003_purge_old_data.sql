-- Migration 003: daily clean-up that keeps the database small (SmartASP has a hard 10 GB limit).
--
-- RUN ON THE NEW HOST ONLY. Conetix keeps its full history untouched as a fallback until it is cancelled.
-- The API runs this procedure a few times a day, but only if it exists, so not creating it on Conetix
-- is what keeps Conetix's data safe.
--
--   * Per-entry readings (WSReport) older than @KeepRawDays are deleted, a whole station-day at a time,
--     and ONLY when that day's hourly rollup (WSReportHourly) holds exactly the same number of readings.
--     A day that doesn't match is left alone and reported in DaysSkipped; rebuild it with
--     EXEC dbo.sp_WSRollupBackfill @StationID = ..., @FromDay = ..., @ToDay = ...; and it is cleaned next time.
--   * Log rows (WSData) older than @KeepLogDays are deleted.
--   * Deletes run in small batches so the transaction log stays small.

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

IF OBJECT_ID('dbo.sp_WSPurgeOldData', 'P') IS NULL EXEC('CREATE PROCEDURE dbo.sp_WSPurgeOldData AS RETURN 0');
GO

ALTER PROCEDURE dbo.sp_WSPurgeOldData
    @KeepRawDays    INT = 7,
    @KeepLogDays    INT = 30,
    @BatchSize      INT = 5000,
    @Now            DATETIME = NULL     -- current time on the same clock as DateAdded (the API passes Brisbane time)
AS
BEGIN
    SET NOCOUNT ON;

    IF @KeepRawDays < 2 OR @KeepLogDays < 1
    BEGIN
        RAISERROR('sp_WSPurgeOldData: keep at least 2 days of readings and 1 day of log.', 16, 1);
        RETURN;
    END

    IF @Now IS NULL SET @Now = GETDATE();

    -- Whole days only: everything before midnight @KeepRawDays days ago.
    DECLARE @RawCutoff DATETIME = CAST(CAST(DATEADD(DAY, -@KeepRawDays, @Now) AS DATE) AS DATETIME);
    DECLARE @LogCutoff DATETIME = DATEADD(DAY, -@KeepLogDays, @Now);
    DECLARE @RawDeleted INT = 0, @LogDeleted INT = 0, @DaysCleaned INT = 0, @DaysSkipped INT = 0, @n INT;

    -- Station-days older than the cutoff, with their raw and rolled-up reading counts.
    -- COLLATE DATABASE_DEFAULT: temp tables otherwise take the server's collation, which can differ from this database's.
    CREATE TABLE #days (Passkey NVARCHAR(50) COLLATE DATABASE_DEFAULT NOT NULL, DayStart DATETIME NOT NULL, RawCount INT NOT NULL, RolledCount INT NULL);

    INSERT INTO #days (Passkey, DayStart, RawCount, RolledCount)
    SELECT r.Passkey, r.DayStart, r.RawCount, h.RolledCount
    FROM (SELECT Passkey, CAST(CAST(DateAdded AS DATE) AS DATETIME) AS DayStart, COUNT(*) AS RawCount
          FROM dbo.WSReport
          WHERE DateAdded < @RawCutoff
          GROUP BY Passkey, CAST(CAST(DateAdded AS DATE) AS DATETIME)) r
    LEFT JOIN dbo.WSStations s ON s.PassKey = r.Passkey
    LEFT JOIN (SELECT StationID, CAST(CAST(HourStart AS DATE) AS DATETIME) AS DayStart, SUM(SampleCount) AS RolledCount
               FROM dbo.WSReportHourly
               WHERE HourStart < @RawCutoff
               GROUP BY StationID, CAST(CAST(HourStart AS DATE) AS DATETIME)) h
           ON h.StationID = s.ID AND h.DayStart = r.DayStart;

    SELECT @DaysSkipped = COUNT(*) FROM #days WHERE RolledCount IS NULL OR RolledCount <> RawCount;
    DELETE FROM #days WHERE RolledCount IS NULL OR RolledCount <> RawCount;
    SELECT @DaysCleaned = COUNT(*) FROM #days;

    WHILE 1 = 1
    BEGIN
        DELETE TOP (@BatchSize) w
        FROM dbo.WSReport w
        JOIN #days d ON d.Passkey = w.Passkey AND w.DateAdded >= d.DayStart AND w.DateAdded < DATEADD(DAY, 1, d.DayStart);
        SET @n = @@ROWCOUNT;
        SET @RawDeleted += @n;
        IF @n < @BatchSize BREAK;
    END

    WHILE 1 = 1
    BEGIN
        DELETE TOP (@BatchSize) FROM dbo.WSData WHERE DateAdded < @LogCutoff;
        SET @n = @@ROWCOUNT;
        SET @LogDeleted += @n;
        IF @n < @BatchSize BREAK;
    END

    SELECT @RawCutoff AS RawCutoff, @DaysCleaned AS DaysCleaned, @RawDeleted AS RawRowsDeleted,
           @DaysSkipped AS DaysSkipped, @LogDeleted AS LogRowsDeleted;
END
GO
