using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using NewRecap.Model.Vehicles;
using NewRecap.MyAppHelper;
using System.Security.Claims;

namespace NewRecap.Pages.AdminPages.Vehicles
{
    [Authorize]
    public class EditVehicleNameModel : PageModel
    {
        [BindProperty]
        public VehicleView Vehicles { get; set; } = new VehicleView();
        public bool IsAdmin { get; set; }

        public IActionResult OnGet(int id)
        {

            if (!User.IsInRole("Admin"))
            {
                return Forbid();
            }

            // Safely access the NameIdentifier claim
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            /*--------------------ADMIN PRIV----------------------*/
            if (userIdClaim != null)
            {
                int userId = int.Parse(userIdClaim.Value); // Use the claim value only if it exists
                CheckIfUserIsAdmin(userId);
            }
            /*--------------------ADMIN PRIV----------------------*/

            PopulateVehicleList(id);
            return Page();
        }// End of 'OnGet'.


        public IActionResult OnPost(int id)
        {
            // normalize the value first
            var vehicleModel = (Vehicles.VehicleModel ?? string.Empty).Trim();
            const int dbMax = 50;

            if (vehicleModel.Length > dbMax)
            {
                ModelState.AddModelError("Vehicles.VehicleModel", "Vehicle Model must be at most 50 characters.");
            }

            if (string.IsNullOrWhiteSpace(vehicleModel))
            {
                ModelState.AddModelError("Vehicles.VehicleModel", "Vehicle Model must be more than 0 characters.");
            }


            if (ModelState.IsValid)
            {

                using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
                {
                    string cmdText = "UPDATE Vehicle SET VehicleModel = @VehicleModel WHERE VehicleID = @VehicleID";
                    SqlCommand cmd = new SqlCommand(cmdText, conn);
                    cmd.Parameters.AddWithValue("@VehicleModel", vehicleModel);
                    cmd.Parameters.AddWithValue("@VehicleID", id);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
                return RedirectToPage("BrowseVehicles");
            }
            else
            {
                OnGet(id);
                return Page();
            }
        }//End of 'OnPost'.


        private void PopulateVehicleList(int id)
        {
            using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
            {
                string query = "SELECT VehicleID, VehicleModel FROM Vehicle WHERE VehicleID = @VehicleID";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@VehicleID", id);
                conn.Open();
                SqlDataReader reader = cmd.ExecuteReader();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        Vehicles = new VehicleView
                        {
                            VehicleID = reader.GetInt32(0),
                            VehicleModel = reader.GetString(1)
                        };
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
    }// End of 'EditVehicleModel' Class.
}// End of 'namespace'.
