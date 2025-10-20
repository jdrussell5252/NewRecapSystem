namespace NewRecap.Model
{
    public class Recap
    {
        //public int RecapID { get; set; }
        public int? StoreNumber { get; set; }
        //public string Date {  get; set; } 
        public List<WorkSegment> WorkSegments { get; set; } = new List<WorkSegment>();
        public string? RecapCity { get; set; }
        public string? RecapState { get; set; }
        public int? VehicleID { get; set; }
        public string RecapDescription { get; set; }
        public DateTime? RecapDate { get; set; }
        public int? RecapWorkorderNumber { get; set; }
        public int? RecapAssetNumber { get; set; }
        public string? RecapSerialNumber { get; set; }


    }// End of 'RecapInfo' Class.
}// End of 'NewRecap.Model'.
