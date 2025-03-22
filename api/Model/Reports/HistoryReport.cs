using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace api.Model
{
    public class HistoryReport : BaseReport
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string OutsideTemperatureMin { get; set; }
        public string OutsideTemperatureMax { get; set; }
        public string InsideTemperatureMin { get; set; }
        public string InsideTemperatureMax { get; set; }
        public string TotalRain { get; set; }
        public string RainRateMax { get; set; }
        public string WindSpeedMax { get; set; }
        public string WindGustMax { get; set; }
        public int WindDirectionAngleAvg { get; set; }
        public string WindDirectionAvg { get; set; }
        public string OutsideHumidityMax { get; set; }
        public string OutsideHumidityMin { get; set; }
        public string InsideHumidityMax { get; set; }
        public string InsideHumidityMin { get; set; }
        public string PressureMin { get; set; }
        public string PressureMax { get; set; }
        public int UVIndexMax { get; set; }
    }
}
