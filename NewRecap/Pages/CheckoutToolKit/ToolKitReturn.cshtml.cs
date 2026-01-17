using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using NewRecap.Model;
using NewRecap.MyAppHelper;
using System.Security.Claims;

namespace NewRecap.Pages.CheckoutToolKit
{
    [Authorize]
    [BindProperties]
    public class ToolKitReturnModel : PageModel
    {
        public List<SelectListItem> ToolKitOptions { get; set; } = new();
        public int SelectedToolKitId { get; set; }
        public bool IsAdmin { get; set; }
        public IActionResult OnGet()
        {
            int? employeeId = null;


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
                employeeId = GetEmployeeIdForUser(userId);
            }
            /*--------------------End of ADMIN PRIV----------------------*/

            PopulateToolKitOptions(employeeId);
            return Page();
        }// End of 'OnGet'.

        public IActionResult OnPost()
        {

            if (SelectedToolKitId <= 0)
            {
                ModelState.AddModelError("SelectedToolKitId", "Please select a toolkit to return.");
            }

            // Get SystemUserID from claims
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int systemUserId))
            {
                // Force re-login if somehow lost auth state
                return RedirectToPage("/Account/Login");
            }

            // Get EmployeeID for this user
            int employeeId = GetEmployeeIdForUser(systemUserId);
            if (employeeId == 0)
            {
                ModelState.AddModelError(string.Empty, "Could not resolve your employee record.");
            }

            if (ModelState.IsValid)
            {
                using (var conn = new SqlConnection(AppHelper.GetDBConnectionString()))
                {
                    conn.Open();
                    string updateCheckoutSql = @"
                        UPDATE ToolKit
                        SET IsActive = @IsActive, IsReturned = @IsReturned
                        WHERE ToolKitID = @ToolKitID;";

                    using (var cmd = new SqlCommand(updateCheckoutSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@IsActive", false);
                        cmd.Parameters.AddWithValue("@IsReturned", true);
                        cmd.Parameters.AddWithValue("@ToolKitID", SelectedToolKitId);
                        cmd.ExecuteNonQuery();
                    }

                    string deleteEmployeeToolkit = "DELETE FROM EmployeeToolKits WHERE ToolKitID = @ToolKitID;";
                    using (var cmd2 = new SqlCommand(deleteEmployeeToolkit, conn))
                    {
                        cmd2.Parameters.AddWithValue("@ToolKitID", SelectedToolKitId);
                        cmd2.ExecuteNonQuery();
                    }
                }

                return RedirectToPage("/Index");
            }
            else
            {
                OnGet();
                return Page();
            }
        }// End of 'OnPost'.

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
        }// End of 'GetEmployeeIdForUser'.

        /*private void PopulateToolKitOptions(int? employeeId)
        {
            ToolKitOptions = new List<SelectListItem>();

            using (var conn = new SqlConnection(AppHelper.GetDBConnectionString()))
            {
                string sql;

                if (IsAdmin)
                {
                    // Admins see all active toolkits
                    sql = @"
                SELECT ToolKitID, ToolKitName
                FROM ToolKit
                WHERE IsActive = 1
                ORDER BY ToolKitName;";
                }
                else
                {
                    // Regular users only see toolkits checked out to them
                    sql = @"
                SELECT tk.ToolKitID, tk.ToolKitName
                FROM ToolKit AS tk
                INNER JOIN EmployeeToolKits AS etk ON tk.ToolKitID = etk.ToolKitID
                WHERE IsActive = 1 AND EmployeeID = @EmployeeID
                ORDER BY tk.ToolKitName;";
                }

                using (var cmd = new SqlCommand(sql, conn))
                {
                    if (!IsAdmin)
                    {
                        cmd.Parameters.AddWithValue("@EmployeeID", employeeId.Value);
                    }
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
        }// End of 'PopulateToolKitOptions'.*/

        private void PopulateToolKitOptions(int? employeeId)
        {
            using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
            {
                if (IsAdmin)
                {
                    // Admins see all active toolkits
                    string sql = @"
                SELECT ToolKitID, ToolKitName, ToolKitBarcode
                FROM ToolKit
                WHERE IsActive = 1
                ORDER BY ToolKitName;";

                    using (SqlCommand command = new SqlCommand(sql, conn))
                    {
                        conn.Open();
                        SqlDataReader reader = command.ExecuteReader();
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                var toolkits = new SelectListItem()
                                {
                                    Value = reader["ToolKitID"].ToString(),
                                    Text = $"{reader["ToolKitName"]} ({reader["ToolKitBarcode"]})"
                                };
                                ToolKitOptions.Add(toolkits);
                            }
                        }
                    }
                }
                
            }
        }// End of 'PopulateToolKitOptions'.

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
    }// End of 'ToolKitReturn' Class.
}// End of 'namespace'.
