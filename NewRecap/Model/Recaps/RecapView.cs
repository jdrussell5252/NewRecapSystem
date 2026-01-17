using Microsoft.AspNetCore.Mvc;

namespace NewRecap.Model.Recaps
{
    public class RecapView
    {
        public int RecapID { get; set; }
        public int RecapWorkorderNumber { get; set; }
        public DateTime RecapDate { get; set; }
        public List<string> EmployeeName { get; set; } = new List<string>();
        public decimal TotalWorkTime { get; set; }
        public decimal TotalDriveTime { get; set; }
        public decimal TotalLunchTime { get; set; }
        public decimal TotalSupportTime { get; set; }
        public decimal TotalTime { get; set; }
        public decimal TotalRecapTime { get; set; }
        public string? RecapState { get; set; }
        public string? RecapCity { get; set; }
        public string? RecapDescription { get; set; }
        public List<string> RecapEmployees { get; set; } = new List<string>();
        public string? RecapVehicle { get; set; }
        public int? StoreLocationID { get; set; }
        public int? StoreNumber { get; set; }
        public string StoreState { get; set; }
        public string StoreCity { get; set; }
        public int? RecapAssetNumber { get; set; }
        public string? RecapSerialNumber { get; set; }
        public bool? IsHardware { get; set; }
        public string RecapStoreLocation { get; set; }
        public int? AddedBy { get; set; }
        public string Segments { get; set; }
        public int? StartingMileage { get; set; }
        public int? EndingMileage { get; set; }
        public string? IP { get; set; }
        public string? WAM { get; set; }
        public string? Hostname { get; set; }
        public string Customer { get; set; }
        public string Technician { get; set; }
        public bool IsHardwareRecap => !string.IsNullOrWhiteSpace(RecapStoreLocation);
        public List<int> RecapEmployeeIds { get; set; } = new();
        public double Total182 { get; set; }
        public double Total184 { get; set; }
        public double Total186 { get; set; }
        public double TotalCat6 { get; set; }
        public double TotalFiber { get; set; }
        public double TotalCoax { get; set; }
    }// End of 'RecapView' Class.
}// End of 'namespace'.
