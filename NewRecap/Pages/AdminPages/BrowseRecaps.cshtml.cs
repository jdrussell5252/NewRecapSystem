using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NewRecap.Model;
using System.Data.OleDb;
using System.IO;
using System.Security.Claims;

namespace NewRecap.Pages.AdminPages
{
    [Authorize]
    public class BrowseRecapsModel : PageModel
    {
        public List<RecapView> Recaps { get; set; } = new List<RecapView>();
        public RecapView Recapp { get; set; } 
        public bool IsAdmin { get; set; }
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
                PopulateRecapList(userId);
            }
            /*--------------------ADMIN PRIV----------------------*/
        }

        public IActionResult OnPostDelete(int id)
        {
            // delete the book from the database
            using (OleDbConnection conn = new OleDbConnection(this.connectionString))
            {
                conn.Open();
                string deleteCmdText = "DELETE FROM Recap WHERE RecapID = @RecapID";
                OleDbCommand deleteCmd = new OleDbCommand(deleteCmdText, conn);
                deleteCmd.Parameters.AddWithValue("@RecapID", id);
                deleteCmd.ExecuteNonQuery();

            }

            return RedirectToPage();
        }//End of 'OnPostDelete'.

        private void PopulateRecapList(int id)
        {
            using (OleDbConnection conn = new OleDbConnection(connectionString))
            {
                if (IsAdmin == true)
                {
                    string cmdText = @"
                    SELECT
                      r.RecapID,
                      r.RecapWorkorderNumber,
                      r.RecapDate,
                      r.RecapDescription,
                      r.RecapState,
                      r.RecapCity,
                      r.RecapAssetNumber,
                      r.RecapSerialNumber,
                      r.IP,
                      r.WAM,
                      r.Hostname,
                      r.StartingMileage,
                      r.EndingMileage,
                      SUM(se.TotalWorkTime) AS TotalWorkTime,
                      SUM(se.TotalLunchTime) AS TotalLunchTime,
                      SUM(se.TotalDriveTime) AS TotalDriveTime,
                      SUM(se.TotalSupportTime) AS TotalSupportTime,
                      SUM(se.TotalTime) AS TotalTime
                    FROM ((Recap AS r
                    LEFT JOIN StartEndTime AS se ON se.RecapID = r.RecapID)
                    LEFT JOIN EmployeeRecaps AS er ON er.RecapID = r.RecapID)
                    GROUP BY
                      r.RecapID, r.RecapWorkorderNumber, r.RecapDate,
                      r.RecapDescription, r.RecapState, r.RecapCity,
                      r.RecapAssetNumber, r.RecapSerialNumber, r.IP, r.WAM, r.Hostname, r.StartingMileage, r.EndingMileage
                    ORDER BY r.RecapDate DESC, r.RecapID DESC;";

                    OleDbCommand cmd = new OleDbCommand(cmdText, conn);
                    conn.Open();
                    OleDbDataReader reader = cmd.ExecuteReader();
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            RecapView recap = new RecapView
                            {
                                RecapID = reader.GetInt32(0),
                                RecapWorkorderNumber = reader.GetInt32(1),
                                RecapDate = reader.GetDateTime(2),
                                RecapDescription = reader.GetString(3),
                                RecapState = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                                RecapCity = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                                RecapAssetNumber = reader.IsDBNull(6) ? null : reader.GetInt32(6),
                                RecapSerialNumber = reader.IsDBNull(7) ? string.Empty : reader.GetString(7),

                                IP = reader.IsDBNull(8) ? string.Empty : reader.GetString(8),
                                WAM = reader.IsDBNull(9) ? string.Empty : reader.GetString(9),
                                Hostname = reader.IsDBNull(10) ? string.Empty : reader.GetString(10),
                                StartingMileage = reader.IsDBNull(11) ? null : reader.GetInt32(11),
                                EndingMileage = reader.IsDBNull(12) ? null : reader.GetInt32(12),

                                RecapEmployees = PopulateRecapEmployees(reader.GetInt32(0)),
                                RecapStoreLocation = PopulateRecapStoreLocation(reader.GetInt32(0)),

                                TotalWorkTime = reader.IsDBNull(13) ? 0.0 : Math.Round(reader.GetDouble(13), 2),
                                TotalLunchTime = reader.IsDBNull(14) ? 0.0 : Math.Round(reader.GetDouble(14), 2),
                                TotalDriveTime = reader.IsDBNull(15) ? 0.0 : Math.Round(reader.GetDouble(15), 2),
                                TotalSupportTime = reader.IsDBNull(16) ? 0.0 : Math.Round(reader.GetDouble(16), 2),
                                TotalTime = reader.IsDBNull(17) ? 0.0 : Math.Round(reader.GetDouble(17), 2),

                                RecapVehicle = PopulateRecapVehcile(reader.GetInt32(0)),
                            };
                            bool isHardwareAdmin =
                                (!string.IsNullOrWhiteSpace(recap.RecapStoreLocation));
                            recap.Segments = PopulateRecapSegments(reader.GetInt32(0), isHardwareAdmin);
                            Recaps.Add(recap);
                        }
                    }
                }
                else
                {
                    int employeeId = GetEmployeeIdForUser(id);
                    string cmdText = @"
                    SELECT
                      r.RecapID,
                      r.RecapWorkorderNumber,
                      r.RecapDate,
                      r.AddedBy,
                      r.RecapDescription,
                      r.RecapState,
                      r.RecapCity,
                      r.RecapAssetNumber,
                      r.RecapSerialNumber,
                      r.IP,
                      r.WAM,
                      r.Hostname,
                      r.StartingMileage,
                      r.EndingMileage,
                      SUM(se.TotalWorkTime) AS TotalWorkTime,
                      SUM(se.TotalLunchTime) AS TotalLunchTime,
                      SUM(se.TotalDriveTime) AS TotalDriveTime,
                      SUM(se.TotalSupportTime) AS TotalSupportTime,
                      SUM(se.TotalTime) AS TotalTime
                    FROM ((Recap AS r
                    LEFT JOIN StartEndTime AS se ON se.RecapID = r.RecapID)
                    LEFT JOIN EmployeeRecaps AS er ON er.RecapID = r.RecapID)
                    WHERE r.AddedBy = @AddedBy
                    GROUP BY
                      r.RecapID,
                      r.RecapWorkorderNumber,
                      r.RecapDate,
                      r.AddedBy,
                      r.RecapDescription,
                      r.RecapState,
                      r.RecapCity,
                      r.RecapAssetNumber,
                      r.RecapSerialNumber,
                      r.StartingMileage, r.EndingMileage, r.IP, r.WAM, r.Hostname
                    ORDER BY r.RecapDate DESC, r.RecapID DESC;";

                    OleDbCommand cmd = new OleDbCommand(cmdText, conn);
                    cmd.Parameters.AddWithValue("@AddedBy", id);
                    //cmd.Parameters.AddWithValue("@EmployeeID", employeeId);
                    conn.Open();
                    OleDbDataReader reader = cmd.ExecuteReader();
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            RecapView recap = new RecapView
                            {

                                RecapID = reader.GetInt32(0),
                                RecapWorkorderNumber = reader.GetInt32(1),
                                RecapDate = reader.GetDateTime(2),

                                RecapDescription = reader.GetString(4),
                                RecapState = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                                RecapCity = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                                RecapAssetNumber = reader.IsDBNull(7) ? null : reader.GetInt32(7),
                                RecapSerialNumber = reader.IsDBNull(8) ? string.Empty : reader.GetString(8),

                                IP = reader.IsDBNull(9) ? string.Empty : reader.GetString(9),
                                WAM = reader.IsDBNull(10) ? string.Empty : reader.GetString(10),
                                Hostname = reader.IsDBNull(11) ? string.Empty : reader.GetString(11),
                                StartingMileage = reader.IsDBNull(12) ? null : reader.GetInt32(12),
                                EndingMileage = reader.IsDBNull(13) ? null : reader.GetInt32(13),

                                RecapEmployees = PopulateRecapEmployees(reader.GetInt32(0)),
                                RecapStoreLocation = PopulateRecapStoreLocation(reader.GetInt32(0)),

                                TotalWorkTime = reader.IsDBNull(14) ? 0.0 : Math.Round(reader.GetDouble(14), 2),
                                TotalLunchTime = reader.IsDBNull(15) ? 0.0 : Math.Round(reader.GetDouble(15), 2),
                                TotalDriveTime = reader.IsDBNull(16) ? 0.0 : Math.Round(reader.GetDouble(16), 2),
                                TotalSupportTime = reader.IsDBNull(17) ? 0.0 : Math.Round(reader.GetDouble(17), 2),
                                TotalTime = reader.IsDBNull(18) ? 0.0 : Math.Round(reader.GetDouble(18), 2),

                                RecapVehicle = PopulateRecapVehcile(reader.GetInt32(0)),


                            };

                            bool isHardwareEmployee =
                                (string.IsNullOrWhiteSpace(recap.RecapStoreLocation));

                            recap.Segments = PopulateRecapSegments(reader.GetInt32(0), isHardwareEmployee);
                            Recaps.Add(recap);
                        }
                    }
                }
            }
        }

        private List<string> PopulateRecapEmployees(int recapID)
        {
            List<string> Employees = new List<string>();
            using (OleDbConnection conn = new OleDbConnection(this.connectionString))
            {
                string query = "SELECT e.EmployeeFName, e.EmployeeLName " +
                               "FROM Employee AS e " +
                               "INNER JOIN EmployeeRecaps AS er ON e.EmployeeID = er.EmployeeID " +
                               "WHERE er.RecapID = @RecapID";
                OleDbCommand cmd = new OleDbCommand(query, conn);
                cmd.Parameters.AddWithValue("@RecapID", recapID);
                conn.Open();
                OleDbDataReader reader = cmd.ExecuteReader();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        string fName = reader.IsDBNull(0) ? "" : reader.GetString(0);
                        string lName = reader.IsDBNull(1) ? "" : reader.GetString(1);
                        Employees.Add($"{fName} {lName}");
                    }
                }
            }
            return Employees;
        }

        private string PopulateRecapStoreLocation(int recapID)
        {
            using (OleDbConnection conn = new OleDbConnection(this.connectionString))
            {
                string query = "SELECT StoreNumber, StoreState, StoreCity " +
                               "FROM StoreLocations AS sl " +
                               "INNER JOIN Recap AS r ON sl.StoreLocationID = r.StoreLocationID " +
                               "WHERE r.RecapID = @RecapID";
                OleDbCommand cmd = new OleDbCommand(query, conn);
                cmd.Parameters.AddWithValue("@RecapID", recapID);
                conn.Open();
                OleDbDataReader reader = cmd.ExecuteReader();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        int StoreNumber = reader.GetInt32(0);
                        string StoreState = reader.IsDBNull(1) ? "" : reader.GetString(1);
                        string StoreCity = reader.IsDBNull(2) ? "" : reader.GetString(2);
                        return $"Store Number: {StoreNumber} | Store State: {StoreState} | Store City: {StoreCity}";
                    }
                }
            }
            return "";
        }


        private string PopulateRecapVehcile(int recapID)
        {
            using (OleDbConnection conn = new OleDbConnection(this.connectionString))
            {
                string query = "SELECT VehicleNumber, VehicleVin, VehicleName " +
                               "FROM Vehicle AS v " +
                               "LEFT JOIN Recap AS r ON v.VehicleID = r.VehicleID " +
                               "WHERE r.RecapID = @RecapID";
                OleDbCommand cmd = new OleDbCommand(query, conn);
                cmd.Parameters.AddWithValue("@RecapID", recapID);
                conn.Open();
                OleDbDataReader reader = cmd.ExecuteReader();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        int VehicleNumber = reader.GetInt32(0);
                        string VehicleVin = reader.IsDBNull(1) ? "" : reader.GetString(1);
                        string VehicleName = reader.IsDBNull(2) ? "" : reader.GetString(2);
                        return $"Vehicle Number: {VehicleNumber} | Vehicle Vin: {VehicleVin} | Vehicle Name: {VehicleName}";
                    }
                }
                return "";
            }
        }

        private string PopulateRecapSegments(int recapID, bool isHardware)
        {
            using var conn = new OleDbConnection(this.connectionString);
            const string query = @"
            SELECT
                StartTime, EndTime,
                StartTimeDate, EndTimeDate,
                StartDriveTime, EndDriveTime,
                StartDriveDate, EndDriveDate,
                StartLunchTime, EndLunchTime,
                StartLunchDate, EndLunchDate,
                StartSupportTime, EndSupportTime,
                StartSupportDate, EndSupportDate
                FROM StartEndTime
            WHERE RecapID = @RecapID";

            using var cmd = new OleDbCommand(query, conn);
            cmd.Parameters.AddWithValue("@RecapID", recapID);
            conn.Open();

            using var reader = cmd.ExecuteReader();
            var parts = new List<string>();
            int seg = 1;

            DateTime? GetDT(int i) => reader.IsDBNull(i) ? (DateTime?)null : reader.GetDateTime(i);
            string fdt(DateTime? d) => d?.ToString("MM/dd/yyyy") ?? "--";
            string ftm(DateTime? d) => d?.ToString("hh:mm tt") ?? "--";
            string combine(DateTime? date, DateTime? time)
                => (date == null && time == null) ? "--" : $"{fdt(date)} {ftm(time)}";

            while (reader.Read())
            {
                var startTime = GetDT(0);
                var endTime = GetDT(1);
                var startTimeDate = GetDT(2);
                var endTimeDate = GetDT(3);
                var startDrive = GetDT(4);
                var endDrive = GetDT(5);
                var startDriveDate = GetDT(6);
                var endDriveDate = GetDT(7);
                var startLunch = GetDT(8);
                var endLunch = GetDT(9);
                var startLunchDate = GetDT(10);
                var endLunchDate = GetDT(11);

                var startSupport = GetDT(12);
                var endSupport = GetDT(13);
                var startSupportDate = GetDT(14);
                var endSupportDate = GetDT(15);

                if(isHardware)
                {
                    string segment1 =
                        $"Segment {seg}: " +
                        $"Work {combine(startTimeDate, startTime)} → {combine(endTimeDate, endTime)} | " +
                        $"Lunch {combine(startLunchDate, startLunch)} → {combine(endLunchDate, endLunch)}";
                    parts.Add(segment1);
                    seg++;
                }
                else
                {
                    string segment =
                        $"Segment {seg}: " +
                        $"Work {combine(startTimeDate, startTime)} → {combine(endTimeDate, endTime)} | " +
                        $"Travel {combine(startDriveDate, startDrive)} → {combine(endDriveDate, endDrive)} | " +
                        $"Lunch {combine(startLunchDate, startLunch)} → {combine(endLunchDate, endLunch)} | " +
                        $"Support {combine(startSupportDate, startSupport)} → {combine(endSupportDate, endSupport)}";
                    parts.Add(segment);
                    seg++;
                }
            }
            return parts.Count == 0 ? "" : string.Join("<br></br>", parts);    
        }

        private int GetEmployeeIdForUser(int systemUserID)
        {
            using (OleDbConnection conn = new OleDbConnection(this.connectionString))
            {
                string query = "SELECT EmployeeID FROM SystemUser WHERE SystemUserID = @SystemUserID";
                OleDbCommand cmd = new OleDbCommand(query, conn);
                cmd.Parameters.AddWithValue("@SystemUserID", systemUserID);
                conn.Open();
                OleDbDataReader reader = cmd.ExecuteReader();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        int EmployeeID = reader.GetInt32(0);
                        return EmployeeID;
                    }
                }
                return 0;
            }
        }

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
    }
}
