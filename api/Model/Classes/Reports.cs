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
                report.Error = ex.Message;
            }

            return report;

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
                using (SqlCommand cmd = new SqlCommand("sp_History", cnn))
                {

                    cmd.CommandType = System.Data.CommandType.StoredProcedure;
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
                                report.WindDirectionAngleAvg = (int.Parse(rdr["avgWindDir"].ToString()));
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
                                        report.TotalRain = decimal.Parse(rdr["TotalRain"].ToString()).ToString("F3") + " in/h";
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
                report.Error = ex.Message;
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
                    startDate = reportDate.AddDays(-(int)reportDate.DayOfWeek);
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

        public static StationList GetAllStations(string filter, int page, int stationsPerPage)
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
                        SELECT Id, StationName, Suburb, State, Country, Latitude, Longitude
                        FROM WSStations WITH(NOLOCK)
                        ORDER BY StationName
                        OFFSET @Skip ROWS FETCH NEXT @StationsPerPage ROWS ONLY;";

                    if (!string.IsNullOrWhiteSpace(filter))
                    {
                        command = @"
                        SELECT Id, StationName, Suburb, State, Country, Latitude, Longitude
                        FROM WSStations WITH(NOLOCK)
                        WHERE StationName LIKE @Filter
                        ORDER BY StationName
                        OFFSET @Skip ROWS FETCH NEXT @StationsPerPage ROWS ONLY;";
                    }

                    using (SqlCommand cmd = new SqlCommand(command, cnn)) // SQL Server pagination

                    {
                        cnn.Open();
                        cmd.Parameters.AddWithValue("@Skip", skip);
                        cmd.Parameters.AddWithValue("@StationsPerPage", stationsPerPage);

                        if (!string.IsNullOrWhiteSpace(filter))
                        {
                            cmd.Parameters.AddWithValue("@Filter", $"%{filter}%");
                        }

                        using (SqlDataReader rdr = cmd.ExecuteReader())
                        {
                            while (rdr.Read())
                            {
                                Station station = new Station();
                                station.Id = (int)rdr["Id"];
                                station.Name = rdr["StationName"].ToString();
                                station.Address = $"{rdr["Suburb"].ToString()} {rdr["State"].ToString()}, {rdr["Country"].ToString()}";
                                station.Coordinates = $"{rdr["Latitude"].ToString()}, {rdr["Longitude"].ToString()}";
                                stations.Stations.Add(station);
                            }
                        }
                    }

                    // Get the total count for pagination information (Important!)
                    using (SqlCommand countCmd = new SqlCommand("SELECT COUNT(*) FROM WSStations WITH(NOLOCK)", cnn))
                    {
                        int totalStations = (int)countCmd.ExecuteScalar();
                        stations.TotalCount = totalStations; // Add total count to your StationList class
                        stations.TotalPages = (int)Math.Ceiling((double)totalStations / stationsPerPage); // Calculate total pages
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
                stations.Error = ex.Message;
            }

            return stations;
        }

        public static void CheckForStation(string passKey, ref eWSStatus status)
        {

            using (SqlConnection cnn = new SqlConnection(MyData.ConnectionString))
            {

                using (SqlCommand cmd = new SqlCommand("SELECT Status FROM WSStations WITH(NOLOCK) WHERE (Passkey = @Passkey)", cnn))
                {

                    cnn.Open();

                    cmd.Parameters.AddWithValue("@PassKey", passKey);

                    short? result;

                    result = (short?)cmd.ExecuteScalar();

                    if (result == null)
                    {
                        status = eWSStatus.New;
                    }
                    else
                    {
                        status = (eWSStatus)result;
                    }

                }

            }

        }

        public static bool AddStation(string passKey, string ipAddress, string softwareType, string wsModel, string sampleData)
        {

            using (SqlConnection
                cnn = new SqlConnection(MyData.ConnectionString))
            {

                using (SqlCommand
                    cmd = new SqlCommand("INSERT INTO WSStations (PassKey, SoftwareType, WSModel, IPAddress, SampleData, Status) " +
                                                               "VALUES(@PassKey, @SoftwareType, @WSModel, @IPAddress, @SampleData, @Status)", cnn))
                {

                    cnn.Open();

                    cmd.Parameters.AddWithValue("@PassKey", passKey);
                    cmd.Parameters.AddWithValue("@SoftwareType", softwareType);
                    cmd.Parameters.AddWithValue("@WSModel", wsModel);
                    cmd.Parameters.AddWithValue("@IPAddress", ipAddress);
                    cmd.Parameters.AddWithValue("@SampleData", sampleData);
                    cmd.Parameters.AddWithValue("@Status", eWSStatus.Added);

                    if (cmd.ExecuteNonQuery() > 0)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }

                }

            }

        }

        public static bool UpdateStation(string passKey, string ipAddress, string softwareType, string sampleData)
        {

            if (passKey == String.Empty || ipAddress == String.Empty)
            {
                return false;
            }

            using (SqlConnection
                cnn = new SqlConnection(MyData.ConnectionString))
            {

                using (SqlCommand
                    cmd = new SqlCommand("UPDATE WSStations " +
                                                                "SET SoftwareType = @SoftwareType, SampleData = @SampleData, LastActive = @LastActive, IPAddress = @IPAddress " +
                                                                "WHERE PassKey = @PassKey", cnn))
                {

                    cnn.Open();

                    cmd.Parameters.AddWithValue("@PassKey", passKey);
                    cmd.Parameters.AddWithValue("@SoftwareType", softwareType);
                    cmd.Parameters.AddWithValue("@IPAddress", ipAddress);
                    cmd.Parameters.AddWithValue("@SampleData", sampleData);
                    cmd.Parameters.AddWithValue("@LastActive", DateTime.Now);

                    if (cmd.ExecuteNonQuery() > 0)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }

                }

            }

        }

        public static bool AddData(string passKey, string dateutc, string tempinf, string humidityin, string baromrelin, string baromabsin, string tempf,
                            string humidity, string winddir, string windspeedmph, string windgustmph, string maxdailygust, string rainratein, string eventrainin, string hourlyrainin,
                            string dailyrainin, string weeklyrainin, string monthlyrainin, string totalrainin, string solarradiation, string uv)
        {

            if (humidity == "0")
            {
                //Invalid Data. Sometimes this can be zero when the PWS is offline.
                return false;
            }

            using (SqlConnection cnn = new SqlConnection(MyData.ConnectionString))
            {

                using (SqlCommand cmd = new SqlCommand("INSERT INTO WSReport " +
                    "   (Passkey, DateUtc, TempInF, HumidityIn, BaromRelIn, BaromAbsIn, TempOutF, " +
                    "       HumidityOut, WindDir, WindSpeedMPH, WindGustMPH, MaxDailyGust, RainRateInch, EventRainInch, HourlyRainInch, " +
                    "       DailyRainInch, WeeklyRainInch, MonthlyRainIn, TotalRainInch, SolarRadiation, UV) " +
                    "VALUES (@Passkey, @DateUtc, @TempInF, @HumidityIn, @BaromRelIn, @BaromAbsIn, " +
                    "       @TempOutF, @HumidityOut, @WindDir, @WindSpeedMPH, @WindGustMPH, @MaxDailyGust, @RainRateInch, @EventRainInch, " +
                    "       @HourlyRainInch, @DailyRainInch, @WeeklyRainInch, @MonthlyRainIn, @TotalRainInch, @SolarRadiation, @UV)", cnn))
                {

                    cnn.Open();

                    cmd.Parameters.AddWithValue("@PassKey", passKey);
                    cmd.Parameters.AddWithValue("@DateUtc", dateutc);
                    cmd.Parameters.AddWithValue("@TempInF", tempinf);
                    cmd.Parameters.AddWithValue("@HumidityIn", humidityin);
                    cmd.Parameters.AddWithValue("@BaromRelIn", baromrelin);
                    cmd.Parameters.AddWithValue("@BaromAbsIn", baromabsin);
                    cmd.Parameters.AddWithValue("@TempOutF", tempf);
                    cmd.Parameters.AddWithValue("@HumidityOut", humidity);
                    cmd.Parameters.AddWithValue("@WindDir", winddir);
                    cmd.Parameters.AddWithValue("@WindSpeedMPH", windspeedmph);
                    cmd.Parameters.AddWithValue("@WindGustMPH", windgustmph);
                    cmd.Parameters.AddWithValue("@MaxDailyGust", maxdailygust);
                    cmd.Parameters.AddWithValue("@RainRateInch", rainratein);
                    cmd.Parameters.AddWithValue("@EventRainInch", eventrainin);
                    cmd.Parameters.AddWithValue("@HourlyRainInch", hourlyrainin);
                    cmd.Parameters.AddWithValue("@DailyRainInch", dailyrainin);
                    cmd.Parameters.AddWithValue("@WeeklyRainInch", weeklyrainin);
                    cmd.Parameters.AddWithValue("@MonthlyRainIn", monthlyrainin);
                    cmd.Parameters.AddWithValue("@TotalRainInch", totalrainin);
                    cmd.Parameters.AddWithValue("@SolarRadiation", solarradiation);
                    cmd.Parameters.AddWithValue("@UV", uv);
                    cmd.ExecuteNonQuery();

                    return true;

                }

            }

        }

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
