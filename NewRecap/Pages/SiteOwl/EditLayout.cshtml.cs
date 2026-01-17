using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using NewRecap.Model.SiteLayouts;
using NewRecap.Model.StoreLocations;
using NewRecap.MyAppHelper;
using System.Security.Claims;

namespace NewRecap.Pages.SiteOwl
{
    [BindProperties]
    [Authorize]
    public class EditLayoutModel : PageModel
    {
        public bool IsAdmin { get; set; }
        public MyLayoutView MyLayouts { get; set; } = new MyLayoutView();
        public List<SelectListItem> Cameras { get; set; } = new List<SelectListItem>();
        public List<HardwareView> ActiveCameras { get; set; } = new();
        public int SelectedCameraID { get; set; }

        public IActionResult OnGet(int id)
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
            LoadLayout(id);
            PopulateCameraTypes();
            LoadCameras();
            return Page();
        }// End of 'OnGet'.

        private void LoadLayout(int id)
        {
            using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
            {
                string query = "SELECT SiteLayoutID, LayoutName, ImagePath FROM SiteLayout WHERE SiteLayoutID = @SiteLayoutID";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@SiteLayoutID", id);
                conn.Open();
                SqlDataReader reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    MyLayouts = new MyLayoutView
                    {
                        id = reader.GetInt32(0),
                        LayoutName = reader.GetString(1),
                        ImagePath = reader.GetString(2)
                    };
                }
            }
        }//End of 'LoadLayout'.

        /*private void PopulateCameraTypes()
        {
            using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
            {
                string query = "SELECT * FROM Cameras WHERE IsActive = 1 ORDER BY CameraName ";
                SqlCommand cmd = new SqlCommand(query, conn);
                conn.Open();
                SqlDataReader reader = cmd.ExecuteReader();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        var cameras = new SelectListItem
                        {
                            Value = reader["CameraTypeID"].ToString(),
                            Text = $"{reader["CameraName"]}"
                        };
                        Cameras.Add(cameras);

                    }
                }
            }
        }//End of 'PopulateLocationList'.

        private void LoadCameras()
        {
            using var conn = new SqlConnection(AppHelper.GetDBConnectionString());

            var sql = @"
                SELECT CameraTypeID, CameraType, CameraName, ImagePath
                FROM Cameras
                WHERE IsActive = 1
            ";

            using var cmd = new SqlCommand(sql, conn);
            conn.Open();
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                ActiveCameras.Add(new HardwareView
                {
                    CameraID = r.GetInt32(0),
                    //StoreLocationID = r.GetInt32(1),
                    CameraType = r.GetString(1),
                    CameraName = r.GetString(2),
                    ImagePath = r.GetString(3)
                    //UpdatedOn = r.GetDateTime(3)
                });
            }
        }// End of 'LoadCameras'.*/

        private void PopulateCameraTypes()
        {
            using var conn = new SqlConnection(AppHelper.GetDBConnectionString());
            string query = "SELECT CameraTypeID, CameraName FROM Cameras WHERE IsActive = 1 ORDER BY CameraName";
            using var cmd = new SqlCommand(query, conn);

            conn.Open();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                Cameras.Add(new SelectListItem
                {
                    Value = reader.GetInt32(0).ToString(),
                    Text = reader.GetString(1)
                });
            }
        }


        private void LoadCameras()
        {
            using var conn = new SqlConnection(AppHelper.GetDBConnectionString());
            var sql = @"
        SELECT CameraTypeID, CameraName, CameraType, ImagePath
        FROM Cameras
        WHERE IsActive = 1
    ";
            using var cmd = new SqlCommand(sql, conn);

            conn.Open();
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                ActiveCameras.Add(new HardwareView
                {
                    CameraTypeID = r.GetInt32(0),          // this is CameraTypeID (PK)
                    CameraName = r.GetString(1),
                    CameraType = r.GetString(2),
                    ImagePath = r.GetString(3)
                });
            }
        }





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
    }// End of 'EditLayoutModel' Class.
}// End of 'namespace'.
