-- Move to SmartASP, step 1: create the Home Weather Hub tables in the NEW (empty) database.
-- Run this on SmartASP only, not on Conetix.
--
-- Matches the Conetix tables column for column (types, NULLs, defaults, identity), read from the live database
-- on 2026-10-05, with these deliberate differences:
--   * Every table is in dbo. (On Conetix, WSData and WSStationSettings are in the "storrs" schema; the API uses
--     unqualified names, which find dbo tables whatever the login's default schema is.)
--   * The duplicate index IDX_WSReport_Passkey_ASC is not created (IDX_WSReport_Passkey_DateAdded covers it).
--   * WSStations gets a unique index on PassKey, and WSStationSettings a primary key (StationID, SettingName),
--     matching how the code already uses them.
--   * Dropped/unused: ErrorLogs (not used by Home Weather Hub), CalendarOrders (another project).
-- Then run, in order: ../migrations/001_sp_WSReportData_DateAdded.sql, ../migrations/002_hourly_rollup.sql,
-- ../migrations/003_purge_old_data.sql.

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE dbo.WSStations (
    ID              INT IDENTITY(1,1) NOT NULL,
    PassKey         NVARCHAR(50)  NOT NULL,
    UserID          INT           NOT NULL CONSTRAINT DF_WSStations_UserID DEFAULT 0,
    StationName     NVARCHAR(50)  NULL,
    SoftwareType    NVARCHAR(50)  NULL,
    WSModel         NVARCHAR(50)  NULL,
    IPAddress       NVARCHAR(50)  NULL,
    SampleData      NVARCHAR(MAX) NULL,
    LastActive      DATETIME      NOT NULL CONSTRAINT DF_WSStations_LastActive DEFAULT GETDATE(),
    Status          SMALLINT      NOT NULL CONSTRAINT DF_WSStations_Status DEFAULT 0,
    Suburb          NVARCHAR(50)  NULL,
    State           NVARCHAR(50)  NULL,
    Country         NVARCHAR(50)  NULL,
    Latitude        FLOAT         NULL,
    Longitude       FLOAT         NULL,
    HasPower        BIT           NOT NULL CONSTRAINT DF_WSStations_HasPower DEFAULT 0,
    CONSTRAINT PK_WSStations PRIMARY KEY CLUSTERED (ID)
);
CREATE UNIQUE NONCLUSTERED INDEX UX_WSStations_PassKey ON dbo.WSStations (PassKey);
GO

CREATE TABLE dbo.WSStationSettings (
    SettingName     NVARCHAR(50)  NOT NULL,
    SettingValue    NVARCHAR(MAX) NULL,
    StationID       INT           NOT NULL CONSTRAINT DF_WSStationSettings_StationID DEFAULT 0,
    CONSTRAINT PK_WSStationSettings PRIMARY KEY CLUSTERED (StationID, SettingName)
);
GO

CREATE TABLE dbo.WSUsers (
    UserID          INT IDENTITY(1,1) NOT NULL,
    Username        NVARCHAR(50)  NOT NULL,
    Password        NVARCHAR(50)  NOT NULL,
    EmailAddress    NVARCHAR(100) NOT NULL,
    AccessLevel     SMALLINT      NOT NULL CONSTRAINT DF_WSUsers_AccessLevel DEFAULT 0,
    CONSTRAINT PK_WSUsers PRIMARY KEY CLUSTERED (UserID)
);
GO

CREATE TABLE dbo.WSReport (
    ID              BIGINT IDENTITY(1,1) NOT NULL,
    DateAdded       DATETIME      NULL CONSTRAINT DF_WSReport_DateAdded DEFAULT GETDATE(), -- the API supplies Brisbane time (migration 001)
    Passkey         NVARCHAR(50)  NULL,
    DateUtc         NVARCHAR(25)  NULL,
    TempInF         REAL          NULL,
    HumidityIn      SMALLINT      NULL,
    BaromRelIn      REAL          NULL,
    BaromAbsIn      REAL          NULL,
    TempOutF        REAL          NULL,
    HumidityOut     SMALLINT      NULL,
    WindDir         SMALLINT      NULL,
    WindSpeedMPH    REAL          NULL,
    WindGustMPH     REAL          NULL,
    MaxDailyGust    REAL          NULL,
    RainRateInch    REAL          NULL,
    EventRainInch   REAL          NULL,
    HourlyRainInch  REAL          NULL,
    DailyRainInch   REAL          NULL,
    WeeklyRainInch  REAL          NULL,
    MonthlyRainIn   REAL          NULL,
    TotalRainInch   REAL          NULL,
    SolarRadiation  REAL          NULL,
    UV              REAL          NULL,
    AddRain         REAL          NOT NULL CONSTRAINT DF_WSReport_AddRain DEFAULT 0,
    CONSTRAINT PK_WSReport PRIMARY KEY CLUSTERED (ID)
);
CREATE NONCLUSTERED INDEX IDX_WSReport_Passkey_DateAdded ON dbo.WSReport (Passkey, DateAdded);
CREATE NONCLUSTERED INDEX IDX_WSReport_DateAdded_ASC ON dbo.WSReport (DateAdded); -- used by the 7-day clean-up
GO

CREATE TABLE dbo.WSData (
    ID              INT IDENTITY(1,1) NOT NULL,
    DateAdded       DATETIME      NOT NULL CONSTRAINT DF_WSData_DateAdded DEFAULT GETDATE(),
    RawData         NVARCHAR(MAX) NULL,
    IPAddress       NVARCHAR(50)  NULL,
    CONSTRAINT PK_WSData PRIMARY KEY CLUSTERED (ID)
);
GO

-- Placeholder so migration 001 (an ALTER PROCEDURE) can run here.
IF OBJECT_ID('dbo.sp_WSReportData', 'P') IS NULL EXEC('CREATE PROCEDURE dbo.sp_WSReportData AS RETURN 0');
GO
