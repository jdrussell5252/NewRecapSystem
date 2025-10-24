using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NewRecap.Model;
using System.Data.OleDb;
using System.Security.Claims;

namespace NewRecap.Pages.AdminPages
{
    [Authorize]
    public class EditStoreStateModel : PageModel
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
                        string cmdText = "UPDATE StoreLocations SET StoreState = @StoreState WHERE StoreLocationID = @StoreLocationID";
                        OleDbCommand cmd = new OleDbCommand(cmdText, conn);
                        cmd.Parameters.AddWithValue("@StoreState", Locations.StoreState);
                        cmd.Parameters.AddWithValue("@StoreLocationID", id);
                        conn.Open();
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
                            StoreLocationID = reader.GetInt32(0),
                            StoreState = reader.GetString(3)
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
