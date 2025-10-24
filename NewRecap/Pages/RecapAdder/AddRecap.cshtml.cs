using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using NewRecap.Model;
//using NewRecap.MyAppHelper;
using System.Data.OleDb;
using System.Security.Claims;
using System.Xml;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace NewRecap.Pages.RecapAdder
{
    [Authorize]
    [BindProperties]
    public class AddRecapModel : PageModel
    {
        public Recap NewRecap { get; set; } = new Recap();
        public bool IsAdmin { get; set; }
        public List<EmployeeInfo> Employees { get; set; } = new List<EmployeeInfo>();
        public List<SelectListItem> Vehicles { get; set; } = new List<SelectListItem>();
        public int? SelectedVehicleID { get; set; }
        public List<int> SelectedEmployeeIds { get; set; } = new();
        public List<WorkSegment> WorkSegments { get; set; }

        public string connectionString = "Provider = Microsoft.ACE.OLEDB.12.0; Data Source = C:\\Users\\jaker\\OneDrive\\Desktop\\Nacspace\\New Recap\\NewRecapDB\\NewRecapDB.accdb;";


        public void OnGet()
        {
            /*--------------------ADMIN PRIV----------------------*/
            // Safely access the NameIdentifier claim
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null)
            {
                int userId = int.Parse(userIdClaim.Value); // Use the claim value only if it exists
                CheckIfUserIsAdmin(userId);
            }
            PopulateEmployeeList();
            PopulateVehicleList();
            /*--------------------ADMIN PRIV----------------------*/
        }// End of 'OnGet'.

        public IActionResult OnPost()
        {
            // Validate employee selection
            if (!SelectedEmployeeIds.Any())
            {
                ModelState.AddModelError("SelectedEmployeeIds", "Please select at least one employee.");
            }
            if (SelectedVehicleID <= 0)
            {
                ModelState.AddModelError("SelectedVehicleID", "Please select a Vehicle.");
            }

            // === Require at least 1 complete segment ===
            bool hasAnySegments = NewRecap.WorkSegments != null && NewRecap.WorkSegments.Any();
            bool hasAtLeastOneComplete =
                hasAnySegments &&
                NewRecap.WorkSegments.Any(s =>
                    // Work pair
                    (s.WorkStartDate.HasValue && s.WorkStart.HasValue &&
                     s.WorkEndDate.HasValue && s.WorkEnd.HasValue)
                    ||
                    // Drive pair
                    (s.DriveStartDate.HasValue && s.DriveStart.HasValue &&
                     s.DriveEndDate.HasValue && s.DriveEnd.HasValue)
                    ||
                    // Lunch pair
                    (s.LunchStartDate.HasValue && s.LunchStart.HasValue &&
                     s.LunchEndDate.HasValue && s.LunchEnd.HasValue)
                );

            if (!hasAtLeastOneComplete)
                ModelState.AddModelError("NewRecap.WorkSegments", "Please add at least one complete time segment (Work, Drive, or Lunch) with both start and end date/time.");
            if (ModelState.IsValid)
            {
                int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                using (OleDbConnection conn = new OleDbConnection(this.connectionString))
                {
                    conn.Open();

                    string cmdTextRecap = "INSERT INTO Recap (RecapWorkorderNumber, RecapDate, AddedBy, VehicleID, RecapDescription, RecapState, RecapCity) VALUES (@RecapWorkorderNumber, @RecapDate, @AddedBy, @VehicleID, @RecapDescription, @RecapState, @RecapCity);";
                    OleDbCommand cmdRecap = new OleDbCommand(cmdTextRecap, conn);
                    cmdRecap.Parameters.AddWithValue("@RecapWorkorderNumber", NewRecap.RecapWorkorderNumber);
                    cmdRecap.Parameters.AddWithValue("@RecapDate", NewRecap.RecapDate);
                    cmdRecap.Parameters.AddWithValue("@AddedBy", userId);

                    cmdRecap.Parameters.AddWithValue("@VehicleID", SelectedVehicleID);

                    cmdRecap.Parameters.AddWithValue("@RecapDescription", NewRecap.RecapDescription);
                    cmdRecap.Parameters.AddWithValue("@RecapState", NewRecap.RecapState);
                    cmdRecap.Parameters.AddWithValue("@RecapCity", NewRecap.RecapCity);
                    cmdRecap.ExecuteNonQuery();

                    int RecapID;
                    // Now fetch the generated AutoNumber (RecapID)
                    using (var idCmd = new OleDbCommand("SELECT @@IDENTITY;", conn))
                    {
                        RecapID = Convert.ToInt32(idCmd.ExecuteScalar());
                    }

                    foreach (var empId in SelectedEmployeeIds)
                    {

                        string cmdTextEmployeeRecap = "INSERT INTO EmployeeRecaps (RecapID, EmployeeID) VALUES (@RecapID, @EmployeeID)";
                        OleDbCommand cmdEmployeeRecap = new OleDbCommand(cmdTextEmployeeRecap, conn);
                        cmdEmployeeRecap.Parameters.AddWithValue("@RecapID", RecapID);
                        cmdEmployeeRecap.Parameters.AddWithValue("@EmployeeID", empId);
                        cmdEmployeeRecap.ExecuteNonQuery();
                    }

                    // For each segment that has at least one valid start/end pair
                    var segments = NewRecap.WorkSegments.Where(s =>
                        (s.WorkStart.HasValue && s.WorkEnd.HasValue) ||
                        (s.DriveStart.HasValue && s.DriveEnd.HasValue) ||
                        (s.LunchStart.HasValue && s.LunchEnd.HasValue));

                    foreach (var seg in segments)
                    {
                        const string sql = @"
                            INSERT INTO StartEndTime
                            (RecapID,
                            StartTime, EndTime, StartTimeDate, EndTimeDate,
                            StartDriveTime, EndDriveTime, StartDriveDate, EndDriveDate,
                            StartLunchTime, EndLunchTime, StartLunchDate, EndLunchDate)
                            VALUES
                            (@RecapID,
                            @StartTime, @EndTime, @StartTimeDate, @EndTimeDate,
                            @StartDriveTime, @EndDriveTime, @StartDriveDate, @EndDriveDate,
                            @StartLunchTime, @EndLunchTime, @StartLunchDate, @EndLunchDate);";

                        using var cmd = new OleDbCommand(sql, conn);

                        cmd.Parameters.Add("@RecapID", OleDbType.Integer).Value = RecapID;

                        cmd.Parameters.AddWithValue("@StartTime", (object?)seg.WorkStart ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@EndTime", (object?)seg.WorkEnd ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@StartTimeDate", (object?)seg.WorkStartDate ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@EndTimeDate", (object?)seg.WorkEndDate ?? DBNull.Value);

                        cmd.Parameters.AddWithValue("@StartDriveTime", (object?)seg.DriveStart ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@EndDriveTime", (object?)seg.DriveEnd ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@StartDriveDate", (object?)seg.DriveStartDate ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@EndDriveDate", (object?)seg.DriveEndDate ?? DBNull.Value);

                        cmd.Parameters.AddWithValue("@StartLunchTime", (object?)seg.LunchStart ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@EndLunchTime", (object?)seg.LunchEnd ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@StartLunchDate", (object?)seg.LunchStartDate ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@EndLunchDate", (object?)seg.LunchEndDate ?? DBNull.Value);

                        cmd.ExecuteNonQuery();
                    }
                }
                return RedirectToPage("/Index");
            }
            else
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim != null)
                {
                    int userId = int.Parse(userIdClaim.Value);
                    CheckIfUserIsAdmin(userId);
                }
                PopulateVehicleList();
                PopulateEmployeeList();

                return Page();
            }
        }// End of 'OnPost'.

        private void PopulateVehicleList()
        {
            using (OleDbConnection conn = new OleDbConnection(this.connectionString))
            {
                string query = "SELECT * FROM Vehicle";
                using (OleDbCommand command = new OleDbCommand(query, conn))
                {
                    conn.Open();
                    OleDbDataReader reader = command.ExecuteReader();
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            var vehicle = new SelectListItem()
                            {
                                Value = reader["VehicleID"].ToString(),
                                Text = $"{reader["VehicleNumber"]}, {reader["VehicleName"]}, Vin: {reader["VehicleVin"]}"
                            };
                            Vehicles.Add(vehicle);
                        }
                    }
                }
            }
        }// End of 'PopulateVechileList'.

        private void PopulateEmployeeList()
        {
            using (OleDbConnection conn = new OleDbConnection(this.connectionString))
            {
                string query = "SELECT EmployeeID, EmployeeFName, EmployeeLName FROM Employee;";
                OleDbCommand cmd = new OleDbCommand(query, conn);
                conn.Open();
                OleDbDataReader reader = cmd.ExecuteReader();
                if (reader.HasRows)
                {
                    while(reader.Read())
                    {
                        var employees = new EmployeeInfo();
                        employees.EmployeeID = int.Parse(reader["EmployeeID"].ToString());
                        employees.EmployeeFName = reader["EmployeeFName"].ToString();
                        employees.EmployeeLName = reader["EmployeeLName"].ToString();
                        employees.IsSelected = false;
                        Employees.Add(employees);
                    }
                }
            }
        }//End of 'PopulateEmployeeList'.

        /*--------------------ADMIN PRIV----------------------*/
        private void CheckIfUserIsAdmin(int userId)
        {
            using (var conn = new OleDbConnection(this.connectionString))
            {
                // Adjust names to match your schema exactly:
                // If your column is AccountTypeID instead of SystemUserRole, swap it below.
                string query = "SELECT SystemUserRole FROM SystemUser WHERE SystemUserID = @SystemUserID;";

                using (var cmd = new OleDbCommand(query, conn))
                {
                    // OleDb uses positional parameters (names ignored), so add in the same order as the '?'..
                    cmd.Parameters.AddWithValue("@SystemUserID", userId);

                    conn.Open();
                    var roleObj = cmd.ExecuteScalar();

                    // Handle both null and DBNull
                    if (roleObj != null && roleObj != DBNull.Value)
                    {
                        int role = Convert.ToInt32(roleObj);

                        // If your schema uses AccountTypeID (1=user, 2=admin), adjust accordingly
                        this.IsAdmin = (role == 2);
                        ViewData["IsAdmin"] = this.IsAdmin;
                    }
                    else
                    {
                        // No row or NULL role
                        this.IsAdmin = false;
                        ViewData["IsAdmin"] = false;
                    }
                }
            }
        }//End of 'CheckIfUserIsAdmin'.
        /*--------------------ADMIN PRIV----------------------*/

    }// End of 'AddRecap' Class.
}// End of 'namespace'.
