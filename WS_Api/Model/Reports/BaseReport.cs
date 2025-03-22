using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace api.Model
{
    public class BaseReport : ResponseClass 
    {

        public int WSID { get; set; }
        public string PassKey { get; set; }
        public string WSName { get; set; }
        public ReportType Type { get; set; }
        public MeasurementSystem Measurement { get; set; }
        public string MeasurementSymbol { get; set; }

        public enum ReportType
        {
            Current,
            Day,
            Week,
            Month,
            Year,
            All
        }

        public enum MeasurementSystem
        { 
            Imperial,
            Metric
        }

    }

}
