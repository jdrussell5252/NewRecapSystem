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
    public class EditToolKitModel : PageModel
    {
        [BindProperty]
        public ToolKitView ToolKits { get; set; } = new ToolKitView();
        public bool IsAdmin { get; set; }

        public IActionResult OnGet(int id)
        {

            if (!User.IsInRole("Admin"))
            {
                return Forbid();
            }

            // Safely access the NameIdentifier claim
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            /*--------------------ADMIN PRIV----------------------*/
            if (userIdClaim != null)
            {
                int userId = int.Parse(userIdClaim.Value); // Use the claim value only if it exists
                CheckIfUserIsAdmin(userId);
            }
            /*--------------------ADMIN PRIV----------------------*/

            PopulateToolKitList(id);
            return Page();
        }// End of 'Onget'.

        public IActionResult OnPost(int id)
        {
            var kitName = (ToolKits.ToolKitName ?? string.Empty).Trim();
            var barcodeValue = (ToolKits.BarcodeValue ?? string.Empty).Trim();
            const int dbMaxName = 50;
            const int dbMaxBarcode = 50;


            if (kitName.Length > dbMaxName)
            {
                ModelState.AddModelError("ToolKits.ToolKitName", "Toolkit name must be at most 50 characters.");
            }

            if (barcodeValue.Length > dbMaxBarcode)
            {
                ModelState.AddModelError("ToolKits.BarcodeValue", "Toolkit name must be at most 15 characters.");
            }

            if (ModelState.IsValid)
            {

                using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
                {
                    string cmdText = "UPDATE ToolKit SET ToolKitName = @ToolKitName, ToolKitBarcode = @ToolKitBarcode WHERE ToolKitID = @ToolKitID";
                    SqlCommand cmd = new SqlCommand(cmdText, conn);
                    cmd.Parameters.AddWithValue("@ToolKitName", ToolKits.ToolKitName);
                    cmd.Parameters.AddWithValue("@ToolKitBarcode", ToolKits.BarcodeValue);
                    cmd.Parameters.AddWithValue("@ToolKitID", id);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
                return RedirectToPage("BrowseToolKits");
                

            }
            else
            {
                OnGet(id);
                return Page();
            }
        }//End of 'OnPost'.

        private void PopulateToolKitList(int id)
        {
            using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
            {
                string query = "SELECT ToolKitID, ToolKitName, ToolKitBarcode FROM ToolKit WHERE ToolKitID = @ToolKitID";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@ToolKitID", id);
                conn.Open();
                SqlDataReader reader = cmd.ExecuteReader();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        ToolKits = new ToolKitView
                        {
                            ToolKitID = reader.GetInt32(0),
                            ToolKitName = reader.GetString(1),
                            BarcodeValue = reader.GetString(2)
                        };
                    }
                }
            }
        }//End of 'PopulateToolKitList'.

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
    }// End of 'EditToolKit' Class.
}// End of 'namespace'.
