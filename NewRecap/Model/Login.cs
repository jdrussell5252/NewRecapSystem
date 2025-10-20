using System.ComponentModel.DataAnnotations;

namespace NewRecap.Model
{
    public class Login
    {
        [Required(ErrorMessage = "Email is required")]
        //public String Email { get; set; }
        public String Username { get; set; }

        [Required(ErrorMessage = "Password is required")]
        public String Password { get; set; }
    }//End of 'Login' Class.
}//End of 'namespace'.
