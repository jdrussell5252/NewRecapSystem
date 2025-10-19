using System.ComponentModel.DataAnnotations;

namespace NewRecap.Model
{
    public class MyVehicles
    {
        [Required]
        public int VehicleNumber { get; set; }
        [Required(ErrorMessage = "Vehicle Vin is required.")]
        public string VehicleVin { get; set; }
        [Required(ErrorMessage = "Vehicle name is required.")]
        public string VehicleName { get; set; }
    }//End of 'MyVehicles' Class.
}//End of 'namespace'.
