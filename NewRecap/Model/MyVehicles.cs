using System.ComponentModel.DataAnnotations;

namespace NewRecap.Model
{
    public class MyVehicles
    {
        [Required]
        public int VehicleID { get; set; }
        [Required(ErrorMessage = "Vehicle name is required.")]
        public string VehicleName { get; set; }
    }//End of 'MyVehicles' Class.
}//End of 'namespace'.
