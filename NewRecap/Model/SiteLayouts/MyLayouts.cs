using System.ComponentModel.DataAnnotations;

namespace NewRecap.Model.SiteLayouts
{
    public class MyLayouts
    {
        [Required(ErrorMessage = "Layout Name is required")]
        public string LayoutName { get; set; }
        [Required(ErrorMessage = "Image is required")]
        public IFormFile? LayoutImage { get; set; }
    }// End of 'MyLayouts' Class.
}// End of 'namespace'.
