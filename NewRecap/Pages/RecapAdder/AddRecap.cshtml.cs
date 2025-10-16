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
        //public List<SelectListItem> Locations { get; set; } = new List<SelectListItem>();
        public List<int> SelectedEmployeeIds { get; set; } = new();
        public List<WorkSegment> WorkSegments { get; set; }

        public string connectionString = "Provider = Microsoft.ACE.OLEDB.12.0; Data Source = C:\\Users\\jaker\\OneDrive\\Desktop\\Nacspace\\New Recap\\NewRecapDB\\NewRecapDB.accdb;";


        public void OnGet()
        {
            //PopulateLocationList();
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
            if(ModelState.IsValid)
            {
                int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                using (OleDbConnection conn = new OleDbConnection(this.connectionString))
                {
                    conn.Open();

                    int LocationID = NewRecap.LocationID;
                    string cmdTextRecap = "INSERT INTO Recap (RecapWorkorderNumber, RecapDate, AddedBy, VehicleID, RecapDescription, RecapState, RecapCity) VALUES (@RecapDate, @AddedBy, @VehicleID, @RecapDescription, @RecapState, @RecapCity);";
                    OleDbCommand cmdRecap = new OleDbCommand(cmdTextRecap, conn);
                    cmdRecap.Parameters.AddWithValue("@RecapDate", NewRecap.RecapWorkorderNumber);
                    cmdRecap.Parameters.AddWithValue("@RecapDate", NewRecap.RecapDate);
                    cmdRecap.Parameters.AddWithValue("@AddedBy", userId);
                    cmdRecap.Parameters.AddWithValue("@VehicleID", NewRecap.VehicleID);
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

                    string cmdTextRecapLocation = "INSERT INTO RecapLocation (LocationID, RecapID) VALUES (@LocationID, @RecapID);";
                    OleDbCommand cmdRecapLocation = new OleDbCommand(cmdTextRecapLocation, conn);
                    cmdRecapLocation.Parameters.AddWithValue("@LocationID", LocationID);
                    cmdRecapLocation.Parameters.AddWithValue("@RecapID", RecapID);
                    cmdRecapLocation.ExecuteNonQuery();

                    /*Work Segments*/
                    foreach (var seg in NewRecap.WorkSegments.Where(s => s.WorkStart.HasValue && s.WorkEnd.HasValue))
                    {
                        string cmdTextRecapTimes = "INSERT INTO StartEnd (RecapID, EmployeeID, StartTime, EndTime, StartTimeDate, EndTimeDate) VALUES (@RecapID, @EmployeeID, @StartTime, @EndTime, @StartTimeDate, @EndTimeDate);";
                        OleDbCommand cmdTimes = new OleDbCommand(cmdTextRecapTimes, conn);
                        cmdTimes.Parameters.AddWithValue("@RecapID", RecapID);
                        cmdTimes.Parameters.AddWithValue("@EmployeeID", userId);
                        cmdTimes.Parameters.AddWithValue("@StartTime", seg.WorkStart.Value);
                        cmdTimes.Parameters.AddWithValue("@EndTime", seg.WorkEnd.Value);
                        cmdTimes.Parameters.AddWithValue("@StartTimeDate", seg.WorkStartDate.Value);
                        cmdTimes.Parameters.AddWithValue("@EndTimeDate", seg.WorkEndDate.Value);
                        cmdTimes.ExecuteNonQuery();
                    }

                    foreach (var seg in NewRecap.WorkSegments.Where(s => s.DriveStart.HasValue && s.DriveEnd.HasValue))
                    {
                        string cmdTextRecapTimes = "INSERT INTO StartEnd (RecapID, EmployeeID, StartDriveTime, StartDriveDate, EndDriveTime, EndDriveDate) VALUES (@RecapID, @EmployeeID, @StartDriveTime, @StartDriveDate, @EndDriveTime, @EndDriveDate);";
                        OleDbCommand cmdTimes = new OleDbCommand(cmdTextRecapTimes, conn);
                        cmdTimes.Parameters.AddWithValue("@RecapID", RecapID);
                        cmdTimes.Parameters.AddWithValue("@EmployeeID", userId);
                        cmdTimes.Parameters.AddWithValue("@StartDriveTime", seg.DriveStart.Value);
                        cmdTimes.Parameters.AddWithValue("@EndDriveTime", seg.DriveEnd.Value);
                        cmdTimes.Parameters.AddWithValue("@StartDriveDate", seg.DriveStartDate.Value);
                        cmdTimes.Parameters.AddWithValue("@EndDriveDate", seg.DriveEndDate.Value);
                        cmdTimes.ExecuteNonQuery();
                    }

                    foreach (var seg in NewRecap.WorkSegments.Where(s => s.LunchStart.HasValue && s.LunchEnd.HasValue))
                    {
                        string cmdTextRecapTimes = "INSERT INTO StartEnd (RecapID, EmployeeID, StartLunchTime, EndLunchTime, StartLunchDate, EndLunchDate) VALUES (@RecapID, @EmployeeID, @StartLunchTime, @EndLunchTime, @StartLunchDate, @EndLunchDate);";
                        OleDbCommand cmdTimes = new OleDbCommand(cmdTextRecapTimes, conn);
                        cmdTimes.Parameters.AddWithValue("@RecapID", RecapID);
                        cmdTimes.Parameters.AddWithValue("@EmployeeID", userId);
                        cmdTimes.Parameters.AddWithValue("@StartLunchTime", seg.LunchStart.Value);
                        cmdTimes.Parameters.AddWithValue("@EndLunchTime", seg.LunchEnd.Value);
                        cmdTimes.Parameters.AddWithValue("@StartLunchDate", seg.LunchStartDate.Value);
                        cmdTimes.Parameters.AddWithValue("@EndLunchDate", seg.LunchEndDate.Value);
                        cmdTimes.ExecuteNonQuery();
                    }
                    /*End of Work Segments*/
                }
                return RedirectToAction("/Pages/Index");
            }
            else
            {

                PopulateEmployeeList();
                //PopulateLocationList();
                PopulateVehicleList();
                return Page();
            }
        }// End of 'OnPost'.

        private void PopulateVehicleList()
        {
            using (OleDbConnection conn = new OleDbConnection(this.connectionString))
            {
                string query = "SELECT VehicleID, VehicleName, VehicleVin FROM Vehicle";
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
                                Text = $"{reader["VehicleName"]}, Vin: {reader["VehicleVin"]}"
                            };
                            Vehicles.Add(vehicle);
                        }
                    }
                }
            }
        }// End of 'PopulateVechileList'.

        /*
        private void PopulateLocationList()
        {
            using (OleDbConnection conn = new OleDbConnection(this.connectionString))
            {
                string query = "SELECT LocationID, State, City FROM Locations";
                OleDbCommand cmd = new OleDbCommand(query, conn);
                conn.Open();
                OleDbDataReader reader = cmd.ExecuteReader();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        var location = new SelectListItem
                        {
                            Value = reader["LocationID"].ToString(),
                            Text = $"{reader["State"]}, {reader["City"]}"
                        };
                        Locations.Add(location);

                    }
                }
            }
        }//End of 'PopulateLocationList'.
        */

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
                string query = "SELECT SystemUserRole FROM SystemUser WHERE SystemUserID = ?;";

                using (var cmd = new OleDbCommand(query, conn))
                {
                    // OleDb uses positional parameters (names ignored), so add in the same order as the '?'..
                    cmd.Parameters.Add("@?", OleDbType.Integer).Value = userId;

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
