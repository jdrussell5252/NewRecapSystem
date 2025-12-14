using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using NewRecap.Model;

using NewRecap.MyAppHelper;
using Microsoft.Data.SqlClient;

namespace NewRecap.Pages.Account
{
    public class LoginModel : PageModel
    {
        
        [BindProperty]
        public Login LoginUser { get; set; }
        public bool IsAdmin { get; set; }


        public void OnGet()
        {
            /*--------------------ADMIN PRIV----------------------*/
            // Safely access the NameIdentifier claim
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null)
            {
                int userId = int.Parse(userIdClaim.Value); // Use the claim value only if it exists
                CheckIfUserIsAdmin(userId);
            }
            /*--------------------ADMIN PRIV----------------------*/
        }//End of 'OnGet'.

        public IActionResult OnPost()
        {
            if (ModelState.IsValid)
            {
                // Validate User Input.
                // Check if the user exists in the database.
                // If the user exists, redirect to the profile page.
                // If the user does not exist, display an error message.
                using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
                {
                    string cmdText = "SELECT * FROM [SystemUser] WHERE SystemUsername = @SystemUsername";
                    //string cmdText = "SELECT * FROM [SystemUser] WHERE SystemUserEmail = @Email";
                    SqlCommand cmd = new SqlCommand(cmdText, conn);
                    cmd.Parameters.AddWithValue("@SystemUsername", LoginUser.Username);
                    conn.Open();
                    SqlDataReader reader = cmd.ExecuteReader();
                    if (reader.HasRows)
                    {
                        reader.Read();
                        string passwordHash = reader.GetString(3);
                        if (AppHelper.VerifyPassword(LoginUser.Password, passwordHash))
                        {
                            bool mustChangePassword = false;
                            if (!reader.IsDBNull(6))      
                                mustChangePassword = reader.GetBoolean(6);

                            // create a email claim
                            //Claim emailClaim = new Claim(ClaimTypes.Email, LoginUser.Email);
                            // create a user id claim
                            Claim userIdClaim = new Claim(ClaimTypes.NameIdentifier, reader.GetInt32(0).ToString());
                            // create a name claim
                            Claim nameClaim = new Claim(ClaimTypes.Name, reader.GetString(2));
                            // create a role claim
                            //Claim roleClaim = new Claim(ClaimTypes.Role, reader.GetInt32(4).ToString());

                            bool isAdmin = reader.GetBoolean(4);
                            Claim roleClaim = new Claim(ClaimTypes.Role, isAdmin ? "Admin" : "Employee");


                            Claim mustChangeClaim = new Claim(
                                "MustChangePassword",
                                mustChangePassword ? "true" : "false"
                            );
                            // create a list of claims
                            //List<Claim> claims = new List<Claim> { emailClaim, userIdClaim, nameClaim, roleClaim };
                            List<Claim> claims = new List<Claim> {userIdClaim, nameClaim, roleClaim, mustChangeClaim };

                            // create a claims identity
                            ClaimsIdentity identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                            // create a claims principal
                            ClaimsPrincipal principal = new ClaimsPrincipal(identity);

                            // sign in the user
                            HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

                            if (mustChangePassword)
                                return RedirectToPage("/Account/EditPassword");

                            // update user login time
                            // UpdateUserLoginTime(reader.GetInt32(0));
                            return RedirectToPage("/Index");
                        }
                        else
                        {
                            ModelState.AddModelError("LoginUser.Username", "Invalid Credentials.");
                            return Page();
                        }
                    }
                    else
                    {
                        ModelState.AddModelError("LoginUser.Username", "Invalid Credentials.");
                        return Page();
                    }
                }
            }

            else
            {
                return Page();
            }
        }//End of 'OnPost'.

        /*--------------------ADMIN PRIV----------------------*/
        private void CheckIfUserIsAdmin(int userId)
        {
            using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
            {
                string cmdText = "SELECT SystemUserRole FROM SystemUser WHERE SystemUserID = @SystemUserID";
                SqlCommand cmd = new SqlCommand(cmdText, conn);
                cmd.Parameters.AddWithValue("@SystemUserID", userId);
                conn.Open();
                var result = cmd.ExecuteScalar();

                // If SystemUserRole is 2, set IsUserAdmin to true
                if (result != null && result.ToString() == "1")
                {
                    IsAdmin = true;
                    ViewData["IsAdmin"] = true;
                }
                else
                {
                    IsAdmin = false;
                }
            }
        }//End of 'CheckIfUserIsAdmin'.
        /*--------------------ADMIN PRIV----------------------*/

    }//End of 'Login' Class.
}//End of 'namespace'.
