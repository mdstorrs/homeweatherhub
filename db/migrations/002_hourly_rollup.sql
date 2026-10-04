-- Migration 002: hourly rollups (WSReportHourly) + procedures to fill them.
--
-- Additive only: creates one new table and three new procedures. WSReport, WSStations and the
-- existing procedures are not changed. Safe to run more than once.
--
-- What it does:
--   * WSReportHourly holds one row per station per hour: min/max/sum for each reading, rain that fell,
--     and how many readings the hour had (about 120 when the station was online the whole hour).
--   * Days the old manual rollup reduced to 2 rows become ONE row (IsDailySummary = 1, HourStart = midnight):
--     their min/max are still right, but they have no hourly detail.
--   * Hours with no readings (station offline) get no row. Nothing is invented for gaps.
--   * Rain comes from DailyRainInch rising above its highest value so far that day (TotalRainInch stopped
--     updating in 2024, and consoles briefly report 0 after a restart). Each day totals its highest DailyRainInch.
--     Rain that fell while the station was offline lands in the first hour after the gap;
--     PrevReadingAt shows how long the gap was.
--   * Times use the same clock as WSReport.DateAdded (Brisbane), so they line up with existing reports.
--
-- After running this, fill the history with:   EXEC dbo.sp_WSRollupBackfill;
-- (see the notes on sp_WSRollupBackfill below). Undo with 002_hourly_rollup_rollback.sql.

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

IF OBJECT_ID('dbo.WSReportHourly', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.WSReportHourly (
        StationID           INT       NOT NULL,
        HourStart           DATETIME  NOT NULL, -- start of the hour (midnight for daily summaries), Brisbane time
        IsDailySummary      BIT       NOT NULL CONSTRAINT DF_WSReportHourly_IsDailySummary DEFAULT 0,
        SampleCount         INT       NOT NULL, -- raw readings in this bucket (~120 for a full hour)
        ValidCount          INT       NOT NULL, -- readings used for weather values (TempOutF <> 0, as History does)
        FirstReadingAt      DATETIME  NULL,
        LastReadingAt       DATETIME  NULL,
        PrevReadingAt       DATETIME  NULL,     -- the reading before this bucket; a big gap = station was offline

        TempOutMin          REAL      NULL,
        TempOutMax          REAL      NULL,
        TempOutSum          FLOAT     NULL,     -- average = Sum / ValidCount
        TempInMin           REAL      NULL,
        TempInMax           REAL      NULL,
        TempInSum           FLOAT     NULL,
        HumidityOutMin      SMALLINT  NULL,
        HumidityOutMax      SMALLINT  NULL,
        HumidityOutSum      INT       NULL,
        HumidityInMin       SMALLINT  NULL,
        HumidityInMax       SMALLINT  NULL,
        HumidityInSum       INT       NULL,
        BaromRelMin         REAL      NULL,
        BaromRelMax         REAL      NULL,
        BaromRelSum         FLOAT     NULL,

        WindSpeedMax        REAL      NULL,
        WindSpeedSum        FLOAT     NULL,
        WindGustMax         REAL      NULL,
        WindSinWeighted     FLOAT     NULL,     -- wind direction as speed-weighted vectors (see History)
        WindCosWeighted     FLOAT     NULL,
        WindSin             FLOAT     NULL,     -- unweighted, used when there was no wind at all
        WindCos             FLOAT     NULL,

        RainInch            FLOAT     NULL,     -- rain that fell in this bucket (from DailyRainInch increases)
        RainRateMax         REAL      NULL,
        DailyRainMax        REAL      NULL,     -- highest DailyRainInch seen, for cross-checking
        UVMax               REAL      NULL,
        SolarRadiationMax   REAL      NULL,

        UpdatedAtUtc        DATETIME  NOT NULL CONSTRAINT DF_WSReportHourly_UpdatedAtUtc DEFAULT GETUTCDATE(),

        CONSTRAINT PK_WSReportHourly PRIMARY KEY CLUSTERED (StationID, HourStart)
    );
END
GO

-- Placeholders so the ALTERs below work on any SQL Server version (no CREATE OR ALTER needed).
IF OBJECT_ID('dbo.sp_WSRollupRange', 'P') IS NULL EXEC('CREATE PROCEDURE dbo.sp_WSRollupRange AS RETURN 0');
IF OBJECT_ID('dbo.sp_WSRollupReading', 'P') IS NULL EXEC('CREATE PROCEDURE dbo.sp_WSRollupReading AS RETURN 0');
IF OBJECT_ID('dbo.sp_WSRollupBackfill', 'P') IS NULL EXEC('CREATE PROCEDURE dbo.sp_WSRollupBackfill AS RETURN 0');
GO

-- Rebuilds WSReportHourly for one station between @From (inclusive) and @To (exclusive) from the raw readings.
-- Recalculating from WSReport every time (rather than adding to running totals) means it can be re-run
-- safely and always ends up with the same answer.
ALTER PROCEDURE dbo.sp_WSRollupRange
    @StationID          INT,
    @From               DATETIME,
    @To                 DATETIME,
    @AllowDailySummary  BIT = 0     -- 1 = a range with only 1-2 readings becomes one daily summary row (backfill only)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @PassKey NVARCHAR(50) = (SELECT Passkey FROM dbo.WSStations WITH (NOLOCK) WHERE ID = @StationID);
    IF @PassKey IS NULL RETURN;

    -- Callers pass one day (backfill) or one hour (live); the rain logic below relies on not crossing midnight.
    IF CAST(@From AS DATE) <> CAST(DATEADD(SECOND, -1, @To) AS DATE)
    BEGIN
        RAISERROR('sp_WSRollupRange: @From and @To must be within the same day.', 16, 1);
        RETURN;
    END

    DECLARE @Summary BIT = 0;
    IF @AllowDailySummary = 1
       AND (SELECT COUNT(*) FROM dbo.WSReport WITH (NOLOCK)
            WHERE Passkey = @PassKey AND DateAdded >= @From AND DateAdded < @To) BETWEEN 1 AND 2
        SET @Summary = 1;

    -- Rain comes from DailyRainInch (TotalRainInch stopped updating in 2024). DailyRainInch resets at midnight,
    -- and when a console restarts it briefly reports 0 before restoring its real value (seen in March 2025).
    -- So rain only counts when DailyRainInch rises above the highest value so far that day; false zeros
    -- add nothing, and each day's total equals that day's highest DailyRainInch (as History reports it).
    -- @RainBaseline = the highest DailyRainInch earlier the same day, before this range (0 from midnight).
    DECLARE @RainBaseline FLOAT = ISNULL((
        SELECT MAX(CAST(DailyRainInch AS FLOAT)) FROM dbo.WSReport WITH (NOLOCK)
        WHERE Passkey = @PassKey AND DateAdded >= CAST(CAST(@From AS DATE) AS DATETIME) AND DateAdded < @From), 0);

    BEGIN TRANSACTION;

    DELETE FROM dbo.WSReportHourly
    WHERE StationID = @StationID AND HourStart >= @From AND HourStart < @To;

    WITH prev AS (
        -- The last reading before the range, to record how long any gap before this bucket was.
        SELECT TOP (1) ID, DateAdded
        FROM dbo.WSReport WITH (NOLOCK)
        WHERE Passkey = @PassKey AND DateAdded < @From
        ORDER BY DateAdded DESC, ID DESC
    ),
    r AS (
        SELECT ID, DateAdded, DailyRainInch, CAST(0 AS BIT) AS IsPrev,
               TempOutF, TempInF, HumidityOut, HumidityIn, BaromRelIn, WindDir, WindSpeedMPH, WindGustMPH,
               RainRateInch, UV, SolarRadiation
        FROM dbo.WSReport WITH (NOLOCK)
        WHERE Passkey = @PassKey AND DateAdded >= @From AND DateAdded < @To
        UNION ALL
        SELECT ID, DateAdded, NULL, CAST(1 AS BIT),
               NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL
        FROM prev
    ),
    l AS (
        SELECT r.*,
               LAG(DateAdded) OVER (ORDER BY DateAdded, ID) AS PrevAt,
               MAX(CAST(DailyRainInch AS FLOAT)) OVER (ORDER BY DateAdded, ID ROWS UNBOUNDED PRECEDING) AS RunMax
        FROM r
    ),
    m AS (
        SELECT l.*,
               CASE WHEN RunMax IS NULL OR RunMax < @RainBaseline THEN @RainBaseline ELSE RunMax END AS DayMaxSoFar
        FROM l
    ),
    c AS (
        SELECT m.*,
               DayMaxSoFar - LAG(DayMaxSoFar, 1, @RainBaseline) OVER (ORDER BY DateAdded, ID) AS RainDelta,
               CASE WHEN @Summary = 1 THEN CAST(CAST(DateAdded AS DATE) AS DATETIME)
                    ELSE DATEADD(HOUR, DATEDIFF(HOUR, 0, DateAdded), 0) END AS Bucket,
               CASE WHEN TempOutF <> 0 THEN 1 ELSE 0 END AS Valid
        FROM m
    ),
    d AS (
        -- RowInBucket = 1 marks each bucket's first reading; its PrevAt is the reading before the bucket.
        SELECT c.*, ROW_NUMBER() OVER (PARTITION BY IsPrev, Bucket ORDER BY DateAdded, ID) AS RowInBucket
        FROM c
    )
    INSERT INTO dbo.WSReportHourly (
        StationID, HourStart, IsDailySummary, SampleCount, ValidCount, FirstReadingAt, LastReadingAt, PrevReadingAt,
        TempOutMin, TempOutMax, TempOutSum, TempInMin, TempInMax, TempInSum,
        HumidityOutMin, HumidityOutMax, HumidityOutSum, HumidityInMin, HumidityInMax, HumidityInSum,
        BaromRelMin, BaromRelMax, BaromRelSum,
        WindSpeedMax, WindSpeedSum, WindGustMax, WindSinWeighted, WindCosWeighted, WindSin, WindCos,
        RainInch, RainRateMax, DailyRainMax, UVMax, SolarRadiationMax)
    SELECT
        @StationID, Bucket, @Summary, COUNT(*), SUM(Valid), MIN(DateAdded), MAX(DateAdded),
        MAX(CASE WHEN RowInBucket = 1 THEN PrevAt END),
        MIN(CASE WHEN Valid = 1 THEN TempOutF END), MAX(CASE WHEN Valid = 1 THEN TempOutF END), SUM(CASE WHEN Valid = 1 THEN CAST(TempOutF AS FLOAT) END),
        MIN(CASE WHEN Valid = 1 THEN TempInF END),  MAX(CASE WHEN Valid = 1 THEN TempInF END),  SUM(CASE WHEN Valid = 1 THEN CAST(TempInF AS FLOAT) END),
        MIN(CASE WHEN Valid = 1 THEN HumidityOut END), MAX(CASE WHEN Valid = 1 THEN HumidityOut END), SUM(CASE WHEN Valid = 1 THEN CAST(HumidityOut AS INT) END),
        MIN(CASE WHEN Valid = 1 THEN HumidityIn END),  MAX(CASE WHEN Valid = 1 THEN HumidityIn END),  SUM(CASE WHEN Valid = 1 THEN CAST(HumidityIn AS INT) END),
        MIN(CASE WHEN Valid = 1 THEN BaromRelIn END),  MAX(CASE WHEN Valid = 1 THEN BaromRelIn END),  SUM(CASE WHEN Valid = 1 THEN CAST(BaromRelIn AS FLOAT) END),
        MAX(CASE WHEN Valid = 1 THEN WindSpeedMPH END), SUM(CASE WHEN Valid = 1 THEN CAST(WindSpeedMPH AS FLOAT) END),
        MAX(CASE WHEN Valid = 1 THEN WindGustMPH END),
        SUM(CASE WHEN Valid = 1 THEN WindSpeedMPH * SIN(RADIANS(CAST(WindDir AS FLOAT))) END),
        SUM(CASE WHEN Valid = 1 THEN WindSpeedMPH * COS(RADIANS(CAST(WindDir AS FLOAT))) END),
        SUM(CASE WHEN Valid = 1 THEN SIN(RADIANS(CAST(WindDir AS FLOAT))) END),
        SUM(CASE WHEN Valid = 1 THEN COS(RADIANS(CAST(WindDir AS FLOAT))) END),
        SUM(CASE WHEN RainDelta > 0 THEN RainDelta ELSE 0 END),
        MAX(CASE WHEN Valid = 1 THEN RainRateInch END),
        MAX(DailyRainInch),
        MAX(CASE WHEN Valid = 1 THEN UV END),
        MAX(CASE WHEN Valid = 1 THEN SolarRadiation END)
    FROM d
    WHERE IsPrev = 0
    GROUP BY Bucket;

    COMMIT;
END
GO

-- Called by the API after each reading is saved: rebuilds that reading's hour for its station.
-- Separate from sp_WSReportData on purpose, so a rollup problem can never stop a reading being saved.
ALTER PROCEDURE dbo.sp_WSRollupReading
    @PassKey    NVARCHAR(50),
    @At         DATETIME        -- the reading's DateAdded
AS
BEGIN
    SET NOCOUNT ON;

    -- Only claimed stations (Status 2) have readings in WSReport.
    DECLARE @StationID INT = (SELECT ID FROM dbo.WSStations WITH (NOLOCK) WHERE Passkey = @PassKey AND Status = 2);
    IF @StationID IS NULL RETURN;

    DECLARE @From DATETIME = DATEADD(HOUR, DATEDIFF(HOUR, 0, @At), 0);
    DECLARE @To DATETIME = DATEADD(HOUR, 1, @From);

    EXEC dbo.sp_WSRollupRange @StationID = @StationID, @From = @From, @To = @To, @AllowDailySummary = 0;
END
GO

-- Fills WSReportHourly from all existing readings, one station-day at a time.
-- Safe to stop and re-run (each day is rebuilt from scratch). Stations keep posting while it runs.
-- Examples:
--   EXEC dbo.sp_WSRollupBackfill;                                         -- everything (allow 10-30+ minutes)
--   EXEC dbo.sp_WSRollupBackfill @StationID = 1, @FromDay = '2026-09-01'; -- one station, from a date
ALTER PROCEDURE dbo.sp_WSRollupBackfill
    @StationID  INT  = NULL,    -- NULL = every station with readings
    @FromDay    DATE = NULL,    -- NULL = the station's first day
    @ToDay      DATE = NULL     -- NULL = the station's latest day (inclusive)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ID INT, @PassKey NVARCHAR(50), @First DATE, @Last DATE, @DataLast DATE, @Day DATE, @Done INT, @Msg NVARCHAR(200),
            @DayStart DATETIME, @DayEnd DATETIME, @Allow BIT;

    DECLARE stations CURSOR LOCAL FAST_FORWARD FOR
        SELECT s.ID, s.Passkey FROM dbo.WSStations s WITH (NOLOCK)
        WHERE (@StationID IS NULL OR s.ID = @StationID)
          AND EXISTS (SELECT 1 FROM dbo.WSReport r WITH (NOLOCK) WHERE r.Passkey = s.Passkey)
        ORDER BY s.ID;

    OPEN stations;
    FETCH NEXT FROM stations INTO @ID, @PassKey;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        SELECT @First = CAST(MIN(DateAdded) AS DATE), @DataLast = CAST(MAX(DateAdded) AS DATE)
        FROM dbo.WSReport WITH (NOLOCK) WHERE Passkey = @PassKey;
        SET @Last = @DataLast;

        IF @FromDay IS NOT NULL AND @FromDay > @First SET @First = @FromDay;
        IF @ToDay IS NOT NULL AND @ToDay < @Last SET @Last = @ToDay;

        SET @Day = @First;
        SET @Done = 0;

        WHILE @Day <= @Last
        BEGIN
            SET @DayStart = CAST(@Day AS DATETIME);
            SET @DayEnd = DATEADD(DAY, 1, @DayStart);
            -- The station's latest day may still be in progress, so never turn it into a daily summary.
            -- (Uses the data, not GETDATE(), because the server clock may not be Brisbane time.)
            SET @Allow = CASE WHEN @Day < @DataLast THEN 1 ELSE 0 END;

            EXEC dbo.sp_WSRollupRange @StationID = @ID, @From = @DayStart, @To = @DayEnd, @AllowDailySummary = @Allow;

            SET @Done += 1;
            IF @Done % 100 = 0
            BEGIN
                SET @Msg = CONCAT('Station ', @ID, ': ', @Done, ' days done (up to ', CONVERT(VARCHAR(10), @Day, 120), ')');
                RAISERROR(@Msg, 0, 1) WITH NOWAIT;
            END

            SET @Day = DATEADD(DAY, 1, @Day);
        END

        SET @Msg = CONCAT('Station ', @ID, ': finished, ', @Done, ' days');
        RAISERROR(@Msg, 0, 1) WITH NOWAIT;

        FETCH NEXT FROM stations INTO @ID, @PassKey;
    END

    CLOSE stations;
    DEALLOCATE stations;
END
GO
