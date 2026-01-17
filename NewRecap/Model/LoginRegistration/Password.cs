using System.ComponentModel.DataAnnotations;

namespace NewRecap.Model.LoginRegistration
{
    public class Password
    {
        public string MyPassword { get; set; }
        [Display(Name = "Confirm Password")]
        [Required(ErrorMessage = "Confirm Password is required")]
        [Compare("MyPassword", ErrorMessage = "Password and Confirm Password do not match")]
        public string ConfirmPassword { get; set; }
    }// End of 'Password' Class.
}// End of 'namespace'.
