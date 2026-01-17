using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using NewRecap.Model.Toolkits;
using NewRecap.MyAppHelper;
using System.Security.Claims;

namespace NewRecap.Pages.AdminPages.Toolkits
{
    [Authorize]
    public class AddToolKitModel : PageModel
    {
        public bool IsAdmin { get; set; }

        [BindProperty]
        public ToolKit ToolKit { get; set; } = new ToolKit();
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

        public IActionResult OnPost()
        {
            var toolkitName = (ToolKit.ToolKitName ?? string.Empty).Trim();
            var toolkitBarcode = (ToolKit.BarcodeValue ?? string.Empty).Trim();
            const int dbMax = 50;

            if (toolkitName.Length > dbMax)
            {
                ModelState.AddModelError("ToolKit.ToolKitName", "Toolkit name must be at most 50 characters.");
            }

            if (toolkitBarcode.Length > dbMax)
            {
                ModelState.AddModelError("ToolKit.ToolKitName", "Toolkit name must be at most 50 characters.");
            }

            if (ModelState.IsValid)
            {
                using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
                {
                    conn.Open();

                    string insertcmdText = "INSERT INTO ToolKit (ToolKitName, ToolKitBarcode, IsActive, IsReturned) VALUES (@ToolKitName, @ToolKitBarcode, @IsActive, @IsReturned);";
                    SqlCommand insertcmd = new SqlCommand(insertcmdText, conn);
                    insertcmd.Parameters.AddWithValue("@ToolKitName", ToolKit.ToolKitName);
                    insertcmd.Parameters.AddWithValue("@ToolKitBarcode", ToolKit.BarcodeValue);
                    insertcmd.Parameters.AddWithValue("@IsActive", false);
                    insertcmd.Parameters.AddWithValue("@IsReturned", false);

                    insertcmd.ExecuteNonQuery();
                }
                return RedirectToPage("/Index");
            }
            else
            {
                // If the model state is not valid, return to the same page with validation errors
                OnGet();
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
    }// End of 'AddToolKit' Class.
}// End of 'namespace'.
