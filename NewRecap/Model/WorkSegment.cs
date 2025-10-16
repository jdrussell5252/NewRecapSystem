namespace NewRecap.Model
{
    public class WorkSegment
    {
        public TimeSpan? WorkStart { get; set; }
        public TimeSpan? WorkEnd { get; set; }
        public TimeSpan? DriveStart { get; set; }
        public TimeSpan? DriveEnd { get; set; }
        public TimeSpan? LunchStart { get; set; }
        public TimeSpan? LunchEnd { get; set; }
        public DateTime? DriveStartDate { get; set; }
        public DateTime? DriveEndDate { get; set; }
        public DateTime? WorkStartDate { get; set; }
        public DateTime? WorkEndDate { get; set; }
        public DateTime? LunchStartDate { get; set; }
        public DateTime? LunchEndDate { get; set; }

    }
}
