using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using NewRecap.Model;
using NewRecap.MyAppHelper;
using System.Security.Claims;

namespace NewRecap.Pages.AdminPages
{
    [Authorize]
    public class EditStoreCityModel : PageModel
    {
        [BindProperty]
        public LocationView Locations { get; set; } = new LocationView();
        public bool IsAdmin { get; set; }

        public IActionResult OnGet(int id)
        {
            /*--------------------ADMIN PRIV----------------------*/
            // Safely access the NameIdentifier claim
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            int userId = int.Parse(userIdClaim.Value); // Use the claim value only if it exists
            if (userIdClaim != null)
            {
                CheckIfUserIsAdmin(userId);
            }
            /*--------------------ADMIN PRIV----------------------*/

            if (!IsUserActive(userId))
            {
                return Forbid();
            }

            PopulateStoreCity(id);
            return Page();
        }// End of 'OnGet'.

        public IActionResult OnPost(int id)
        {
            var storeCity = (Locations.StoreCity ?? string.Empty).Trim();
            const int dbMax = 30;


            if (storeCity.Length > dbMax)
            {
                ModelState.AddModelError("Locations.StoreCity", "City must be at most 30 characters.");
            }

            if (string.IsNullOrWhiteSpace(storeCity))
            {
                ModelState.AddModelError("Locations.StoreCity", "City must be more than 0 characters.");
            }

            if (ModelState.IsValid)
            {

                using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
                {
                    string cmdText = "UPDATE StoreLocations SET StoreCity = @StoreCity WHERE StoreLocationID = @StoreLocationID";
                    SqlCommand cmd = new SqlCommand(cmdText, conn);
                    cmd.Parameters.AddWithValue("@StoreCity", Locations.StoreCity);
                    cmd.Parameters.AddWithValue("@StoreLocationID", id);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
                return RedirectToPage("BrowseStoreLocations");
            }
            else
            {
                OnGet(id);
                return Page();
            }
        }//End of 'OnPost'.

        private void PopulateStoreCity(int id)
        {
            using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
            {
                string query = "SELECT StoreLocationID, StoreCity FROM StoreLocations WHERE StoreLocationID = @StoreLocationID";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@StoreLocationID", id);
                conn.Open();
                SqlDataReader reader = cmd.ExecuteReader();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        Locations = new LocationView
                        {
                            StoreLocationID = reader.GetInt32(0),
                            StoreCity = reader.GetString(1)
                        };
                    }
                }
            }
        }//End of 'PopulateLocationList'.

        private bool IsUserActive(int userID)
        {
            using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
            {
                string sql = "SELECT IsActive FROM SystemUser WHERE SystemUserID = @SystemUserID";
                SqlCommand cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@SystemUserID", userID);

                conn.Open();
                var result = cmd.ExecuteScalar();

                return result != null && (bool)result;
            }
        }// End of 'IsUserActive'.

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
    }// End of 'EditStoreCity' Class.
}// End of 'namespace'.
