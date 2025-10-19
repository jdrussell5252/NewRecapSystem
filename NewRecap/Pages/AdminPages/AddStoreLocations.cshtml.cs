using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NewRecap.Model;
using System.Data.OleDb;
using System.Security.Claims;

namespace NewRecap.Pages.AdminPages
{
    [Authorize]
    public class AddStoreLocationsModel : PageModel
    {
        [BindProperty]
        public MyLocations NewLocation { get; set; }
        public LocationView Locations { get; set; } = new LocationView();
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
            }
        }

        public IActionResult OnPost(int id)
        {
            if (ModelState.IsValid)
            {
                using (OleDbConnection conn = new OleDbConnection(this.connectionString))
                {

                    conn.Open();

                    string cmdText = "SELECT Count(*) FROM StoreLocations WHERE StoreNumber = @StoreNumber AND StoreState = @StoreState AND StoreCity = @StoreCity;";
                    OleDbCommand checkCmd = new OleDbCommand(cmdText, conn);
                    checkCmd.Parameters.AddWithValue("@StoreLocationID", NewLocation.StoreNumber);
                    checkCmd.Parameters.AddWithValue("@StoreState", NewLocation.StoreState);
                    checkCmd.Parameters.AddWithValue("@StoreCity", NewLocation.StoreCity);
                    
                    /*
                    int Count = (int)checkCmd.ExecuteScalar();
                    if (Count > 0)
                    {
                        ModelState.AddModelError(string.Empty, "This location already exists.");
                        return RedirectToPage("BrowseStoreLocations");
                    }
                    */
                    

                    string insertcmdText = "INSERT INTO StoreLocations (StoreNumber, StoreState, StoreCity) VALUES (@StoreNumber, @StoreState, @StoreCity);";
                    OleDbCommand insertcmd = new OleDbCommand(insertcmdText, conn);
                    insertcmd.Parameters.AddWithValue("@StoreNumber", NewLocation.StoreNumber);
                    insertcmd.Parameters.AddWithValue("@StoreState", NewLocation.StoreState);
                    insertcmd.Parameters.AddWithValue("@StoreCity", NewLocation.StoreCity);

                    insertcmd.ExecuteNonQuery();
                }
                return RedirectToPage("BrowseStoreLocations");
            }
            // If the model state is not valid, return to the same page with validation errors
            return Page();
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
