using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using api.Model;

namespace api.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class StationsController : ControllerBase
    {

        // GET: Report/5/1 - weather station id / measurement system. eg Metric or Imperial
        [HttpGet("{page?}/{stationsperpage?}/{filter?}", Name = "Stations")]
        public StationList Get(int page = 0, int stationsperpage = 100, string filter = "")
        {
            return Business.Reports.GetAllStations(filter, page, stationsperpage);
        }

        // POST: Report
        [HttpPost]
        public void Post([FromForm] Model.Station station)
        {
            //WSData.SaveRawData(model.PASSKEY);
        }

        // PUT: Report/5
        [HttpPut()]
        public void Put([FromForm] Model.Station station)
        {
        }

        [HttpDelete("{id}")]
        public void Delete(int id)
        {

        }

    }

}
