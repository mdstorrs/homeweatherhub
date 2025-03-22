using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace api.Model
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

        public static void WriteFile(string filename, string data)
        {
            try
            {
                using StreamWriter sw = new StreamWriter(filename);
                {
                    sw.WriteLine(data);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                //Do Nothing 
            }
        }

    }

}
