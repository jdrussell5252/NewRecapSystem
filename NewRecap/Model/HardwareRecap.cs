using System.ComponentModel.DataAnnotations;

namespace NewRecap.Model
{
    public class HardwareRecap
    {
        public int? StoreNumber { get; set; }
        public List<WorkSegment> WorkSegments { get; set; } = new List<WorkSegment>();
        [Required(ErrorMessage = "Recap Description is required.")]
        public string RecapDescription { get; set; }
        [Required(ErrorMessage = "Recap Date is required.")]
        public DateTime? RecapDate { get; set; }
        [Required(ErrorMessage = "Workorder Number is required.")]
        public int? RecapWorkorderNumber { get; set; }
        public int? RecapAssetNumber { get; set; }
        public string? RecapSerialNumber { get; set; }
    }
}
