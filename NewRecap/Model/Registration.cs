using System.ComponentModel.DataAnnotations;

namespace NewRecap.Model
{
    public class Registration
    {
        [Display(Name = "First Name")]
        public string FirstName { get; set; }

        [Display(Name = "Last Name")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "User Name is required")]
        public string UserName { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid Email Address")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required")]
        public string Password { get; set; }

        [Display(Name = "Confirm Password")]
        [Required(ErrorMessage = "Confirm Password is required")]
        [Compare("Password", ErrorMessage = "Password and Confirm Password do not match")]
        public string ConfirmPassword { get; set; }
    }// End of 'Registration' Class.
}// End of 'namespace'.
