using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using NewRecap.Model.SiteLayouts;
using NewRecap.MyAppHelper;
using System.Data;
using System.Security.Claims;

namespace NewRecap.Pages.SiteOwl
{
    [Authorize]
    [BindProperties]
    public class AddLayoutsModel : PageModel
    {
        public bool IsAdmin { get; set; }
        public MyLayouts NewLayout { get; set; } = new();
        public IActionResult OnGet()
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
            }
            /*--------------------ADMIN PRIV----------------------*/
            return Page();
        }// End of 'OnGet'.

        public IActionResult OnPost()
        {
            if (ModelState.IsValid)
            {
                using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
                {
                    // Save the file to the project.
                    NewLayout.LayoutImage.CopyTo(new FileStream(Path.Combine("wwwroot", "LayoutImages", NewLayout.LayoutImage.FileName), FileMode.Create));

                    //Get the image URl
                    string imageUrl = "/LayoutImages/" + NewLayout.LayoutImage.FileName;
                    conn.Open();

                    string insertcmdText = "INSERT INTO SiteLayout (LayoutName, ImagePath, IsActive) VALUES (@LayoutName, @ImagePath, @IsActive);";
                    SqlCommand insertcmd = new SqlCommand(insertcmdText, conn);
                    insertcmd.Parameters.AddWithValue("@LayoutName", NewLayout.LayoutName);
                    insertcmd.Parameters.AddWithValue("@ImagePath", imageUrl);
                    insertcmd.Parameters.AddWithValue("@IsActive", true);

                    insertcmd.ExecuteNonQuery();
                }
                return RedirectToPage("/SiteOwl/BrowseLayouts");
            }
            else
            {
                OnGet();
                return Page();
            }
        }// End of 'OnPost'.

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
    }// End of 'AddLayouts' Class.
}// End of 'namespace'.
