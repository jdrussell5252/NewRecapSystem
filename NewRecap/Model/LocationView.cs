namespace NewRecap.Model
{
    public class LocationView
    {
        public int StoreLocationID { get; set; }
        public string? StoreState { get; set; }
        public string? StoreCity { get; set; }
        public int StoreLocationID_Original { get; set; }     // hidden original
    }
}
