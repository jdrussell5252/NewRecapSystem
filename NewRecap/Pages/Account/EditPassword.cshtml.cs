using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using NewRecap.Model.LoginRegistration;
using NewRecap.MyAppHelper;

using System.Security.Claims;
using System.Text.RegularExpressions;

namespace NewRecap.Pages.Account
{
    [Authorize]
    public class EditPasswordModel : PageModel
    {
        [BindProperty]
        public Password NewPassword { get; set; } = new Password();

        public List<string> PasswordErrors { get; set; } = new();
        public bool IsAdmin { get; set; }

        public IActionResult OnGet()
        {

            // Safely access the NameIdentifier claim
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            /*--------------------ADMIN PRIV----------------------*/
            if (userIdClaim != null)
            {
                int userId = int.Parse(userIdClaim.Value); // Use the claim value only if it exists
                if (!IsUserActive(userId))
                {
                    return Forbid();
                }
                CheckIfUserIsAdmin(userId);
            }
            /*--------------------ADMIN PRIV----------------------*/

            return Page();
        }//End of 'OnGet'.

        public IActionResult OnPost()
        {
            PasswordErrors.Clear();
            string password = (NewPassword.MyPassword ?? string.Empty).Trim();
            const int dbMaxPassword = 20;

            if (password == null)
                return Page();
            if (password.Length < 10)
                PasswordErrors.Add("Password must be at least 10 characters long.");
            if (!Regex.IsMatch(password, @"\d"))
                PasswordErrors.Add("Password must contain at least one number.");
            if (!Regex.IsMatch(password, @"[A-Z]"))
                PasswordErrors.Add("Password must contain at least one uppercase letter.");
            if (!Regex.IsMatch(password, @"[a-z]"))
                PasswordErrors.Add("Password must contain at least one lowercase letter.");

            if (NewPassword.MyPassword != NewPassword.ConfirmPassword)
            {
                ModelState.AddModelError(string.Empty, "Passwords do not match.");
            }

            if (password.Length > dbMaxPassword)
            {
                ModelState.AddModelError("NewUser.Password", "Password must be at most 50 characters.");
            }

            if (ModelState.IsValid)
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    // If for some reason we don't have a valid claim, force re-login
                    return RedirectToPage("/Account/Login");
                }
                using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
                {
                    string cmdTextImage = "UPDATE SystemUser SET SystemUserPassword = @SystemUserPassword, MustChangePassword = @MustChangePassword WHERE SystemUserID = @UserID";
                    SqlCommand cmd = new SqlCommand(cmdTextImage, conn);
                    cmd.Parameters.AddWithValue("@SystemUserPassword", AppHelper.GeneratePasswordHash(NewPassword.MyPassword)); // Set the updated password
                    cmd.Parameters.AddWithValue("@MustChangePassword", false);
                    cmd.Parameters.AddWithValue("@UserID", userId);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
                return RedirectToPage("/Index"); // Redirect to the profile page after the update
            }
            else
            {
                return Page();
            }
        }//End of 'OnPost'.

        private bool IsUserActive(int userID)
        {
            using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
            {
                string sql = "SELECT IsActive FROM SystemUser WHERE SystemUserID = @SystemUserID";
                SqlCommand cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@SystemUserID", userID);

                conn.Open();
                var result = cmd.ExecuteScalar();

                return result != null && (bool)result;
            }
        }// End of 'IsUserActive'.

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
    }// End of 'EditPassword' Class.
}// End of 'namespace'.
