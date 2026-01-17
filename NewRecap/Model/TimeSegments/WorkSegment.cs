namespace NewRecap.Model.TimeSegments
{
    public class WorkSegment
    {
        public int SegmentNo { get; set; }
        public TimeSpan? WorkStart { get; set; }
        public TimeSpan? WorkEnd { get; set; }
        public DateTime? WorkStartDate { get; set; }
        public DateTime? WorkEndDate { get; set; }

    }// End of 'WorkSegment' Class.
}// End of 'namespace'.
