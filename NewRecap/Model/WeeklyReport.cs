namespace NewRecap.Model
{
    public class WeeklyReport
    {
        public int RecapID { get; set; }
        public int EmployeeID { get; set; }
        public string EmployeeFName { get; set; }
        public string  EmployeeLName { get; set; }
        public decimal WeeklyWorkHours { get; set; }
        //public decimal WeeklyLunchHours { get; set; }
        public decimal WeeklyTravelHours { get; set; }
        public decimal WeeklySupportHours { get; set; }
        public decimal WeeklyRecapHours { get; set; }
        public decimal TotalHours { get; set; }
        public DateTime WeekStart { get; set; }
        public int WeeklyRecapCount { get; set; } = 0;

    }// End of 'WeeklyReport' Class.
}// End of 'namespace'.
