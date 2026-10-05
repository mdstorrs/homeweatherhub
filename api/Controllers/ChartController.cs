using Microsoft.AspNetCore.Mvc;
using api.Model;

namespace api.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class ChartController : ControllerBase
    {

        // GET: Chart/5/2/2026-09-28/1 - station id / report type (same as History: 1 Day, 2 Week, 3 Month, 4 Year, 5 All)
        //                               / date in the period / measurement system (0 Imperial, 1 Metric)
        [HttpGet("{id}/{rep}/{date}/{ms?}", Name = "GetChart")]
        public ChartReport Get(int id, int rep, string date, int ms = 1)
        {
            return Business.Reports.GetChartReport(id, rep, date, (BaseReport.MeasurementSystem)ms);
        }

    }
}
