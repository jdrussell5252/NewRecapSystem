using System.ComponentModel.DataAnnotations;

namespace NewRecap.Model
{
    public class MyLocations
    {
        [Required]
        public int StoreNumber { get; set; }
        [Required(ErrorMessage = "State is required")]
        public string StoreState { get; set; }
        [Required(ErrorMessage = "City is required")]
        public string StoreCity { get; set; }
    }
}
