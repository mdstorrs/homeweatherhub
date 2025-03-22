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

        //[HttpPost]
        //public IActionResult Post()
        //{
        //    try
        //    {
        //        var passKey = Request.Form["PASSKEY"];
        //        var stationType = Request.Form["stationtype"];

        //        WSData.SaveRawData(passKey, stationType);

        //        if (!string.IsNullOrEmpty(passKey) && !string.IsNullOrEmpty(stationType))
        //        {
        //            Console.WriteLine($"PASSKEY: {passKey}, Station Type: {stationType}");
        //            return Ok("Data received.");
        //        }

        //        return BadRequest("Missing parameters.");
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Error: {ex.Message}");
        //        return StatusCode(500, "Internal Server Error");
        //    }
        //}

        ////POST: Report
        //[HttpPost]
        //public void Post([FromForm] Model.WSData model)
        //{
        //    WSData.SaveRawData(model.PASSKEY, "From form");
        //}

        //[HttpPost]
        //public async Task Post()
        //{
        //    string rawData;

        //    using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
        //    {
        //        rawData = await reader.ReadToEndAsync();
        //    }

        //    string ipAddress = "something"; //Reports.GetIP();

        //    // Log the IP address or use it as needed
        //    //System.Diagnostics.Debug.WriteLine($"Request from IP: {ipAddress}");

        //    WSData.SaveRawData(rawData, ipAddress);
        //}

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

                WSData.SaveRawData(rawData, ipAddress);

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
                Reports.eWSStatus status = Reports.eWSStatus.New;

                //Check if the WS exists and get its status
                Reports.CheckForStation(passKey, ref status);

                switch (status)
                {
                    case Reports.eWSStatus.New:
                        if (Reports.AddStation(passKey, ipAddress, stationtype, model, rawData) == true)
                        {
                            //Success!
                        }
                        break;
                    case Reports.eWSStatus.Added:
                    case Reports.eWSStatus.Blocked:
                    case Reports.eWSStatus.Disabled:
                        Reports.UpdateStation(passKey, ipAddress, stationtype, rawData);
                        break;
                    case Reports.eWSStatus.Authorised:
                        //Update the station record so we can see whether the station is active
                        Reports.UpdateStation(passKey, ipAddress, stationtype, rawData);
                        //Add the weather data to the WSReport table
                        Reports.AddData(passKey, dateutc, tempinf, humidityin, baromrelin, baromabsin, tempf, humidity, winddir, windspeedmph, windgustmph, maxdailygust, rainratein, eventrainin,
                            hourlyrainin, dailyrainin, weeklyrainin, monthlyrainin, totalrainin, solarradiation, uv);
                        break;
                    default:
                        return;
                }

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
