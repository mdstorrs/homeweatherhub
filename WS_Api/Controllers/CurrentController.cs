using System;
using System.Collections.Generic;
using System.IO;
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
    public class CurrentController : ControllerBase
    {

        // GET: Report/5/1 - weather station id / measurement system. eg Metric or Imperial
        [HttpGet("{id}/{ms?}", Name = "GetCurrent")]
        public BaseReport Get(int id, int ms = 1)
        {
            return Business.Reports.GetCurrentReport(id, (BaseReport.MeasurementSystem)ms);
        }

        // POST: Report
        //[HttpPost]
        //public void Post([FromForm] Model.WSData model)
        //{
        //    WSData.SaveRawData(model.ToString());
        //}

        //[HttpPost]
        //public async Task Post()
        //{
        //    string rawData;

        //    using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
        //    {
        //        rawData = await reader.ReadToEndAsync();
        //    }

        //    WSData.SaveRawData(rawData);
        //}

        //// PUT: Report/5
        //[HttpPut("{id}")]
        //public void Put(int id, [FromBody] string value)
        //{
        //}

        //// DELETE: ApiWithActions/5
        //[HttpDelete("{id}")]
        //public void Delete(int id)
        //{
        //}

    }

}
