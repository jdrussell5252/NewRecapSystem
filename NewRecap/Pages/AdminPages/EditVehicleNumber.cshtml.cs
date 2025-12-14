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
    public class EditVehicleNumberModel : PageModel
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
            /*--------------------ADMIN PRIV----------------------*/
            // Safely access the NameIdentifier claim
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null)
            {
                int userId = int.Parse(userIdClaim.Value); // Use the claim value only if it exists
                CheckIfUserIsAdmin(userId);
                PopulateLocationList(id);
            }
            /*--------------------ADMIN PRIV----------------------*/
            return Page();
        }

        public IActionResult OnPost(int id)
        {
            // normalize the value first
            var vehicleNumber = (Vehicles.VehicleNumber ?? string.Empty).Trim();
            const int dbMax = 6;

            if (vehicleNumber.Length > dbMax)
            {
                ModelState.AddModelError("Vehicles.VehicleNumber", "Vehicle number must be at most 6 characters."); ;
            }

            if (string.IsNullOrWhiteSpace(vehicleNumber))
            {
                ModelState.AddModelError("Vehicles.VehicleNumber", "Vehicle Number must be more than 0 characters.");
            }

            if (ModelState.IsValid)
            {


                using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
                {
                    string cmdText = "UPDATE Vehicle SET VehicleNumber = @VehicleNumber WHERE VehicleID = @VehicleID";
                    using (SqlCommand cmd = new SqlCommand(cmdText, conn))
                    {
                        // use SqlParameter with explicit type/size instead of AddWithValue
                        cmd.Parameters.Add("@VehicleNumber", SqlDbType.NVarChar, dbMax).Value = vehicleNumber;
                        cmd.Parameters.Add("@VehicleID", SqlDbType.Int).Value = id;
                        conn.Open();
                        cmd.ExecuteNonQuery();
                    }
                }
                return RedirectToPage("BrowseVehicles");
                

            }
            else
            {
                OnGet(id);
                return Page();
            }
        }// End of 'OnPost'.


        private void PopulateLocationList(int id)
        {
            using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
            {
                string query = "SELECT VehicleID, VehicleNumber FROM Vehicle WHERE VehicleID = @VehicleID";
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
                            VehicleNumber = reader.GetString(1)
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

                // If SystemUserRole is True, set IsUserAdmin to true
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
    }
}
