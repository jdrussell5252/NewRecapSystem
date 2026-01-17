using System.ComponentModel.DataAnnotations;

namespace NewRecap.Model.StoreLocations
{
    public class MyLocations
    {
        [Required(ErrorMessage = "Store Number is required.")]
        public int? StoreNumber { get; set; }
        [Required(ErrorMessage = "Store State is required.")]
        public string? StoreState { get; set; }
        [Required(ErrorMessage = "Store City is required.")]
        public string? StoreCity { get; set; }
    }// End of 'MyLocations' Class.
}// End of 'namespace'.
