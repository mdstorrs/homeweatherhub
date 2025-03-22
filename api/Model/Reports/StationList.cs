using System;
using System.Collections.Generic;
using System.Drawing;

namespace api.Model
{
    public class StationList : ResponseClass
    {
        public List<Station> Stations { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
    }
}
