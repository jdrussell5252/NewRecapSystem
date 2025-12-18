using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using NewRecap.Model;
using NewRecap.MyAppHelper;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Security.Claims;

namespace NewRecap.Pages.AdminPages
{
    [Authorize]
    public class BrowseRecapsModel : PageModel
    {


        public List<SelectListItem> EmployeeOptions { get; set; } = new();
        [BindProperty(SupportsGet = true)]
        public List<int> SelectedEmployeeIds { get; set; } = new();


        public List<StoreGroup> RecapsByStore { get; set; } = new();
        public List<RecapView> Recaps { get; set; } = new List<RecapView>();
        public bool IsAdmin { get; set; }


        public List<SelectListItem> StoreOptions { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        [DataType(DataType.Date)]
        public DateTime? FilterDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? FilterStoreNumber { get; set; }
        public string StoreJson { get; set; } = "[]";
        [BindProperty(SupportsGet = true)]
        public string? FilterCustomer { get; set; }
        [BindProperty(SupportsGet = true)]
        public int? FilterWorkorderNumber { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? FilterTechnician { get; set; }


        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 5;
        public int TotalCount { get; set; }
        public int TotalPages => Math.Max(1, (int)Math.Ceiling((double)TotalCount / Math.Max(1, PageSize)));

        public IActionResult OnGet(int pageNumber = 1, int pageSize = 5)
        {
            var redirect = EnforcePasswordChange();
            if (redirect != null)
                return redirect;

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
                PopulateRecapList(userId);
                PopulateHardwareRecapList(userId);
            }
            /*--------------------ADMIN PRIV----------------------*/

            PopulateStoreOptions();
            PopulateEmployeeOptions();


            // --- Filter by store (if selected) ---
            if (FilterStoreNumber.HasValue)
            {
                string prefix = $"Store Number: {FilterStoreNumber.Value} |";

                Recaps = Recaps
                    .Where(r => !string.IsNullOrWhiteSpace(r.RecapStoreLocation) &&
                                r.RecapStoreLocation.StartsWith(prefix))
                    .ToList();
            }


            // Filter by exact recap date
            if (FilterDate.HasValue)
            {
                var targetDate = FilterDate.Value.Date;
                Recaps = Recaps
                    .Where(r => r.RecapDate.Date == targetDate)
                    .ToList();
            }

            // --- Filter by exact customer name ---
            if (!string.IsNullOrWhiteSpace(FilterCustomer))
            {
                var term = FilterCustomer.Trim();

                Recaps = Recaps
                    .Where(r => !string.IsNullOrWhiteSpace(r.Customer) &&
                                string.Equals(r.Customer.Trim(), term, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            // --- Filter by Workorder Number ---
            if (FilterWorkorderNumber.HasValue)
            {
                int target = FilterWorkorderNumber.Value;

                Recaps = Recaps
                    .Where(r => r.RecapWorkorderNumber == target)
                    .ToList();
            }

            if (SelectedEmployeeIds != null && SelectedEmployeeIds.Any())
            {
                var selectedSet = new HashSet<int>(SelectedEmployeeIds);

                Recaps = Recaps
                    .Where(r =>
                        r.RecapEmployeeIds != null &&
                        r.RecapEmployeeIds.Any(empId => selectedSet.Contains(empId)))
                    .ToList();
            }

            // === Pagination logic ===
            PageNumber = pageNumber < 1 ? 1 : pageNumber;
            PageSize = pageSize < 1 ? 5 : pageSize;

            TotalCount = Recaps.Count;

            // Clamp PageNumber so it’s not past the last page
            if (TotalCount > 0 && (PageNumber - 1) * PageSize >= TotalCount)
            {
                PageNumber = (int)Math.Ceiling((double)TotalCount / PageSize);
            }

            if (TotalCount > 0)
            {
                int skip = (PageNumber - 1) * PageSize;
                Recaps = Recaps
                    .Skip(skip)
                    .Take(PageSize)
                    .ToList();
            }
            return Page();
        }// End of 'OnGet'.

        public IActionResult OnPostDelete(int id, bool isHardware)
        {
            // Set up the connection with the database.
            using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
            {
                conn.Open();

                string sql = isHardware
                    ? "DELETE FROM HardwareRecap WHERE HardwareRecapID = @ID"
                    : "DELETE FROM Recap WHERE RecapID = @ID";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@ID", id);
                    cmd.ExecuteNonQuery();
                }

            }
            return RedirectToPage(); // Redirect to the same page "Browse Recaps".
        }//End of 'OnPostDelete'.

        private void PopulateHardwareRecapList(int id)
        {
            using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
            {
                if (IsAdmin == true)
                {
                    string cmdText = @"
                    SELECT
                      HardwareRecapID,
                      HardwareRecapWorkorderNumber,
                      HardwareRecapDate,
                      HardwareRecapDescription,
                      HardwareRecapAssetNumber,
                      HardwareRecapSerialNumber,
                      HardwareRecapIP,
                      HardwareRecapWAM,
                      HardwareRecapHostname
                    FROM HardwareRecap
                    ORDER BY HardwareRecapDate DESC;";

                    SqlCommand cmd = new SqlCommand(cmdText, conn);
                    conn.Open();
                    SqlDataReader reader = cmd.ExecuteReader();
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            int recapId = reader.GetInt32(0);
                            var cableTotals = GetCableTotals(recapId);
                            int effCount = GetEffectiveEmployeeCountHardwareRecap(recapId);

                            decimal baseWork = GetHardwareWorkTimeBase(recapId);
                            decimal totalWork = baseWork * effCount;

                            decimal baseLunch = GetHardwareLunchTimeBase(recapId);
                            decimal totalLunch = baseLunch * effCount;

                            decimal baseRecap = GetHardwareRecapTimeBase(recapId);
                            decimal totalRecap = baseRecap;

                            decimal totalTime = totalWork + totalRecap - totalLunch;

                            RecapView recap = new RecapView
                            {
                                IsHardware = true,
                                RecapID = recapId,
                                RecapWorkorderNumber = reader.GetInt32(1),
                                RecapDate = reader.GetDateTime(2),
                                RecapDescription = reader.GetString(3),

                                RecapAssetNumber = reader.IsDBNull(4) ? null : reader.GetInt32(4),
                                RecapSerialNumber = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),

                                IP = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                                WAM = reader.IsDBNull(7) ? string.Empty : reader.GetString(7),
                                Hostname = reader.IsDBNull(8) ? string.Empty : reader.GetString(8),

                                RecapStoreLocation = PopulateRecapStoreLocation(recapId),


                                TotalWorkTime = Math.Round(totalWork, 2),
                                TotalLunchTime = Math.Round(totalLunch, 2),
                                TotalRecapTime = Math.Round(totalRecap, 2),
                                TotalTime = Math.Round(totalTime, 2),
                            };
                            recap.RecapEmployees = PopulateHardwareRecapEmployees(recapId);
                            recap.RecapEmployeeIds = GetHardwareRecapEmployeeIds(recapId);

                            bool isHardwareAdmin =
                                (!string.IsNullOrWhiteSpace(recap.RecapStoreLocation));
                            recap.Segments = PopulateHardwareRecapSegments(reader.GetInt32(0), isHardwareAdmin);
                            Recaps.Add(recap);

                        }
                    }
                }
                else
                {
                    int employeeId = GetEmployeeIdForUser(id);
                    string cmdText = @"
                    SELECT
                      hr.HardwareRecapID,
                      hr.HardwareRecapWorkorderNumber,
                      hr.HardwareRecapDate,
                      hr.HardwareRecapDescription,
                      hr.HardwareRecapAssetNumber,
                      hr.HardwareRecapSerialNumber,
                      hr.HardwareRecapIP,
                      hr.HardwareRecapWAM,
                      hr.HardwareRecapHostname
                    FROM HardwareRecap as hr INNER JOIN EmployeeHardwareRecaps AS er ON hr.HardwareRecapID = er.HardwareRecapID WHERE er.EmployeeID = @EmployeeID
                    ORDER BY hr.HardwareRecapDate DESC;";

                    SqlCommand cmd = new SqlCommand(cmdText, conn);
                    cmd.Parameters.AddWithValue("@EmployeeID", employeeId);
                    conn.Open();
                    SqlDataReader reader = cmd.ExecuteReader();
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            int recapId = reader.GetInt32(0);
                            var cableTotals = GetCableTotals(recapId);
                            int effCount = GetEffectiveEmployeeCountHardwareRecap(recapId);

                            decimal baseWork = GetWorkTimeBase(recapId);
                            decimal totalWork = baseWork * effCount;

                            decimal baseLunch = GetLunchTimeBase(recapId);
                            decimal totalLunch = baseLunch * effCount;

                            decimal baseRecap = GetRecapTimeBase(recapId);
                            decimal totalRecap = baseRecap;

                            decimal totalTime = totalWork + totalRecap - totalLunch;

                            RecapView recap = new RecapView
                            {
                                IsHardware = true,
                                RecapID = recapId,
                                RecapWorkorderNumber = reader.GetInt32(1),
                                RecapDate = reader.GetDateTime(2),
                                RecapDescription = reader.GetString(3),
                                RecapAssetNumber = reader.IsDBNull(4) ? null : reader.GetInt32(4),
                                RecapSerialNumber = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                                IP = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                                WAM = reader.IsDBNull(7) ? string.Empty : reader.GetString(7),
                                Hostname = reader.IsDBNull(8) ? string.Empty : reader.GetString(8),
                                RecapStoreLocation = PopulateRecapStoreLocation(recapId),
                                TotalWorkTime = Math.Round(totalWork, 2),
                                TotalLunchTime = Math.Round(totalLunch, 2),
                                TotalRecapTime = Math.Round(totalRecap, 2),
                                TotalTime = Math.Round(totalTime, 2),
                            };
                            recap.RecapEmployees = PopulateHardwareRecapEmployees(recapId);
                            recap.RecapEmployeeIds = GetHardwareRecapEmployeeIds(recapId);

                            bool isHardwareEmployee =
                                (string.IsNullOrWhiteSpace(recap.RecapStoreLocation));

                            recap.Segments = PopulateHardwareRecapSegments(reader.GetInt32(0), isHardwareEmployee);
                            Recaps.Add(recap);
                        }
                    }
                }
            }
        }// End of 'PopulateHardwareRecapList'.

        private void PopulateRecapList(int id)
        {
            using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
            {
                if (IsAdmin == true)
                {
                    string cmdText = @"
                    SELECT
                      RecapID,
                      RecapWorkorderNumber,
                      RecapDate,
                      RecapDescription,
                      RecapState,
                      RecapCity,
                      StartingMileage,
                      EndingMileage,
                      Customer
                    FROM Recap
                    ORDER BY RecapDate DESC;";

                    SqlCommand cmd = new SqlCommand(cmdText, conn);
                    conn.Open();
                    SqlDataReader reader = cmd.ExecuteReader();
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            int recapId = reader.GetInt32(0);
                            var cableTotals = GetCableTotals(recapId);
                            int effCount = GetEffectiveEmployeeCountRecap(recapId);

                            decimal baseWork = GetWorkTimeBase(recapId);
                            decimal totalWork = baseWork * effCount;

                            decimal baseLunch = GetLunchTimeBase(recapId);
                            decimal totalLunch = baseLunch * effCount;

                            decimal baseTravel = GetTravelTimeBase(recapId);
                            decimal totalTravel = baseTravel * effCount;

                            decimal baseSupport = GetSupportTimeBase(recapId);
                            decimal totalSupport = baseSupport;

                            decimal baseRecap = GetRecapTimeBase(recapId);
                            decimal totalRecap = baseRecap;

                            decimal totalTime = totalWork + totalTravel + totalSupport + totalRecap - totalLunch;

                            RecapView recap = new RecapView
                            {
                                IsHardware = false,
                                RecapID = recapId,
                                RecapWorkorderNumber = reader.GetInt32(1),
                                RecapDate = reader.GetDateTime(2),
                                RecapDescription = reader.GetString(3),
                                RecapState = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                                RecapCity = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),

                                StartingMileage = reader.IsDBNull(6) ? null : reader.GetInt32(6),
                                EndingMileage = reader.IsDBNull(7) ? null : reader.GetInt32(7),
                                Customer = reader.IsDBNull(8) ? string.Empty : reader.GetString(8),

                                RecapVehicle = PopulateRecapVehcile(recapId),
                                
                                TotalWorkTime = Math.Round(totalWork, 2),
                                TotalLunchTime = Math.Round(totalLunch, 2),
                                TotalDriveTime = Math.Round(totalTravel, 2),
                                TotalSupportTime = Math.Round(totalSupport, 2),
                                TotalRecapTime = Math.Round(totalRecap, 2),
                                TotalTime = Math.Round(totalTime, 2),
                                
                                Total182 = cableTotals.Total182,
                                Total184 = cableTotals.Total184,
                                Total186 = cableTotals.Total186,
                                TotalCat6 = cableTotals.TotalCat6,
                                TotalFiber = cableTotals.TotalFiber,
                                TotalCoax = cableTotals.TotalCoax
                                
                            };
                            recap.RecapEmployees = PopulateRecapEmployees(recapId);
                            recap.RecapEmployeeIds = GetRecapEmployeeIds(recapId);
                            recap.Segments = PopulateRecapSegments(reader.GetInt32(0), false);
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
                      r.RecapDescription,
                      r.RecapState,
                      r.RecapCity,
                      r.StartingMileage,
                      r.EndingMileage,
                      r.Customer
                    FROM Recap as r INNER JOIN EmployeeRecaps AS er ON r.RecapID = er.RecapID WHERE er.EmployeeID = @EmployeeID
                    ORDER BY RecapDate DESC;";

                    SqlCommand cmd = new SqlCommand(cmdText, conn);
                    cmd.Parameters.AddWithValue("@EmployeeID", employeeId);
                    conn.Open();
                    SqlDataReader reader = cmd.ExecuteReader();
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            int recapId = reader.GetInt32(0);
                            var cableTotals = GetCableTotals(recapId);
                            int effCount = GetEffectiveEmployeeCountRecap(recapId);

                            decimal baseWork = GetWorkTimeBase(recapId);
                            decimal totalWork = baseWork * effCount;

                            decimal baseLunch = GetLunchTimeBase(recapId);
                            decimal totalLunch = baseLunch * effCount;

                            decimal baseTravel = GetTravelTimeBase(recapId);
                            decimal totalTravel = baseTravel * effCount;

                            decimal baseSupport = GetSupportTimeBase(recapId);
                            decimal totalSupport = baseSupport;

                            decimal baseRecap = GetRecapTimeBase(recapId);
                            decimal totalRecap = baseRecap;

                            decimal totalTime = totalWork + totalTravel + totalSupport + totalRecap - totalLunch;

                            RecapView recap = new RecapView
                            {
                                IsHardware = false,
                                RecapID = recapId,
                                RecapWorkorderNumber = reader.GetInt32(1),
                                RecapDate = reader.GetDateTime(2),
                                RecapDescription = reader.GetString(3),
                                RecapState = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                                RecapCity = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),

                                StartingMileage = reader.IsDBNull(6) ? null : reader.GetInt32(6),
                                EndingMileage = reader.IsDBNull(7) ? null : reader.GetInt32(7),
                                Customer = reader.IsDBNull(8) ? string.Empty : reader.GetString(8),

                                RecapVehicle = PopulateRecapVehcile(recapId),

                                TotalWorkTime = Math.Round(totalWork, 2),
                                TotalLunchTime = Math.Round(totalLunch, 2),
                                TotalDriveTime = Math.Round(totalTravel, 2),
                                TotalSupportTime = Math.Round(totalSupport, 2),
                                TotalRecapTime = Math.Round(totalRecap, 2),
                                TotalTime = Math.Round(totalTime, 2),

                                Total182 = cableTotals.Total182,
                                Total184 = cableTotals.Total184,
                                Total186 = cableTotals.Total186,
                                TotalCat6 = cableTotals.TotalCat6,
                                TotalFiber = cableTotals.TotalFiber,
                                TotalCoax = cableTotals.TotalCoax
                            };
                            recap.RecapEmployees = PopulateRecapEmployees(recapId);
                            recap.RecapEmployeeIds = GetRecapEmployeeIds(recapId);

                            bool isHardwareEmployee =
                                (string.IsNullOrWhiteSpace(recap.RecapStoreLocation));

                            recap.Segments = PopulateRecapSegments(reader.GetInt32(0), isHardwareEmployee);
                            Recaps.Add(recap);
                        }
                    }
                }
            }
        }// End of 'PopulateRecapList'.

        private List<string> PopulateRecapEmployees(int recapID)
        {
            var employees = new List<string>();

            using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
            {
                string query = @"
                   SELECT e.EmployeeFName,
                   e.EmployeeLName,
                   er.IsTraining
                   FROM (Employee AS e
                   INNER JOIN EmployeeRecaps AS er
                   ON e.EmployeeID = er.EmployeeID)
                   WHERE er.RecapID = @RecapID";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@RecapID", recapID);
                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string fName = reader.IsDBNull(0) ? "" : reader.GetString(0);
                            string lName = reader.IsDBNull(1) ? "" : reader.GetString(1);

                            // Per-recap training flag
                            bool isTraining = !reader.IsDBNull(2) && reader.GetBoolean(2);

                            string suffix = isTraining ? " (Training)" : "";
                            employees.Add($"{fName} {lName}{suffix}");
                        }
                    }
                }
            }
            return employees;
        }// End of 'PopulateRecapEmployees'.

        private List<string> PopulateHardwareRecapEmployees(int recapID)
        {
            var employees = new List<string>();

            using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
            {
                string query = @"
                   SELECT e.EmployeeFName,
                   e.EmployeeLName,
                   er.IsTraining
                   FROM (Employee AS e
                   INNER JOIN EmployeeHardwareRecaps AS er
                   ON e.EmployeeID = er.EmployeeID)
                   WHERE er.HardwareRecapID = @HardwareRecapID";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@HardwareRecapID", recapID);
                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string fName = reader.IsDBNull(0) ? "" : reader.GetString(0);
                            string lName = reader.IsDBNull(1) ? "" : reader.GetString(1);

                            // Per-recap training flag
                            bool isTraining = !reader.IsDBNull(2) && reader.GetBoolean(2);

                            string suffix = isTraining ? " (Training)" : "";
                            employees.Add($"{fName} {lName}{suffix}");
                        }
                    }
                }
            }
            return employees;
        }// End of 'PopulateHardwareRecapEmployees'.

        private string PopulateRecapStoreLocation(int recapID)
        {
            using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
            {
                string query = "SELECT StoreNumber, StoreState, StoreCity " +
                               "FROM StoreLocations AS sl " +
                               "INNER JOIN HardwareRecap AS hr ON sl.StoreLocationID = hr.StoreLocationID " +
                               "WHERE hr.HardwareRecapID = @HardwareRecapID";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@HardwareRecapID", recapID);
                conn.Open();
                SqlDataReader reader = cmd.ExecuteReader();
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
        }// End of 'PopulateRecapStoreLocation'.

        private string PopulateRecapVehcile(int recapID)
        {
            using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
            {
                string query = "SELECT VehicleNumber, VehicleModel " +
                               "FROM Vehicle AS v " +
                               "LEFT JOIN Recap AS r ON v.VehicleID = r.VehicleID " +
                               "WHERE r.RecapID = @RecapID";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@RecapID", recapID);
                conn.Open();
                SqlDataReader reader = cmd.ExecuteReader();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        string VehicleNumber = reader.GetString(0);
                        string VehicleModel = reader.IsDBNull(1) ? "" : reader.GetString(1);
                        return $"Vehicle Number: {VehicleNumber} | Vehicle Model: {VehicleModel}";
                    }
                }
                return "";
            }
        }// End of 'PopulateRecapVehicle'.

        private string PopulateRecapSegments(int recapID, bool isHardware)
        {
            using var conn = new SqlConnection(AppHelper.GetDBConnectionString());
            conn.Open();

            // Helper local list type: (startDate, startTime, endDate, endTime)
            var workSegments = new List<(DateTime? sDate, DateTime? sTime, DateTime? eDate, DateTime? eTime)>();
            var travelSegments = new List<(DateTime? sDate, DateTime? sTime, DateTime? eDate, DateTime? eTime)>();
            var lunchSegments = new List<(DateTime? sDate, DateTime? sTime, DateTime? eDate, DateTime? eTime)>();
            var supportSegments = new List<(DateTime? sDate, DateTime? sTime, DateTime? eDate, DateTime? eTime)>();
            var recapSegments = new List<(DateTime? sDate, DateTime? sTime, DateTime? eDate, DateTime? eTime)>();

            // Small helper to read a segment row safely
            static (DateTime? sDate, DateTime? sTime, DateTime? eDate, DateTime? eTime)
                ReadSegmentRow(SqlDataReader r, int sDateIdx, int sTimeIdx, int eDateIdx, int eTimeIdx)
            {
                DateTime? getDT(int i) =>
                    r.IsDBNull(i) ? (DateTime?)null : r.GetDateTime(i);

                return (getDT(sDateIdx), getDT(sTimeIdx), getDT(eDateIdx), getDT(eTimeIdx));
            }

            // ---------- Work segments (StartEndTime) ----------
            using (var cmd = new SqlCommand(@"
                SELECT StartWorkDate, StartWorkTime, EndWorkDate, EndWorkTime
                FROM StartEndWork
                WHERE RecapID = @RecapID
                ORDER BY StartWorkDate, StartWorkTime;", conn))
            {
                cmd.Parameters.AddWithValue("@RecapID", recapID);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    workSegments.Add(ReadSegmentRow(reader, 0, 1, 2, 3));
                }
            }

            // ---------- Travel segments (StartEndTravel) ----------
            using (var cmd = new SqlCommand(@"
                SELECT StartTravelDate, StartTravelTime, EndTravelDate, EndTravelTime
                FROM StartEndTravel
                WHERE RecapID = @RecapID
                ORDER BY StartTravelDate, StartTravelTime;", conn))
            {
                cmd.Parameters.AddWithValue("@RecapID", recapID);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    travelSegments.Add(ReadSegmentRow(reader, 0, 1, 2, 3));
                }
            }

            // ---------- Lunch segments (StartEndLunch) ----------
            using (var cmd = new SqlCommand(@"
                SELECT StartLunchDate, StartLunchTime, EndLunchDate, EndLunchTime
                FROM StartEndLunch
                WHERE RecapID = @RecapID
                ORDER BY StartLunchDate, StartLunchTime;", conn))
            {
                cmd.Parameters.AddWithValue("@RecapID", recapID);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    lunchSegments.Add(ReadSegmentRow(reader, 0, 1, 2, 3));
                }
            }

            // ---------- Support segments (StartEndSupport) ----------
            using (var cmd = new SqlCommand(@"
                SELECT StartSupportDate, StartSupportTime, EndSupportDate, EndSupportTime
                FROM StartEndSupport
                WHERE RecapID = @RecapID
                ORDER BY StartSupportDate, StartSupportTime;", conn))
            {
                cmd.Parameters.AddWithValue("@RecapID", recapID);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    supportSegments.Add(ReadSegmentRow(reader, 0, 1, 2, 3));
                }
            }

            // ---------- Recap segments (StartEndRecap) ----------
            using (var cmd = new SqlCommand(@"
                SELECT StartRecapDate, StartRecapTime, EndRecapDate, EndRecapTime
                FROM StartEndRecap
                WHERE RecapID = @RecapID
                ORDER BY StartRecapDate, StartRecapTime;", conn))
            {
                cmd.Parameters.AddWithValue("@RecapID", recapID);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    recapSegments.Add(ReadSegmentRow(reader, 0, 1, 2, 3));
                }
            }

            // ---------- Formatting helpers ----------
            string fdt(DateTime? d) => d?.ToString("MM/dd/yyyy") ?? "--";
            string ftm(DateTime? d) => d?.ToString("hh:mm tt") ?? "--";
            string combine(DateTime? date, DateTime? time)
                => (date == null && time == null) ? "--" : $"{fdt(date)} {ftm(time)}";

            string fmtSeg((DateTime? sDate, DateTime? sTime, DateTime? eDate, DateTime? eTime) seg)
                => $"{combine(seg.sDate, seg.sTime)} → {combine(seg.eDate, seg.eTime)}";

            // ---------- Merge lists by index into "Segment N" lines ----------
            var parts = new List<string>();
            int maxCount = Math.Max(
                Math.Max(workSegments.Count, travelSegments.Count),
                Math.Max(
                    Math.Max(lunchSegments.Count, supportSegments.Count),
                    recapSegments.Count));

            int segNum = 1;

            for (int i = 0; i < maxCount; i++)
            {
                var work = i < workSegments.Count ? workSegments[i] : default;
                var travel = i < travelSegments.Count ? travelSegments[i] : default;
                var lunch = i < lunchSegments.Count ? lunchSegments[i] : default;
                var support = i < supportSegments.Count ? supportSegments[i] : default;
                var recap = i < recapSegments.Count ? recapSegments[i] : default;

                if (isHardware)
                {
                    string segment =
                        $"Segment {segNum}: " +
                        $"Work {fmtSeg(work)} | " +
                        $"Lunch {fmtSeg(lunch)} | " +
                        $"Recap {fmtSeg(recap)}";

                    parts.Add(segment);
                }
                else
                {
                    string segment =
                        $"Segment {segNum}: " +
                        $"Work {fmtSeg(work)} | " +
                        $"Travel {fmtSeg(travel)} | " +
                        $"Lunch {fmtSeg(lunch)} | " +
                        $"Support {fmtSeg(support)} | " +
                        $"Recap {fmtSeg(recap)}";

                    parts.Add(segment);
                }

                segNum++;
            }

            return parts.Count == 0 ? "" : string.Join("<br></br>", parts);
        }// End of 'PopulateRecapSegments'.

        private string PopulateHardwareRecapSegments(int recapID, bool isHardware)
        {
            using var conn = new SqlConnection(AppHelper.GetDBConnectionString());
            conn.Open();

            // Helper local list type: (startDate, startTime, endDate, endTime)
            var workSegments = new List<(DateTime? sDate, DateTime? sTime, DateTime? eDate, DateTime? eTime)>();

            var lunchSegments = new List<(DateTime? sDate, DateTime? sTime, DateTime? eDate, DateTime? eTime)>();

            var recapSegments = new List<(DateTime? sDate, DateTime? sTime, DateTime? eDate, DateTime? eTime)>();

            // Small helper to read a segment row safely
            static (DateTime? sDate, DateTime? sTime, DateTime? eDate, DateTime? eTime)
                ReadSegmentRow(SqlDataReader r, int sDateIdx, int sTimeIdx, int eDateIdx, int eTimeIdx)
            {
                DateTime? getDT(int i) =>
                    r.IsDBNull(i) ? (DateTime?)null : r.GetDateTime(i);

                return (getDT(sDateIdx), getDT(sTimeIdx), getDT(eDateIdx), getDT(eTimeIdx));
            }

            // ---------- Work segments (StartEndTime) ----------
            using (var cmd = new SqlCommand(@"
                SELECT StartWorkDate, StartWorkTime, EndWorkDate, EndWorkTime
                FROM StartEndWorkHardware
                WHERE HardwareRecapID = @HardwareRecapID
                ORDER BY StartWorkDate, StartWorkTime;", conn))
            {
                cmd.Parameters.AddWithValue("@HardwareRecapID", recapID);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    workSegments.Add(ReadSegmentRow(reader, 0, 1, 2, 3));
                }
            }

            // ---------- Lunch segments (StartEndLunch) ----------
            using (var cmd = new SqlCommand(@"
                SELECT StartLunchDate, StartLunchTime, EndLunchDate, EndLunchTime
                FROM StartEndLunchHardware
                WHERE HardwareRecapID = @HardwareRecapID
                ORDER BY StartLunchDate, StartLunchTime;", conn))
            {
                cmd.Parameters.AddWithValue("@HardwareRecapID", recapID);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    lunchSegments.Add(ReadSegmentRow(reader, 0, 1, 2, 3));
                }
            }

            // ---------- Recap segments (StartEndRecap) ----------
            using (var cmd = new SqlCommand(@"
                SELECT StartRecapDate, StartRecapTime, EndRecapDate, EndRecapTime
                FROM StartEndRecapHardware
                WHERE HardwareRecapID = @HardwareRecapID
                ORDER BY StartRecapDate, StartRecapTime;", conn))
            {
                cmd.Parameters.AddWithValue("@HardwareRecapID", recapID);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    recapSegments.Add(ReadSegmentRow(reader, 0, 1, 2, 3));
                }
            }

            // ---------- Formatting helpers ----------
            string fdt(DateTime? d) => d?.ToString("MM/dd/yyyy") ?? "--";
            string ftm(DateTime? d) => d?.ToString("hh:mm tt") ?? "--";
            string combine(DateTime? date, DateTime? time)
                => (date == null && time == null) ? "--" : $"{fdt(date)} {ftm(time)}";

            string fmtSeg((DateTime? sDate, DateTime? sTime, DateTime? eDate, DateTime? eTime) seg)
                => $"{combine(seg.sDate, seg.sTime)} → {combine(seg.eDate, seg.eTime)}";

            // ---------- Merge lists by index into "Segment N" lines ----------
            var parts = new List<string>();
            int maxCount = Math.Max(
                Math.Max(workSegments.Count, lunchSegments.Count),
                recapSegments.Count
            );


            int segNum = 1;

            for (int i = 0; i < maxCount; i++)
            {
                var work = i < workSegments.Count ? workSegments[i] : default;
                var lunch = i < lunchSegments.Count ? lunchSegments[i] : default;
                var recap = i < recapSegments.Count ? recapSegments[i] : default;

                string segment =
                    $"Segment {segNum}: " +
                    $"Work {fmtSeg(work)} | " +
                    $"Lunch {fmtSeg(lunch)} | " +
                    $"Recap {fmtSeg(recap)}";

                parts.Add(segment);

                segNum++;
            }

            return parts.Count == 0 ? "" : string.Join("<br></br>", parts);
        }// End of 'PopulateHardwareRecapSegments'.

        private int GetEmployeeIdForUser(int systemUserID)
        {
            using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
            {
                string query = "SELECT EmployeeID FROM SystemUser WHERE SystemUserID = @SystemUserID";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@SystemUserID", systemUserID);
                conn.Open();
                SqlDataReader reader = cmd.ExecuteReader();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        int EmployeeID = reader.GetInt32(0);
                        return EmployeeID;
                    }
                }
            }
            return 0;
        }// End of 'GetEmployeeIdForUser'.

        private void PopulateStoreOptions()
        {
            StoreOptions = new List<SelectListItem>();
            var storeNumbers = new List<int>();

            using (var conn = new SqlConnection(AppHelper.GetDBConnectionString()))
            {
                string query = @"
                    SELECT StoreNumber
                    FROM StoreLocations
                    ORDER BY StoreNumber";

                using (var cmd = new SqlCommand(query, conn))
                {
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int storeNumber = reader.GetInt32(0);

                            StoreOptions.Add(new SelectListItem
                            {
                                Value = storeNumber.ToString(),
                                Text = storeNumber.ToString()
                            });

                            storeNumbers.Add(storeNumber);
                        }
                    }
                }
            }

            // For JS autocomplete on the page
            StoreJson = System.Text.Json.JsonSerializer.Serialize(storeNumbers);
        }// End of 'PopulateStoreOptions'.

        private int GetEffectiveEmployeeCountRecap(int recapID)
        {
            var allEmployees = new HashSet<int>();
            var trainingEmployees = new HashSet<int>();

            using var conn = new SqlConnection(AppHelper.GetDBConnectionString());
            conn.Open();

            string sql = @"
        SELECT  
            ER.EmployeeID,
            ER.IsTraining
        FROM EmployeeRecaps ER
        WHERE ER.RecapID = @RecapID;";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@RecapID", recapID);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                if (reader["EmployeeID"] == DBNull.Value)
                    continue;

                int empId = Convert.ToInt32(reader["EmployeeID"]);
                allEmployees.Add(empId);

                bool isTraining =
                    reader["IsTraining"] != DBNull.Value &&
                    Convert.ToBoolean(reader["IsTraining"]);

                if (isTraining)
                {
                    trainingEmployees.Add(empId);
                }
            }

            int totalEmployees = allEmployees.Count;
            int trainingCount = trainingEmployees.Count;

            // Billable = everyone except training
            int billableCount = totalEmployees - trainingCount;

            // If we only had trainees, still treat recap as 1x
            if (billableCount <= 0 && trainingCount > 0)
                billableCount = 1;

            // Absolute minimum 1
            if (billableCount <= 0)
                billableCount = 1;

            return billableCount;
        }// End of 'GetEffectiveEmployeeCountRecap'.

        private int GetEffectiveEmployeeCountHardwareRecap(int recapID)
        {
            var allEmployees = new HashSet<int>();
            var trainingEmployees = new HashSet<int>();

            using var conn = new SqlConnection(AppHelper.GetDBConnectionString());
            conn.Open();

            string sql = @"
        SELECT  
            ER.EmployeeID,
            ER.IsTraining
        FROM EmployeeHardwareRecaps ER
        WHERE ER.HardwareRecapID = @HardwareRecapID;";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@HardwareRecapID", recapID);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                if (reader["EmployeeID"] == DBNull.Value)
                    continue;

                int empId = Convert.ToInt32(reader["EmployeeID"]);
                allEmployees.Add(empId);

                bool isTraining =
                    reader["IsTraining"] != DBNull.Value &&
                    Convert.ToBoolean(reader["IsTraining"]);

                if (isTraining)
                {
                    trainingEmployees.Add(empId);
                }
            }

            int totalEmployees = allEmployees.Count;
            int trainingCount = trainingEmployees.Count;

            // Billable = everyone except training
            int billableCount = totalEmployees - trainingCount;

            // If we only had trainees, still treat recap as 1x
            if (billableCount <= 0 && trainingCount > 0)
                billableCount = 1;

            // Absolute minimum 1
            if (billableCount <= 0)
                billableCount = 1;

            return billableCount;
        }// End of 'GetEffectiveEmployeeCountHardwareRecap'.


        private void PopulateEmployeeOptions()
        {
            EmployeeOptions = new List<SelectListItem>();

            using var conn = new SqlConnection(AppHelper.GetDBConnectionString());
            string sql = @"
                SELECT E.EmployeeID,
                       E.EmployeeFName,
                       E.EmployeeLName,
                       SU.SystemUserRole
                FROM Employee AS E
                INNER JOIN SystemUser AS SU
                       ON E.EmployeeID = SU.EmployeeID
                ORDER BY E.EmployeeFName, E.EmployeeLName;";

            using var cmd = new SqlCommand(sql, conn);
            conn.Open();
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                int employeeId = r.GetInt32(0);

                string fName = Convert.ToString(r["EmployeeFName"]) ?? "";
                string lName = Convert.ToString(r["EmployeeLName"]) ?? "";

                string label = $"{fName} {lName}";

                EmployeeOptions.Add(new SelectListItem
                {
                    Value = employeeId.ToString(),
                    Text = label
                });
            }
        }// End of 'PopulateEmployeeOptions'.

        private List<int> GetRecapEmployeeIds(int recapId)
        {
            var ids = new List<int>();

            using var conn = new SqlConnection(AppHelper.GetDBConnectionString());
            string sql = @"
                SELECT EmployeeID
                FROM EmployeeRecaps
                WHERE RecapID = @RecapID;";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@RecapID", recapId);
            conn.Open();

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                if (!reader.IsDBNull(0))
                {
                    ids.Add(reader.GetInt32(0));
                }
            }
            return ids;
        }// End of 'GetRecapEmployeeIds'.

        private List<int> GetHardwareRecapEmployeeIds(int recapId)
        {
            var ids = new List<int>();

            using var conn = new SqlConnection(AppHelper.GetDBConnectionString());
            string sql = @"
                SELECT EmployeeID
                FROM EmployeeHardwareRecaps
                WHERE HardwareRecapID = @HardwareRecapID;";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@HardwareRecapID", recapId);
            conn.Open();

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                if (!reader.IsDBNull(0))
                {
                    ids.Add(reader.GetInt32(0));
                }
            }
            return ids;
        }// End of 'GetRecapEmployeeIds'.


        private CableTotals GetCableTotals(int recapId)
        {
            return new CableTotals
            {
                TotalCat6 = GetCat6CableBase(recapId),
                Total182 = Get182CableBase(recapId),
                Total184 = Get184CableBase(recapId),
                Total186 = Get186CableBase(recapId),
                TotalFiber = GetFiberCableBase(recapId),
                TotalCoax = GetCoaxCableBase(recapId)
            };
        }// End of 'GetCableTotals'.

        private int GetCat6CableBase(int recapId)
        {
            int baseCat6Total = 0;

            using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
            {
                string cmdText = @"
                    SELECT TotalCat6
                    FROM StartEndCat6
                    WHERE RecapID = @RecapID;";

                using (SqlCommand cmd = new SqlCommand(cmdText, conn))
                {
                    cmd.Parameters.AddWithValue("@RecapID", recapId);
                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (!reader.IsDBNull(0))
                            {
                                baseCat6Total += reader.GetInt32(0);
                            }
                        }
                    }
                }
            }

            return baseCat6Total;
        }// End of 'GetCat6CableBase'.

        private int Get182CableBase(int recapId)
        {
            int base182Total = 0;

            using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
            {
                string cmdText = @"
                    SELECT Total182
                    FROM StartEnd182
                    WHERE RecapID = @RecapID;";

                using (SqlCommand cmd = new SqlCommand(cmdText, conn))
                {
                    cmd.Parameters.AddWithValue("@RecapID", recapId);
                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (!reader.IsDBNull(0))
                            {
                                base182Total += reader.GetInt32(0);
                            }
                        }
                    }
                }
            }

            return base182Total;
        }// End of 'Get182CableBase'.

        private int Get184CableBase(int recapId)
        {
            int base184Total = 0;

            using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
            {
                string cmdText = @"
                    SELECT Total184
                    FROM StartEnd184
                    WHERE RecapID = @RecapID;";

                using (SqlCommand cmd = new SqlCommand(cmdText, conn))
                {
                    cmd.Parameters.AddWithValue("@RecapID", recapId);
                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (!reader.IsDBNull(0))
                            {
                                base184Total += reader.GetInt32(0);
                            }
                        }
                    }
                }
            }

            return base184Total;
        }// End of 'Get184CableBase'.

        private int Get186CableBase(int recapId)
        {
            int base186Total = 0;

            using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
            {
                string cmdText = @"
                    SELECT Total186
                    FROM StartEnd186
                    WHERE RecapID = @RecapID;";

                using (SqlCommand cmd = new SqlCommand(cmdText, conn))
                {
                    cmd.Parameters.AddWithValue("@RecapID", recapId);
                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (!reader.IsDBNull(0))
                            {
                                base186Total += reader.GetInt32(0);
                            }
                        }
                    }
                }
            }

            return base186Total;
        }// End of 'Get186CableBase'.

        private int GetFiberCableBase(int recapId)
        {
            int baseFiberTotal = 0;

            using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
            {
                string cmdText = @"
                    SELECT TotalFiber
                    FROM StartEndFiber
                    WHERE RecapID = @RecapID;";

                using (SqlCommand cmd = new SqlCommand(cmdText, conn))
                {
                    cmd.Parameters.AddWithValue("@RecapID", recapId);
                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (!reader.IsDBNull(0))
                            {
                                baseFiberTotal += reader.GetInt32(0);
                            }
                        }
                    }
                }
            }

            return baseFiberTotal;
        }// End of 'GetFiberCableBase'.

        private int GetCoaxCableBase(int recapId)
        {
            int baseCoaxTotal = 0;

            using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
            {
                string cmdText = @"
                    SELECT TotalCoax
                    FROM StartEndCoax
                    WHERE RecapID = @RecapID;";

                using (SqlCommand cmd = new SqlCommand(cmdText, conn))
                {
                    cmd.Parameters.AddWithValue("@RecapID", recapId);
                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (!reader.IsDBNull(0))
                            {
                                baseCoaxTotal += reader.GetInt32(0);
                            }
                        }
                    }
                }
            }

            return baseCoaxTotal;
        }// End of 'GetFiberCableBase'.

        private decimal GetWorkTimeBase(int recapId)
        {
            decimal baseWorkTotal = 0;

            using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
            {
                string cmdText = @"
                    SELECT TotalWorkTime
                    FROM StartEndWork
                    WHERE RecapID = @RecapID;";

                using (SqlCommand cmd = new SqlCommand(cmdText, conn))
                {
                    cmd.Parameters.AddWithValue("@RecapID", recapId);
                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (!reader.IsDBNull(0))
                            {
                                baseWorkTotal += reader.GetDecimal(0);
                            }
                        }
                    }
                }
            }

            return baseWorkTotal;
        }// End of 'GetWorkTimeBase'.

        private decimal GetHardwareWorkTimeBase(int recapId)
        {
            decimal baseWorkTotal = 0;

            using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
            {
                string cmdText = @"
                    SELECT TotalWorkTime
                    FROM StartEndWorkHardware
                    WHERE HardwareRecapID = @HardwareRecapID;";

                using (SqlCommand cmd = new SqlCommand(cmdText, conn))
                {
                    cmd.Parameters.AddWithValue("@HardwareRecapID", recapId);
                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (!reader.IsDBNull(0))
                            {
                                baseWorkTotal += reader.GetDecimal(0);
                            }
                        }
                    }
                }
            }

            return baseWorkTotal;
        }// End of 'GetHardwareWorkTimeBase'.

        private decimal GetLunchTimeBase(int recapId)
        {
            decimal baseLunchTotal = 0;

            using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
            {
                string cmdText = @"
                    SELECT TotalLunchTime
                    FROM StartEndLunch
                    WHERE RecapID = @RecapID;";

                using (SqlCommand cmd = new SqlCommand(cmdText, conn))
                {
                    cmd.Parameters.AddWithValue("@RecapID", recapId);
                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (!reader.IsDBNull(0))
                            {
                                baseLunchTotal += reader.GetDecimal(0);
                            }
                        }
                    }
                }
            }

            return baseLunchTotal;
        }// End of 'GetLunchTimeBase'.

        private decimal GetHardwareLunchTimeBase(int recapId)
        {
            decimal baseLunchTotal = 0;

            using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
            {
                string cmdText = @"
                    SELECT TotalLunchTime
                    FROM StartEndLunchHardware
                    WHERE HardwareRecapID = @HardwareRecapID;";

                using (SqlCommand cmd = new SqlCommand(cmdText, conn))
                {
                    cmd.Parameters.AddWithValue("@HardwareRecapID", recapId);
                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (!reader.IsDBNull(0))
                            {
                                baseLunchTotal += reader.GetDecimal(0);
                            }
                        }
                    }
                }
            }

            return baseLunchTotal;
        }// End of 'GetHardwareLunchTimeBase'.

        private decimal GetTravelTimeBase(int recapId)
        {
            decimal baseTravelTotal = 0;

            using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
            {
                string cmdText = @"
                    SELECT TotalTravelTime
                    FROM StartEndTravel
                    WHERE RecapID = @RecapID;";

                using (SqlCommand cmd = new SqlCommand(cmdText, conn))
                {
                    cmd.Parameters.AddWithValue("@RecapID", recapId);
                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (!reader.IsDBNull(0))
                            {
                                baseTravelTotal += reader.GetDecimal(0);
                            }
                        }
                    }
                }
            }

            return baseTravelTotal;
        }// End of 'GetTravelTimeBase'.

        private decimal GetSupportTimeBase(int recapId)
        {
            decimal baseSupportTotal = 0;

            using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
            {
                string cmdText = @"
                    SELECT TotalSupportTime
                    FROM StartEndSupport
                    WHERE RecapID = @RecapID;";

                using (SqlCommand cmd = new SqlCommand(cmdText, conn))
                {
                    cmd.Parameters.AddWithValue("@RecapID", recapId);
                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (!reader.IsDBNull(0))
                            {
                                baseSupportTotal += reader.GetDecimal(0);
                            }
                        }
                    }
                }
            }

            return baseSupportTotal;
        }// End of 'GetSupportTimeBase'.

        private decimal GetRecapTimeBase(int recapId)
        {
            decimal baseRecapTotal = 0;

            using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
            {
                string cmdText = @"
                    SELECT TotalRecapTime
                    FROM StartEndRecap
                    WHERE RecapID = @RecapID;";

                using (SqlCommand cmd = new SqlCommand(cmdText, conn))
                {
                    cmd.Parameters.AddWithValue("@RecapID", recapId);
                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (!reader.IsDBNull(0))
                            {
                                baseRecapTotal += reader.GetDecimal(0);
                            }
                        }
                    }
                }
            }

            return baseRecapTotal;
        }// End of 'GetRecapTimeBase'.

        private decimal GetHardwareRecapTimeBase(int recapId)
        {
            decimal baseRecapTotal = 0;

            using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
            {
                string cmdText = @"
                    SELECT TotalRecapTime
                    FROM StartEndRecapHardware
                    WHERE HardwareRecapID = @HardwareRecapID;";

                using (SqlCommand cmd = new SqlCommand(cmdText, conn))
                {
                    cmd.Parameters.AddWithValue("@HardwareRecapID", recapId);
                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (!reader.IsDBNull(0))
                            {
                                baseRecapTotal += reader.GetDecimal(0);
                            }
                        }
                    }
                }
            }

            return baseRecapTotal;
        }// End of 'GetHardwareRecapTimeBase'.

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
    }// End of 'BrowseRecaps' Class.
}// End of 'namespace'.