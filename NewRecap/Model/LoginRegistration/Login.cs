using System.ComponentModel.DataAnnotations;

namespace NewRecap.Model.LoginRegistration
{
    public class Login
    {
        [Required(ErrorMessage = "Email is required")]
        //public String Email { get; set; }
        public string Username { get; set; }

        [Required(ErrorMessage = "Password is required")]
        public string Password { get; set; }
    }// End of 'Login' Class.
}// End of 'namespace'.
