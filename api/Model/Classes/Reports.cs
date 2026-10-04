using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Model;
//using System.Data.SqlClient;
using System.Drawing;
//using System.Composition;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Http;
using System.Net.Sockets;
using System.Net;

namespace api.Business
{
    public class Reports
    {

        public enum eWSStatus : short
        {
            New = 0, //No Record of this weather station
            Added = 1, //This station has not been authorised yet
            Authorised = 2, //The user has created an account and confirmed that the station is real and theirs
            Disabled = 3, //The user has disabled the WS
            Blocked = 4 //Admin has disabled the WS
        }

        public static CurrentReport GetCurrentReport(int id, BaseReport.MeasurementSystem ms)
        {

            CurrentReport report = new CurrentReport() { WSID = id };

            try
            {

                using (SqlConnection cnn = new SqlConnection(MyData.ConnectionString))
                {

                    using (SqlCommand cmd = new SqlCommand("SELECT TOP (1) WSReport.*, WSStations.PassKey, WSStations.StationName, GETDATE() AS ServerTime FROM WSReport WITH(NOLOCK) INNER JOIN WSStations WITH(NOLOCK) ON WSReport.Passkey = WSStations.Passkey WHERE (WSStations.ID = @ID) ORDER BY WSReport.DateAdded DESC;", cnn))
                    {

                        cnn.Open();
                        cmd.Parameters.AddWithValue("@ID", id);

                        using (SqlDataReader rdr = cmd.ExecuteReader())
                        {

                            if (rdr.Read())
                            {

                                decimal tempOut = decimal.Parse(rdr["TempOutF"].ToString());

                                report.WSName = rdr["StationName"].ToString();
                                report.PassKey = rdr["PassKey"].ToString();
                                report.Measurement = ms;

                                report.HumidityInside = (int.Parse(rdr["HumidityIn"].ToString())) + "%";
                                report.HumidityOutside = (int.Parse(rdr["HumidityOut"].ToString())) + "%";
                                report.ServerTime = (DateTime.Parse(rdr["ServerTime"].ToString()));
                                report.LastUpdated = (DateTime.Parse(rdr["DateAdded"].ToString()));
                                report.WindDirAngle = (int.Parse(rdr["WindDir"].ToString()));
                                report.UVIndex = (int.Parse(rdr["UV"].ToString()));
                                report.WindDirection = WSGlobal.GetWindDirection(report.WindDirAngle);

                                if (ms == BaseReport.MeasurementSystem.Metric)
                                {
                                    report.MeasurementSymbol = "C";
                                    report.TempOutside = Math.Round((decimal.Parse(rdr["TempOutF"].ToString()) - 32) * 5 / 9, 1).ToString("F1");
                                    report.TempInside = Math.Round((decimal.Parse(rdr["TempInF"].ToString()) - 32) * 5 / 9, 1).ToString("F1");
                                    report.Pressure = Math.Round(decimal.Parse(rdr["BaromRelIn"].ToString()) * (decimal)33.863886666667, 0) + " hPa";
                                    report.WindSpeed = Math.Round(decimal.Parse(rdr["WindSpeedMPH"].ToString()) * (decimal)1.609344, 1) + " km/h";
                                    report.WindGust = Math.Round(decimal.Parse(rdr["WindGustMPH"].ToString()) * (decimal)1.609344, 1) + " km/h";
                                    report.RainRate = Math.Round(decimal.Parse(rdr["RainRateInch"].ToString()) * (decimal)25.4, 1) + " mm/h";
                                    report.RainAccumulation = Math.Round(decimal.Parse(rdr["DailyRainInch"].ToString()) * (decimal)25.4, 1) + " mm";
                                }
                                else
                                {
                                    report.MeasurementSymbol = "F";
                                    report.TempOutside = decimal.Parse(rdr["TempOutF"].ToString()).ToString("F1");
                                    report.TempInside = decimal.Parse(rdr["TempInF"].ToString()).ToString("F1");
                                    report.Pressure = decimal.Parse(rdr["BaromRelIn"].ToString()).ToString("F3") + " in";

                                    report.WindSpeed = decimal.Parse(rdr["WindSpeedMPH"].ToString()).ToString("F1") + " mph";
                                    report.WindGust = decimal.Parse(rdr["WindGustMPH"].ToString()).ToString("F1") + " mph";
                                    report.RainRate = decimal.Parse(rdr["RainRateInch"].ToString()).ToString("F3") + " in/h";
                                    report.RainAccumulation = decimal.Parse(rdr["DailyRainInch"].ToString()).ToString("F3") + " in";

                                }

                                if (tempOut <= 32)
                                {
                                    report.TempFeel = "Freezing";
                                }
                                else if (tempOut <= 65)
                                {
                                    report.TempFeel = "Cold";
                                }
                                else if (tempOut > 89)
                                {
                                    report.TempFeel = "Hot";
                                }
                                else if (tempOut > 79)
                                {
                                    report.TempFeel = "Warm";
                                }
                                else
                                {
                                    report.TempFeel = "Normal";
                                }

                                report.Success = true;
                                report.Message = "OK";
                                report.Error = "";

                            }
                            else 
                            {
                                report.Success = false;
                                report.Message = "No Data";
                                report.Error = "";
                            }

                        }

                    }

                }

            }
            catch (Exception ex)
            {
                report.Success = false;
                report.Message = "ERROR";
                report.Error = ServerError(ex);
            }

            return report;

        }

        // Same three result sets as sp_History (station, min/max, total rain), with these fixes:
        //  - half-open range (>= From AND < To) so a reading at midnight isn't counted in two periods;
        //  - wind direction averaged as vectors (350 and 10 degrees average to N, not S),
        //    weighted by wind speed so calm periods don't skew the result.
        // sp_History is left in place, so the previous API version still works if it is redeployed.
        private const string HistoryQuery = @"
DECLARE @PassKey NVARCHAR(50), @WSName NVARCHAR(50);
SELECT @PassKey = Passkey, @WSName = StationName FROM WSStations WITH (NOLOCK) WHERE ID = @WSID;
SELECT @PassKey AS PassKey, @WSName AS StationName;

SELECT MAX(TempOutF) AS MaxTempOut, MIN(TempOutF) AS MinTempOut,
       MAX(TempInF) AS MaxTempIn, MIN(TempInF) AS MinTempIn,
       MAX(WindSpeedMPH) AS MaxWind, MAX(WindGustMPH) AS MaxWindGust,
       MAX(HumidityOut) AS MaxHumidityOut, MIN(HumidityOut) AS MinHumidityOut,
       MAX(HumidityIn) AS MaxHumidityIn, MIN(HumidityIn) AS MinHumidityIn,
       MAX(BaromRelIn) AS MaxBarom, MIN(BaromRelIn) AS MinBarom,
       MAX(RainRateInch) AS MaxRainRate, MAX(UV) AS MaxUV,
       SUM(WindSpeedMPH * SIN(RADIANS(CAST(WindDir AS FLOAT)))) AS WindSinWeighted,
       SUM(WindSpeedMPH * COS(RADIANS(CAST(WindDir AS FLOAT)))) AS WindCosWeighted,
       SUM(SIN(RADIANS(CAST(WindDir AS FLOAT)))) AS WindSin,
       SUM(COS(RADIANS(CAST(WindDir AS FLOAT)))) AS WindCos
FROM WSReport WITH (NOLOCK)
WHERE Passkey = @PassKey AND DateAdded >= @FromDate AND DateAdded < @ToDate
      AND TempOutF <> 0 -- kept from sp_History: zero readings come from a station that is offline
GROUP BY Passkey;

SELECT SUM(MaxDaily.TotalDailyRain) AS TotalRain FROM (
    SELECT MAX(DailyRainInch) AS TotalDailyRain
    FROM WSReport WITH (NOLOCK)
    WHERE Passkey = @PassKey AND DateAdded >= @FromDate AND DateAdded < @ToDate
    GROUP BY CAST(DateAdded AS DATE)) MaxDaily;";

        private static int AverageWindDirection(SqlDataReader rdr)
        {
            double Read(string column) => rdr[column] == DBNull.Value ? 0 : Convert.ToDouble(rdr[column]);

            double sin = Read("WindSinWeighted");
            double cos = Read("WindCosWeighted");

            // No wind at all over the period: fall back to the unweighted vector average.
            if (Math.Abs(sin) < 1e-9 && Math.Abs(cos) < 1e-9)
            {
                sin = Read("WindSin");
                cos = Read("WindCos");
            }

            if (Math.Abs(sin) < 1e-9 && Math.Abs(cos) < 1e-9)
            {
                return 0;
            }

            double degrees = Math.Atan2(sin, cos) * 180.0 / Math.PI;
            return (int)Math.Round((degrees + 360.0) % 360.0) % 360;
        }

        public static HistoryReport GetHistoryReport(int id, int rep, string dateString, BaseReport.MeasurementSystem ms)
        {

            HistoryReport report = GetDateRange(rep, dateString);
            
            try
            {

                if (report.Success==false)
                {
                    return report;
                }

                using (SqlConnection cnn = new SqlConnection(MyData.ConnectionString))
                using (SqlCommand cmd = new SqlCommand(HistoryQuery, cnn))
                {

                    cmd.Parameters.AddWithValue("@WSID", id);
                    cmd.Parameters.AddWithValue("@FromDate", report.StartDate);
                    cmd.Parameters.AddWithValue("@ToDate", report.EndDate);

                    cnn.Open();

                    using (SqlDataReader rdr = cmd.ExecuteReader())
                    {

                        if (rdr.Read())
                        {

                            report.WSID = id;
                            report.PassKey = rdr["PassKey"].ToString();
                            report.WSName = rdr["StationName"].ToString();
                            report.Measurement = ms;

                        }
                        else
                        {
                            //No Data
                            return new HistoryReport { Success = false, Message = "No data for the selected range" };
                        }

                        if (rdr.NextResult() == true)
                        {

                            if (rdr.Read())
                            {

                                report.InsideHumidityMin = (int.Parse(rdr["minHumidityIn"].ToString())) + "%";
                                report.InsideHumidityMax = (int.Parse(rdr["maxHumidityIn"].ToString())) + "%";
                                report.OutsideHumidityMin = (int.Parse(rdr["minHumidityOut"].ToString())) + "%";
                                report.OutsideHumidityMax = (int.Parse(rdr["maxHumidityOut"].ToString())) + "%";
                                report.UVIndexMax = (int.Parse(rdr["maxUV"].ToString()));
                                report.WindDirectionAngleAvg = AverageWindDirection(rdr);
                                report.WindDirectionAvg = WSGlobal.GetWindDirection(report.WindDirectionAngleAvg);

                                if (ms == BaseReport.MeasurementSystem.Metric)
                                {

                                    report.MeasurementSymbol = "C";
                                    report.OutsideTemperatureMin = Math.Round((decimal.Parse(rdr["MinTempOut"].ToString()) - 32) * 5 / 9, 1).ToString("F1");
                                    report.OutsideTemperatureMax = Math.Round((decimal.Parse(rdr["MaxTempOut"].ToString()) - 32) * 5 / 9, 1).ToString("F1");
                                    report.InsideTemperatureMin = Math.Round((decimal.Parse(rdr["MinTempIn"].ToString()) - 32) * 5 / 9, 1).ToString("F1");
                                    report.InsideTemperatureMax = Math.Round((decimal.Parse(rdr["MaxTempIn"].ToString()) - 32) * 5 / 9, 1).ToString("F1");
                                    report.PressureMin = Math.Round(decimal.Parse(rdr["minBarom"].ToString()) * (decimal)33.863886666667, 0) + " hPa";
                                    report.PressureMax = Math.Round(decimal.Parse(rdr["maxBarom"].ToString()) * (decimal)33.863886666667, 0) + " hPa";
                                    report.WindSpeedMax = Math.Round(decimal.Parse(rdr["MaxWind"].ToString()) * (decimal)1.609344, 1) + " km/h";
                                    report.WindGustMax = Math.Round(decimal.Parse(rdr["MaxWindGust"].ToString()) * (decimal)1.609344, 1) + " km/h";
                                    report.RainRateMax = Math.Round(decimal.Parse(rdr["MaxRainRate"].ToString()) * (decimal)25.4, 1) + " mm/h";

                                }
                                else
                                {

                                    report.MeasurementSymbol = "F";
                                    report.OutsideTemperatureMin = decimal.Parse(rdr["MinTempOut"].ToString()).ToString("F1");
                                    report.OutsideTemperatureMax = decimal.Parse(rdr["MaxTempOut"].ToString()).ToString("F1");
                                    report.InsideTemperatureMin = decimal.Parse(rdr["MinTempIn"].ToString()).ToString("F1");
                                    report.InsideTemperatureMax = decimal.Parse(rdr["MaxTempIn"].ToString()).ToString("F1");
                                    report.PressureMin = decimal.Parse(rdr["minBarom"].ToString()).ToString("F3") + " in";
                                    report.PressureMax = decimal.Parse(rdr["maxBarom"].ToString()).ToString("F3") + " in";
                                    report.WindSpeedMax = decimal.Parse(rdr["MaxWind"].ToString()).ToString("F1") + " mph";
                                    report.WindGustMax = decimal.Parse(rdr["MaxWindGust"].ToString()).ToString("F1") + " mph";
                                    report.RainRateMax = decimal.Parse(rdr["MaxRainRate"].ToString()).ToString("F3") + " in/h";

                                }

                            }

                        }

                        if (rdr.NextResult() == true)
                        {

                            if (rdr.Read())
                            {

                                if (rdr.IsDBNull(0))
                                    report.TotalRain = null;
                                else
                                {
                                    if (ms == BaseReport.MeasurementSystem.Metric)
                                        report.TotalRain = Math.Round(decimal.Parse(rdr["TotalRain"].ToString()) * (decimal)25.4, 1) + " mm";
                                    else
                                        report.TotalRain = decimal.Parse(rdr["TotalRain"].ToString()).ToString("F3") + " in";
                                }
                            }
                            else
                            {
                                report.TotalRain = "N/a";
                            }

                        }

                    }

                }

            }
            catch (Exception ex)
            {
                report.Success = false;
                report.Message = "ERROR";
                report.Error = ServerError(ex);
            }

            return report;

        }

        public static HistoryReport GetDateRange(int rep, string dateString) 
        {

            DateTime reportDate;

            if (DateTime.TryParse(dateString, out reportDate) == false)
            {
                return new HistoryReport { Success = false, Message = "Invalid Date" };
            }

            int month = reportDate.Month;
            int year = reportDate.Year;

            DateTime startDate;
            DateTime endDate;

            switch ((BaseReport.ReportType)rep)
            {
                case BaseReport.ReportType.Day:
                    startDate = reportDate.Date;
                    endDate = startDate.AddDays(1);
                    break;
                case BaseReport.ReportType.Week:
                    // Weeks run Monday to Sunday, matching the app and website (which send a Monday).
                    startDate = reportDate.Date.AddDays(-(((int)reportDate.DayOfWeek + 6) % 7));
                    endDate = startDate.AddDays(7);
                    break;
                case BaseReport.ReportType.Month:
                    startDate = new DateTime(year, month, 1);
                    endDate = startDate.AddMonths(1);
                    break;
                case BaseReport.ReportType.Year:
                    startDate = new DateTime(year, 1, 1);
                    endDate = startDate.AddYears(1);
                    break;
                case BaseReport.ReportType.All:
                    startDate = new DateTime(2000, 1, 1);
                    endDate = startDate.AddYears(100);
                    break;
                default:
                    return new HistoryReport { Success = false, Message = "Invalid Date Range" };
            }

            return new HistoryReport { Success = true, Message = "OK", StartDate = startDate, EndDate = endDate, Type = (BaseReport.ReportType)rep };

        }

        public static StationList GetAllStations(string filter, int page, int stationsPerPage, int stationId = 0)
        {
            StationList stations = new StationList();
            stations.Stations = new List<Station>();

            if (page <= 0) page = 1; // Handle invalid page numbers
            if (stationsPerPage <= 0) stationsPerPage = 10; // Handle invalid stations per page

            int skip = (page - 1) * stationsPerPage;  // Calculate how many rows to skip

            try
            {
                using (SqlConnection cnn = new SqlConnection(MyData.ConnectionString))
                {

                    string command = @"
                        WITH PagedStations AS
                        (
                            SELECT Id, StationName, Suburb, State, Country, Latitude, Longitude, HasPower
                            FROM WSStations WITH(NOLOCK)
                            WHERE UserID > 0
                            ORDER BY StationName
                            OFFSET @Skip ROWS FETCH NEXT @StationsPerPage ROWS ONLY
                        )
                        SELECT ps.Id, ps.StationName, ps.Suburb, ps.State, ps.Country, ps.Latitude, ps.Longitude, ps.HasPower,
                               ss.SettingName, ss.SettingValue
                        FROM PagedStations ps
                        LEFT JOIN WSStationSettings ss WITH(NOLOCK) ON ss.StationID = ps.Id
                        ORDER BY ps.StationName, ss.SettingName;";

                    if (stationId > 0)
                    {
                        command = @"
                        SELECT s.Id, s.StationName, s.Suburb, s.State, s.Country, s.Latitude, s.Longitude, s.HasPower,
                               ss.SettingName, ss.SettingValue
                        FROM WSStations s WITH(NOLOCK)
                        LEFT JOIN WSStationSettings ss WITH(NOLOCK) ON ss.StationID = s.Id
                        WHERE s.UserID > 0 AND s.Id = @StationID
                        ORDER BY s.StationName, ss.SettingName;";
                    }
                    else if (!string.IsNullOrWhiteSpace(filter))
                    {
                        command = @"
                        WITH PagedStations AS
                        (
                            SELECT Id, StationName, Suburb, State, Country, Latitude, Longitude, HasPower
                            FROM WSStations WITH(NOLOCK)
                            WHERE UserID > 0 AND StationName LIKE @Filter
                            ORDER BY StationName
                            OFFSET @Skip ROWS FETCH NEXT @StationsPerPage ROWS ONLY
                        )
                        SELECT ps.Id, ps.StationName, ps.Suburb, ps.State, ps.Country, ps.Latitude, ps.Longitude, ps.HasPower,
                               ss.SettingName, ss.SettingValue
                        FROM PagedStations ps
                        LEFT JOIN WSStationSettings ss WITH(NOLOCK) ON ss.StationID = ps.Id
                        ORDER BY ps.StationName, ss.SettingName;";
                    }

                    using (SqlCommand cmd = new SqlCommand(command, cnn)) // SQL Server pagination

                    {
                        cnn.Open();

                        if (stationId > 0)
                        {
                            cmd.Parameters.AddWithValue("@StationID", stationId);
                        }
                        else
                        {
                            cmd.Parameters.AddWithValue("@Skip", skip);
                            cmd.Parameters.AddWithValue("@StationsPerPage", stationsPerPage);

                            if (!string.IsNullOrWhiteSpace(filter))
                            {
                                cmd.Parameters.AddWithValue("@Filter", $"%{filter}%");
                            }
                        }

                        using (SqlDataReader rdr = cmd.ExecuteReader())
                        {
                            Dictionary<int, Station> stationMap = new Dictionary<int, Station>();

                            while (rdr.Read())
                            {
                                int currentStationId = (int)rdr["Id"];

                                if (!stationMap.TryGetValue(currentStationId, out Station station))
                                {
                                    station = new Station();
                                    station.Id = currentStationId;
                                    station.Name = rdr["StationName"].ToString();
                                    station.Address = $"{rdr["Suburb"].ToString()} {rdr["State"].ToString()}, {rdr["Country"].ToString()}";
                                    station.Suburb = rdr["Suburb"].ToString();
                                    station.State = rdr["State"].ToString();
                                    station.Country = rdr["Country"].ToString();
                                    station.Coordinates = $"{rdr["Latitude"].ToString()}, {rdr["Longitude"].ToString()}";
                                    station.HasPower = (bool)rdr["HasPower"];

                                    stationMap.Add(currentStationId, station);
                                    stations.Stations.Add(station);
                                }

                                if (!rdr.IsDBNull(rdr.GetOrdinal("SettingName")) && !rdr.IsDBNull(rdr.GetOrdinal("SettingValue")))
                                {
                                    station.Settings.Add(new KeyValuePair<string, string>(
                                        rdr["SettingName"].ToString(),
                                        rdr["SettingValue"].ToString()));
                                }
                            }
                        }
                    }

                    // Get the total count for pagination information (Important!)
                    // Must use the same WHERE as the page query, or searches report the wrong number of pages.
                    string countCommand;
                    if (stationId > 0)
                        countCommand = "SELECT COUNT(*) FROM WSStations WITH(NOLOCK) WHERE UserID > 0 AND ID = @StationID";
                    else if (!string.IsNullOrWhiteSpace(filter))
                        countCommand = "SELECT COUNT(*) FROM WSStations WITH(NOLOCK) WHERE UserID > 0 AND StationName LIKE @Filter";
                    else
                        countCommand = "SELECT COUNT(*) FROM WSStations WITH(NOLOCK) WHERE UserID > 0";

                    using (SqlCommand countCmd = new SqlCommand(countCommand, cnn))
                    {
                        if (stationId > 0)
                        {
                            countCmd.Parameters.AddWithValue("@StationID", stationId);
                        }
                        else if (!string.IsNullOrWhiteSpace(filter))
                        {
                            countCmd.Parameters.AddWithValue("@Filter", $"%{filter}%");
                        }

                        int totalStations = (int)countCmd.ExecuteScalar();
                        stations.TotalCount = totalStations; // Add total count to your StationList class
                        stations.TotalPages = totalStations > 0 ? (int)Math.Ceiling((double)totalStations / stationsPerPage) : 0; // Calculate total pages
                    }


                    stations.Success = stations.Stations.Count > 0;
                    stations.Message = stations.Success ? "OK" : "No Data";
                    stations.Error = "";

                }
            }
            catch (Exception ex)
            {
                stations.Success = false;
                stations.Message = "ERROR";
                stations.Error = ServerError(ex);
            }

            return stations;
        }

        public static ResponseClass AddStation(StationUpsertRequest station)
        {
            ResponseClass response = new ResponseClass();

            if (station == null)
            {
                response.Success = false;
                response.Message = "ERROR";
                response.Error = "No station provided";
                return response;
            }

            if (string.IsNullOrWhiteSpace(station.Name))
            {
                response.Success = false;
                response.Message = "ERROR";
                response.Error = "Station name is required";
                return response;
            }

            try
            {
                ParseAddress(station.Address, out string suburb, out string state, out string country);
                ParseCoordinates(station.Coordinates, out string latitude, out string longitude);

                using (SqlConnection cnn = new SqlConnection(MyData.ConnectionString))
                using (SqlCommand cmd = new SqlCommand(@"
INSERT INTO WSStations (StationName, Suburb, State, Country, Latitude, Longitude, HasPower, UserID)
VALUES (@StationName, @Suburb, @State, @Country, @Latitude, @Longitude, @HasPower, @UserID);", cnn))
                {
                    cnn.Open();

                    cmd.Parameters.AddWithValue("@StationName", station.Name.Trim());
                    cmd.Parameters.AddWithValue("@Suburb", suburb);
                    cmd.Parameters.AddWithValue("@State", state);
                    cmd.Parameters.AddWithValue("@Country", country);
                    cmd.Parameters.AddWithValue("@Latitude", latitude);
                    cmd.Parameters.AddWithValue("@Longitude", longitude);
                    cmd.Parameters.AddWithValue("@HasPower", station.HasPower);
                    cmd.Parameters.AddWithValue("@UserID", 1);

                    cmd.ExecuteNonQuery();
                }

                response.Success = true;
                response.Message = "OK";
                response.Error = "";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "ERROR";
                response.Error = ServerError(ex);
            }

            return response;
        }

        public static ResponseClass UpdateStation(StationUpsertRequest station)
        {
            ResponseClass response = new ResponseClass();

            if (station == null || station.Id <= 0)
            {
                response.Success = false;
                response.Message = "ERROR";
                response.Error = "Invalid station id";
                return response;
            }

            if (string.IsNullOrWhiteSpace(station.Name))
            {
                response.Success = false;
                response.Message = "ERROR";
                response.Error = "Station name is required";
                return response;
            }

            try
            {
                ParseAddress(station.Address, out string suburb, out string state, out string country);
                ParseCoordinates(station.Coordinates, out string latitude, out string longitude);

                using (SqlConnection cnn = new SqlConnection(MyData.ConnectionString))
                using (SqlCommand cmd = new SqlCommand(@"
UPDATE WSStations
SET StationName = @StationName,
    Suburb = @Suburb,
    State = @State,
    Country = @Country,
    Latitude = @Latitude,
    Longitude = @Longitude,
    HasPower = @HasPower
WHERE ID = @ID;", cnn))
                {
                    cnn.Open();

                    cmd.Parameters.AddWithValue("@ID", station.Id);
                    cmd.Parameters.AddWithValue("@StationName", station.Name.Trim());
                    cmd.Parameters.AddWithValue("@Suburb", suburb);
                    cmd.Parameters.AddWithValue("@State", state);
                    cmd.Parameters.AddWithValue("@Country", country);
                    cmd.Parameters.AddWithValue("@Latitude", latitude);
                    cmd.Parameters.AddWithValue("@Longitude", longitude);
                    cmd.Parameters.AddWithValue("@HasPower", station.HasPower);

                    int rows = cmd.ExecuteNonQuery();
                    if (rows == 0)
                    {
                        response.Success = false;
                        response.Message = "ERROR";
                        response.Error = "Station not found";
                        return response;
                    }
                }

                response.Success = true;
                response.Message = "OK";
                response.Error = "";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "ERROR";
                response.Error = ServerError(ex);
            }

            return response;
        }

        private static void ParseAddress(string address, out string suburb, out string state, out string country)
        {
            suburb = string.Empty;
            state = string.Empty;
            country = string.Empty;

            if (string.IsNullOrWhiteSpace(address))
            {
                return;
            }

            string[] commaParts = address.Split(',', StringSplitOptions.TrimEntries);
            if (commaParts.Length > 1)
            {
                country = commaParts[1];
            }

            string[] leftParts = commaParts[0].Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (leftParts.Length > 0)
            {
                state = leftParts[leftParts.Length - 1];
                suburb = string.Join(' ', leftParts.Take(leftParts.Length - 1));
            }
            else
            {
                suburb = commaParts[0];
            }
        }

        private static void ParseCoordinates(string coordinates, out string latitude, out string longitude)
        {
            latitude = string.Empty;
            longitude = string.Empty;

            if (string.IsNullOrWhiteSpace(coordinates))
            {
                return;
            }

            string[] parts = coordinates.Split(',', StringSplitOptions.TrimEntries);
            if (parts.Length > 0)
            {
                latitude = parts[0];
            }

            if (parts.Length > 1)
            {
                longitude = parts[1];
            }
        }

        public static ResponseClass UpsertStationSettings(StationSettingsUpdateRequest request)
        {
            ResponseClass response = new ResponseClass();

            if (request == null || request.Id <= 0)
            {
                response.Success = false;
                response.Message = "ERROR";
                response.Error = "Invalid station id";
                return response;
            }

            if (request.Settings == null || request.Settings.Count == 0)
            {
                response.Success = false;
                response.Message = "ERROR";
                response.Error = "No settings provided";
                return response;
            }

            try
            {
                using (SqlConnection cnn = new SqlConnection(MyData.ConnectionString))
                {
                    cnn.Open();

                    using (SqlTransaction txn = cnn.BeginTransaction())
                    {
                        string updateStationCommand = @"
UPDATE WSStations
SET StationName = @StationName,
    Suburb = @Suburb,
    State = @State,
    Country = @Country,
    Latitude = @Latitude,
    Longitude = @Longitude,
    HasPower = @HasPower
WHERE ID = @ID;";

                        using (SqlCommand stationCmd = new SqlCommand(updateStationCommand, cnn, txn))
                        {
                            string latitude = string.Empty;
                            string longitude = string.Empty;

                            ParseCoordinates(request.Coordinates, out latitude, out longitude);

                            stationCmd.Parameters.AddWithValue("@ID", request.Id);
                            stationCmd.Parameters.AddWithValue("@StationName", request.Name ?? string.Empty);
                            stationCmd.Parameters.AddWithValue("@Suburb", request.Suburb ?? string.Empty);
                            stationCmd.Parameters.AddWithValue("@State", request.State ?? string.Empty);
                            stationCmd.Parameters.AddWithValue("@Country", request.Country ?? string.Empty);
                            stationCmd.Parameters.AddWithValue("@Latitude", latitude);
                            stationCmd.Parameters.AddWithValue("@Longitude", longitude);
                            stationCmd.Parameters.AddWithValue("@HasPower", request.HasPower);

                            int stationRows = stationCmd.ExecuteNonQuery();
                            if (stationRows == 0)
                            {
                                txn.Rollback();
                                response.Success = false;
                                response.Message = "ERROR";
                                response.Error = "Station not found";
                                return response;
                            }
                        }

                        string settingsCommand = @"
MERGE WSStationSettings AS target
USING (SELECT @StationID AS StationID, @SettingName AS SettingName, @SettingValue AS SettingValue) AS source
ON target.StationID = source.StationID AND target.SettingName = source.SettingName
WHEN MATCHED THEN
    UPDATE SET SettingValue = source.SettingValue
WHEN NOT MATCHED THEN
    INSERT (StationID, SettingName, SettingValue)
    VALUES (source.StationID, source.SettingName, source.SettingValue);";

                        using (SqlCommand settingsCmd = new SqlCommand(settingsCommand, cnn, txn))
                        {
                            settingsCmd.Parameters.Add("@StationID", System.Data.SqlDbType.Int);
                            settingsCmd.Parameters.Add("@SettingName", System.Data.SqlDbType.NVarChar, 200);
                            settingsCmd.Parameters.Add("@SettingValue", System.Data.SqlDbType.NVarChar, -1);

                            foreach (var setting in request.Settings)
                            {
                                if (string.IsNullOrWhiteSpace(setting.Key))
                                {
                                    continue;
                                }

                                settingsCmd.Parameters["@StationID"].Value = request.Id;
                                settingsCmd.Parameters["@SettingName"].Value = setting.Key.Trim();
                                settingsCmd.Parameters["@SettingValue"].Value = setting.Value ?? string.Empty;
                                settingsCmd.ExecuteNonQuery();
                            }
                        }

                        txn.Commit();
                    }
                }

                response.Success = true;
                response.Message = "OK";
                response.Error = "";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "ERROR";
                response.Error = ServerError(ex);
            }

            return response;
        }

        public static void SubmitWSData(string passKey, string ipAddress, string? stationType, string? wsModel, string sampleData,
                                        string? dateutc, string? tempinf, string? humidityin, string? baromrelin, string? baromabsin, string? tempf,
                                        string? humidity, string? winddir, string? windspeedmph, string? windgustmph, string? maxdailygust,
                                        string? rainratein, string? eventrainin, string? hourlyrainin, string? dailyrainin, string? weeklyrainin,
                                        string? monthlyrainin, string? totalrainin, string? solarradiation, string? uv)
        {

            //Check data validity
            if (humidity == "0")
            {
                return; //Invalid Data. Sometimes this can be zero when the PWS is offline.
            }

            using (SqlConnection cnn = new SqlConnection(MyData.ConnectionString))
            {

                using (SqlCommand cmd = new SqlCommand("sp_WSReportData", cnn))
                {
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@PassKey", passKey);
                    cmd.Parameters.AddWithValue("@StationType", DbValue(stationType));
                    cmd.Parameters.AddWithValue("@WSModel", DbValue(wsModel));
                    cmd.Parameters.AddWithValue("@IPAddress", ipAddress);
                    cmd.Parameters.AddWithValue("@SampleData", sampleData);
                    cmd.Parameters.AddWithValue("@LastActive", DateTime.Now);
                    cmd.Parameters.AddWithValue("@DateUtc", DbValue(dateutc));
                    cmd.Parameters.AddWithValue("@TempInF", DbValue(tempinf));
                    cmd.Parameters.AddWithValue("@HumidityIn", DbValue(humidityin));
                    cmd.Parameters.AddWithValue("@BaromRelIn", DbValue(baromrelin));
                    cmd.Parameters.AddWithValue("@BaromAbsIn", DbValue(baromabsin));
                    cmd.Parameters.AddWithValue("@TempOutF", DbValue(tempf));
                    cmd.Parameters.AddWithValue("@HumidityOut", DbValue(humidity));
                    cmd.Parameters.AddWithValue("@WindDir", DbValue(winddir));
                    cmd.Parameters.AddWithValue("@WindSpeedMPH", DbValue(windspeedmph));
                    cmd.Parameters.AddWithValue("@WindGustMPH", DbValue(windgustmph));
                    cmd.Parameters.AddWithValue("@MaxDailyGust", DbValue(maxdailygust));
                    cmd.Parameters.AddWithValue("@RainRateInch", DbValue(rainratein));
                    cmd.Parameters.AddWithValue("@EventRainInch", DbValue(eventrainin));
                    cmd.Parameters.AddWithValue("@HourlyRainInch", DbValue(hourlyrainin));
                    cmd.Parameters.AddWithValue("@DailyRainInch", DbValue(dailyrainin));
                    cmd.Parameters.AddWithValue("@WeeklyRainInch", DbValue(weeklyrainin));
                    cmd.Parameters.AddWithValue("@MonthlyRainIn", DbValue(monthlyrainin));
                    cmd.Parameters.AddWithValue("@TotalRainInch", DbValue(totalrainin));
                    cmd.Parameters.AddWithValue("@SolarRadiation", DbValue(solarradiation));
                    cmd.Parameters.AddWithValue("@UV", DbValue(uv));

                    cnn.Open();

                    cmd.ExecuteNonQuery();

                }

            }

        }

        // Log the full exception server-side (WSData) and return a generic message,
        // so database details never reach the app or website.
        private static string ServerError(Exception ex, [System.Runtime.CompilerServices.CallerMemberName] string caller = "")
        {
            WSData.SaveRawData($"API error in {caller}: {ex}", "API");
            return "Server error. Please try again later.";
        }

        // Missing fields are stored as NULL. AddWithValue with a plain null fails with "parameter not supplied".
        private static object DbValue(string? value) => value == null ? DBNull.Value : value;

        public static string GetIP(HttpRequest request, ConnectionInfo connection)
        {
            string ipAddress = null;

            //HttpContext.Request
            //HttpContext.Connection

            // Check for X-Forwarded-For header first (if behind a proxy)
            string forwardedFor = request.Headers["X-Forwarded-For"].FirstOrDefault();

            if (!string.IsNullOrEmpty(forwardedFor))
            {
                // Split the header value and take the first IP address
                string[] forwardedIps = forwardedFor.Split(',');
                foreach (string forwardedIp in forwardedIps)
                {
                    if (IPAddress.TryParse(forwardedIp.Trim(), out IPAddress parsedIp) && parsedIp.AddressFamily == AddressFamily.InterNetwork)
                    {
                        ipAddress = parsedIp.ToString();
                        break;
                    }
                }
            }

            // Fallback to RemoteIpAddress if X-Forwarded-For is not available or doesn't contain IPv4
            if (string.IsNullOrEmpty(ipAddress) && connection.RemoteIpAddress?.AddressFamily == AddressFamily.InterNetwork)
            {
                ipAddress = connection.RemoteIpAddress.ToString();
            }

            System.Diagnostics.Debug.WriteLine($"IPv4 address: {ipAddress}");

            if (ipAddress != null)
            {
                return ipAddress;
            }
            else
            {
                return "IPv4 address not found";
            }
        }

    }

}
