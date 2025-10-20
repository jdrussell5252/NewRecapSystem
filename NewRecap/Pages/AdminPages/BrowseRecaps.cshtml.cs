using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NewRecap.Model;
using System.Data.OleDb;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace NewRecap.Pages.AdminPages
{
    [Authorize]
    public class BrowseRecapsModel : PageModel
    {
        public List<RecapView> Recaps { get; set; } = new List<RecapView>();
        public bool IsAdmin { get; set; }
        public string connectionString = "Provider = Microsoft.ACE.OLEDB.12.0; Data Source = C:\\Users\\jaker\\OneDrive\\Desktop\\Nacspace\\New Recap\\NewRecapDB\\NewRecapDB.accdb;";
        public void OnGet()
        {
            /*--------------------ADMIN PRIV----------------------*/
            // Safely access the NameIdentifier claim
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null)
            {
                int userId = int.Parse(userIdClaim.Value); // Use the claim value only if it exists
                CheckIfUserIsAdmin(userId);
                PopulateRecapList();
            }
            /*--------------------ADMIN PRIV----------------------*/
        }

        public IActionResult OnPostDelete(int id)
        {
            // delete the book from the database
            using (OleDbConnection conn = new OleDbConnection(this.connectionString))
            {
                conn.Open();


                string deleteCmdText = "DELETE FROM Recap WHERE RecapID = @RecapID";
                OleDbCommand deleteCmd = new OleDbCommand(deleteCmdText, conn);
                deleteCmd.Parameters.AddWithValue("@RecapID", id);
                deleteCmd.ExecuteNonQuery();

            }

            return RedirectToPage();
        }//End of 'OnPostDelete'.

        private void PopulateRecapList()
        {
            using (OleDbConnection conn = new OleDbConnection(this.connectionString))
            {
                string query = "SELECT r.RecapID, r.RecapWorkorderNumber, r.RecapDate, r.RecapDescription, r.RecapState, r.RecapCity, r.RecapAssetNumber, r.RecapSerialNumber, v.VehicleID, v.VehicleName, v.VehicleNumber, v.VehicleVin, se.TotalWorkTime, se.TotalLunchTime, se.TotalDriveTime, sl.StoreLocationID, sl.StoreNumber, sl.StoreState, sl.StoreCity FROM ((Recap AS r LEFT JOIN Vehicle AS v ON v.VehicleID = r.VehicleID) LEFT JOIN StoreLocations AS sl ON sl.StoreLocationID = r.StoreLocationID) LEFT JOIN StartEnd AS se ON se.RecapID = r.RecapID ORDER BY r.RecapDate DESC, r.RecapID DESC;";

                OleDbCommand cmd = new OleDbCommand(query, conn);
                conn.Open();
                OleDbDataReader reader = cmd.ExecuteReader();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        RecapView ARecap = new RecapView
                        {
                            RecapID = reader.GetInt32(0),
                            RecapWorkorderNumber = reader.GetInt32(1),
                            RecapDate = reader.GetDateTime(2),
                            RecapDescription = reader.GetString(3),
                            RecapState = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                            RecapCity = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),

                            RecapAssetNumber = reader.IsDBNull(6) ? null : reader.GetInt32(6),
                            RecapSerialNumber = reader.IsDBNull(7) ? string.Empty : reader.GetString(7),

                            VehicleID = reader.IsDBNull(8) ? null : reader.GetInt32(8),
                            VehicleName = reader.IsDBNull(9) ? null : reader.GetString(9),
                            VehicleNumber = reader.IsDBNull(10) ? null : reader.GetInt32(10),
                            VehicleVin = reader.IsDBNull(11) ? null : reader.GetString(11),

                            TotalWorkTime = reader.IsDBNull(12) ? 0.0 : Math.Round(reader.GetDouble(12), 2),
                            TotalLunchTime = reader.IsDBNull(13) ? 0.0 : Math.Round(reader.GetDouble(13), 2),
                            TotalDriveTime = reader.IsDBNull(14) ? 0.0 : Math.Round(reader.GetDouble(14), 2),

                            StoreLocationID = reader.IsDBNull(15) ? null : reader.GetInt32(15),
                            StoreNumber = reader.IsDBNull(16) ? null : reader.GetInt32(16),
                            StoreState = reader.IsDBNull(17) ? null : reader.GetString(17),
                            StoreCity = reader.IsDBNull(18) ? null : reader.GetString(18),


                            RecapEmployees = PopulateRecapEmployees(reader.GetInt32(0)),
                        };
                        Recaps.Add(ARecap);

                    }
                }
            }
        }//End of 'PopulateRecapList'.

        private List<string> PopulateRecapEmployees(int recapID)
        {
            List<string> Employees = new List<string>();
            using (OleDbConnection conn = new OleDbConnection(this.connectionString))
            {
                string query = "SELECT e.EmployeeFName, e.EmployeeLName " +
                               "FROM Employee AS e " +
                               "INNER JOIN EmployeeRecaps AS er ON e.EmployeeID = er.EmployeeID " +
                               "WHERE er.RecapID = @RecapID";
                OleDbCommand cmd = new OleDbCommand(query, conn);
                cmd.Parameters.AddWithValue("@RecapID", recapID);
                conn.Open();
                OleDbDataReader reader = cmd.ExecuteReader();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        string fName = reader.IsDBNull(0) ? "" : reader.GetString(0);
                        string lName = reader.IsDBNull(1) ? "" : reader.GetString(1);
                        Employees.Add($"{fName} {lName}");
                    }
                }
            }
            return Employees;
        }

        /*--------------------ADMIN PRIV----------------------*/
        private void CheckIfUserIsAdmin(int userId)
        {
            using (var conn = new OleDbConnection(this.connectionString))
            {
                // Adjust names to match your schema exactly:
                // If your column is AccountTypeID instead of SystemUserRole, swap it below.
                string query = "SELECT SystemUserRole FROM SystemUser WHERE SystemUserID = ?;";

                using (var cmd = new OleDbCommand(query, conn))
                {
                    // OleDb uses positional parameters (names ignored), so add in the same order as the '?'..
                    cmd.Parameters.Add("@?", OleDbType.Integer).Value = userId;

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
