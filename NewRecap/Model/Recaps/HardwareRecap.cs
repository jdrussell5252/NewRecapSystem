using NewRecap.Model.TimeSegments;
using System.ComponentModel.DataAnnotations;

namespace NewRecap.Model.Recaps
{
    public class HardwareRecap
    {
        public List<WorkSegment> WorkSegments { get; set; } = new List<WorkSegment>();
        public List<LunchSegments> LunchSegments { get; set; } = new List<LunchSegments>();
        public List<RecapSegments> RecapSegments { get; set; } = new List<RecapSegments>();

        [Required(ErrorMessage = "Recap Description is required.")]
        public string RecapDescription { get; set; }

        public DateTime? RecapDate { get; set; }
        [Required(ErrorMessage = "Workorder Number is required.")]
        public int? RecapWorkorderNumber { get; set; }
        public int? RecapAssetNumber { get; set; }
        public string? RecapSerialNumber { get; set; }
        public int? StoreNumber { get; set; }
        public string? IP { get; set; }
        public string? WAM { get; set; }
        public string? Hostname { get; set; }
        public int RecapID { get; set; }
    }// End of 'HardwareRecap' Class.
}// End of 'namespace'.
