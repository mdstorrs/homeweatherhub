USE [storrs]
GO
/****** Object:  StoredProcedure [dbo].[sp_History]    Script Date: 4/10/2026 11:05:22 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
ALTER PROCEDURE [dbo].[sp_History]
(
	@WSID AS INT,
	@FromDate DATETIME,
	@ToDate DATETIME
)
AS
BEGIN
	SET NOCOUNT ON;

	DECLARE @PassKey NVARCHAR(50)
	DECLARE @WSName NVARCHAR(50)

	SELECT @PassKey = Passkey, @WSName = StationName FROM WSStations WITH(NOLOCK) WHERE (ID = @WSID)

	SELECT @PassKey AS PassKey, @WSName AS StationName;

--WITH DailyTempStats AS (
--    SELECT 
--        CAST(DateAdded AS DATE) AS LogDate, 
--        MAX(TempOutF) AS MaxTempOutF,
--        MIN(TempOutF) AS MinTempOutF,
--        MAX(TempInF) AS MaxTempInF,
--        MIN(TempInF) AS MinTempInF
--    FROM WSReport WITH(NOLOCK)
--    WHERE Passkey = @PassKey
--        AND DateAdded BETWEEN @FromDate AND @ToDate
--        AND TempOutF <> 0
--    GROUP BY CAST(DateAdded AS DATE)
--)
SELECT 
    --AVG(MaxTempOutF) AS AvgMaxTempOut,  -- Average of daily max temperatures
    --AVG(MinTempOutF) AS AvgMinTempOut,  -- Average of daily min temperatures
    MAX(TempOutF) AS MaxTempOut, 
    MIN(TempOutF) AS MinTempOut, 

    --AVG(MaxTempInF) AS AvgMaxTempIn,  -- Average of daily max indoor temperatures
    --AVG(MinTempInF) AS AvgMinTempIn,  -- Average of daily min indoor temperatures
    MAX(TempInF) AS MaxTempIn, 
    MIN(TempInF) AS MinTempIn, 

    MAX(WSReport.WindSpeedMPH) AS MaxWind, 
    MAX(WSReport.WindGustMPH) AS MaxWindGust, 
    MAX(WSReport.HumidityOut) AS MaxHumidityOut, 
    MIN(WSReport.HumidityOut) AS MinHumidityOut, 
    MAX(WSReport.HumidityIn) AS MaxHumidityIn, 
    MIN(WSReport.HumidityIn) AS MinHumidityIn, 
    MAX(WSReport.BaromRelIn) AS MaxBarom, 
    MIN(WSReport.BaromRelIn) AS MinBarom, 
    MAX(WSReport.RainRateInch) AS MaxRainRate, 
    MAX(WSReport.UV) AS MaxUV, 
    AVG(WSReport.WindDir) AS AvgWindDir, 
    MAX(WSReport.DailyRainInch) AS MaxDailyRain, 
    MAX(WSReport.WeeklyRainInch) AS MaxWeeklyRain, 
    MAX(WSReport.MonthlyRainIn) AS MaxMonthlyRain, 
    MAX(WSReport.TotalRainInch) AS TotalRain 
FROM WSReport WITH(NOLOCK)
--JOIN DailyTempStats ON CAST(WSReport.DateAdded AS DATE) = DailyTempStats.LogDate
WHERE WSReport.Passkey = @PassKey
    AND WSReport.DateAdded BETWEEN @FromDate AND @ToDate AND TempOutF <> 0
GROUP BY WSReport.Passkey;


SELECT (SUM(MaxDaily.TotalDailyRain)) AS TotalRain FROM (
	SELECT MAX(DailyRainInch) AS TotalDailyRain, DATEADD(DAY, 0, DATEDIFF(DAY, 0, DateAdded)) AS DateAdded 
	FROM WSReport WITH(NOLOCK) 
	WHERE(WSReport.Passkey = @PassKey) AND(WSReport.DateAdded BETWEEN @FromDate AND @ToDate) 
	GROUP BY DATEADD(DAY, 0, DATEDIFF(DAY, 0, DateAdded))) MaxDaily
	
END

