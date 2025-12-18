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
    public class BrowseStoreLocationsModel : PageModel
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 11;
        public int TotalCount { get; set; }
        public int TotalPages => Math.Max(1, (int)Math.Ceiling((double)TotalCount / Math.Max(1, PageSize)));
        public List<LocationView> Locations { get; set; } = new List<LocationView>();
        public bool IsAdmin { get; set; }

        [TempData]
        public string ErrorMessage { get; set; }


        public IActionResult OnGet(int pageNumber = 1, int pageSize = 11)
        {
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
            PopulateLocationList();

            // === Pagination logic ===
            PageNumber = pageNumber < 1 ? 1 : pageNumber;
            PageSize = pageSize < 1 ? 11 : pageSize;

            TotalCount = Locations.Count;

            // Clamp PageNumber so it’s not past the last page
            if (TotalCount > 0 && (PageNumber - 1) * PageSize >= TotalCount)
            {
                PageNumber = (int)Math.Ceiling((double)TotalCount / PageSize);
            }

            if (TotalCount > 0)
            {
                int skip = (PageNumber - 1) * PageSize;
                Locations = Locations
                    .Skip(skip)
                    .Take(PageSize)
                    .ToList();
            }
            return Page();
        }// End of 'OnGet'.

        private void PopulateLocationList()
        {
            using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
            {
                string query = "SELECT StoreLocationID, StoreNumber, StoreCity, StoreState, IsActive FROM StoreLocations ORDER BY StoreNumber";
                SqlCommand cmd = new SqlCommand(query, conn);
                conn.Open();
                SqlDataReader reader = cmd.ExecuteReader();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        LocationView ALocation = new LocationView
                        {
                            StoreLocationID = reader.GetInt32(0),
                            StoreNumber = reader.GetInt32(1),
                            StoreCity = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                            StoreState = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                            IsActive = reader.GetBoolean(4)
                        };
                        Locations.Add(ALocation);

                    }
                }
            }
        }//End of 'PopulateLocationList'.

        /*public IActionResult OnPostDelete(int id)
        {
            // delete the book from the database
            using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
            {
                conn.Open();
                string deleteCmdText = "DELETE FROM StoreLocations WHERE StoreLocationID = @StoreLocationID";
                SqlCommand deleteCmd = new SqlCommand(deleteCmdText, conn);
                deleteCmd.Parameters.AddWithValue("@StoreLocationID", id);
                deleteCmd.ExecuteNonQuery();
            }
            return RedirectToPage();
        }//End of 'OnPostDelete'.*/

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
    }// End of 'BrowseStoreLocations' Class.
}// End of 'namespace'.
