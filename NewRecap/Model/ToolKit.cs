using System.ComponentModel.DataAnnotations;

namespace NewRecap.Model
{
    public class ToolKit
    {
        public int ToolKitID { get; set; }
        [Required(ErrorMessage = "Toolkit name is required.")]
        public string ToolKitName { get; set; }
        [Required(ErrorMessage = "Toolkit barcode value is required.")]
        public string BarcodeValue { get; set; }
        public bool IsActive { get; set; }
    }// End of 'ToolKit' Class.
}// End of 'namespace'.
