using System;
using System.Collections.Generic;

namespace api.Model
{
    // Data for history charts: one point per hour (Day/Week), per day (Month/Year) or per month (All).
    // Unlike the History report, values are numbers (not formatted text) in the requested units.
    public class ChartReport : ResponseClass
    {
        public int WSID { get; set; }
        public string? WSName { get; set; }
        public BaseReport.ReportType Type { get; set; }
        public BaseReport.MeasurementSystem Measurement { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Granularity { get; set; } = "hour"; // "hour", "day" or "month"
        public ChartUnits Units { get; set; } = new ChartUnits();
        public List<ChartPoint> Points { get; set; } = new List<ChartPoint>();
    }

    public class ChartUnits
    {
        public string Temperature { get; set; } = "";
        public string Rain { get; set; } = "";
        public string RainRate { get; set; } = "";
        public string Wind { get; set; } = "";
        public string Pressure { get; set; } = "";
    }

    // A missing value (null) means the station sent no usable readings for it in this period.
    // Periods with no readings at all (station offline) have no point; charts should show a gap.
    public class ChartPoint
    {
        public DateTime Time { get; set; }             // start of the hour, day or month (station time)
        public bool IsSummary { get; set; }            // includes days kept only as a daily min/max summary (no hourly detail; averages approximate)
        public int Samples { get; set; }               // raw readings in this period
        public double? TempOutMin { get; set; }
        public double? TempOutMax { get; set; }
        public double? TempOutAvg { get; set; }
        public double? TempInAvg { get; set; }
        public double? HumidityOutAvg { get; set; }
        public double? PressureAvg { get; set; }
        public double? Rain { get; set; }              // rain that fell in this period
        public double? RainRateMax { get; set; }
        public double? WindSpeedAvg { get; set; }
        public double? WindGustMax { get; set; }
        public int? WindDirectionAvg { get; set; }     // degrees, vector average weighted by speed
        public double? UVMax { get; set; }
    }
}
