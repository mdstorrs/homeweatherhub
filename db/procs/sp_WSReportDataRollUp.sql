USE [storrs]
GO
/****** Object:  StoredProcedure [dbo].[sp_WSReportDataRollup]    Script Date: 4/10/2026 10:57:15 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
ALTER PROCEDURE [dbo].[sp_WSReportDataRollup] (
	@PassKey NVARCHAR(50) = '64342F32A039AFA8CACC2061B1A77938',
	@FromDate AS DateTime = '2024-01-01 0:00:00'
)
AS
SET NOCOUNT ON

DECLARE @ToDate DateTime

DECLARE @FirstID BIGINT
DECLARE @LastID BIGINT

DECLARE @MinTempInF REAL
DECLARE @MaxTempInF REAL
DECLARE @MinTempOutF REAL
DECLARE @MaxTempOutF REAL

DECLARE @MinHumidityIn SmallInt
DECLARE @MaxHumidityIn SmallInt
DECLARE @MinHumidityOut SmallInt
DECLARE @MaxHumidityOut SmallInt

DECLARE @MinBaromRelIn REAL
DECLARE @MaxBaromRelIn REAL
DECLARE @MinBaromAbsIn REAL
DECLARE @MaxBaromAbsIn REAL

DECLARE @MaxWindSpeedMPH REAL
DECLARE @MaxWindGustMPH REAL

DECLARE @MaxRainRateInch REAL
DECLARE @MaxSolarRadiation REAL
DECLARE @MaxUV REAL
DECLARE @AvgWindDir AS SmallInt

SET @ToDate = @FromDate + 1

SELECT @FirstID = MIN(ID), @LastID = MAX(ID), 
	  @MaxTempInF = MAX(TempInF), @MinTempInF = MIN(TempInF), 
	  @MaxTempOutF = MAX(TempOutF), @MinTempOutF = MIN(TempOutF),
	  @MaxHumidityIn = MAX(HumidityIn), @MinHumidityIn = MIN(HumidityIn),
	  @MaxHumidityOut = MAX(HumidityOut), @MinHumidityOut = MIN(HumidityOut),
	  @MaxBaromRelIn = MAX(BaromRelIn), @MinBaromRelIn = MIN(BaromRelIn), 
	  @MaxBaromAbsIn = MAX(BaromAbsIn), @MinBaromAbsIn = MIN(BaromAbsIn), 
	  @MaxWindSpeedMPH = MAX(WindSpeedMPH), 
	  @MaxWindGustMPH = MAX(WindGustMPH), 
	  @MaxRainRateInch = MAX(RainRateInch), 
	  @MaxSolarRadiation = MAX(SolarRadiation),
	  @MaxUV = MAX(UV),
	  @AvgWindDir = AVG(WindDir)
FROM WSReport WITH(NOLOCK)
WHERE Passkey = @PassKey AND 
	  DateAdded BETWEEN @FromDate AND @ToDate

UPDATE WSReport SET TempInF = @MinTempInF,
				   TempOutF = @MinTempOutF,
				   HumidityIn = @MinHumidityIn,
				   HumidityOut = @MinHumidityOut,
				   BaromRelIn = @MinBaromRelIn, 
				   BaromAbsIn = @MinBaromAbsIn, 
				   WindDir = @AvgWindDir
WHERE ID = @FirstID

UPDATE WSReport SET TempInF = @MaxTempInF,
				   TempOutF = @MaxTempOutF, 
				   HumidityIn = @MaxHumidityIn,
				   HumidityOut = @MaxHumidityOut, 
				   BaromRelIn = @MaxBaromRelIn, 
				   BaromAbsIn = @MaxBaromAbsIn,
				   WindSpeedMPH = @MaxWindSpeedMPH, 
				   WindGustMPH = @MaxWindGustMPH,
				   RainRateInch = @MaxRainRateInch,
				   SolarRadiation = @MaxSolarRadiation,
				   UV = @MaxUV,
				   WindDir= @AvgWindDir
WHERE ID = @LastID

DELETE FROM WSReport 
WHERE Passkey = @PassKey AND 
	  ID > @FirstID AND ID < @LastID

SELECT @@ROWCOUNT AS RowsDeleted