using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
//using System.Data.SqlClient;

namespace api.Model
{
    public class WSData
    {

        public string PASSKEY { get; set; }
        public string stationtype { get; set; }
        public string dateutc { get; set; }
        public string tempinf { get; set; }
        public string humidityin { get; set; }
        public string baromrelin { get; set; }
        public string baromabsin { get; set; }
        public string tempf { get; set; }
        public string humidity { get; set; }
        public string winddir { get; set; }
        public string windspeedmph { get; set; }
        public string windgustmph { get; set; }
        public string maxdailygust { get; set; }
        public string rainratein { get; set; }
        public string eventrainin { get; set; }
        public string hourlyrainin { get; set; }
        public string dailyrainin { get; set; }
        public string weeklyrainin { get; set; }
        public string monthlyrainin { get; set; }
        public string totalrainin { get; set; }
        public string solarradiation { get; set; }
        public string uv { get; set; }
        public string wh65batt { get; set; }
        public string freq { get; set; }
        public string model { get; set; }

        public static void SaveRawData(string content, string ipAddress)
        {

            try
            {

                using (SqlConnection cnn = new SqlConnection(MyData.ConnectionString))
                using (SqlCommand cmd = new SqlCommand(
                    "INSERT INTO WSData (RawData, IPAddress) VALUES (@RawData,@IPAddress)", cnn))
                {
                    if (string.IsNullOrEmpty(content))
                    {
                        content = "empty";
                    }
                    cnn.Open();
                    cmd.Parameters.AddWithValue("@RawData", content);
                    cmd.Parameters.AddWithValue("@IPAddress", ipAddress);
                    cmd.ExecuteNonQuery();
                }

            }
            catch (Exception ex) 
            {
                    Console.WriteLine(ex.ToString());
            }

        }

    }
}
