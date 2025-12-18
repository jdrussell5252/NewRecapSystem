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
    public class EditStoreNumberModel : PageModel
    {
        [BindProperty]
        public LocationView Locations { get; set; } = new LocationView();
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
            }
            /*--------------------ADMIN PRIV----------------------*/
            PopulateLocationList(id);
            return Page();
        }// End of 'OnGet'.

        public IActionResult OnPost(int id)
        {
            // normalize the value first
            var storeNumber = Locations.StoreNumber;

            if (storeNumber < 0)
            {
                ModelState.AddModelError("Locations.StoreNumber", "Store Number must be greater than or equal to 0."); ;
            }

            if (storeNumber == null)
            {
                ModelState.AddModelError("Locations.StoreNumber", "Store Number must not be null."); ;
            }

            if (ModelState.IsValid)
            {
                    using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
                    {
                        string cmdText = "UPDATE StoreLocations SET StoreNumber = @StoreNumber WHERE StoreLocationID = @StoreLocationID";
                        SqlCommand cmd = new SqlCommand(cmdText, conn);
                        cmd.Parameters.AddWithValue("@StoreNumber", Locations.StoreNumber);
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

        private void PopulateLocationList(int id)
        {
            using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
            {
                string query = "SELECT StoreLocationID, StoreNumber FROM StoreLocations WHERE StoreLocationID = @StoreLocationID";
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
                            StoreNumber = reader.GetInt32(1)
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
    }// End of 'EditStoreNumber' Class.
}// End of 'namespace'.
