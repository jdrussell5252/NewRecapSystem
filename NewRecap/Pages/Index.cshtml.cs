using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using NewRecap.MyAppHelper;
using System.Data;
using System.Security.Claims;

namespace NewRecap.Pages
{
    [Authorize]
    public class IndexModel : PageModel
    {

        private readonly ILogger<IndexModel> _logger;
        public bool IsAdmin { get; set; }

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
        }

        public IActionResult OnGet()
        {
            var redirect = EnforcePasswordChange();
            if (redirect != null)
                return redirect;

            /*--------------------ADMIN PRIV----------------------*/
            // Check if the user is authenticated first
            if (User.Identity.IsAuthenticated)
            {
                // Safely access the NameIdentifier claim
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim != null)
                {
                    int userId = int.Parse(userIdClaim.Value); // Use the claim value only if it exists
                    CheckIfUserIsAdmin(userId);
                }
            }
            /*--------------------ADMIN PRIV----------------------*/
            return Page();
        }//End of 'OnGet'.

        private IActionResult EnforcePasswordChange()
        {
            // If not logged in, nothing to enforce
            if (!User.Identity.IsAuthenticated)
                return null;

            // Get the current user ID from the auth cookie
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                return null;

            bool mustChange = false;

            using (var conn = new SqlConnection(AppHelper.GetDBConnectionString()))
            {
                string query = "SELECT MustChangePassword FROM SystemUser WHERE SystemUserID = @SystemUserID;";

                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.Add("@SystemUserID", SqlDbType.Int).Value = userId;

                    conn.Open();
                    var result = cmd.ExecuteScalar();

                    if (result != null && result != DBNull.Value)
                    {
                        mustChange = Convert.ToBoolean(result);
                    }
                }
            }

            if (mustChange)
            {
                // Force the user back to EditPassword until they fix it
                return RedirectToPage("/Account/EditPassword");
            }

            // OK to continue
            return null;
        }// End of 'EnforcePasswordChange'.


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
    }
}// End of 'namespace'.
