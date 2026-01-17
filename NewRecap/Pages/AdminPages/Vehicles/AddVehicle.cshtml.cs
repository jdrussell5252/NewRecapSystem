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
    public class AddVehicleModel : PageModel
    {
        [BindProperty]
        public MyVehicles NewVehicles { get; set; }
        public bool IsAdmin { get; set; }

        public IActionResult OnGet()
        {
            /*--------------------ADMIN PRIV----------------------*/
            if (!User.IsInRole("Admin"))
            {
                return Forbid();
            }
            // Safely access the NameIdentifier claim
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null)
            {
                int userId = int.Parse(userIdClaim.Value); // Use the claim value only if it exists
                CheckIfUserIsAdmin(userId);
            }
            return Page();
            /*--------------------ADMIN PRIV----------------------*/
        }// End of 'OnGet'.

        public IActionResult OnPost(int id)
        {
            var vehicleModel = (NewVehicles.VehicleModel ?? string.Empty).Trim();
            var vehicleNumber = (NewVehicles.VehicleNumber ?? string.Empty).Trim();
            const int dbMaxNumber = 6;
            const int dbMaxModel = 50;

            if (vehicleModel.Length > dbMaxModel)
            {
                ModelState.AddModelError("NewVehicles.VehicleModel", "Vehicle Model must be at most 50 characters.");
            }

            if (vehicleNumber.Length > dbMaxNumber)
            {
                ModelState.AddModelError("NewVehicles.VehicleNumber", "Vehicle number must be at most 6 characters."); ;
            }

            if (ModelState.IsValid)
            {
                using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
                {
                    conn.Open();
                    string insertcmdText = "INSERT INTO Vehicle (VehicleNumber, VehicleModel, IsActive) VALUES (@VehicleNumber, @VehicleModel, @IsActive);";
                    SqlCommand insertcmd = new SqlCommand(insertcmdText, conn);
                    insertcmd.Parameters.AddWithValue("@VehicleNumber", NewVehicles.VehicleNumber);
                    insertcmd.Parameters.AddWithValue("@VehicleModel", NewVehicles.VehicleModel);
                    insertcmd.Parameters.AddWithValue("@IsActive", true);

                    insertcmd.ExecuteNonQuery();
                }
                return RedirectToPage("BrowseVehicles");
            }
            else
            {
                OnGet();
                // If the model state is not valid, return to the same page with validation errors
                return Page();
            }
        }// End of 'OnPost'.

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
    }// End of 'AddVehicle' Class.
}// End of 'namespace'.
