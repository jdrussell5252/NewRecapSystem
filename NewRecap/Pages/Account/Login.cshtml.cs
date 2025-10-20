using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using NewRecap.Model;
using System.Data.OleDb;
using NewRecap.MyAppHelper;

namespace NewRecap.Pages.Account
{
    public class LoginModel : PageModel
    {
        
        [BindProperty]
        public Login LoginUser { get; set; }
        public string connectionString = "Provider = Microsoft.ACE.OLEDB.12.0; Data Source = C:\\Users\\jaker\\OneDrive\\Desktop\\Nacspace\\New Recap\\NewRecapDB\\NewRecapDB.accdb;";

        public IActionResult OnPost()
        {
            if (ModelState.IsValid)
            {
                // Validate User Input.
                // Check if the user exists in the database.
                // If the user exists, redirect to the profile page.
                // If the user does not exist, display an error message.
                using (OleDbConnection conn = new OleDbConnection(this.connectionString))
                {
                    string cmdText = "SELECT * FROM [SystemUser] WHERE SystemUsername = @SystemUsername";
                    //string cmdText = "SELECT * FROM [SystemUser] WHERE SystemUserEmail = @Email";
                    OleDbCommand cmd = new OleDbCommand(cmdText, conn);
                    cmd.Parameters.AddWithValue("@SystemUsername", LoginUser.Username);
                    conn.Open();
                    OleDbDataReader reader = cmd.ExecuteReader();
                    if (reader.HasRows)
                    {
                        reader.Read();
                        string passwordHash = reader.GetString(3);
                        if (AppHelper.VerifyPassword(LoginUser.Password, passwordHash))
                        {
                            // create a email claim
                            //Claim emailClaim = new Claim(ClaimTypes.Email, LoginUser.Email);
                            // create a user id claim
                            Claim userIdClaim = new Claim(ClaimTypes.NameIdentifier, reader.GetInt32(0).ToString());
                            // create a name claim
                            Claim nameClaim = new Claim(ClaimTypes.Name, reader.GetString(2));
                            // create a role claim
                            Claim roleClaim = new Claim(ClaimTypes.Role, reader.GetInt32(4).ToString());

                            // create a list of claims
                            //List<Claim> claims = new List<Claim> { emailClaim, userIdClaim, nameClaim, roleClaim };
                            List<Claim> claims = new List<Claim> {userIdClaim, nameClaim, roleClaim };

                            // create a claims identity
                            ClaimsIdentity identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                            // create a claims principal
                            ClaimsPrincipal principal = new ClaimsPrincipal(identity);

                            // sign in the user
                            HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

                            // update user login time
                            // UpdateUserLoginTime(reader.GetInt32(0));
                            return RedirectToPage("/Index");
                        }
                        else
                        {
                            ModelState.AddModelError("LoginError", "Invalid Credentials.");
                            return Page();
                        }
                    }
                    else
                    {
                        ModelState.AddModelError("LoginError", "Invalid Credentials.");
                        return Page();
                    }
                }
            }

            else
            {
                return Page();
            }
        }//End of 'OnPost'.
        
    }//End of 'Login' Class.
}//End of 'namespace'.
