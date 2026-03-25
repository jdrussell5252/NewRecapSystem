using NewRecap.Model.TimeSegments;
using System.ComponentModel.DataAnnotations;

namespace NewRecap.Model.Recaps
{
    public class Recap
    {
        public List<WorkSegment> WorkSegments { get; set; } = new List<WorkSegment>();
        public List<LunchSegments> LunchSegments { get; set; } = new List<LunchSegments>();
        public List<TravelSegments> TravelSegments { get; set; } = new List<TravelSegments>();
        public List<SupportSegments> SupportSegments { get; set; } = new List<SupportSegments>();
        public List<RecapSegments> RecapSegments { get; set; } = new List<RecapSegments>();


        [Required(ErrorMessage = "Recap City is required.")]
        public string? RecapCity { get; set; }
        [Required(ErrorMessage = "Recap State is required.")]
        public string? RecapState { get; set; }
        public int? VehicleID { get; set; }
        [Required(ErrorMessage = "What did you do is required.")]
        public string RecapWDYD { get; set; }
        [Required(ErrorMessage = "What do you have left to do is required.")]
        public string RecapWLTD { get; set; }
        public DateTime? RecapDate { get; set; }
        [Required(ErrorMessage = "Workorder Number is required.")]
        public int? RecapWorkorderNumber { get; set; }

        public int? StartingMileage { get; set; }
        public int? EndingMileage { get; set; }
        public string? RecapCredsSetUsed { get; set; }
        public string? RecapInventoryUsed { get; set; }
        public string? RecapInventoryLeft { get; set; }

        [Required(ErrorMessage = "Recap Customer is required.")]
        public string? Customer { get; set; }

    }// End of 'Recap' Class.
}// End of 'namespace'.
