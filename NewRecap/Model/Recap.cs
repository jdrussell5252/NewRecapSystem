using System.ComponentModel.DataAnnotations;

namespace NewRecap.Model
{
    public class Recap
    {
        public List<WorkSegment> WorkSegments { get; set; } = new List<WorkSegment>();
        [Required(ErrorMessage = "Recap City is required.")]
        public string? RecapCity { get; set; }
        [Required(ErrorMessage = "Recap State is required.")]
        public string? RecapState { get; set; }
        public int? VehicleID { get; set; }
        [Required(ErrorMessage = "Recap Description is required.")]
        public string RecapDescription { get; set; }
        [Required(ErrorMessage = "Recap Date is required.")]
        public DateTime? RecapDate { get; set; }
        [Required(ErrorMessage = "Workorder Number is required.")]
        public int? RecapWorkorderNumber { get; set; }

        public int? StartingMileage { get; set; }
        public int? EndingMileage { get; set; }


    }// End of 'Recap' Class.
}// End of 'NewRecap.Model'.
