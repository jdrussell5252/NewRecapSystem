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
    public class EditEmployeeFNameModel : PageModel
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
        }

        // Pass in the ID of the Employee.
        public IActionResult OnPost(int id)
        {
            var firstName = (Employees.EmployeeFName ?? string.Empty).Trim();
            const int dbMaxName = 50;

            if (firstName.Length > dbMaxName)
            {
                ModelState.AddModelError("Employees.EmployeeFName", "Username must be at most 50 characters.");
            }
            if (string.IsNullOrWhiteSpace(firstName))
            {
                ModelState.AddModelError("Employees.EmployeeFName", "Username must be more than 0 characters.");
            }


            // If the model state is valid, it will run the update.
            if (ModelState.IsValid)
            {
                // Set up the connection with the DB.
                using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
                {
                    // Query for update
                    string cmdText = "UPDATE Employee SET EmployeeFName = @EmployeeFName WHERE EmployeeID = @EmployeeID";
                    // Create the command.
                    SqlCommand cmd = new SqlCommand(cmdText, conn);
                    // Add the value to the command
                    cmd.Parameters.AddWithValue("@EmployeeFName", Employees.EmployeeFName);
                    cmd.Parameters.AddWithValue("@EmployeeID", id);
                    // Open the connection with the db.
                    conn.Open();
                    // Execute the command.
                    cmd.ExecuteNonQuery();
                }
                // Redirect to page if the model is valid.
                return RedirectToPage("BrowseEmployees");
            }
            else
            {
                OnGet(id);
                // Return the page if the model state is not valid.
                return Page();
            }
        }//End of 'OnPost'.

        private void PopulateEmployeeList(int id)
        {
            using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
            {
                string query = "SELECT EmployeeID, EmployeeFName FROM Employee WHERE EmployeeID = @EmployeeID";
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
                            EmployeeFName = reader.GetString(1)
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
    }
}
