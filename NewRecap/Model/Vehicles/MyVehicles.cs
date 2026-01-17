using System.ComponentModel.DataAnnotations;

namespace NewRecap.Model.Vehicles
{
    public class MyVehicles
    {
        [Required(ErrorMessage = "Vehicle Number is required.")]
        public string? VehicleNumber { get; set; }

        [Required(ErrorMessage = "Vehicle model is required.")]
        public string? VehicleModel { get; set; }
    }// End of 'MyVehicles' Class.
}// End of 'namespace'.
