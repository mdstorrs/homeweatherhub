USE [storrs]
GO
/****** Object:  StoredProcedure [dbo].[sp_WSReportData]    Script Date: 4/10/2026 11:05:17 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
ALTER PROCEDURE [dbo].[sp_WSReportData] 
	@Passkey NVARCHAR(50),
	@StationType NVARCHAR(50) = NULL,
	@WSModel NVARCHAR(50) = NULL,
	@IPAddress NVARCHAR(50) = NULL,
	@SampleData NVARCHAR(MAX) = NULL,
	@Status SMALLINT = NULL,
	@LastActive DATETIME = NULL,
	@DateUtc NVARCHAR(25) = NULL,
	@TempInF REAL = NULL,
	@HumidityIn SMALLINT = NULL,
	@BaromRelIn REAL = NULL,
	@BaromAbsIn REAL = NULL,
	@TempOutF REAL = NULL,
	@HumidityOut SMALLINT = NULL,
	@WindDir SMALLINT = NULL,
	@WindSpeedMPH REAL = NULL,
	@WindGustMPH REAL = NULL,
	@MaxDailyGust REAL = NULL,
	@RainRateInch REAL = NULL,
	@EventRainInch REAL = NULL,
	@HourlyRainInch REAL = NULL,
	@DailyRainInch REAL = NULL,
	@WeeklyRainInch REAL = NULL,
	@MonthlyRainIn REAL = NULL,
	@TotalRainInch REAL = NULL,
	@SolarRadiation REAL = NULL,
	@UV REAL = NULL
AS
BEGIN

	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	DECLARE @CurrentStatus INT

	-- Check for a station.
	SET @CurrentStatus = (SELECT Status FROM WSStations WITH(NOLOCK) WHERE Passkey = @Passkey)

	--Station Record doesn't exist. Add it
    IF (@CurrentStatus IS NULL)
    BEGIN
		--If this is the first time posting data, add a new record to the WS Stations. This way, somebody can own it and enable it. 
		--Only add it once, in case somebody is adding shit data.
		INSERT INTO WSStations (PassKey, SoftwareType, WSModel, IPAddress, SampleData, Status) 
        VALUES(@PassKey, @StationType, @WSModel, @IPAddress, @SampleData, 1)

    END
    ELSE --Station Record exists. 
    BEGIN

		IF (@Status IS NULL) 
			SET @Status = @CurrentStatus

		--If the station has already been added, update it with the latest information about the station. Not the weather data.. If it has been claimed, add it in the next section
		UPDATE WSStations 
		SET SoftwareType = @StationType, SampleData = @SampleData, LastActive = @LastActive, IPAddress = @IPAddress, [Status] = @Status
		WHERE PassKey = @PassKey

    END

	--Station has been claimed
	IF (@CurrentStatus = 2)
    BEGIN

		--If the station has been claimed, add the data to the WS Report. All other status are to be ignored.
		INSERT INTO WSReport 
            (Passkey, DateUtc, TempInF, HumidityIn, BaromRelIn, BaromAbsIn, TempOutF, 
                HumidityOut, WindDir, WindSpeedMPH, WindGustMPH, MaxDailyGust, RainRateInch, EventRainInch, HourlyRainInch, 
                DailyRainInch, WeeklyRainInch, MonthlyRainIn, TotalRainInch, SolarRadiation, UV) 
        VALUES (@Passkey, @DateUtc, @TempInF, @HumidityIn, @BaromRelIn, @BaromAbsIn, 
                @TempOutF, @HumidityOut, @WindDir, @WindSpeedMPH, @WindGustMPH, @MaxDailyGust, @RainRateInch, @EventRainInch, 
                @HourlyRainInch, @DailyRainInch, @WeeklyRainInch, @MonthlyRainIn, @TotalRainInch, @SolarRadiation, @UV)

    END

END
