using System;
using System.IO;
using System.Collections.Specialized;
using Home_Weather_Hub.App_Code;
using System.Web.UI.WebControls;
using System.Web.Services.Configuration;

namespace Home_Weather_Hub.report
{
    public partial class Default : System.Web.UI.Page
    {

        private enum eWSStatus : short
        {
            New = 0, //No Record of this weather station
            Added = 1, //This station has not been authorised yet
            Authorised = 2, //The user has created an account and confirmed that the station is real and theirs
            Disabled = 3, //The user has disabled the WS
            Blocked = 4 //Admin has disabled the WS
        }

        protected void Page_Load(object sender, EventArgs e)
        {

            string content = "empty";

            try
            {

                //Declare all variables
                DateTime localDateTime = DateTime.Now;
                eWSStatus status = eWSStatus.New;
                String passKey = "", stationtype = "", dateutc = "", tempinf = "", humidityin = "", baromrelin = "", baromabsin = "", tempf = "", humidity = "", winddir = "", windspeedmph = "", windgustmph = "",
                    maxdailygust = "", rainratein = "", eventrainin = "", hourlyrainin = "", dailyrainin = "", weeklyrainin = "", monthlyrainin = "", totalrainin = "", solarradiation = "", uv = "", wh65batt = "",
                    freq = "", model = "", ipAddress = "";

                //Read all Raw data from POST Stream
                using (var reader = new StreamReader(Request.InputStream))
                    content = reader.ReadToEnd();

                //Get the request IP address
                ipAddress = Request.UserHostAddress.ToString();

                //Read all of the fields and return them from the POST Data.
                ReadFields(ref passKey, ref stationtype, ref dateutc, ref tempinf, ref humidityin, ref baromrelin, ref baromabsin, ref tempf, ref humidity, ref winddir,
                    ref windspeedmph, ref windgustmph, ref maxdailygust, ref rainratein, ref eventrainin, ref hourlyrainin, ref dailyrainin, ref weeklyrainin, ref monthlyrainin,
                    ref totalrainin, ref solarradiation, ref uv, ref wh65batt, ref freq, ref model);

                if (string.IsNullOrEmpty(passKey))
                {
                    SaveRawData(content);
                    return;
                }

                //Check if the WS exists and get its status
                CheckForStation(passKey, ref status);

                switch (status)
                {
                    case eWSStatus.New:
                        if (AddStation(passKey, ipAddress, stationtype, model, content) == true)
                        {
                            //Success!
                        }
                        break;
                    case eWSStatus.Added:
                    case eWSStatus.Blocked:
                    case eWSStatus.Disabled:
                        UpdateStation(passKey, ipAddress, stationtype, content);
                        break;
                    case eWSStatus.Authorised:
                        //Update the station record so we can see whether the station is active
                        UpdateStation(passKey, ipAddress, stationtype, content);
                        //Add the weather data to the WSReport table
                        AddData(passKey, dateutc, tempinf, humidityin, baromrelin, baromabsin, tempf, humidity, winddir, windspeedmph, windgustmph, maxdailygust, rainratein, eventrainin,
                            hourlyrainin, dailyrainin, weeklyrainin, monthlyrainin, totalrainin, solarradiation, uv);
                        break;
                    default:
                        return;
                }

            }
            catch (Exception ex)
            {
                SaveRawData(ex.ToString() + Environment.NewLine + content);
            }
            finally
            {

            }

        }

        private void ReadFields(ref string passKey, ref string stationtype, ref string dateutc, ref string tempinf, ref string humidityin, ref string baromrelin, ref string baromabsin, ref string tempf,
            ref string humidity, ref string winddir, ref string windspeedmph, ref string windgustmph, ref string maxdailygust, ref string rainratein, ref string eventrainin, ref string hourlyrainin,
            ref string dailyrainin, ref string weeklyrainin, ref string monthlyrainin, ref string totalrainin, ref string solarradiation, ref string uv, ref string wh65batt, ref string freq, ref string model)
        {

            NameValueCollection nvc = Request.Form;

            if (!string.IsNullOrEmpty(nvc["PASSKEY"]))
            {
                passKey = nvc["PASSKEY"];
            }

            if (!string.IsNullOrEmpty(nvc["stationtype"]))
            {
                stationtype = nvc["stationtype"];
            }

            if (!string.IsNullOrEmpty(nvc["dateutc"]))
            {
                dateutc = nvc["dateutc"];
            }

            if (!string.IsNullOrEmpty(nvc["tempinf"]))
            {
                tempinf = nvc["tempinf"];
            }

            if (!string.IsNullOrEmpty(nvc["humidityin"]))
            {
                humidityin = nvc["humidityin"];
            }

            if (!string.IsNullOrEmpty(nvc["baromrelin"]))
            {
                baromrelin = nvc["baromrelin"];
            }

            if (!string.IsNullOrEmpty(nvc["baromabsin"]))
            {
                baromabsin = nvc["baromabsin"];
            }

            if (!string.IsNullOrEmpty(nvc["tempf"]))
            {
                tempf = nvc["tempf"];
            }

            if (!string.IsNullOrEmpty(nvc["humidity"]))
            {
                humidity = nvc["humidity"];
            }

            if (!string.IsNullOrEmpty(nvc["winddir"]))
            {
                winddir = nvc["winddir"];
            }

            if (!string.IsNullOrEmpty(nvc["windspeedmph"]))
            {
                windspeedmph = nvc["windspeedmph"];
            }

            if (!string.IsNullOrEmpty(nvc["windgustmph"]))
            {
                windgustmph = nvc["windgustmph"];
            }

            if (!string.IsNullOrEmpty(nvc["maxdailygust"]))
            {
                maxdailygust = nvc["maxdailygust"];
            }

            if (!string.IsNullOrEmpty(nvc["rainratein"]))
            {
                rainratein = nvc["rainratein"];
            }

            if (!string.IsNullOrEmpty(nvc["eventrainin"]))
            {
                eventrainin = nvc["eventrainin"];
            }

            if (!string.IsNullOrEmpty(nvc["hourlyrainin"]))
            {
                hourlyrainin = nvc["hourlyrainin"];
            }

            if (!string.IsNullOrEmpty(nvc["dailyrainin"]))
            {
                dailyrainin = nvc["dailyrainin"];
            }

            if (!string.IsNullOrEmpty(nvc["weeklyrainin"]))
            {
                weeklyrainin = nvc["weeklyrainin"];
            }

            if (!string.IsNullOrEmpty(nvc["monthlyrainin"]))
            {
                monthlyrainin = nvc["monthlyrainin"];
            }

            if (!string.IsNullOrEmpty(nvc["totalrainin"]))
            {
                totalrainin = nvc["totalrainin"];
            }

            if (!string.IsNullOrEmpty(nvc["solarradiation"]))
            {
                solarradiation = nvc["solarradiation"];
            }

            if (!string.IsNullOrEmpty(nvc["uv"]))
            {
                uv = nvc["uv"];
            }

            if (!string.IsNullOrEmpty(nvc["wh65batt"]))
            {
                wh65batt = nvc["wh65batt"];
            }

            if (!string.IsNullOrEmpty(nvc["freq"]))
            {
                freq = nvc["freq"];
            }

            if (!string.IsNullOrEmpty(nvc["model"]))
            {
                model = nvc["model"];
            }

        }

        private void SaveRawData(string content)
        {

            try
            {

                using (System.Data.SqlClient.SqlConnection cnn = new System.Data.SqlClient.SqlConnection(WSSession.ConnectionString))
                using (System.Data.SqlClient.SqlCommand cmd = new System.Data.SqlClient.SqlCommand("INSERT INTO WSData (RawData) VALUES (@RawData)", cnn))
                {
                    if (string.IsNullOrEmpty(content))
                    {
                        content = "empty";
                    }
                    cnn.Open();
                    cmd.Parameters.AddWithValue("@RawData", content);
                    cmd.ExecuteNonQuery();
                }

            }
            catch
            {

            }

        }

        private void CheckForStation(string passKey, ref eWSStatus status)
        {

            using (System.Data.SqlClient.SqlConnection cnn = new System.Data.SqlClient.SqlConnection(WSSession.ConnectionString))
            {

                using (System.Data.SqlClient.SqlCommand cmd = new System.Data.SqlClient.SqlCommand("SELECT Status FROM WSStations WITH(NOLOCK) WHERE (Passkey = @Passkey)", cnn))
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

        private bool AddStation(string passKey, string ipAddress, string softwareType, string wsModel, string sampleData)
        {

            using (System.Data.SqlClient.SqlConnection
                cnn = new System.Data.SqlClient.SqlConnection(WSSession.ConnectionString))
            {

                using (System.Data.SqlClient.SqlCommand
                    cmd = new System.Data.SqlClient.SqlCommand("INSERT INTO WSStations (PassKey, SoftwareType, WSModel, IPAddress, SampleData, Status) " +
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

        private bool UpdateStation(string passKey, string ipAddress, string softwareType, string sampleData)
        {

            if (passKey == String.Empty || ipAddress == String.Empty)
            {
                return false;
            }

            using (System.Data.SqlClient.SqlConnection
                cnn = new System.Data.SqlClient.SqlConnection(WSSession.ConnectionString))
            {

                using (System.Data.SqlClient.SqlCommand
                    cmd = new System.Data.SqlClient.SqlCommand("UPDATE WSStations " +
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


        private bool AddData(string passKey, string dateutc, string tempinf, string humidityin, string baromrelin, string baromabsin, string tempf,
                            string humidity, string winddir, string windspeedmph, string windgustmph, string maxdailygust, string rainratein, string eventrainin, string hourlyrainin,
                            string dailyrainin, string weeklyrainin, string monthlyrainin, string totalrainin, string solarradiation, string uv)
        {

            if (humidity == "0")
            {
                //Invalid Data. Sometimes this can be zero when the PWS is offline.
                return false;
            }

            using (System.Data.SqlClient.SqlConnection cnn = new System.Data.SqlClient.SqlConnection(WSSession.ConnectionString))
            {

                using (System.Data.SqlClient.SqlCommand cmd = new System.Data.SqlClient.SqlCommand("INSERT INTO WSReport " +
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

    }

}