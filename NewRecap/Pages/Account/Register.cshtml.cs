using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using NewRecap.Model;
using NewRecap.MyAppHelper;

using System.Security.Claims;
using System.Text.RegularExpressions;

namespace NewRecap.Pages.Account
{
    //[Authorize]
    public class RegisterModel : PageModel
    {
        [BindProperty]
        public Registration NewUser { get; set; }

        public List<string> PasswordErrors { get; set; } = new();


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
        }// End of 'OnGet'.

        public IActionResult OnPost()
        {
            PasswordErrors.Clear();
            var firstName = (NewUser.FirstName ?? string.Empty).Trim();
            var lastName = (NewUser.LastName ?? string.Empty).Trim();
            string password = (NewUser.Password ?? string.Empty).Trim();
            var userName = (NewUser.UserName ?? string.Empty).Trim();
            const int dbMaxName = 50;
            const int dbMaxPassword = 20;


            if (password.Length < 10)
                PasswordErrors.Add("Password must be at least 10 characters long.");
            if (!Regex.IsMatch(password, @"\d"))
                PasswordErrors.Add("Password must contain at least one number.");
            if (!Regex.IsMatch(password, @"[A-Z]"))
                PasswordErrors.Add("Password must contain at least one uppercase letter.");
            if (!Regex.IsMatch(password, @"[a-z]"))
                PasswordErrors.Add("Password must contain at least one lowercase letter.");

            if (password.Length > dbMaxPassword)
            {
                ModelState.AddModelError("NewUser.Password", "Password must be at most 50 characters.");
            }

            if (firstName.Length > dbMaxName)
            {
                ModelState.AddModelError("NewUser.FirstName", "First name must be at most 50 characters.");
            }

            if (firstName.Length > dbMaxName)
            {
                ModelState.AddModelError("NewUser.LastName", "Last name must be at most 50 characters.");
            }

            if (userName.Length > dbMaxName)
            {
                ModelState.AddModelError("NewUser.UserName", "Username must be at most 50 characters.");
            }

            if (ModelState.IsValid)
            {
                using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
                {
                    conn.Open();
                    string cmdEmployeeText = "INSERT INTO Employee (EmployeeFName, EmployeeLName) VALUES (@EmployeeFName, @EmployeeLName);";
                    SqlCommand cmdE = new SqlCommand(cmdEmployeeText, conn);        
                    cmdE.Parameters.AddWithValue("@EmployeeFName", NewUser.FirstName);
                    cmdE.Parameters.AddWithValue("@EmployeeLName", NewUser.LastName);
                    cmdE.ExecuteNonQuery();

                    // Get the new AutoNumber (must be SAME connection)
                    int employeeId;
                    using (var idCmd = new SqlCommand("SELECT @@IDENTITY;", conn))
                    {
                        employeeId = Convert.ToInt32(idCmd.ExecuteScalar());
                    }

                    string cmdSystemUserText = "INSERT INTO SystemUser (EmployeeID, SystemUsername, SystemUserPassword, SystemUserRole, SystemUserEmail, MustChangePassword) VALUES (@EmployeeID, @SystemUsername, @SystemUserPassword, @SystemUserRole, @SystemUserEmail, @MustChangePassword);";
                    SqlCommand cmdS = new SqlCommand(cmdSystemUserText, conn);
                    cmdS.Parameters.AddWithValue("@EmployeeID", employeeId);
                    cmdS.Parameters.AddWithValue("@SystemUsername", NewUser.UserName);
                    cmdS.Parameters.AddWithValue("@SystemUserPassword", AppHelper.GeneratePasswordHash(NewUser.Password));
                    cmdS.Parameters.AddWithValue("@SystemUserRole", false);
                    cmdS.Parameters.AddWithValue("@SystemUserEmail", NewUser.Email);
                    cmdS.Parameters.AddWithValue("@MustChangePassword", true);
                    cmdS.ExecuteNonQuery();
                    
                }
                return RedirectToPage("/AdminPages/BrowseEmployees");
            }
            else
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim != null)
                {
                    int userId = int.Parse(userIdClaim.Value); // Use the claim value only if it exists
                    CheckIfUserIsAdmin(userId);
                }
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

                // If SystemUserRole is 1, set IsUserAdmin to true
                if (result != null && result.ToString() == "True")
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

    }//End of 'Register'.
}//End of 'namespace'.
