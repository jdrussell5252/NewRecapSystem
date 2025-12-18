using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using NewRecap.Model;
using NewRecap.MyAppHelper;
using System.Security.Claims;

namespace NewRecap.Pages.AdminPages
{
    [Authorize]
    public class EditEmployeeUsernameModel : PageModel
    {
        [BindProperty]
        public EmployeeView Employees { get; set; } = new EmployeeView();
        public bool IsAdmin { get; set; }

        public IActionResult OnGet(int id)
        {
            if (!User.IsInRole("Admin"))
            {
                return Forbid();
            }
            // Safely access the NameIdentifier claim
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null)
            {
                int userId = int.Parse(userIdClaim.Value); // Use the claim value only if it exists
                CheckIfUserIsAdmin(userId);
            }
            PopulateEmployeeList(id);
            return Page();
        }// End of 'OnGet'.

        public IActionResult OnPost(int id)
        {
            var userName = (Employees.EmployeeUsername ?? string.Empty).Trim();
            const int dbMaxName = 50;

            if (userName.Length > dbMaxName)
            {
                ModelState.AddModelError("Employees.EmployeeUsername", "Username must be at most 50 characters.");
            }
            if (string.IsNullOrWhiteSpace(userName))
            {
                ModelState.AddModelError("Employees.EmployeeUsername", "Username must be more than 0 characters.");
            }


            if (ModelState.IsValid)
            {
                using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
                {
                    string cmdText = "UPDATE SystemUser SET SystemUsername = @SystemUsername WHERE EmployeeID = @EmployeeID";
                    SqlCommand cmd = new SqlCommand(cmdText, conn);
                    cmd.Parameters.AddWithValue("@SystemUsername", Employees.EmployeeUsername);
                    cmd.Parameters.AddWithValue("@EmployeeID", id);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
                return RedirectToPage("BrowseEmployees");
            }
            else
            {
                OnGet(id);
                return Page();
            }
        }//End of 'OnPost'.

        private void PopulateEmployeeList(int id)
        {
            using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
            {
                string query = "SELECT EmployeeID, SystemUsername FROM SystemUser WHERE EmployeeID = @EmployeeID";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@EmployeeID", id);
                conn.Open();
                SqlDataReader reader = cmd.ExecuteReader();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        Employees = new EmployeeView
                        {
                            EmployeeID = reader.GetInt32(0),
                            EmployeeUsername = reader.GetString(1)
                        };
                    }
                }
            }
        }//End of 'PopulateEmployeeList'.

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
    }// End of 'EditEmployeeUsername' Class.
}// End of 'namespace'.
