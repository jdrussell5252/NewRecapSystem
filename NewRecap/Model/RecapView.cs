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
        public double TotalSupportTime { get; set; }
        public double TotalTime { get; set; }
        public string? RecapState { get; set; }
        public string? RecapCity { get; set; }
        public string? RecapDescription { get; set; }
        public List<string> RecapEmployees { get; set; } = new List<string>();
        public string? RecapVehicle { get; set; }

        public int? RecapAssetNumber { get; set; }
        public string? RecapSerialNumber { get; set; }

        public string RecapStoreLocation { get; set; }
        public int? AddedBy { get; set; }
        public string Segments { get; set; }
        public int? StartingMileage { get; set; }
        public int? EndingMileage { get; set; }
        public string? IP { get; set; }
        public string? WAM { get; set; }
        public string? Hostname { get; set; }
        public bool IsHardwareRecap => !string.IsNullOrWhiteSpace(RecapStoreLocation);
    }
}
