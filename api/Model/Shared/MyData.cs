using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace api.Model
{
    public class MyData
    {

        // Set once at startup from configuration (ConnectionStrings:WeatherDb).
        // The value lives in appsettings.Secrets.json, which is not committed to source control.
        public static string ConnectionString { get; set; } = string.Empty;

    }
}
