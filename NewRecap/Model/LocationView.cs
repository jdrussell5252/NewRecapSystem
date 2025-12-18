namespace NewRecap.Model
{
    public class LocationView
    {
        public int StoreLocationID { get; set; }
        public string? StoreState { get; set; }
        public string? StoreCity { get; set; }
        public int StoreLocationID_Original { get; set; }     // hidden original
        public int? StoreNumber { get; set; }
        public bool IsActive { get; set; }
    }// End of 'LocationView' Class.
}// End of 'namespace'.
