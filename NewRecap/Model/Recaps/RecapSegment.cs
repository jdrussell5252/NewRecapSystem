namespace NewRecap.Model.Recaps
{
    public class RecapSegment
    {
        public string SegmentType { get; set; } = "";
        public DateTime? StartDate { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime? EndTime { get; set; }
        public DateTime? SortStart { get; set; }
    }
}
