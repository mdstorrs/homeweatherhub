using System.Collections.Generic;

namespace api.Model
{
    public class StationSettingsUpdateRequest
    {
        public int Id { get; set; }
        public List<KeyValuePair<string, string>> Settings { get; set; } = new List<KeyValuePair<string, string>>();
    }
}
