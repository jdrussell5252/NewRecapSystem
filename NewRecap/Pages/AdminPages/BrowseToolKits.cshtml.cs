using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using NewRecap.Model;
using NewRecap.MyAppHelper;
using System.Data;
using System.Security.Claims;

namespace NewRecap.Pages.AdminPages
{
    [Authorize]
    public class BrowseToolKitsModel : PageModel
    {
        public List<ToolKitView> Toolkit { get; set; } = new List<ToolKitView>();
        public int? CurrentEmployeeId { get; set; }

        public bool IsAdmin { get; set; }

        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 5;
        public int TotalCount { get; set; }
        public int TotalPages => Math.Max(1, (int)Math.Ceiling((double)TotalCount / Math.Max(1, PageSize)));

        [BindProperty(SupportsGet = true)]
        public string? FilterToolkitName { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? FilterToolkitBarcode { get; set; }
        public IActionResult OnGet(int pageNumber = 1, int pageSize = 5)
        {
            var redirect = EnforcePasswordChange();
            if (redirect != null)
                return redirect;

            int? currentEmployeeId = null;
            /*--------------------ADMIN PRIV----------------------*/
            // Safely access the NameIdentifier claim
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null)
            {
                int userId = int.Parse(userIdClaim.Value); // Use the claim value only if it exists
                CheckIfUserIsAdmin(userId);
                currentEmployeeId = GetEmployeeIdForUser(userId);
                CurrentEmployeeId = currentEmployeeId;
            }
            /*--------------------End of ADMIN PRIV----------------------*/

            PopulateToolkitList(currentEmployeeId);

            // === Pagination logic ===
            PageNumber = pageNumber < 1 ? 1 : pageNumber;
            PageSize = pageSize < 1 ? 5 : pageSize;

            TotalCount = Toolkit.Count;

            // Clamp PageNumber so it’s not past the last page
            if (TotalCount > 0 && (PageNumber - 1) * PageSize >= TotalCount)
            {
                PageNumber = (int)Math.Ceiling((double)TotalCount / PageSize);
            }

            if (TotalCount > 0)
            {
                int skip = (PageNumber - 1) * PageSize;
                Toolkit = Toolkit
                    .Skip(skip)
                    .Take(PageSize)
                    .ToList();
            }


            // --- Filter by exact customer name ---
            if (!string.IsNullOrWhiteSpace(FilterToolkitName))
            {
                var term = FilterToolkitName.Trim();

                Toolkit = Toolkit
                    .Where(r => !string.IsNullOrWhiteSpace(r.ToolKitName) &&
                                string.Equals(r.ToolKitName.Trim(), term, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            // --- Filter by exact customer name ---
            if (!string.IsNullOrWhiteSpace(FilterToolkitBarcode))
            {
                var term = FilterToolkitBarcode.Trim();

                Toolkit = Toolkit
                    .Where(r => !string.IsNullOrWhiteSpace(r.BarcodeValue) &&
                                string.Equals(r.BarcodeValue.Trim(), term, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
            return Page();
        }// End of 'OnGet'.

        public IActionResult OnPostDelete(int id)
        {
            // delete the book from the database
            using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
            {
                conn.Open();

                // delete the toolkit.
                string deleteTkSql = "DELETE FROM ToolKit WHERE ToolKitID = @ToolKitID";
                using (var cmd2 = new SqlCommand(deleteTkSql, conn))
                {
                    cmd2.Parameters.AddWithValue("@ToolKitID", id);
                    cmd2.ExecuteNonQuery();
                }

            }

            return RedirectToPage();
        }//End of 'OnPostDelete'.

        private void PopulateToolkitList(int? currentEmployeeId)
        {
            using (var conn = new SqlConnection(AppHelper.GetDBConnectionString()))
            {
                conn.Open();

                if (IsAdmin)
                {
                    // Admins see all toolkits
                    string query = @"
                        SELECT 
                            tk.ToolKitID,
                            tk.ToolKitName,
                            tk.ToolKitBarcode,
                            tk.IsActive,
                            etk.EmployeeID,
                            e.EmployeeFName,
                            e.EmployeeLName
                        FROM ToolKit AS tk
                        LEFT JOIN EmployeeToolKits AS etk ON tk.ToolKitID = etk.ToolKitID
                        LEFT JOIN Employee AS e ON etk.EmployeeID = e.EmployeeID
                        ORDER BY ToolKitName;";

                    SqlCommand cmd = new SqlCommand(query, conn);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                int toolKitId = reader.GetInt32(0);
                                string name = reader.IsDBNull(1) ? "" : reader.GetString(1);
                                string barcode = reader.IsDBNull(2) ? "" : reader.GetString(2);
                                bool isActive = !reader.IsDBNull(3) && reader.GetBoolean(3);
                                //int toolkitEmployeeId = reader.GetInt32(4);

                                string employeeName = null;
                                if (!reader.IsDBNull(5) || !reader.IsDBNull(6))
                                {
                                    string f = reader.IsDBNull(5) ? "" : reader.GetString(5);
                                    string l = reader.IsDBNull(6) ? "" : reader.GetString(6);
                                    if (!string.IsNullOrWhiteSpace(f) || !string.IsNullOrWhiteSpace(l))
                                        employeeName = $"{f} {l}".Trim();
                                }

                                ToolKitView AToolkit = new ToolKitView
                                {
                                    ToolKitID = toolKitId,
                                    ToolKitName = name,
                                    BarcodeValue = barcode,
                                    IsActive = isActive,
                                    //EmployeeID = toolkitEmployeeId,
                                    CurrentEmployeeName = employeeName,

                                    // Only the person who has it (or an admin) can return it
                                    /*CanReturn = isActive
                                        && (
                                                (currentEmployeeId.HasValue && toolkitEmployeeId == currentEmployeeId)
                                                || IsAdmin
                                            )*/
                                    CanReturn = isActive && (IsAdmin)
                                };

                                Toolkit.Add(AToolkit);
                            }
                        }
                    }
                }
                /*else
                {
                    query = @"
                SELECT 
                    tk.ToolKitID,
                    tk.ToolKitName,
                    tk.ToolKitBarcode,
                    tk.IsActive,
                    tk.EmployeeID,
                    e.EmployeeFName,
                    e.EmployeeLName
                FROM ToolKit AS tk
                    LEFT JOIN Employee AS e
                    ON tk.EmployeeID = e.EmployeeID
                WHERE tk.EmployeeID = @EmployeeID
                ORDER BY tk.ToolKitName;";
                }*/
            }

            /*using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
            {
                string query = "SELECT ToolKitID, ToolKitName, ToolKitBarcode, IsActive FROM ToolKit WHERE ToolKitID = @ToolKitID";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@ToolKidID", id);
                conn.Open();
                SqlDataReader reader = cmd.ExecuteReader();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        Toolkit = new ToolKitView
                        {
                            ToolKitID = reader.GetInt32(0),
                            ToolKitName = reader.GetString(1)
                        };
                    }
                }
            }*/

        }//End of 'PopulateToolkitList'.



        private int? GetEmployeeIdForUser(int systemUserId)
        {
            using (var conn = new SqlConnection(AppHelper.GetDBConnectionString()))
            {
                string query = "SELECT EmployeeID FROM SystemUser WHERE SystemUserID = @SystemUserID;";

                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@SystemUserID", systemUserId);

                    conn.Open();
                    var obj = cmd.ExecuteScalar();

                    if (obj == null || obj == DBNull.Value)
                        return null;

                    return Convert.ToInt32(obj);
                }
            }
        }// End of 'GetEmployeeIdForUser'.

        private IActionResult EnforcePasswordChange()
        {
            // If not logged in, nothing to enforce
            if (!User.Identity.IsAuthenticated)
                return null;

            // Get the current user ID from the auth cookie
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                return null;

            bool mustChange = false;

            using (var conn = new SqlConnection(AppHelper.GetDBConnectionString()))
            {
                string query = "SELECT MustChangePassword FROM SystemUser WHERE SystemUserID = @SystemUserID;";

                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.Add("@SystemUserID", SqlDbType.Int).Value = userId;

                    conn.Open();
                    var result = cmd.ExecuteScalar();

                    if (result != null && result != DBNull.Value)
                    {
                        mustChange = Convert.ToBoolean(result);
                    }
                }
            }

            if (mustChange)
            {
                // Force the user back to EditPassword until they fix it
                return RedirectToPage("/Account/EditPassword");
            }

            // OK to continue
            return null;
        }// End of 'EnforcePasswordChange'.

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
    }// End of 'BrowseToolKits' Class.
}// End of 'namespace'.
