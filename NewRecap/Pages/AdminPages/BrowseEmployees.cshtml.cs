using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NewRecap.Model;
using System.Data.OleDb;
using System.Security.Claims;

namespace NewRecap.Pages.AdminPages
{
    [Authorize]
    public class BrowseEmployeesModel : PageModel
    {
        public List<EmployeeView> Employees { get; set; } = new List<EmployeeView>();
        public bool IsAdmin { get; set; }
        public string connectionString = "Provider = Microsoft.ACE.OLEDB.12.0; Data Source = C:\\Users\\jaker\\OneDrive\\Desktop\\Nacspace\\New Recap\\NewRecapDB\\NewRecapDB.accdb;";
        public void OnGet(int id)
        {
            // Safely access the NameIdentifier claim
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null)
            {
                int userId = int.Parse(userIdClaim.Value); // Use the claim value only if it exists
                CheckIfUserIsAdmin(userId);
            }
            PopulateEmployeeList();
        }

        public IActionResult OnPostDelete(int id)
        {
            // delete the book from the database
            using (OleDbConnection conn = new OleDbConnection(this.connectionString))
            {
                conn.Open();
                string cmdText1 = "DELETE FROM SystemUser WHERE EmployeeID = @EmployeeID";
                OleDbCommand checkCmd1 = new OleDbCommand(cmdText1, conn);
                checkCmd1.Parameters.AddWithValue("@EmployeeID", id);
                checkCmd1.ExecuteNonQuery();

                string cmdText = "DELETE FROM Employee WHERE EmployeeID = @EmployeeID";
                OleDbCommand checkCmd = new OleDbCommand(cmdText, conn);
                checkCmd.Parameters.AddWithValue("@EmployeeID", id);
                checkCmd.ExecuteNonQuery();
            }

            return RedirectToPage();
        }//End of 'OnPostDelete'.

        private void PopulateEmployeeList()
        {
            using (OleDbConnection conn = new OleDbConnection(this.connectionString))
            {
                string query = "SELECT e.EmployeeID, e.EmployeeFname, e.EmployeeLName, su.SystemUsername, su.SystemUserRole FROM Employee AS e INNER JOIN SystemUser AS su ON su.EmployeeID = e.EmployeeID";
                OleDbCommand cmd = new OleDbCommand(query, conn);
                conn.Open();
                OleDbDataReader reader = cmd.ExecuteReader();
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
                            EmployeeRole = reader.GetInt32(4)
                        };
                        Employees.Add(Aemployee);

                    }
                }
            }
        }//End of 'PopulateLocationList'.

        /*--------------------ADMIN PRIV----------------------*/
        private void CheckIfUserIsAdmin(int userId)
        {
            using (var conn = new OleDbConnection(this.connectionString))
            {
                // Adjust names to match your schema exactly:
                // If your column is AccountTypeID instead of SystemUserRole, swap it below.
                string query = "SELECT SystemUserRole FROM SystemUser WHERE SystemUserID = @SystemUserID;";

                using (var cmd = new OleDbCommand(query, conn))
                {
                    // OleDb uses positional parameters (names ignored), so add in the same order as the '?'..
                    cmd.Parameters.AddWithValue("@SystemUserID", userId);

                    conn.Open();
                    var roleObj = cmd.ExecuteScalar();

                    // Handle both null and DBNull
                    if (roleObj != null && roleObj != DBNull.Value)
                    {
                        int role = Convert.ToInt32(roleObj);

                        // If your schema uses AccountTypeID (1=user, 2=admin), adjust accordingly
                        this.IsAdmin = (role == 2);
                        ViewData["IsAdmin"] = this.IsAdmin;
                    }
                    else
                    {
                        // No row or NULL role
                        this.IsAdmin = false;
                        ViewData["IsAdmin"] = false;
                    }
                }
            }
        }//End of 'CheckIfUserIsAdmin'.
        /*--------------------ADMIN PRIV----------------------*/
    }
}
