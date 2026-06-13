using System.Collections.Generic;

namespace api.Model
{
    public class Station
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public string Coordinates { get; set; }
        public bool HasPower { get; set; }
        public List<KeyValuePair<string, string>> Settings { get; set; } = new List<KeyValuePair<string, string>>();
    }
}
