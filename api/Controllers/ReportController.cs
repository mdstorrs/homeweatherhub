using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
//using Microsoft.VisualStudio.Web.CodeGeneration.Contracts.Messaging;
using api.Business;
using api.Model;

namespace api.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class ReportController : ControllerBase
    {

        [HttpPost]
        public async void Post()
        {

            string ipAddress = "Unknown";

            try
            {

                string rawData;

                ipAddress = Reports.GetIP(HttpContext.Request, HttpContext.Connection);

                using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
                {
                    rawData = await reader.ReadToEndAsync();
                }

                //WSData.SaveRawData(rawData, ipAddress);

                Dictionary<string, string> parsedValues = ParseQueryString(rawData);

                var passKey = parsedValues["PASSKEY"];
                var stationtype = parsedValues["stationtype"];
                var dateutc = parsedValues["dateutc"];
                var tempinf = parsedValues["tempinf"];
                var humidityin = parsedValues["humidityin"];
                var baromrelin = parsedValues["baromrelin"];
                var baromabsin = parsedValues["baromabsin"];
                var tempf = parsedValues["tempf"];
                var humidity = parsedValues["humidity"];
                var winddir = parsedValues["winddir"];
                var windspeedmph = parsedValues["windspeedmph"];
                var windgustmph = parsedValues["windgustmph"];
                var maxdailygust = parsedValues["maxdailygust"];
                var rainratein = parsedValues["rainratein"];
                var eventrainin = parsedValues["eventrainin"];
                var hourlyrainin = parsedValues["hourlyrainin"];
                var dailyrainin = parsedValues["dailyrainin"];
                var weeklyrainin = parsedValues["weeklyrainin"];
                var monthlyrainin = parsedValues["monthlyrainin"];
                var totalrainin = parsedValues["totalrainin"];
                var solarradiation = parsedValues["solarradiation"];
                var uv = parsedValues["uv"];
                var wh65batt = parsedValues["wh65batt"];
                var freq = parsedValues["freq"];
                var model = parsedValues["model"];

                Reports.SubmitWSData(passKey, ipAddress, stationtype, model, rawData,
                    dateutc, tempinf, humidityin, baromrelin, baromabsin,
                    tempf, humidity, winddir, windspeedmph, windgustmph, maxdailygust,
                    rainratein, eventrainin, hourlyrainin, dailyrainin, weeklyrainin,
                    monthlyrainin, totalrainin, solarradiation, uv);

            }
            catch (Exception ex)
            {
                WSData.SaveRawData(ex.ToString(), ipAddress);
            }

        }

        public static Dictionary<string, string> ParseQueryString(string queryString)
        {
            var values = new Dictionary<string, string>();
            if (string.IsNullOrEmpty(queryString))
            {
                return values;
            }

            string[] pairs = queryString.Split('&');
            foreach (string pair in pairs)
            {
                string[] keyValue = pair.Split('=');
                if (keyValue.Length == 2)
                {
                    string key = HttpUtility.UrlDecode(keyValue[0]);
                    string value = HttpUtility.UrlDecode(keyValue[1]);
                    values[key] = value;
                }
            }
            return values;
        }

    }

}
