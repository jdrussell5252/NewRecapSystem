namespace NewRecap.Model
{
    public class RecapView
    {
        public int RecapID { get; set; }
        public int RecapWorkorderNumber { get; set; }
        public DateTime RecapDate { get; set; }
        public List<string> EmployeeName { get; set; } = new List<string>();
        public DateTime TotalWorkTime { get; set; }
        public DateTime TotalDriveTime { get; set; }
        public DateTime TotalLunchTime { get; set; }
        public string RecapState { get; set; }
        public string RecapCity { get; set; }
        public string RecapDescription { get; set; }
        public List<string> RecapEmployees { get; set; } = new List<string>();
    }
}
