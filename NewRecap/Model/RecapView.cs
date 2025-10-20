namespace NewRecap.Model
{
    public class RecapView
    {
        public int RecapID { get; set; }
        public int RecapWorkorderNumber { get; set; }
        public DateTime RecapDate { get; set; }
        public List<string> EmployeeName { get; set; } = new List<string>();
        public double TotalWorkTime { get; set; }
        public double TotalDriveTime { get; set; }
        public double TotalLunchTime { get; set; }
        public double TotalTime { get; set; }
        public string? RecapState { get; set; }
        public string? RecapCity { get; set; }
        public string? RecapDescription { get; set; }
        public List<string> RecapEmployees { get; set; } = new List<string>();


        public int? VehicleID { get; set; }
        public int? VehicleNumber { get; set; }
        public string? VehicleName { get; set; }
        public string? VehicleVin { get; set; }

        public int? RecapAssetNumber { get; set; }
        public string? RecapSerialNumber { get; set; }

        public int? StoreLocationID { get; set; }
        public int? StoreNumber { get; set; }
        public string? StoreState { get; set; }
        public string? StoreCity { get; set; }

        public int? AddedBy { get; set; }
    }
}
