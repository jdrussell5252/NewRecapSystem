using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using NewRecap.MyAppHelper;

using System.Security.Claims;

namespace NewRecap.Pages.CheckoutToolKit
{
    [Authorize]
    public class ToolKitCheckoutModel : PageModel
    {
        public List<SelectListItem> ToolKitOptions { get; set; } = new();
        [BindProperty]
        public int SelectedToolKitId { get; set; }
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
            /*--------------------End of ADMIN PRIV----------------------*/
            PopulateToolKitOptions();
        }// End of 'OnGet'.

        public IActionResult OnPost()
        {

            if (SelectedToolKitId <= 0)
            {
                ModelState.AddModelError("SelectedToolKitId", "Please select a toolkit to check out.");
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int systemUserId))
            {
                // Force re-login if somehow lost auth state
                return RedirectToPage("/Account/Login");
            }
            int employeeId = GetEmployeeIdForUser(systemUserId);

            if (ModelState.IsValid)
            {
                using (var conn = new SqlConnection(AppHelper.GetDBConnectionString()))
                {
                    conn.Open();

                    //Mark checkout row as returned for this toolkit + employee
                    /*string updateCheckoutSql = @"
                        UPDATE ToolKit
                        SET IsActive = @IsActive, IsReturned = @IsReturned, EmployeeID = @EmployeeID
                        WHERE ToolKitID = @ToolKitID;";*/
                    string insertEmployeeToolkits = "INSERT INTO EmployeeToolKits (EmployeeID, ToolKitID) VALUES (@EmployeeID, @ToolKitID);";
                    using (var cmd = new SqlCommand(insertEmployeeToolkits, conn))
                    {
                        cmd.Parameters.AddWithValue("@EmployeeID", employeeId);
                        cmd.Parameters.AddWithValue("@ToolKitID", SelectedToolKitId);
                        cmd.ExecuteNonQuery();
                    }

                    string updateToolKit = "UPDATE ToolKit Set IsActive = @IsActive, IsReturned = @IsReturned WHERE ToolKitID = @ToolKitID";
                    using (var cmd2 = new SqlCommand(updateToolKit, conn))
                    {
                        // Order of AddWithValue must match parameter order in SQL
                        cmd2.Parameters.AddWithValue("@IsActive", true);
                        cmd2.Parameters.AddWithValue("@IsReturned", false);
                        cmd2.Parameters.AddWithValue("@ToolKitID", SelectedToolKitId);
                        cmd2.ExecuteNonQuery();
                    }
                }
            }
            else
            {
                OnGet();
                return Page();
            }

            return RedirectToPage("/Index");
        }

        private void PopulateToolKitOptions()
        {
            ToolKitOptions = new List<SelectListItem>();

            using (var conn = new SqlConnection(AppHelper.GetDBConnectionString()))
            {
                string sql = @"
                    SELECT ToolKitID, ToolKitName
                    FROM ToolKit
                    WHERE IsActive = 0
                    ORDER BY ToolKitName;";

                using (var cmd = new SqlCommand(sql, conn))
                {
                    conn.Open();
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            int id = r.GetInt32(0);
                            string name = r.IsDBNull(1) ? "" : r.GetString(1);

                            ToolKitOptions.Add(new SelectListItem
                            {
                                Value = id.ToString(),
                                Text = name
                            });
                        }
                    }
                }
            }
        }

        private int GetEmployeeIdForUser(int systemUserID)
        {
            using (var conn = new SqlConnection(AppHelper.GetDBConnectionString()))
            {
                string query = "SELECT EmployeeID FROM SystemUser WHERE SystemUserID = @SystemUserID;";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@SystemUserID", systemUserID);
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read() && !reader.IsDBNull(0))
                        {
                            return reader.GetInt32(0);
                        }
                    }
                }
            }
            return 0;
        }

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
    }// End of 'ToolKitCheckout' Class.
}// End of 'namespace'.
