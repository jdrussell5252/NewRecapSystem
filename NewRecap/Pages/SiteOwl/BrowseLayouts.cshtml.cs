using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using NewRecap.Model.SiteLayouts;
using NewRecap.Model.StoreLocations;
using NewRecap.MyAppHelper;
using System.Data;
using System.Security.Claims;

namespace NewRecap.Pages.SiteOwl
{
    [Authorize]
    public class BrowseLayoutsModel : PageModel
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 11;
        public int TotalCount { get; set; }
        public int TotalPages => Math.Max(1, (int)Math.Ceiling((double)TotalCount / Math.Max(1, PageSize)));
        public bool IsAdmin { get; set; }
        public List<SiteLayout> Layouts { get; set; } = new();
        public IActionResult OnGet(int pageNumber = 1, int pageSize = 11)
        {
            // Safely access the NameIdentifier claim
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            /*--------------------ADMIN PRIV----------------------*/
            if (userIdClaim != null)
            {
                int userId = int.Parse(userIdClaim.Value); // Use the claim value only if it exists
                if (!IsUserActive(userId))
                {
                    return Forbid();
                }
                CheckIfUserIsAdmin(userId);
                LoadLayouts();
            }
            /*--------------------ADMIN PRIV----------------------*/
            // === Pagination logic ===
            PageNumber = pageNumber < 1 ? 1 : pageNumber;
            PageSize = pageSize < 1 ? 11 : pageSize;

            TotalCount = Layouts.Count;

            // Clamp PageNumber so it’s not past the last page
            if (TotalCount > 0 && (PageNumber - 1) * PageSize >= TotalCount)
            {
                PageNumber = (int)Math.Ceiling((double)TotalCount / PageSize);
            }

            if (TotalCount > 0)
            {
                int skip = (PageNumber - 1) * PageSize;
                Layouts = Layouts
                    .Skip(skip)
                    .Take(PageSize)
                    .ToList();
            }
            return Page();
        }// End of 'OnGet'.

        public IActionResult OnPostDelete(int id)
        {
            using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
            {
                conn.Open();
                string deleteCmdText = "DELETE FROM SiteLayout WHERE SiteLayoutID = @SiteLayoutID";
                SqlCommand deleteCmd = new SqlCommand(deleteCmdText, conn);
                deleteCmd.Parameters.AddWithValue("@SiteLayoutID", id);
                deleteCmd.ExecuteNonQuery();

            }
            return RedirectToPage();
        }//End of 'OnPostDelete'.

        private void LoadLayouts()
        {
            using var conn = new SqlConnection(AppHelper.GetDBConnectionString());

            var sql = @"
                SELECT SiteLayoutID, LayoutName, ImagePath
                FROM SiteLayout
                WHERE IsActive = 1
            ";

            using var cmd = new SqlCommand(sql, conn);
            conn.Open();
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                Layouts.Add(new SiteLayout
                {
                    SiteLayoutID = r.GetInt32(0),
                    //StoreLocationID = r.GetInt32(1),
                    LayoutName = r.GetString(1),
                    ImagePath = r.GetString(2)
                    //UpdatedOn = r.GetDateTime(3)
                });
            }
        }// End of 'LoadLayouts'.

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
    }// End of 'BrowseLayouts' Class.
}// End of 'namespace'.