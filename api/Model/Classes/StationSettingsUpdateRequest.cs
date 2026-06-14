using System.Collections.Generic;

namespace api.Model
{
    public class StationSettingsUpdateRequest
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Suburb { get; set; }
        public string State { get; set; }
        public string Country { get; set; }
        public string Coordinates { get; set; }
        public bool HasPower { get; set; }
        public List<KeyValuePair<string, string>> Settings { get; set; } = new List<KeyValuePair<string, string>>();
    }
}
