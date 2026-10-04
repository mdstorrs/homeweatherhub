-- Undo migration 002. Removes only what 002 added; WSReport and the raw readings are not touched.
-- The API keeps working: it only calls sp_WSRollupReading when it exists.

IF OBJECT_ID('dbo.sp_WSRollupBackfill', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_WSRollupBackfill;
IF OBJECT_ID('dbo.sp_WSRollupReading', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_WSRollupReading;
IF OBJECT_ID('dbo.sp_WSRollupRange', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_WSRollupRange;
IF OBJECT_ID('dbo.WSReportHourly', 'U') IS NOT NULL DROP TABLE dbo.WSReportHourly;
