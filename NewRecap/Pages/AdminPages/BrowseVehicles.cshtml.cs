using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NewRecap.Model;
using System.Data.OleDb;
using System.Security.Claims;

namespace NewRecap.Pages.AdminPages
{
    public class BrowseVehiclesModel : PageModel
    {
        public List<VehicleView> Vehicles { get; set; } = new List<VehicleView>();
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
                PopulateVehicleList();
            }
            /*--------------------ADMIN PRIV----------------------*/
        }

        public IActionResult OnPostDelete(int id)
        {
            // delete the book from the database
            using (OleDbConnection conn = new OleDbConnection(this.connectionString))
            {
                conn.Open();


                string deleteCmdText = "DELETE FROM Vehicle WHERE VehicleID = @VehicleID";
                OleDbCommand deleteCmd = new OleDbCommand(deleteCmdText, conn);
                deleteCmd.Parameters.AddWithValue("@VehicleID", id);
                deleteCmd.ExecuteNonQuery();

            }

            return RedirectToPage();
        }//End of 'OnPostDelete'.

        private void PopulateVehicleList()
        {
            using (OleDbConnection conn = new OleDbConnection(this.connectionString))
            {
                string query = "SELECT VehicleID, VehicleNumber, VehicleVin, VehicleName FROM Vehicle";
                OleDbCommand cmd = new OleDbCommand(query, conn);
                conn.Open();
                OleDbDataReader reader = cmd.ExecuteReader();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        VehicleView AVehicle = new VehicleView
                        {
                            VehicleID = reader.GetInt32(0),
                            VehicleNumber = reader.GetInt32(1),
                            VehicleVin = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                            VehicleName = reader.IsDBNull(3) ? string.Empty : reader.GetString(3)
                        };
                        Vehicles.Add(AVehicle);

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
