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
    public class AddStoreLocationsModel : PageModel
    {
        [BindProperty]
        public MyLocations NewLocation { get; set; }
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
            var storeCity = (NewLocation.StoreCity ?? string.Empty).Trim();
            var storeState = (NewLocation.StoreState ?? string.Empty).Trim();
            var storeNumber = NewLocation.StoreNumber;
            const int dbMaxState = 2;
            const int dbMaxCity = 30;

            if (storeCity.Length > dbMaxCity)
            {
                ModelState.AddModelError("NewLocation.StoreCity", "City must be at most 30 characters.");
            }

            if (storeState.Length > dbMaxState)
            {
                ModelState.AddModelError("NewLocation.StoreState", "State must be at most 2 characters.");
            }


            if (storeNumber < 0)
            {
                ModelState.AddModelError("NewLocation.StoreNumber", "Store Number must be greater than 0."); ;
            }

            if (ModelState.IsValid)
            {
                using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
                {

                    conn.Open();
                    string insertcmdText = "INSERT INTO StoreLocations (StoreNumber, StoreState, StoreCity) VALUES (@StoreNumber, @StoreState, @StoreCity);";
                    SqlCommand insertcmd = new SqlCommand(insertcmdText, conn);
                    insertcmd.Parameters.AddWithValue("@StoreNumber", NewLocation.StoreNumber);
                    insertcmd.Parameters.AddWithValue("@StoreState", NewLocation.StoreState);
                    insertcmd.Parameters.AddWithValue("@StoreCity", NewLocation.StoreCity);

                    insertcmd.ExecuteNonQuery();
                }
                return RedirectToPage("BrowseStoreLocations");
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
    }// End of 'AddStoreLocations' Class.
}// End of 'namespace'.
