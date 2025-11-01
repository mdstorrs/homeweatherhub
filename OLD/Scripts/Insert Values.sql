DECLARE @DateAdded DATE = '2022-02-26 10:21:36.213'
DECLARE @Passkey NVARCHAR(50) = '64342F32A039AFA8CACC2061B1A77938'
DECLARE @DateUtc NVARCHAR(25) = '2022-02-26 00:21:31'
DECLARE @TempInF REAL = 74.3
DECLARE @HumidityIn SMALLINT = 85
DECLARE @BaromRelIn REAL = 29.717
DECLARE @BaromAbsIn REAL = 29.472
DECLARE @TempOutF REAL = 71.8
DECLARE @HumidityOut SMALLINT = 99
DECLARE @WindDir SMALLINT = 120
DECLARE @WindSpeedMPH REAL = 7.4
DECLARE @WindGustMPH REAL = 9.2
DECLARE @MaxDailyGust REAL = 19.5
DECLARE @RainRateInch REAL = 0.236
DECLARE @EventRainInch REAL = 25.89
DECLARE @HourlyRainInch REAL = 0.11
DECLARE @DailyRainInch REAL = 8.728
DECLARE @WeeklyRainInch REAL = 26.028
DECLARE @MonthlyRainIn REAL = 31.209
DECLARE @TotalRainInch REAL = 166.78
DECLARE @SolarRadiation REAL = 28.14
DECLARE @UV REAL = 0

INSERT INTO WSReport(DateAdded, Passkey, DateUtc, TempInF, HumidityIn, BaromRelIn, BaromAbsIn, TempOutF, HumidityOut, WindDir, WindSpeedMPH, WindGustMPH, MaxDailyGust, RainRateInch, EventRainInch, 
                         HourlyRainInch, DailyRainInch, WeeklyRainInch, MonthlyRainIn, TotalRainInch, SolarRadiation, UV)
VALUES        (@DateAdded, @Passkey, @DateUtc, @TempInF, @HumidityIn, @BaromRelIn, @BaromAbsIn, @TempOutF, @HumidityOut, @WindDir, @WindSpeedMPH, @WindGustMPH, @MaxDailyGust, @RainRateInch, @EventRainInch, 
                         @HourlyRainInch, @DailyRainInch, @WeeklyRainInch, @MonthlyRainIn, @TotalRainInch, @SolarRadiation, @UV)