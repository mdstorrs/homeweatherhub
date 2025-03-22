using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
//using Microsoft.CodeAnalysis.Operations;
//using Microsoft.Extensions.Logging.Abstractions;
using api.Model;

namespace api.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class HistoryController : ControllerBase
    {

        // GET: api/History/5/1/1/1/1/1
        [HttpGet("{id}/{rep}/{date}/{ms}", Name = "GetHistory")]
        public ResponseClass Get(int id, int rep, string date, int ms)
        {

            return Business.Reports.GetHistoryReport(id, rep, date, (BaseReport.MeasurementSystem)ms);

            ////[FromBody] string text
            //switch ((BaseReport.ReportType)text)
            //{
            //    case BaseReport.ReportType.Current:
            //        return Business.Reports.GetCurrentReport(id, (BaseReport.MeasurementSystem)ms);
            //    default:
            //        return Business.Reports.GetCurrentReport(id, (BaseReport.MeasurementSystem)ms);
            //}

        }

    }
}
