using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using Home_Weather_Hub.App_Code;

namespace Home_Weather_Hub.App_Code
{
    public class WSGlobal
    {

        public static string GetWindDirection(int windDir)
        {

            if ((windDir >= 348.75) || (windDir < 11.25)) { return "N"; }
            if ((windDir >= 11.25) && (windDir < 33.75)) { return "NNE"; }
            if ((windDir >= 33.75) && (windDir < 56.25)) { return "NE"; }
            if ((windDir >= 56.25) && (windDir < 78.75)) { return "ENE"; }
            if ((windDir >= 78.75) && (windDir < 101.25)) { return "E"; }
            if ((windDir >= 101.25) && (windDir < 123.75)) { return "ESE"; }
            if ((windDir >= 123.75) && (windDir < 146.25)) { return "SE"; }
            if ((windDir >= 146.25) && (windDir < 168.75)) { return "SSE"; }
            if ((windDir >= 168.75) && (windDir < 191.25)) { return "S"; }
            if ((windDir >= 191.25) && (windDir < 213.75)) { return "SSW"; }
            if ((windDir >= 213.75) && (windDir < 236.25)) { return "SW"; }
            if ((windDir >= 236.25) && (windDir < 258.75)) { return "WSW"; }
            if ((windDir >= 258.75) && (windDir < 281.25)) { return "W"; }
            if ((windDir >= 281.25) && (windDir < 303.75)) { return "WNW"; }
            if ((windDir >= 303.75) && (windDir < 326.25)) { return "NW"; }
            if ((windDir >= 326.25) && (windDir < 348.75)) { return "NNW"; }

            return "N";

        }


        public static bool GetPassKey(int id, ref string passKey, ref string stationName)
        {

            try
            {

                using (SqlConnection cnn = new SqlConnection(WSSession.ConnectionString))
                {

                    using (SqlCommand cmd = new SqlCommand("SELECT PassKey, StationName FROM WSStations WITH(NOLOCK) WHERE (ID = @ID);", cnn))
                    {

                        cnn.Open();
                        cmd.Parameters.AddWithValue("@ID", id);

                        using (SqlDataReader rdr = cmd.ExecuteReader())
                        {

                            while (rdr.Read())
                            {
                                passKey = (string)rdr[0];
                                stationName = (string)rdr[1];
                                return true;
                            }

                        }

                    }

                }

            }
            catch
            {
                return false;
            }

            return false;

        }

        public static DateTime GetWSWeek(DateTime someDate)
        {
            int dow;
            if ((int)someDate.DayOfWeek == 0)
            {
                dow = 7;
            }
            else
            {
                dow = (int)someDate.DayOfWeek;
            }

            DateTime fromDate = someDate.AddDays(-(dow - 1)).Date;

            return fromDate;

        }

    }
}