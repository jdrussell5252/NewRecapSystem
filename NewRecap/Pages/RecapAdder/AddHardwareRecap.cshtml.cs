using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using NewRecap.Model;
using System.Security.Claims;
using Outlook = Microsoft.Office.Interop.Outlook;

using Microsoft.Data.SqlClient;
using NewRecap.MyAppHelper;
using System.Data;

namespace NewRecap.Pages.RecapAdder
{
    [Authorize]            // EASTER EGG ;) - Jonathan G
    [BindProperties]
    public class AddHardwareRecapModel : PageModel
    {
        public HardwareRecap NewRecap { get; set; } = new HardwareRecap();
        public List<EmployeeInfo> Employees { get; set; } = new List<EmployeeInfo>();
        public List<SelectListItem> Locations { get; set; } = new List<SelectListItem>();
        public int SelectedStoreLocationID { get; set; }
        public List<int> SelectedEmployeeIds { get; set; } = new();
        public List<SelectListItem> EmployeeOptions { get; set; } = new();
        public string? TrainingEmployeeIds { get; set; }

        public bool IsAdmin { get; set; }
        public string connectionString = "Provider = Microsoft.ACE.OLEDB.12.0; Data Source = C:\\Users\\jaker\\OneDrive\\Desktop\\Nacspace\\New Recap\\NewRecapDB\\NewRecapDB.accdb;";

        public void OnGet()
        {
            PopulateEmployeeOptions();
            PopulateLocationList();
            /*--------------------ADMIN PRIV----------------------*/
            // Safely access the NameIdentifier claim
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null)
            {
                int userId = int.Parse(userIdClaim.Value); // Use the claim value only if it exists
                CheckIfUserIsAdmin(userId);
            }
            /*--------------------ADMIN PRIV----------------------*/
        }// End of 'OnGet'.

        public IActionResult OnPost()
        {
            // Validate employee selection
            if (!SelectedEmployeeIds.Any())
            {
                ModelState.AddModelError("SelectedEmployeeIds", "Please select at least one employee.");
            }
            if (SelectedStoreLocationID <= 0)
            {
                ModelState.AddModelError("SelectedStoreLocationID", "Please select a store location.");
            }

            /*-----------------------------------------------------*/

            // === Compute totals directly from your start/end date+time fields ===
            var workSegs = NewRecap.WorkSegments;
            var lunchSegs = NewRecap.LunchSegments;
            var recapSegs = NewRecap.RecapSegments;

            // ========== DATE SPAN PER SEGMENT TYPE (end < start OR 2+ days apart) ==========

            // WORK
            if (workSegs.Any(s => SegmentDatesInvalid(s.WorkStartDate, s.WorkEndDate)))
            {
                ModelState.AddModelError(
                    "NewRecap.WorkSegments",
                    "Work segment end date cannot be before the start date or 2+ days after the start date.");
            }

            // LUNCH
            if (lunchSegs.Any(s => SegmentDatesInvalid(s.LunchStartDate, s.LunchEndDate)))
            {
                ModelState.AddModelError(
                    "NewRecap.LunchSegments",
                    "Lunch segment end date cannot be before the start date or 2+ days after the start date.");
            }

            // RECAP
            if (recapSegs.Any(s => SegmentDatesInvalid(s.RecapStartDate, s.RecapEndDate)))
            {
                ModelState.AddModelError(
                    "NewRecap.RecapSegments",
                    "Recap segment end date cannot be before the start date or 2+ days after the start date.");
            }

            // ========== COMPLETENESS RULES ==========
            // Complete = all 4 values (start date/time + end date/time) present

            bool hasCompleteWork = workSegs.Any(s =>
                IsComplete(s.WorkStartDate, s.WorkStart, s.WorkEndDate, s.WorkEnd));

            bool hasCompleteLunch = lunchSegs.Any(s =>
                IsComplete(s.LunchStartDate, s.LunchStart, s.LunchEndDate, s.LunchEnd));

            bool hasCompleteRecap = recapSegs.Any(s =>
                IsComplete(s.RecapStartDate, s.RecapStart, s.RecapEndDate, s.RecapEnd));

            // At least one complete (non-recap) segment of ANY type
            bool hasAtLeastOneComplete =
                hasCompleteWork || hasCompleteLunch;

            if (!hasAtLeastOneComplete)
            {
                ModelState.AddModelError(
                    "NewRecap.WorkSegments",
                    "Please add at least one complete time segment (Work, Travel, Support, or Lunch) with both start and end date/time."
                );
            }

            // Require at least ONE complete Recap segment
            if (!hasCompleteRecap)
            {
                ModelState.AddModelError(
                    "NewRecap.RecapSegments",
                    "Please add at least one complete Recap segment (start & end date/time)."
                );
            }

            // Require at least ONE complete Recap segment
            if (!hasCompleteRecap)
            {
                ModelState.AddModelError(
                    "NewRecap.RecapSegments",
                    "Please add at least one complete Recap segment (start & end date/time)."
                );
            }

            // ========== INCOMPLETE SEGMENTS (partially filled rows) ==========

            if (workSegs.Any(s => IsIncomplete(s.WorkStartDate, s.WorkStart, s.WorkEndDate, s.WorkEnd)))
            {
                ModelState.AddModelError(
                    "NewRecap.WorkSegments",
                    "Work segments cannot be partially filled; provide start and end date/time or leave all blank."
                );
            }

            if (lunchSegs.Any(s => IsIncomplete(s.LunchStartDate, s.LunchStart, s.LunchEndDate, s.LunchEnd)))
            {
                ModelState.AddModelError(
                    "NewRecap.LunchSegments",
                    "Lunch segments cannot be partially filled; provide start and end date/time or leave all blank."
                );
            }

            if (recapSegs.Any(s => IsIncomplete(s.RecapStartDate, s.RecapStart, s.RecapEndDate, s.RecapEnd)))
            {
                ModelState.AddModelError(
                    "NewRecap.RecapSegments",
                    "Recap segments cannot be partially filled; provide start and end date/time or leave all blank."
                );
            }

            // ========== LUNCH vs OTHER TOTALS ==========

            double workTotal = workSegs.Sum(s =>
                Hours(s.WorkStartDate, s.WorkStart, s.WorkEndDate, s.WorkEnd));

            double lunchTotal = lunchSegs.Sum(s =>
                 Hours(s.LunchStartDate, s.LunchStart, s.LunchEndDate, s.LunchEnd));

            double recapTotal = recapSegs.Sum(s =>
                Hours(s.RecapStartDate, s.RecapStart, s.RecapEndDate, s.RecapEnd));

            double overallOtherTotal = workTotal + recapTotal;

            // Lunch cannot be >= 2 hours total
            if (lunchTotal >= 2.0)
            {
                ModelState.AddModelError(
                    "NewRecap.LunchSegments",
                    "Total lunch time cannot exceed 2 hours."
                );
            }

            // Lunch cannot exceed any other segment total
            if (overallOtherTotal > 0 && lunchTotal > overallOtherTotal)
            {
                ModelState.AddModelError(
                    "NewRecap.LunchSegments",
                    "Total lunch time cannot exceed the total time recorded for the day."
                );
            }

            // If lunch is present but no non-lunch segments are complete, reject
            bool hasCompleteNonLunch = hasCompleteWork;
            if (hasCompleteLunch && !hasCompleteNonLunch)
            {
                ModelState.AddModelError(
                    "NewRecap.LunchSegments",
                    "You must include at least one Work, Travel, or Support segment if you log Lunch."
                );
            }

            // ========== NON-POSITIVE DURATION (end <= start) ==========

            if (workSegs.Any(s => HasNonPositiveDuration(s.WorkStartDate, s.WorkStart, s.WorkEndDate, s.WorkEnd)))
            {
                ModelState.AddModelError(
                    "NewRecap.WorkSegments",
                    "Work segment end date/time must be after the start date/time.");
            }

            if (lunchSegs.Any(s => HasNonPositiveDuration(s.LunchStartDate, s.LunchStart, s.LunchEndDate, s.LunchEnd)))
            {
                ModelState.AddModelError(
                    "NewRecap.LunchSegments",
                    "Lunch segment end date/time must be after the start date/time.");
            }

            if (recapSegs.Any(s => HasNonPositiveDuration(s.RecapStartDate, s.RecapStart, s.RecapEndDate, s.RecapEnd)))
            {
                ModelState.AddModelError(
                    "NewRecap.RecapSegments",
                    "Recap segment end date/time must be after the start date/time.");
            }

            HashSet<int> trainingIdSet = new HashSet<int>();

            if (!string.IsNullOrWhiteSpace(TrainingEmployeeIds))
            {
                var parts = TrainingEmployeeIds
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                foreach (var p in parts)
                {
                    if (int.TryParse(p, out int empId))
                    {
                        trainingIdSet.Add(empId);
                    }
                }
            }

            var description = (NewRecap.RecapDescription ?? string.Empty).Trim();
            var serial = (NewRecap.RecapSerialNumber ?? string.Empty).Trim();
            var hostname = (NewRecap.Hostname ?? string.Empty).Trim();
            var ip = (NewRecap.IP ?? string.Empty).Trim();
            var wam = (NewRecap.WAM ?? string.Empty).Trim();
            const int dbMaxDesc = 500;
            const int dbMaxSerial = 30;
            const int dbMaxHost = 50;
            const int dbMaxIP = 12;
            const int dbMaxWAM = 50;

            if (description.Length > dbMaxDesc)
            {
                ModelState.AddModelError("NewRecap.RecapDescription", "Description must be at most 500 characters.");
            }

            if (serial.Length > dbMaxSerial)
            {
                ModelState.AddModelError("NewRecap.RecapSerialNumber", "S/N must be at most 30 characters.");
            }

            if (hostname.Length > dbMaxHost)
            {
                ModelState.AddModelError("NewRecap.Hostname", "Hostname must be at most 50 characters.");
            }

            if (ip.Length > dbMaxIP)
            {
                ModelState.AddModelError("NewRecap.IP", "IP must be at most 12 characters.");
            }

            if (wam.Length > dbMaxWAM)
            {
                ModelState.AddModelError("NewRecap.WAM", "WAM must be at most 50 characters.");
            }

            if (ModelState.IsValid)
            {
                int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
                {
                    conn.Open();

                    string cmdTextRecap = "INSERT INTO HardwareRecap (HardwareRecapWorkorderNumber, HardwareRecapDate, AddedBy, HardwareRecapDescription, HardwareRecapAssetNumber, HardwareRecapSerialNumber, StoreLocationID, HardwareRecapIP, HardwareRecapWAM, HardwareRecapHostname) VALUES (@RecapWorkorderNumber, @RecapDate, @AddedBy, @RecapDescription, @RecapAssetNumber, @RecapSerialNumber, @StoreLocationID, @IP, @WAM, @Hostname);";
                    SqlCommand cmdRecap = new SqlCommand(cmdTextRecap, conn);
                    cmdRecap.Parameters.AddWithValue("@RecapWorkorderNumber", NewRecap.RecapWorkorderNumber);
                    cmdRecap.Parameters.AddWithValue("@RecapDate", DateTime.Today);
                    cmdRecap.Parameters.AddWithValue("@AddedBy", userId);
                    cmdRecap.Parameters.AddWithValue("@RecapDescription", NewRecap.RecapDescription);
                    var pAsset = cmdRecap.Parameters.Add("@RecapAssetNumber", SqlDbType.Int);
                    pAsset.Value = NewRecap.RecapAssetNumber.HasValue ? NewRecap.RecapAssetNumber.Value : DBNull.Value;
                    cmdRecap.Parameters.AddWithValue("@RecapSerialNumber", string.IsNullOrWhiteSpace(NewRecap.RecapSerialNumber) ? DBNull.Value : NewRecap.RecapSerialNumber);
                    cmdRecap.Parameters.AddWithValue("@StoreLocationID", SelectedStoreLocationID);
                    cmdRecap.Parameters.AddWithValue("@IP", string.IsNullOrWhiteSpace(NewRecap.IP) ? DBNull.Value : NewRecap.IP);
                    cmdRecap.Parameters.AddWithValue("@WAM", string.IsNullOrWhiteSpace(NewRecap.WAM) ? DBNull.Value : NewRecap.WAM);
                    cmdRecap.Parameters.AddWithValue("@Hostname", string.IsNullOrWhiteSpace(NewRecap.Hostname) ? DBNull.Value : NewRecap.Hostname);

                    cmdRecap.ExecuteNonQuery();

                    int HardwareRecapID;
                    // fetch the generated AutoNumber (RecapID)
                    using (var idCmd = new SqlCommand("SELECT @@IDENTITY;", conn))
                    {
                        HardwareRecapID = Convert.ToInt32(idCmd.ExecuteScalar());
                        NewRecap.RecapID = HardwareRecapID;
                    }

                    foreach (var empId in SelectedEmployeeIds)
                    {
                        bool isTraining = trainingIdSet.Contains(empId);
                        string cmdTextEmployeeRecap = "INSERT INTO EmployeeRecaps (HardwareRecapID, EmployeeID, IsTraining) VALUES (@HardwareRecapID, @EmployeeID, @IsTraining)";
                        SqlCommand cmdEmployeeRecap = new SqlCommand(cmdTextEmployeeRecap, conn);
                        cmdEmployeeRecap.Parameters.AddWithValue("@HardwareRecapID", HardwareRecapID);
                        cmdEmployeeRecap.Parameters.AddWithValue("@EmployeeID", empId);
                        cmdEmployeeRecap.Parameters.AddWithValue("@IsTraining", isTraining);
                        cmdEmployeeRecap.ExecuteNonQuery();

                    }

                    // For each segment that has at least one valid start/end pair
                    var Wsegments = NewRecap.WorkSegments.Where(s =>
                        (s.WorkStartDate.HasValue && s.WorkEndDate.HasValue && s.WorkStart.HasValue && s.WorkEnd.HasValue));

                    foreach (var seg in Wsegments)
                    {
                        const string sql = @"
                        INSERT INTO StartEndWork
                        (HardwareRecapID,
                        StartWorkTime, EndWorkTime, StartWorkDate, EndWorkDate)
                        VALUES
                        (@HardwareRecapID,
                        @StartWorkTime, @EndWorkTime, @StartWorkDate, @EndWorkDate);";

                        SqlCommand cmd = new SqlCommand(sql, conn);

                        cmd.Parameters.AddWithValue("@HardwareRecapID", HardwareRecapID);

                        cmd.Parameters.AddWithValue("@StartWorkTime", seg.WorkStart);
                        cmd.Parameters.AddWithValue("@EndWorkTime", seg.WorkEnd);
                        cmd.Parameters.AddWithValue("@StartWorkDate", seg.WorkStartDate);
                        cmd.Parameters.AddWithValue("@EndWorkDate", seg.WorkEndDate);
                        cmd.ExecuteNonQuery();
                    }

                    // For each segment that has at least one valid start/end pair
                    var Rsegments = NewRecap.RecapSegments.Where(s =>
                        (s.RecapStartDate.HasValue && s.RecapEndDate.HasValue && s.RecapStart.HasValue && s.RecapEnd.HasValue));

                    foreach (var seg in Rsegments)
                    {
                        const string sql = @"
                            INSERT INTO StartEndRecap
                            (HardwareRecapID,
                            StartRecapTime, EndRecapTime, StartRecapDate, EndRecapDate)
                            VALUES
                            (@HardwareRecapID,
                            @StartRecapTime, @EndRecapTime, @StartRecapDate, @EndRecapDate);";

                        SqlCommand cmd = new SqlCommand(sql, conn);

                        cmd.Parameters.AddWithValue("@HardwareRecapID", HardwareRecapID);

                        cmd.Parameters.AddWithValue("@StartRecapTime", seg.RecapStart);
                        cmd.Parameters.AddWithValue("@EndRecapTime", seg.RecapEnd);
                        cmd.Parameters.AddWithValue("@StartRecapDate", seg.RecapStartDate);
                        cmd.Parameters.AddWithValue("@EndRecapDate", seg.RecapEndDate);
                        cmd.ExecuteNonQuery();
                    }

                    // For each segment that has at least one valid start/end pair
                    var Lsegments = NewRecap.LunchSegments.Where(s =>
                        (s.LunchStartDate.HasValue && s.LunchEndDate.HasValue && s.LunchStart.HasValue && s.LunchEnd.HasValue));

                    foreach (var seg in Lsegments)
                    {

                        const string sql = @"
                            INSERT INTO StartEndLunch
                            (HardwareRecapID,
                            StartLunchTime, EndLunchTime, StartLunchDate, EndLunchDate)
                            VALUES
                            (@HardwareRecapID,
                            @StartLunchTime, @EndLunchTime, @StartLunchDate, @EndLunchDate);";

                        SqlCommand cmd = new SqlCommand(sql, conn);

                        cmd.Parameters.AddWithValue("@HardwareRecapID", HardwareRecapID);

                        cmd.Parameters.AddWithValue("@StartLunchTime", seg.LunchStart);
                        cmd.Parameters.AddWithValue("@EndLunchTime", seg.LunchEnd);
                        cmd.Parameters.AddWithValue("@StartLunchDate", seg.LunchStartDate);
                        cmd.Parameters.AddWithValue("@EndLunchDate", seg.LunchEndDate);
                        cmd.ExecuteNonQuery();
                    }
                }

                var storeNumber = getStoreNumberById(SelectedStoreLocationID);
                var to = "ClientRecaps@outlook.com";
                var subject = $"Recap | WO#: {NewRecap.RecapWorkorderNumber} | Customer: {storeNumber} | Date: {DateTime.Today:MM/dd/yyyy}";
                var bodyText = BuildRecapEmailBodyText(NewRecap, storeNumber, SelectedEmployeeIds);


                string msOutlookUrl =
                    "ms-outlook://compose?" +
                    $"to={Uri.EscapeDataString(to)}" +
                    $"&subject={Uri.EscapeDataString(subject)}" +
                    $"&body={Uri.EscapeDataString(bodyText)}";

                string mailtoUrl =
                    "mailto:" + Uri.EscapeDataString(to) +
                    $"?subject={Uri.EscapeDataString(subject)}" +
                    $"&body={Uri.EscapeDataString(bodyText)}"; // fallback

                // Stash for the Added page
                TempData["MailtoUrl"] = mailtoUrl;
                TempData["Subject"] = subject;               // optional: show on page
                TempData["To"] = to;                         // optional
                

                return RedirectToPage("/RecapAdder/Added");
            }
            else
            {
                OnGet();
                return Page();
            }
        }// End of 'OnPost'.

        public static void ShowOutlookCompose(string to, string subject, string htmlBody, string? attachmentPath = null)
        {
            Outlook.Application app;
            try
            {
                app = new Outlook.Application();
            }
            catch (System.Exception ex)
            {
                // Outlook not installed / no interactive session
                throw new InvalidOperationException("Outlook is not available for automation.", ex);
            }

            var mail = (Outlook.MailItem)app.CreateItem(Outlook.OlItemType.olMailItem);
            mail.To = to;
            mail.Subject = subject;
            mail.HTMLBody = htmlBody;

            if (!string.IsNullOrWhiteSpace(attachmentPath) && System.IO.File.Exists(attachmentPath))
            {
                mail.Attachments.Add(attachmentPath, Outlook.OlAttachmentType.olByValue, Type.Missing, Type.Missing);
            }

            mail.Display(false);  // User sees it and can click Send
        }// End of 'ShowOutlookCompose'.


        private string BuildRecapEmailBodyText(HardwareRecap recap, string storelocation, List<int> employeeIds)
        {
            //var nl = Environment.NewLine;
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Workorder: {recap.RecapWorkorderNumber}");
            sb.AppendLine($"Store Location: {storelocation}");
            sb.AppendLine($"Date: {DateTime.Today:MM/dd/yyyy}");
            int recapId = recap.RecapID;
            sb.Append("Technician(s): ");

            var employeeNames = GetEmployeeNames(employeeIds, recapId);
            sb.AppendLine(string.Join(", ", employeeNames));
            sb.AppendLine();

            if (recap.WorkSegments != null && recap.WorkSegments.Any())
            {
                foreach (var seg in recap.WorkSegments)
                {
                    if (seg.WorkStartDate.HasValue && seg.WorkStart != null &&
                        seg.WorkEndDate.HasValue && seg.WorkEnd != null)
                    {
                        var work = FormatRange(seg.WorkStartDate, seg.WorkStart, seg.WorkEndDate, seg.WorkEnd);
                        if (!string.IsNullOrEmpty(work))
                            sb.AppendLine($"Work Time: {work}");
                        sb.AppendLine();
                    }
                }

                foreach (var seg in recap.LunchSegments)
                {
                    if (seg.LunchStartDate.HasValue && seg.LunchStart != null &&
                        seg.LunchEndDate.HasValue && seg.LunchEnd != null)
                    {
                        var lunch = FormatRange(seg.LunchStartDate, seg.LunchStart, seg.LunchEndDate, seg.LunchEnd);
                        if (!string.IsNullOrEmpty(lunch))
                            sb.AppendLine($"Lunch Time: {lunch}");
                        sb.AppendLine();
                    }
                }

                foreach (var seg in recap.RecapSegments)
                {
                    if (seg.RecapStartDate.HasValue && seg.RecapStart != null &&
                        seg.RecapEndDate.HasValue && seg.RecapEnd != null)
                    {
                        var recapp = FormatRange(seg.RecapStartDate, seg.RecapStart, seg.RecapEndDate, seg.RecapEnd);
                        if (!string.IsNullOrEmpty(recapp))
                            sb.AppendLine($"Recap Time: {recapp}");
                        sb.AppendLine();
                    }
                }


                // Base times from your StartEnd* tables
                decimal baseWork = GetWorkTimeBase(recapId);
                decimal baseLunch = GetLunchTimeBase(recapId);
                decimal baseRecap = GetRecapTimeBase(recapId);

                // Effective employee count (ignoring trainees, but never < 1)
                int effCount = GetEffectiveEmployeeCountFromDb(recapId);

                // Apply multiplier rules:
                decimal totalWork = baseWork * effCount;
                decimal totalLunch = baseLunch * effCount;
                decimal totalRecap = baseRecap;                 // recap is not multiplied
                decimal totalTime = totalWork + totalRecap - totalLunch;

                sb.AppendLine($"Total Work Time: {totalWork:N2} hrs");
                sb.AppendLine($"Total Lunch Time: {totalLunch:N2} hrs");
                sb.AppendLine($"Total Recap Time: {totalRecap:N2} hrs");
                sb.AppendLine($"Total Time: {totalTime:N2} hrs");

                sb.AppendLine();

                sb.AppendLine($"Job Description: {recap.RecapDescription}");

                sb.AppendLine();

                sb.AppendLine($"Hostname: {recap.Hostname}");
                sb.AppendLine($"IP: {recap.IP}");
                sb.AppendLine($"WAM: {recap.WAM}");
                sb.AppendLine($"Asset #: {recap.RecapAssetNumber}");

            }
            // Keep it concise to avoid URI length limits.
            return sb.ToString();
        }// End of 'BuildRecapEmailBodyText'.

        private void PopulateLocationList()
        {
            using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
            {
                string query = "SELECT * FROM StoreLocations ORDER BY StoreNumber ";
                SqlCommand cmd = new SqlCommand(query, conn);
                conn.Open();
                SqlDataReader reader = cmd.ExecuteReader();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        var location = new SelectListItem
                        {
                            Value = reader["StoreLocationID"].ToString(),
                            Text = $"{reader["StoreNumber"]}, {reader["StoreState"]}, {reader["StoreCity"]}"
                        };
                        Locations.Add(location);

                    }
                }
            }
        }//End of 'PopulateLocationList'.

        private decimal GetWorkTimeBase(int recapId)
        {
            decimal baseWorkTotal = 0;

            using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
            {
                string cmdText = @"
                    SELECT TotalWorkTime
                    FROM StartEndWork
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
        }// End of 'GetWorkTimeBase'.

        private decimal GetLunchTimeBase(int recapId)
        {
            decimal baseLunchTotal = 0;

            using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
            {
                string cmdText = @"
                    SELECT TotalLunchTime
                    FROM StartEndLunch
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
        }// End of 'GetLunchTimeBase'.

        private decimal GetRecapTimeBase(int recapId)
        {
            decimal baseRecapTotal = 0;

            using (SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString()))
            {
                string cmdText = @"
                    SELECT TotalRecapTime
                    FROM StartEndRecap
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
        }// End of 'GetRecapTimeBase'.

        private List<string> GetEmployeeNames(List<int> employeeIds, int recapId)
        {
            var results = new List<string>();

            using (var conn = new SqlConnection(AppHelper.GetDBConnectionString()))
            {
                string sql = @"
            SELECT e.EmployeeFName, e.EmployeeLName, er.IsTraining
            FROM EmployeeRecaps er
            INNER JOIN Employee e ON e.EmployeeID = er.EmployeeID
            WHERE er.HardwareRecapID = @HardwareRecapID AND er.EmployeeID = @EmpID;";

                conn.Open();

                foreach (int empId in employeeIds)
                {
                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@HardwareRecapID", recapId);
                        cmd.Parameters.AddWithValue("@EmpID", empId);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string f = reader.GetString(0);
                                string l = reader.GetString(1);
                                bool isTraining = !reader.IsDBNull(2) && reader.GetBoolean(2);

                                string formatted =
                                    isTraining
                                        ? $"{f} {l} (Training)"
                                        : $"{f} {l}";

                                results.Add(formatted);
                            }
                        }
                    }
                }
            }

            return results;
        }// End of 'GetEmployeeNames'.


        private string getStoreNumberById(int storeLocationID)
        {
            using SqlConnection conn = new SqlConnection(AppHelper.GetDBConnectionString());
            conn.Open();
            string sql = "SELECT StoreNumber FROM StoreLocations WHERE StoreLocationID = @StoreLocationID";
            using SqlCommand cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@StoreLocationID", storeLocationID);
            var val = cmd.ExecuteScalar();
            return (val == null || val == DBNull.Value) ? "" : val.ToString();
        }// End of 'getStoreNumberById'.


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
               LEFT JOIN SystemUser AS SU
               ON E.EmployeeID = SU.EmployeeID;
               ";

            using var cmd = new SqlCommand(sql, conn);
            conn.Open();

            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                int employeeId = Convert.ToInt32(r["EmployeeID"]);

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

        private static DateTime? CombineDateAndTime(DateTime? date, object time)
        {
            if (!date.HasValue || time == null) return null;

            if (time is DateTime dt)
                return new DateTime(date.Value.Year, date.Value.Month, date.Value.Day,
                                    dt.Hour, dt.Minute, dt.Second);

            if (time is TimeSpan ts)
                return new DateTime(date.Value.Year, date.Value.Month, date.Value.Day,
                                    ts.Hours, ts.Minutes, ts.Seconds);

            return null;
        }// End of 'CombineDateAndTime'.

        private static double Hours(DateTime? sd, object st, DateTime? ed, object et)
        {
            var start = CombineDateAndTime(sd, st);
            var end = CombineDateAndTime(ed, et);
            if (!start.HasValue || !end.HasValue) return 0.0;

            var span = end.Value - start.Value;
            return span.TotalHours < 0 ? 0.0 : span.TotalHours;
        }// End of 'Hours'.

        private static string FormatRange(DateTime? sd, object st, DateTime? ed, object et)
        {
            var start = CombineDateAndTime(sd, st);
            var end = CombineDateAndTime(ed, et);
            if (!start.HasValue || !end.HasValue) return string.Empty;
            return $"\nStart: {start.Value:MM/dd/yyyy hh:mm tt} \nEnd: {end.Value:MM/dd/yyyy hh:mm tt}";
        }// End of 'FormatRange'.

        private int GetEffectiveEmployeeCountFromDb(int recapId)
        {
            int totalEmployees = 0;
            int trainingCount = 0;

            using (var conn = new SqlConnection(AppHelper.GetDBConnectionString()))
            {
                string sql = @"
            SELECT ER.EmployeeID, ER.IsTraining
            FROM EmployeeRecaps ER
            WHERE ER.HardwareRecapID = @HardwareRecapID;";

                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@HardwareRecapID", recapId);
                    conn.Open();

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (reader["EmployeeID"] == DBNull.Value)
                                continue;

                            totalEmployees++;

                            bool isTraining = false;
                            if (reader["IsTraining"] != DBNull.Value)
                            {
                                isTraining = Convert.ToBoolean(reader["IsTraining"]);
                            }

                            if (isTraining)
                            {
                                trainingCount++;
                            }
                        }
                    }
                }
            }

            // billable = everyone except training
            int billableCount = totalEmployees - trainingCount;

            // If we *only* had trainees, still treat recap as 1x
            if (billableCount <= 0 && trainingCount > 0)
                billableCount = 1;

            // Absolute minimum 1
            if (billableCount <= 0)
                billableCount = 1;

            return billableCount;
        }// End of 'GetEffectiveEmployeeCountFromDb'.

        private bool SegmentDatesInvalid(DateTime? start, DateTime? end)
        {
            if (!start.HasValue || !end.HasValue)
                return false; // completeness rules handle missing values

            if (end.Value < start.Value)
                return true;

            var spanDays = (end.Value.Date - start.Value.Date).TotalDays;

            // Allow up to 2 calendar days: e.g., 11/01 -> 11/03 is OK
            return spanDays >= 2.0;
        }// End of 'SegmentDatesInvalid'.

        private static bool HasNonPositiveDuration(DateTime? sd, TimeSpan? st, DateTime? ed, TimeSpan? et)
        {
            var start = CombineDateAndTime(sd, st);
            var end = CombineDateAndTime(ed, et);

            // If it's incomplete, let the completeness rules handle it
            if (!start.HasValue || !end.HasValue)
                return false;

            // Not allowed: end <= start (covers equal and “backwards”)
            return end.Value <= start.Value;
        }// End of 'HasNonPositiveDuration'.

        private static bool IsComplete(DateTime? sd, TimeSpan? st, DateTime? ed, TimeSpan? et)
        {
            return sd.HasValue && st.HasValue && ed.HasValue && et.HasValue;
        }// End of 'IsComplete'.

        private static bool IsAnyFilled(DateTime? sd, TimeSpan? st, DateTime? ed, TimeSpan? et)
        {
            return sd.HasValue || st.HasValue || ed.HasValue || et.HasValue;
        }// End of 'IsAnyFilled'.

        private static bool IsIncomplete(DateTime? sd, TimeSpan? st, DateTime? ed, TimeSpan? et)
        {
            return IsAnyFilled(sd, st, ed, et) && !IsComplete(sd, st, ed, et);
        }// End of 'IsIncomplete'.

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
    }// End of 'AddHardwareRecap' Class.
}// End of 'namespace'.
