using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NewRecap.Model;
using System.Data.OleDb;
using System.Security.Claims;

namespace NewRecap.Pages.AdminPages
{
    [Authorize]
    public class EditStoreNumberModel : PageModel
    {
        [BindProperty]
        public LocationView Locations { get; set; } = new LocationView();
        public bool IsAdmin { get; set; }
        public string connectionString = "Provider = Microsoft.ACE.OLEDB.12.0; Data Source = C:\\Users\\jaker\\OneDrive\\Desktop\\Nacspace\\New Recap\\NewRecapDB\\NewRecapDB.accdb;";
        public void OnGet(int id)
        {
            /*--------------------ADMIN PRIV----------------------*/
            // Safely access the NameIdentifier claim
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null)
            {
                int userId = int.Parse(userIdClaim.Value); // Use the claim value only if it exists
                CheckIfUserIsAdmin(userId);
            }
            /*--------------------ADMIN PRIV----------------------*/
            PopulateLocationList(id);
        }

        public IActionResult OnPost(int id)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    using (OleDbConnection conn = new OleDbConnection(this.connectionString))
                    {
                        conn.Open();
                        // Ensure we have the original key on post (fallback to bound model)
                        var oldId = id != 0 ? id : Locations.StoreLocationID_Original; // see note below
                        var newId = Locations.StoreLocationID;

                        if (newId == oldId)
                            return RedirectToPage("BrowseStoreLocations");

                        // Optional: block duplicates before update
                        string queryExists = "SELECT COUNT(*) FROM [StoreLocations] WHERE [StoreLocationID] = ?";
                        OleDbCommand exists = new OleDbCommand(queryExists, conn);
                        
                        exists.Parameters.Add("@p1", OleDbType.Integer).Value = newId;
                        if (Convert.ToInt32(exists.ExecuteScalar()) > 0)
                        {
                            ModelState.AddModelError(nameof(Locations.StoreLocationID),
                                $"Store number {newId} already exists.");
                            return RedirectToPage("BrowseStoreLocations");
                        }
                        

                        string cmdText = "UPDATE StoreLocations SET StoreLocationID = ? WHERE StoreLocationID = ?";
                        OleDbCommand cmd = new OleDbCommand(cmdText, conn);
                        cmd.Parameters.Add("@p1", OleDbType.Integer).Value = newId; // SET value
                        cmd.Parameters.Add("@p2", OleDbType.Integer).Value = oldId;

                        cmd.ExecuteNonQuery();
                    }
                    return RedirectToPage("BrowseStoreLocations");
                }
                catch
                {
                    throw;
                }
            }
            return Page();
        }//End of 'OnPost'.

        private void PopulateLocationList(int id)
        {
            using (OleDbConnection conn = new OleDbConnection(this.connectionString))
            {
                string query = "SELECT * FROM StoreLocations WHERE StoreLocationID = @StoreLocationID";
                OleDbCommand cmd = new OleDbCommand(query, conn);
                cmd.Parameters.AddWithValue("@StoreLocationID", id);
                conn.Open();
                OleDbDataReader reader = cmd.ExecuteReader();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        Locations = new LocationView
                        {
                            StoreLocationID = reader.GetInt32(0)
                        };
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
