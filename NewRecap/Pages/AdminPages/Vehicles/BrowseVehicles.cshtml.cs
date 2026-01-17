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
    public class BrowseVehiclesModel : PageModel
    {
        public List<VehicleView> Vehicles { get; set; } = new List<VehicleView>();
        public bool IsAdmin { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 5;
        public int TotalCount { get; set; }
        public int TotalPages => Math.Max(1, (int)Math.Ceiling((double)TotalCount / Math.Max(1, PageSize)));

        public IActionResult OnGet(int pageNumber = 1, int pageSize = 5)
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
                PopulateVehicleList();
            }
            /*--------------------ADMIN PRIV----------------------*/

            // === Pagination logic ===
            PageNumber = pageNumber < 1 ? 1 : pageNumber;
            PageSize = pageSize < 1 ? 5 : pageSize;

            TotalCount = Vehicles.Count;

            // Clamp PageNumber so it’s not past the last page
            if (TotalCount > 0 && (PageNumber - 1) * PageSize >= TotalCount)
            {
                PageNumber = (int)Math.Ceiling((double)TotalCount / PageSize);
            }

            if (TotalCount > 0)
            {
                int skip = (PageNumber - 1) * PageSize;
                Vehicles = Vehicles
                    .Skip(skip)
                    .Take(PageSize)
                    .ToList();
            }
            return Page();
        }// End of 'OnGet'.

        /*public IActionResult OnPostDelete(int id)
        {
            // delete the book from the database
            using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
            {
                conn.Open();
                string deleteCmdText = "DELETE FROM Vehicle WHERE VehicleID = @VehicleID";
                SqlCommand deleteCmd = new SqlCommand(deleteCmdText, conn);
                deleteCmd.Parameters.AddWithValue("@VehicleID", id);
                deleteCmd.ExecuteNonQuery();

            }

            return RedirectToPage();
        }//End of 'OnPostDelete'.*/

        private void PopulateVehicleList()
        {
            using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
            {
                string query = "SELECT * FROM Vehicle";
                SqlCommand cmd = new SqlCommand(query, conn);
                conn.Open();
                SqlDataReader reader = cmd.ExecuteReader();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        VehicleView AVehicle = new VehicleView
                        {
                            VehicleID = reader.GetInt32(0),
                            VehicleNumber = reader.GetString(1),
                            VehicleModel = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                            IsActive = reader.GetBoolean(3)
                        };
                        Vehicles.Add(AVehicle);

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
    }// End of 'BrowseVehicles' Class.
}// End of 'namespace'.