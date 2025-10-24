using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using NewRecap.Model;
using System.Data.OleDb;
using System.Security.Claims;

namespace NewRecap.Pages.AdminPages
{
    [Authorize]
    public class BrowseStoreLocationsModel : PageModel
    {
        public List<LocationView> Locations { get; set; } = new List<LocationView>();
        public bool IsAdmin { get; set; }

        [TempData]
        public string ErrorMessage { get; set; }
        public string connectionString = "Provider = Microsoft.ACE.OLEDB.12.0; Data Source = C:\\Users\\jaker\\OneDrive\\Desktop\\Nacspace\\New Recap\\NewRecapDB\\NewRecapDB.accdb;";

        public void OnGet()
        {
            // Safely access the NameIdentifier claim
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null)
            {
                int userId = int.Parse(userIdClaim.Value); // Use the claim value only if it exists
                CheckIfUserIsAdmin(userId);
                PopulateLocationList();
            }
        }

        private void PopulateLocationList()
        {
            using (OleDbConnection conn = new OleDbConnection(this.connectionString))
            {
                string query = "SELECT StoreLocationID, StoreNumber, StoreCity, StoreState FROM StoreLocations";
                OleDbCommand cmd = new OleDbCommand(query, conn);
                conn.Open();
                OleDbDataReader reader = cmd.ExecuteReader();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        LocationView ALocation = new LocationView
                        {
                            StoreLocationID = reader.GetInt32(0),
                            StoreNumber = reader.GetInt32(1),
                            StoreCity = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                            StoreState = reader.IsDBNull(3) ? string.Empty : reader.GetString(3)
                        };
                        Locations.Add(ALocation);

                    }
                }
            }
        }//End of 'PopulateLocationList'.

        public IActionResult OnPostDelete(int id)
        {
            // delete the book from the database
            using (OleDbConnection conn = new OleDbConnection(this.connectionString))
            {
                conn.Open();
                string cmdText = "SELECT COUNT(*) FROM RecapLocation WHERE LocationID = @LocationID";
                OleDbCommand checkCmd = new OleDbCommand(cmdText, conn);
                checkCmd.Parameters.AddWithValue("@LocationID", id);

                
                int usageCount = (int)checkCmd.ExecuteScalar();

                if (usageCount > 0)
                {
                    ErrorMessage = "This location is in use and cannot be deleted.";
                    return RedirectToPage();
                }

                
                string deleteCmdText = "DELETE FROM StoreLocations WHERE StoreLocationID = @StoreLocationID";
                OleDbCommand deleteCmd = new OleDbCommand(deleteCmdText, conn);
                deleteCmd.Parameters.AddWithValue("@StoreLocationID", id);
                deleteCmd.ExecuteNonQuery();

            }

            return RedirectToPage();
        }//End of 'OnPostDelete'.

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
