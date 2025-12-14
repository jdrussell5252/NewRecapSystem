using System.ComponentModel.DataAnnotations;

namespace NewRecap.Model
{
    public class ToolKitView
    {
        public int ToolKitID { get; set; }
        [Required(ErrorMessage = "Toolkit name is required.")]
        public string ToolKitName { get; set; }
        public string? CurrentEmployeeName { get; set; }
        public bool IsActive { get; set; }
        [Required(ErrorMessage = "Toolkit barcode value is required.")]
        public string BarcodeValue { get; set; }
        public int? EmployeeID { get; set; }
        public bool CanReturn { get; set; }
    }// End of 'ToolKitView' Class.
}// End of 'namespace'.
