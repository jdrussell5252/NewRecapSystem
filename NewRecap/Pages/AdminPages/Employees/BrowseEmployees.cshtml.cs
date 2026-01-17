using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using NewRecap.Model.Employees;
using NewRecap.MyAppHelper;
using System.Security.Claims;

namespace NewRecap.Pages.AdminPages.Employees
{
    public class BrowseEmployeesModel : PageModel
    {
        public List<EmployeeView> Employees { get; set; } = new List<EmployeeView>();
        public bool IsAdmin { get; set; }

        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 5;
        public int TotalCount { get; set; }
        public int TotalPages => Math.Max(1, (int)Math.Ceiling((double)TotalCount / Math.Max(1, PageSize)));


        public IActionResult OnGet(int id, int pageNumber = 1, int pageSize = 5)
        {
            // Safely access the NameIdentifier claim
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            int userId = int.Parse(userIdClaim.Value); // Use the claim value only if it exists
            if (userIdClaim != null)
            {
                CheckIfUserIsAdmin(userId);
            }

            if (!User.IsInRole("Admin"))
            {
                return Forbid();
            }

            PopulateEmployeeList();

            // === Pagination logic ===
            PageNumber = pageNumber < 1 ? 1 : pageNumber;
            PageSize = pageSize < 1 ? 5 : pageSize;

            TotalCount = Employees.Count;

            // Clamp PageNumber so it’s not past the last page
            if (TotalCount > 0 && (PageNumber - 1) * PageSize >= TotalCount)
            {
                PageNumber = (int)Math.Ceiling((double)TotalCount / PageSize);
            }

            if (TotalCount > 0)
            {
                int skip = (PageNumber - 1) * PageSize;
                Employees = Employees
                    .Skip(skip)
                    .Take(PageSize)
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
                string cmdText1 = "DELETE FROM SystemUser WHERE EmployeeID = @EmployeeID";
                SqlCommand checkCmd1 = new SqlCommand(cmdText1, conn);
                checkCmd1.Parameters.AddWithValue("@EmployeeID", id);
                checkCmd1.ExecuteNonQuery();

                string cmdText = "DELETE FROM Employee WHERE EmployeeID = @EmployeeID";
                SqlCommand checkCmd = new SqlCommand(cmdText, conn);
                checkCmd.Parameters.AddWithValue("@EmployeeID", id);
                checkCmd.ExecuteNonQuery();
            }

            return RedirectToPage();
        }//End of 'OnPostDelete'.

        private void PopulateEmployeeList()
        {
            using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
            {
                string query = "SELECT e.EmployeeID, e.EmployeeFname, e.EmployeeLName, su.SystemUsername, su.SystemUserRole, su.IsActive FROM Employee AS e INNER JOIN SystemUser AS su ON su.EmployeeID = e.EmployeeID";
                SqlCommand cmd = new SqlCommand(query, conn);
                conn.Open();
                SqlDataReader reader = cmd.ExecuteReader();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        EmployeeView Aemployee = new EmployeeView
                        {
                            EmployeeID = reader.GetInt32(0),
                            EmployeeFName = reader.GetString(1),
                            EmployeeLName = reader.GetString(2),
                            EmployeeUsername = reader.GetString(3),
                            EmployeeRole = reader.GetBoolean(4),
                            IsActive = reader.GetBoolean(5)
                        };
                        Employees.Add(Aemployee);

                    }
                }
            }
        }//End of 'PopulateLocationList'.

        /*--------------------ADMIN PRIV----------------------*/
        private void CheckIfUserIsAdmin(int userId)
        {
            using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
            {
                string cmdText = "SELECT SystemUserRole FROM SystemUser WHERE SystemUserID = @SystemUserID";
                SqlCommand cmd = new SqlCommand(cmdText, conn);
                cmd.Parameters.Add("@SystemUserID", System.Data.SqlDbType.Int).Value = userId;
                conn.Open();
                var result = cmd.ExecuteScalar();

                bool isAdmin = result != null && Convert.ToBoolean(result);

                IsAdmin = isAdmin;
                ViewData["IsAdmin"] = isAdmin;
            }
        }//End of 'CheckIfUserIsAdmin'.
        /*--------------------ADMIN PRIV----------------------*/
    }// End of 'BrowseEmployees' Class.
}// End of 'namespace'.
