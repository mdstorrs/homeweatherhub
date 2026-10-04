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

        // Must return Task (not async void) so the request stays open until the body is read and saved.
        [HttpPost]
        public async Task<IActionResult> Post()
        {

            string ipAddress = "Unknown";
            string rawData = "";

            try
            {

                ipAddress = Reports.GetIP(HttpContext.Request, HttpContext.Connection);

                using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
                {
                    rawData = await reader.ReadToEndAsync();
                }

                Dictionary<string, string> parsedValues = ParseQueryString(rawData);

                // Missing fields become null (saved as NULL) instead of throwing and losing the whole reading.
                string? Field(string key) => parsedValues.TryGetValue(key, out var value) ? value : null;

                var passKey = Field("PASSKEY");

                if (string.IsNullOrEmpty(passKey))
                {
                    WSData.SaveRawData("Missing PASSKEY" + Environment.NewLine + rawData, ipAddress);
                    return Ok();
                }

                Reports.SubmitWSData(passKey, ipAddress, Field("stationtype"), Field("model"), rawData,
                    Field("dateutc"), Field("tempinf"), Field("humidityin"), Field("baromrelin"), Field("baromabsin"),
                    Field("tempf"), Field("humidity"), Field("winddir"), Field("windspeedmph"), Field("windgustmph"), Field("maxdailygust"),
                    Field("rainratein"), Field("eventrainin"), Field("hourlyrainin"), Field("dailyrainin"), Field("weeklyrainin"),
                    Field("monthlyrainin"), Field("totalrainin"), Field("solarradiation"), Field("uv"));

            }
            catch (Exception ex)
            {
                // Keep the raw body with the error so the reading can be replayed later.
                WSData.SaveRawData(ex.ToString() + Environment.NewLine + rawData, ipAddress);
            }

            // Always 200: the station has nothing useful to do with an error, and the body is logged above.
            return Ok();

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
                string[] keyValue = pair.Split('=', 2);
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
