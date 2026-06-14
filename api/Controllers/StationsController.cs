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
        public StationList Get(int page = 0, int stationsperpage = 100, string filter = "", int stationid = 0)
        {
            return Business.Reports.GetAllStations(filter, page, stationsperpage, stationid);
        }

        [HttpPost]
        public ResponseClass Post([FromBody] StationUpsertRequest station)
        {
            return Business.Reports.AddStation(station);
        }

        [HttpPut]
        public ResponseClass Put([FromBody] StationUpsertRequest station)
        {
            return Business.Reports.UpdateStation(station);
        }

        [HttpPost("settings")]
        public ResponseClass PostSettings([FromBody] StationSettingsUpdateRequest request)
        {
            return Business.Reports.UpsertStationSettings(request);
        }

        [HttpDelete("{id}")]
        public void Delete(int id)
        {

        }

    }

}
