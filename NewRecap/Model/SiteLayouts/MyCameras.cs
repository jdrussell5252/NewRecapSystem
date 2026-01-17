using System.ComponentModel.DataAnnotations;

namespace NewRecap.Model.SiteLayouts
{
    public class MyCameras
    {
        [Required(ErrorMessage = "Layout Name is required")]
        public string CameraName { get; set; } = "";
        [Required(ErrorMessage = "Image is required")]
        public IFormFile? ImagePath { get; set; }
        public string CameraType { get; set; }
    }// End of 'CameraView' Class.
}// End of 'namespace'.
