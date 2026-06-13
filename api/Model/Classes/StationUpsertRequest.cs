namespace api.Model
{
    public class StationUpsertRequest
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public string Coordinates { get; set; }
        public bool HasPower { get; set; }
    }
}
