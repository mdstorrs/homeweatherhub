using System.Collections.Generic;

namespace api.Model
{
    public class Station
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        // The parts of Address, so clients don't have to split "Suburb State, Country"
        // (which fails for multi-word states like "New South Wales").
        public string Suburb { get; set; }
        public string State { get; set; }
        public string Country { get; set; }
        public string Coordinates { get; set; }
        public bool HasPower { get; set; }
        public List<KeyValuePair<string, string>> Settings { get; set; } = new List<KeyValuePair<string, string>>();
    }
}
