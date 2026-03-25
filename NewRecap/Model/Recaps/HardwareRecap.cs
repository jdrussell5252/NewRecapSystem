using NewRecap.Model.TimeSegments;
using System.ComponentModel.DataAnnotations;

namespace NewRecap.Model.Recaps
{
    public class HardwareRecap
    {
        public List<WorkSegment> WorkSegments { get; set; } = new List<WorkSegment>();
        public List<LunchSegments> LunchSegments { get; set; } = new List<LunchSegments>();
        public List<RecapSegments> RecapSegments { get; set; } = new List<RecapSegments>();

        [Required(ErrorMessage = "What did you do is required.")]
        public string RecapWDYD { get; set; }
        [Required(ErrorMessage = "What do you have left to do is required.")]
        public string RecapWLTD { get; set; }
        public DateTime? RecapDate { get; set; }
        [Required(ErrorMessage = "Workorder Number is required.")]
        public int? RecapWorkorderNumber { get; set; }
        [Required(ErrorMessage = "Ticket Number is required.")]
        public string RecapTicketNumber { get; set; }
        public int? RecapAssetNumber { get; set; }
        public string? RecapSerialNumber { get; set; }
        public int? StoreNumber { get; set; }
        public string? IP { get; set; }
        public string? WAM { get; set; }
        public string? Hostname { get; set; }
        public int RecapID { get; set; }
    }// End of 'HardwareRecap' Class.
}// End of 'namespace'.
