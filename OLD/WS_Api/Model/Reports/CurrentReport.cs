using System;
using System.Drawing;

namespace api.Model
{
    public class CurrentReport : BaseReport
    {
        public DateTime ServerTime { get; set; }
        public DateTime LastUpdated { get; set; }
        public string TempOutside {get; set;}
        public string TempInside { get; set; }
        public string HumidityOutside { get; set; }
        public string HumidityInside { get; set; }
        public string Pressure { get; set; }
        public int UVIndex { get; set; }
        public string RainRate { get; set; }
        public string RainAccumulation { get; set; }
        public int WindDirAngle { get; set; }
        public string WindDirection { get; set; }
        public string WindSpeed { get; set; }
        public string WindGust { get; set; }
        public string TempFeel { get; set; }

    }
}
